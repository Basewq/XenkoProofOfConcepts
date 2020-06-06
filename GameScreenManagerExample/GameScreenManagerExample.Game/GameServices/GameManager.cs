using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace GameScreenManagerExample.GameServices
{
    /// <summary>
    /// The GameManager component on the GameManager entity. This is mainly used to set up the <see cref="GameManager"/> which
    /// is actually a service.
    /// Note that the GameManager entity must exist on the root scene, and persist for the entire lifetime of the game.
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(GameManagerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class GameManager : EntityComponent
    {
        public const string EntityName = "GameManager";

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        internal void Initialize(IServiceRegistry services)
        {
            Services = services;
        }

        // Add any game play related method/properties here
    }
}
