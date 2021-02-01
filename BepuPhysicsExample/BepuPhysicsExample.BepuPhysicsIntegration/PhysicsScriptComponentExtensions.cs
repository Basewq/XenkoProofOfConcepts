using Stride.Engine;

namespace BepuPhysicsExample.BepuPhysicsIntegration
{
    /// <summary>
    /// Extension methods for the <see cref="ScriptComponent"/> related to phystics
    /// </summary>
    public static class PhysicsScriptComponentExtensions
    {
        /// <summary>
        /// Gets the curent <see cref="BepuSimulation"/>.
        /// </summary>
        /// <param name="scriptComponent">The script component to query physics from</param>
        /// <returns>The simulation object or null if there are no simulation running for the current scene.</returns>
        public static BepuSimulation GetBepuSimulation(this ScriptComponent scriptComponent)
        {
            return scriptComponent.SceneSystem.SceneInstance.GetProcessor<BepuPhysicsProcessor>()?.Simulation;
        }
    }
}
