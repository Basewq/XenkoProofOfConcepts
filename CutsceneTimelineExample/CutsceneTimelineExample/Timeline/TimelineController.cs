using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using System;

namespace CutsceneTimelineExample.Timeline
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(TimelineControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [DefaultEntityComponentProcessor(typeof(TimelineControllerEditorProcessor), ExecutionMode = ExecutionMode.Editor)]
    public class TimelineController : EntityComponent
    {
#if GAME_EDITOR
        [DataMember(order: 10)]
        [Display(name: "Editor control", Expand = ExpandRule.Once)]
        public TimelineEditorControl EditorControl { get; }
#endif

        [DataMember(order: 20)]
        public TimelineSequence Timeline { get; set; } = new TimelineSequence();

        public TimelineController()
        {
#if GAME_EDITOR
            EditorControl = new TimelineEditorControl(this);
#endif
        }

        internal void Update(TimeSpan timeElapsed)
        {
            if (Timeline.IsPlaying)
            {
                Timeline.Update(timeElapsed);
            }
        }
    }

    [DataContract]
    public class TimelineEditorControl
    {
        private TimelineController _timelineController;

        private TimeSpan _currentTime;
        [DataMember(name: "Current Time (s)")]
        [DataMemberRange(minimum: 0, maximum: 30.0, smallStep: 0.01, largeStep: 0.5, decimalPlaces: 4)]
        public double CurrentTimeInSeconds
        {
            get => _currentTime.TotalSeconds;
            set
            {
                _currentTime = TimeSpan.FromSeconds(value);
                _timelineController.Timeline.SetTime(_currentTime);
            }
        }

        private Stride.Particles.Components.StateControl _previousState = (Stride.Particles.Components.StateControl)(-1);
        [DataMember(name: "State")]
        public Stride.Particles.Components.StateControl State { get; set; } = Stride.Particles.Components.StateControl.Stop;

        public bool IsPlaybackLooping { get; set; }

        public TimelineEditorControl(TimelineController timelineController)
        {
            _timelineController = timelineController;
        }

        public void Update(TimeSpan timeElapsed)
        {
            switch (State)
            {
                case Stride.Particles.Components.StateControl.Play:
                    if (!_timelineController.Timeline.IsPlaying)
                    {
                        _timelineController.Timeline.Play();
                    }
                    UpdateTimeline(timeElapsed);
                    break;

                case Stride.Particles.Components.StateControl.Pause:
                    if (_previousState != State)
                    {
                        _timelineController.Timeline.Pause();
                    }
                    break;

                case Stride.Particles.Components.StateControl.Stop:
                default:
                    if (_previousState != State)
                    {
                        CurrentTimeInSeconds = 0;

                        _timelineController.Timeline.Stop();
                    }
                    break;
            }
            _previousState = State;
        }

        private void UpdateTimeline(TimeSpan timeElapsed)
        {
            var timeline = _timelineController.Timeline;
            // Save the real IsLooping setting and set it to the editor control's setting
            bool timelineIsLooping = timeline.IsLooping;
            timeline.IsLooping = IsPlaybackLooping;

            timeline.Update(timeElapsed);

            // Restore the real IsLooping setting
            timeline.IsLooping = timelineIsLooping;
        }
    }
}
