using Stride.Animations;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Diagnostics;

namespace CutsceneTimelineExample.Timeline
{
    [DataContract(Inherited = true)]
    public abstract class TimelineTrackBase
    {
        protected TimelineSequence Storyboard { get; private set; }

        [DataMember(order: 0)]
        public bool IsTrackMuted { get; set; }
        [DataMember(name: "Start Time (s)", order: 10)]
        public float TimelineStartTimeInSeconds { get; set; }
        [DataMember(name: "End Time (s)", order: 11)]
        public float TimelineEndTimeInSeconds { get; set; }

        public virtual void RetargetTrackBinding<T>(string bindingName, T newTargetBinding) { }
        internal protected virtual void OnPlay(double timelineTimeInSeconds) { }
        internal protected virtual void OnPause(double timelineTimeInSeconds) { }
        internal protected abstract void UpdateTrack(in TimelineTime time, bool isPaused);

        internal protected bool IsActiveTrack(in TimelineTime time)
        {
            double currentTimelineTimeInSeconds = time.CurrentTime.TotalSeconds;
            double previousTimelineTimeInSeconds = (time.CurrentTime - time.TimeElapsed).TotalSeconds;
            if (currentTimelineTimeInSeconds < TimelineStartTimeInSeconds || previousTimelineTimeInSeconds > TimelineEndTimeInSeconds)
            {
                // Time is before track, or after track
                return false;
            }

            if (IsInRange(previousTimelineTimeInSeconds, TimelineStartTimeInSeconds, TimelineEndTimeInSeconds)
                || IsInRange(currentTimelineTimeInSeconds, TimelineStartTimeInSeconds, TimelineEndTimeInSeconds))
            {
                return true;
            }
            if (previousTimelineTimeInSeconds <= TimelineStartTimeInSeconds && currentTimelineTimeInSeconds >= TimelineEndTimeInSeconds)
            {
                return true;
            }

            return false;
        }

        internal void OnInitialize(TimelineSequence storyboard)
        {
            Storyboard = storyboard;
        }

        private static bool IsInRange(double value, float minValue, float maxValueExcl)
        {
            return minValue <= value && value < maxValueExcl;
        }
    }

    public class TransformTimelineTrack : TimelineTrackBase
    {
        private double _currentTimelineTimeInSeconds = -1;

        [DataMember(order: 20)]
        public string EntityBindingName { get; set; }

        [DataMember(order: 21)]
        public string PositionStartBindingName { get; set; }

        [DataMember(order: 22)]
        public string PositionEndBindingName { get; set; }

        [DataMember(order: 30)]
        public Entity Entity { get; set; }

        private TransformComponent _positionStart;
        [DataMember(order: 31)]
        public TransformComponent PositionStart
        {
            get => _positionStart;
            set
            {
                _positionStart = value;
                var time = new TimelineTime(TimeSpan.FromSeconds(_currentTimelineTimeInSeconds), timeElapsed: TimeSpan.Zero);
                if (!IsActiveTrack(time))
                {
                    return;
                }
                UpdateEntityTransform();
            }
        }

        private TransformComponent _positionEnd;
        [DataMember(order: 32)]
        public TransformComponent PositionEnd
        {
            get => _positionEnd;
            set
            {
                _positionEnd = value;
                var time = new TimelineTime(TimeSpan.FromSeconds(_currentTimelineTimeInSeconds), timeElapsed: TimeSpan.Zero);
                if (!IsActiveTrack(time))
                {
                    return;
                }
                UpdateEntityTransform();
            }
        }

        private bool HasValidObjects => Entity != null && _positionStart != null && _positionEnd != null;

        public override void RetargetTrackBinding<T>(string bindingName, T newTargetBinding)
        {
            if (string.Equals(bindingName, EntityBindingName, StringComparison.OrdinalIgnoreCase))
            {
                if (newTargetBinding is Entity targetEntity)
                {
                    Entity = targetEntity;
                }
                else
                {
                    Debug.Fail($"{nameof(newTargetBinding)} is not of type '{typeof(Entity).Name}' (type passed was '{newTargetBinding.GetType().Name}')");
                }
            }
            if (string.Equals(bindingName, PositionStartBindingName, StringComparison.OrdinalIgnoreCase))
            {
                if (newTargetBinding is TransformComponent targetPositionStart)
                {
                    PositionStart = targetPositionStart;
                }
                else
                {
                    Debug.Fail($"{nameof(newTargetBinding)} is not of type '{typeof(TransformComponent).Name}' (type passed was '{newTargetBinding.GetType().Name}')");
                }
            }
            if (string.Equals(bindingName, PositionEndBindingName, StringComparison.OrdinalIgnoreCase))
            {
                if (newTargetBinding is TransformComponent targetPositionEnd)
                {
                    PositionEnd = targetPositionEnd;
                }
                else
                {
                    Debug.Fail($"{nameof(newTargetBinding)} is not of type '{typeof(TransformComponent).Name}' (type passed was '{newTargetBinding.GetType().Name}')");
                }
            }
        }

        internal protected override void OnPause(double timelineTimeInSeconds)
        {
            var time = new TimelineTime(TimeSpan.FromSeconds(timelineTimeInSeconds), timeElapsed: TimeSpan.Zero);
            UpdateTrack(time, isPaused: true);
        }

