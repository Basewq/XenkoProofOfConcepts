using Stride.Core.Mathematics;

namespace MultiplayerExample.UI
{
    struct AnimationTimer
    {
        private readonly float _durationInSeconds;
        private float _timeRemainingInSeconds;

        public AnimationTimer(float durationInSeconds)
        {
            _durationInSeconds = durationInSeconds;
            _timeRemainingInSeconds = durationInSeconds;
        }

        public bool IsComplete => _timeRemainingInSeconds <= 0;

        /// <summary>
        /// Value in range [0...1], where 0 is animation time not started, and 1 is animation time completed.
        /// </summary>
        public float CompletionValueInDecimal => 1 - (_timeRemainingInSeconds / _durationInSeconds);

        public void Update(float dt)
        {
            _timeRemainingInSeconds -= dt;
            if (_timeRemainingInSeconds < 0)
            {
                _timeRemainingInSeconds = 0;
            }
        }

        public void SetCompletionValue(float completionValueInDecimal)
        {
            completionValueInDecimal = MathUtil.Clamp(completionValueInDecimal, 0, 1);
            float time = _durationInSeconds * completionValueInDecimal;
            _timeRemainingInSeconds = _durationInSeconds - time;
        }

        public void Reset()
        {
            SetCompletionValue(0);
        }
    }
}
