using MultiplayerExample.Core;
using MultiplayerExample.Engine;
using MultiplayerExample.Input;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultiplayerExample.Network.SnapshotStores
{
    class InputSnapshotsInGameInputProcessor : EntityProcessor<InputDeviceComponent, InputSnapshotsInGameInputProcessor.AssociatedData>,
        IInGameProcessor, IPreUpdateProcessor
    {
        private InputManager _inputManager;
        private GameClockManager _gameClockManager;
        private GameEngineContext _gameEngineContext;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    InitializePreviousValues();     // HACK: Required to call this, otherwise the screen would exit because 'JustPressed' values would be triggered on the first update
                }
            }
        }

        public InputSnapshotsInGameInputProcessor() : base(typeof(InputSnapshotsComponent))
        {
            Order = 0;
            // Not using Enabled property, because that completely disables the processor, where it doesn't even pick up newly added entities
            IsEnabled = true;
        }

        protected override void OnSystemAdd()
        {
            // Some services or their fields will be null in the Game Studio
            _inputManager = Services.GetSafeServiceAs<InputManager>();

            _gameClockManager = Services.GetService<GameClockManager>();
            _gameEngineContext = Services.GetService<GameEngineContext>();
            Enabled = _gameEngineContext.IsClient;
        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] InputDeviceComponent component)
        {
            return new AssociatedData
            {
                InputDeviceComponent = component,
                // Can also add other info/components here
                InputSnapshotsComponent = entity.Get<InputSnapshotsComponent>(),
            };
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InputDeviceComponent component, [NotNull] AssociatedData data)
        {
            _inputManager?.VirtualButtonConfigSet?.Add(data.VirtualButtons);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InputDeviceComponent component, [NotNull] AssociatedData data)
        {
            if (data.VirtualButtons != null)
            {
                _inputManager?.VirtualButtonConfigSet?.Remove(data.VirtualButtons);
            }
            // Probably don't need to call Clear, since the whole data object will be garbage collected....
            data?.KeyboardBindings?.Clear();
            data?.MouseBindings?.Clear();
            data?.ControllerBindings?.Clear();
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] InputDeviceComponent component, [NotNull] AssociatedData associatedData)
        {
            // Check the all the components are still the same.
            // This can fail if any component is removed from the entity.
            return associatedData.InputDeviceComponent == component
                && associatedData.InputSnapshotsComponent == entity.Get<InputSnapshotsComponent>();
        }

        public void PreUpdate(GameTime gameTime)
        {
            // Special case: input must be generated AHEAD of the simulation, except when it's exactly on a new sim step
            // because it can be processed immediately instead of being delayed by one 'frame'.
            var simTickNumber = _gameClockManager.SimulationClock.IsNextSimulation ? _gameClockManager.SimulationClock.SimulationTickNumber : _gameClockManager.SimulationClock.SimulationTickNumber + 1;
            foreach (var kv in ComponentDatas)
            {
                var data = kv.Value;
                CheckDeviceRegistration(data);
                ProcessInput(simTickNumber, data);
            }
        }

        private void CheckDeviceRegistration(AssociatedData data)
        {
            // TODO: should probably read a user's game settings for what they want mapped
            var inputDeviceComp = data.InputDeviceComponent;
            var virtualButtons = data.VirtualButtons;
            var keyboardBindings = data.KeyboardBindings;
            var mouseBindings = data.MouseBindings;
            var controllerBindings = data.ControllerBindings;

            if (inputDeviceComp.IsKeyboardControlsEnabled && !data.IsKeyboardVirtualButtonsAssigned)
            {
                RemoveBindings(keyboardBindings, virtualButtons);

                // WASD keys
                var strafe = new VirtualButtonTwoWay(VirtualButton.Keyboard.A, VirtualButton.Keyboard.D);
                keyboardBindings.Add(new VirtualButtonBinding(InputAction.CharacterMoveStrafe, strafe));
                var forwardOrBackward = new VirtualButtonTwoWay(VirtualButton.Keyboard.S, VirtualButton.Keyboard.W);
                keyboardBindings.Add(new VirtualButtonBinding(InputAction.CharacterMoveForwardOrBackward, forwardOrBackward));
                keyboardBindings.Add(new VirtualButtonBinding(InputAction.CharacterJump, VirtualButton.Keyboard.Space));

                keyboardBindings.Add(new VirtualButtonBinding(InputAction.CameraUnlock, VirtualButton.Keyboard.Escape));

                virtualButtons.AddRange(keyboardBindings);
                data.IsKeyboardVirtualButtonsAssigned = true;
            }
            else if (!inputDeviceComp.IsKeyboardControlsEnabled && data.IsKeyboardVirtualButtonsAssigned)
            {
                RemoveBindings(keyboardBindings, virtualButtons);
            }

            if (inputDeviceComp.IsMouseControlsEnabled && !data.IsMouseVirtualButtonsAssigned)
            {
                RemoveBindings(mouseBindings, virtualButtons);

                float mouseScale = inputDeviceComp.MouseSensitivity;
                mouseBindings.Add(new VirtualButtonBindingExt(InputAction.CameraRotateYaw, VirtualButton.Mouse.DeltaX, scaleValue: mouseScale));
                mouseBindings.Add(new VirtualButtonBindingExt(InputAction.CameraRotatePitch, VirtualButton.Mouse.DeltaY, scaleValue: -mouseScale));

                mouseBindings.Add(new VirtualButtonBinding(InputAction.CameraLockIn, VirtualButton.Mouse.Left));

                virtualButtons.AddRange(mouseBindings);
                data.IsMouseVirtualButtonsAssigned = true;
            }
            else if (!inputDeviceComp.IsMouseControlsEnabled && data.IsMouseVirtualButtonsAssigned)
            {
                RemoveBindings(mouseBindings, virtualButtons);
            }

            if (inputDeviceComp.IsControllerEnabled && !data.IsControllerVirtualButtonsAssigned && inputDeviceComp.ActiveController != null)
            {
                RemoveBindings(controllerBindings, virtualButtons);

                int padIndex = inputDeviceComp.ActiveController.Index;
                float deadZoneThreshold = inputDeviceComp.DeadZoneThreshold;

                controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CharacterMoveStrafe, VirtualButton.GamePad.LeftThumbAxisX.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));
                controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CharacterMoveForwardOrBackward, VirtualButton.GamePad.LeftThumbAxisY.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));
                //controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CharacterRotateYaw, VirtualButton.GamePad.RightThumbAxisX.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));
                //controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CharacterRotatePitch, VirtualButton.GamePad.RightThumbAxisY.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));

                controllerBindings.Add(new VirtualButtonBinding(InputAction.CharacterJump, VirtualButton.GamePad.A.WithIndex(padIndex)));

                controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CameraRotateYaw, VirtualButton.GamePad.RightThumbAxisX.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));
                controllerBindings.Add(new VirtualButtonBindingExt(InputAction.CameraRotatePitch, VirtualButton.GamePad.RightThumbAxisY.WithIndex(padIndex), deadZoneThreshold: deadZoneThreshold));

                virtualButtons.AddRange(controllerBindings);
                data.IsControllerVirtualButtonsAssigned = true;
            }
            else if (!inputDeviceComp.IsControllerEnabled && data.IsControllerVirtualButtonsAssigned)
            {
                RemoveBindings(controllerBindings, virtualButtons);
            }

            static void RemoveBindings(List<VirtualButtonBinding> bindingsToRemove, VirtualButtonConfig virtBtnConfig)
            {
                foreach (var bnd in bindingsToRemove)
                {
                    virtBtnConfig.Remove(bnd);
                }
            }
        }

        private void ProcessInput(SimulationTickNumber simTickNumber, AssociatedData data)
        {
            var configExt = data.VirtualButtons;
            int btnConfigIndex = _inputManager.VirtualButtonConfigSet.IndexOf(configExt);

            var inputSnapshotsComp = data.InputSnapshotsComponent;
            if (inputSnapshotsComp != null)
            {
                var inputFindResult = data.InputSnapshotsComponent.SnapshotStore.TryFindSnapshot(simTickNumber);
                Debug.Assert(inputFindResult.IsFound);
                ref var inputData = ref inputFindResult.Result;

                var moveInputVec = Vector2.Zero;
                moveInputVec.X = _inputManager.GetVirtualButton(btnConfigIndex, InputAction.CharacterMoveStrafe);
                moveInputVec.Y = _inputManager.GetVirtualButton(btnConfigIndex, InputAction.CharacterMoveForwardOrBackward);

                if (inputSnapshotsComp.Camera != null)
                {
                    var camAlignedMoveDir = Utils.LogicDirectionToWorldDirection(moveInputVec, inputSnapshotsComp.Camera, Vector3.UnitY);
                    moveInputVec.X = camAlignedMoveDir.X;
                    moveInputVec.Y = camAlignedMoveDir.Z;
                }

                inputData.MoveInput = moveInputVec;
                inputData.IsJumpButtonDown = inputData.IsJumpButtonDown || _inputManager.IsJustPressed(btnConfigIndex, InputAction.CharacterJump, configExt);

                var cameraInputVec = Vector2.Zero;
                cameraInputVec.X = _inputManager.GetVirtualButton(btnConfigIndex, InputAction.CameraRotateYaw);
                cameraInputVec.Y = _inputManager.GetVirtualButton(btnConfigIndex, InputAction.CameraRotatePitch);

                inputData.CameraMoveInput = cameraInputVec;
                inputData.IsCameraLockInButtonDown = inputData.IsCameraLockInButtonDown || _inputManager.IsDown(btnConfigIndex, InputAction.CameraLockIn);
                inputData.IsCameraUnlockButtonDown = inputData.IsCameraUnlockButtonDown || _inputManager.IsDown(btnConfigIndex, InputAction.CameraUnlock);
            }

            configExt.SaveValueAsPreviousValue(_inputManager);
        }

        private void InitializePreviousValues()
        {
            foreach (var kv in ComponentDatas)
            {
                var data = kv.Value;
                var configExt = data.VirtualButtons;
                configExt.SaveValueAsPreviousValue(_inputManager);
            }
        }

        internal class AssociatedData
        {
            internal bool IsKeyboardVirtualButtonsAssigned;
            internal bool IsMouseVirtualButtonsAssigned;
            internal bool IsControllerVirtualButtonsAssigned;

            internal VirtualButtonConfigExt VirtualButtons = new VirtualButtonConfigExt();
            internal List<VirtualButtonBinding> KeyboardBindings = new List<VirtualButtonBinding>();
            internal List<VirtualButtonBinding> MouseBindings = new List<VirtualButtonBinding>();
            internal List<VirtualButtonBinding> ControllerBindings = new List<VirtualButtonBinding>();

            internal InputDeviceComponent InputDeviceComponent;
            internal InputSnapshotsComponent InputSnapshotsComponent;
        }

        // Can't use enum because VirtualButtonBinding requires objects for "name", and using enums would just cause boxing
        private static class InputAction
        {
            /// <summary>
            /// Rotate along x-axis
            /// </summary>
            public static readonly object CharacterRotateYaw = new object();
            /// <summary>
            /// Rotate along y-axis
            /// </summary>
            public static readonly object CharacterRotatePitch = new object();
            /// <summary>
            /// Strafe
            /// </summary>
            public static readonly object CharacterMoveStrafe = new object();
            /// <summary>
            /// Move forward/backward
            /// </summary>
            public static readonly object CharacterMoveForwardOrBackward = new object();
            public static readonly object CharacterJump = new object();

            public static readonly object CameraRotateYaw = new object();
            public static readonly object CameraRotatePitch = new object();
            public static readonly object CameraLockIn = new object();
            public static readonly object CameraUnlock = new object();
        }
    }
}
