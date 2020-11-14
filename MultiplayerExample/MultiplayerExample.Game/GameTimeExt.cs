using Stride.Games;
using System;
using System.Reflection;

namespace MultiplayerExample
{
    class GameTimeExt : GameTime
    {
        // HACK: GameTime hides the Update/Reset methods to be internal, so use reflection
        // to be able to access them! Be wary of GameTime implementation changes when
        // updating Stride versions.
        private delegate void UpdateStrideGameTimeDelegate(TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool incrementFrameCount);
        private delegate void ResetStrideGameTimeDelegate(TimeSpan totalGameTime);

        private readonly UpdateStrideGameTimeDelegate _updateGameTimeDelegate;
        private readonly ResetStrideGameTimeDelegate _resetGameTimeDelegate;

        public GameTimeExt() : this(TimeSpan.Zero, TimeSpan.Zero)
        {
        }

        public GameTimeExt(TimeSpan totalTime, TimeSpan elapsedTime) : base(totalTime, elapsedTime)
        {
            var updateGameTimeMethod = typeof(GameTime).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            _updateGameTimeDelegate = (UpdateStrideGameTimeDelegate)Delegate.CreateDelegate(typeof(UpdateStrideGameTimeDelegate), this, updateGameTimeMethod);

            var resetGameTimeMethod = typeof(GameTime).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
            _resetGameTimeDelegate = (ResetStrideGameTimeDelegate)Delegate.CreateDelegate(typeof(ResetStrideGameTimeDelegate), this, resetGameTimeMethod);
        }

        public void Update(TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool incrementFrameCount)
        {
            _updateGameTimeDelegate(totalGameTime, elapsedGameTime, incrementFrameCount);
        }

        public void Reset(TimeSpan totalGameTime)
        {
            _resetGameTimeDelegate(totalGameTime);
        }
    }
}