        internal protected override void UpdateTrack(in TimelineTime time, bool isPaused)
        {
            var prevTime = _currentTimelineTimeInSeconds;
            _currentTimelineTimeInSeconds = time.CurrentTime.TotalSeconds;
            bool requiresPositionUpdate = prevTime != _currentTimelineTimeInSeconds;
            if (requiresPositionUpdate && HasValidObjects && IsActiveTrack(time))
            {
                UpdateEntityTransform();
            }
        }

        private void UpdateEntityTransform()
        {
            float durationInSeconds = TimelineEndTimeInSeconds - TimelineStartTimeInSeconds;
            if (durationInSeconds <= 0)
            {
                return;
            }

            var curTrackTimeInSeconds = _currentTimelineTimeInSeconds - TimelineStartTimeInSeconds;
            var lerpAmount = curTrackTimeInSeconds / durationInSeconds;
            float clampedLerpAmount = MathUtil.Clamp((float)lerpAmount, 0, 1);
            Vector3.Lerp(ref _positionStart.Position, ref _positionEnd.Position, clampedLerpAmount, out Entity.Transform.Position);
            Quaternion.Slerp(ref _positionStart.Rotation, ref _positionEnd.Rotation, clampedLerpAmount, out Entity.Transform.Rotation);
            Vector3.Lerp(ref _positionStart.Scale, ref _positionEnd.Scale, clampedLerpAmount, out Entity.Transform.Scale);
        }
    }

    public class PlayAnimationTimelineTrack : TimelineTrackBase
    {
        private double _currentTimelineTimeInSeconds = -1;

        [DataMember(order: 20)]
        public string EntityBindingName { get; set; }

        [DataMember(order: 21)]
        public string AnimationNameBindingName { get; set; }

        [DataMember(order: 30)]
        public Entity Entity { get; set; }

        [DataMember(order: 31)]
        public string AnimationName { get; set; }

        private bool HasValidObjects => Entity != null && AnimationName != null;

        public override void RetargetTrackBinding<T>(string bindingName, T newTargetBinding)
        {
            if (string.Equals(bindingName, EntityBindingName, StringComparison.OrdinalIgnoreCase))
            {
                if (newTargetBinding is Entity targetEntity)
                {
                    Entity = targetEntity;
                }
                else
                {
                    Debug.Fail($"{nameof(newTargetBinding)} is not of type '{typeof(Entity).Name}' (type passed was '{newTargetBinding.GetType().Name}')");
                }
            }
            if (string.Equals(bindingName, AnimationNameBindingName, StringComparison.OrdinalIgnoreCase))
            {
                if (newTargetBinding is string targetPositionStart)
                {
                    AnimationName = targetPositionStart;
                }
                else
                {
                    Debug.Fail($"{nameof(newTargetBinding)} is not of type '{typeof(string).Name}' (type passed was '{newTargetBinding.GetType().Name}')");
                }
            }
        }

        internal protected override void OnPlay(double timelineTimeInSeconds)
        {
            if (!HasValidObjects)
            {
                return;
            }

            var animComp = Entity.Components.Get<AnimationComponent>();
            foreach (var playingAnim in animComp.PlayingAnimations)
            {
                if (playingAnim.Name == AnimationName)
                {
                    PlayAnimation(playingAnim, timelineTimeInSeconds, isAnimationRunning: true);
                }
            }
        }

        internal protected override void OnPause(double timelineTimeInSeconds)
        {
            if (!HasValidObjects)
            {
                return;
            }

            var animComp = Entity.Components.Get<AnimationComponent>();
            foreach (var playingAnim in animComp.PlayingAnimations)
            {
                if (playingAnim.Name == AnimationName)
                {
                    PlayAnimation(playingAnim, timelineTimeInSeconds, isAnimationRunning: false);
                }
            }
        }

        internal protected override void UpdateTrack(in TimelineTime time, bool isPaused)
        {
            _currentTimelineTimeInSeconds = time.CurrentTime.TotalSeconds;
            if (!HasValidObjects || !IsActiveTrack(time))
            {
                return;
            }

            var animComp = Entity.Components.Get<AnimationComponent>();
            var animations = animComp?.Animations;

            if (animations != null && animations.TryGetValue(AnimationName, out var animClip) && animClip != null)
            {
                bool isAnimPlaying = animComp.IsPlaying(AnimationName);
                if (!isAnimPlaying)
                {
                    var playingAnim = animComp.Play(AnimationName);
                    PlayAnimation(playingAnim, _currentTimelineTimeInSeconds, isAnimationRunning: !isPaused);
                }
                else if (isAnimPlaying && isPaused)
                {
                    foreach (var playingAnim in animComp.PlayingAnimations)
                    {
                        if (playingAnim.Name == AnimationName)
                        {
                            PlayAnimation(playingAnim, _currentTimelineTimeInSeconds, isAnimationRunning: false);
                        }
                    }
                }
            }
        }

        private void PlayAnimation(PlayingAnimation playingAnim, double timelineTimeInSeconds, bool isAnimationRunning)
        {
            playingAnim.Enabled = isAnimationRunning;
            var clipTime = timelineTimeInSeconds - TimelineStartTimeInSeconds;
            playingAnim.CurrentTime = TimeSpan.FromSeconds(clipTime);       // TODO: do we need to clamp to Math.Max(clipTime, 0)?
        }
    }
}
