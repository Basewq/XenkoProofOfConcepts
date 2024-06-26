using MultiplayerExample.Core;
using MultiplayerExample.Data;
using MultiplayerExample.Engine;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MultiplayerExample.Network.SnapshotStores
{
    class InputSnapshotsInitializerProcessor : EntityProcessor<InputSnapshotsComponent>,
        IInGameProcessor, IPreUpdateProcessor
    {
        private static readonly ObjectPool<SnapshotStore<InputSnapshotsComponent.InputCommandSet>> InputSnapshotsPool
            = new ObjectPool<SnapshotStore<InputSnapshotsComponent.InputCommandSet>>(
                initialCapacity: 64,
                objectCreatorFunc: () => new SnapshotStore<InputSnapshotsComponent.InputCommandSet>(MovementSnapshotsProcessor.SnapshotBufferSize),
                onObjectPutAction: ObjectPoolItemClearer<SnapshotStore<InputSnapshotsComponent.InputCommandSet>>.Default);

        private static readonly ObjectPool<List<InputSnapshotsComponent.InputCommandSet>> UnacknowledgedInputsPool
            = new ObjectPool<List<InputSnapshotsComponent.InputCommandSet>>(
                initialCapacity: 64,
                objectCreatorFunc: () => new List<InputSnapshotsComponent.InputCommandSet>(MovementSnapshotsProcessor.SnapshotBufferSize),
                onObjectPutAction: x => x.Clear());

        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;

        public bool IsEnabled { get; set; }

        public InputSnapshotsInitializerProcessor() : base(typeof(TransformComponent))
        {
            Order = -1100;         // Ensure this occurs before other processors that use MovementSnapshotsComponent
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            _gameClockManager = Services.GetService<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InputSnapshotsComponent component, [NotNull] InputSnapshotsComponent data)
        {
            component.SnapshotStore = InputSnapshotsPool.GetObject();
            component.PendingInputs = UnacknowledgedInputsPool.GetObject();
            // Input snapshots are generated AHEAD of simulation, however we also need to generate one at the time of
            // the simulation as a dummy snapshot, because an entity might be created from the server and then
            // immediately processed by the entity processors that need this snapshot.
            var nextPlayerInputSequenceNumber = component.GetNextPlayerInputSequenceNumber();
            CreateNewSnapshotData(component.SnapshotStore, _gameClockManager.SimulationClock.SimulationTickNumber, nextPlayerInputSequenceNumber);
            nextPlayerInputSequenceNumber = component.GetNextPlayerInputSequenceNumber();
            CreateNewSnapshotData(component.SnapshotStore, _gameClockManager.SimulationClock.SimulationTickNumber + 1, nextPlayerInputSequenceNumber);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InputSnapshotsComponent component, [NotNull] InputSnapshotsComponent data)
        {
            InputSnapshotsPool.PutObject(component.SnapshotStore);
            component.SnapshotStore = null;

            UnacknowledgedInputsPool.PutObject(component.PendingInputs);
            component.PendingInputs = null;
        }

        protected override InputSnapshotsComponent GenerateComponentData([NotNull] Entity entity, [NotNull] InputSnapshotsComponent component)
        {
            return new InputSnapshotsComponent
            {
                // Can also add other info/components here
            };
        }

        public void PreUpdate(GameTime gameTime)
        {
            if (!_gameClockManager.SimulationClock.IsNextSimulation)
            {
                // Only update during a simulation update since we only want to create new snapshots
                return;
            }

            // Input is a special snapshot case. It must generate AHEAD of the simulation, because multiple
            // input changes may occur within a single simulation duration so the consumers of input must
            // only consume the 'final' input.
            var simTickNumber = _gameClockManager.SimulationClock.SimulationTickNumber + 1;
            foreach (var kv in ComponentDatas)
            {
                var inputSnapshotsComp = kv.Key;
                var nextPlayerInputSequenceNumber = _gameEngineContext.IsClient ? inputSnapshotsComp.GetNextPlayerInputSequenceNumber() : default;
                CreateNewSnapshotData(inputSnapshotsComp.SnapshotStore, simTickNumber, nextPlayerInputSequenceNumber);
            }
        }

        private static void CreateNewSnapshotData(
            SnapshotStore<InputSnapshotsComponent.InputCommandSet> snapshotStore,
            SimulationTickNumber simulationTickNumber,
            PlayerInputSequenceNumber nextPlayerInputSequenceNumber)
        {
            ref var inputSet = ref snapshotStore.GetOrCreate(simulationTickNumber, out bool wasCreated);
            if (wasCreated)
            {
                var prevInputFindResult = snapshotStore.TryFindSnapshot(simulationTickNumber - 1);
                if (prevInputFindResult.IsFound)
                {
                    // Copy specific inputs, to handle prediction in case of missing inputs.
                    inputSet.MoveInput = prevInputFindResult.Result.MoveInput;
                }
                inputSet.SimulationTickNumber = simulationTickNumber;
                inputSet.PlayerInputSequenceNumber = nextPlayerInputSequenceNumber;
            }
        }
    }
}
