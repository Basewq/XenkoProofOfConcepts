using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Games;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    public class Bepu2PhysicsSystem : GameSystem, IBepuPhysicsSystem
    {
        private class PhysicsScene
        {
            public BepuPhysicsProcessor Processor;
            public BepuSimulation Simulation;
        }

        private readonly List<PhysicsScene> scenes = new List<PhysicsScene>();

        public Bepu2PhysicsSystem(IServiceRegistry registry)
            : base(registry)
        {
            UpdateOrder = -1000; //make sure physics runs before everything

            Enabled = true; //enabled by default
        }

        private BepuPhysicsSettings physicsConfiguration;

        public override void Initialize()
        {
            physicsConfiguration = Game?.Settings != null ? Game.Settings.Configurations.Get<BepuPhysicsSettings>() : new BepuPhysicsSettings();
        }

        protected override void Destroy()
        {
            base.Destroy();

            lock (this)
            {
                foreach (var scene in scenes)
                {
                    scene.Simulation.Dispose();
                }
            }
        }

        public BepuSimulation Create(BepuPhysicsProcessor sceneProcessor, BepuPhysicsEngineFlags flags = BepuPhysicsEngineFlags.None)
        {
            var scene = new PhysicsScene { Processor = sceneProcessor, Simulation = new BepuSimulation(sceneProcessor, physicsConfiguration) };
            lock (this)
            {
                scenes.Add(scene);
            }
            return scene.Simulation;
        }

        public void Release(BepuPhysicsProcessor processor)
        {
            lock (this)
            {
                var scene = scenes.SingleOrDefault(x => x.Processor == processor);
                if (scene == null) return;
                scenes.Remove(scene);
                scene.Simulation.Dispose();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (BepuSimulation.DisableSimulation) return;

            lock (this)
            {
                // Read skinned meshes bone positions
                foreach (var physicsScene in scenes)
                {
                    // First process any needed cleanup
                    physicsScene.Processor.UpdateRemovals();

                    // Read skinned meshes bone positions and write them to the physics engine
                    physicsScene.Processor.UpdateBones();

                    // Perform clean ups before test contacts in this frame
                    physicsScene.Simulation.BeginContactTesting();

                    // Simulate physics
                    physicsScene.Simulation.Simulate((float)gameTime.Elapsed.TotalSeconds);

                    physicsScene.Processor.UpdateRigidBodies();
                    // Update character bound entity's transforms from physics engine simulation
                    physicsScene.Processor.UpdateCharacters();

                    //// Perform clean ups before test contacts in this frame
                    //physicsScene.Simulation.BeginContactTesting();

                    // Handle frame contacts
                    physicsScene.Processor.UpdateContacts();

                    // This is the heavy contact logic
                    physicsScene.Simulation.EndContactTesting();

                    // Send contact events
                    physicsScene.Simulation.SendEvents();
                }
            }
        }
    }
}
