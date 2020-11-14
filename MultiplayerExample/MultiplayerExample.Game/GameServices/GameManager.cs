using MultiplayerExample.Engine;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using System;

namespace MultiplayerExample.GameServices
{
    /// <summary>
    /// The GameManager component on the GameManager entity.
    /// This component is to be used like a service.
    /// Note that the GameManager entity must exist on the <b>root</b> scene, and persist for the entire lifetime of the game.
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(GameManagerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class GameManager : EntityComponent
    {
        public const string EntityName = "GameManager";

        private IExitGameService _exitGameService;

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        [DataMemberIgnore]
        internal GameEngineContext GameEngineContext { get; private set; }

        internal void Initialize(IServiceRegistry services)
        {
            Services = services;
            GameEngineContext = Services.GetSafeServiceAs<GameEngineContext>();

            _exitGameService = Services.GetSafeServiceAs<IExitGameService>();
        }

        public void ExitGame() => _exitGameService.Exit();

        // Add any game play related method/properties here
    }
}
