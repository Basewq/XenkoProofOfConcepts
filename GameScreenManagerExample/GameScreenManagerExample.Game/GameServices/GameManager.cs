using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using System;

namespace GameScreenManagerExample.GameServices
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

        [DataMemberIgnore]
        public IServiceRegistry Services { get; private set; }

        internal void Initialize(IServiceRegistry services)
        {
            Services = services;
        }

        internal void ResetGameplayFields()
        {
            CoinCollected = 0;
        }

        // Add any game play related method/properties here
        public event Action<int> CoinCollectedChanged;

        private int _coinCollected;
        public int CoinCollected
        {
            get => _coinCollected;
            internal set
            {
                _coinCollected = value;
                CoinCollectedChanged?.Invoke(_coinCollected);
            }
        }
    }
}
