using Stride.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CutsceneTimelineExample.Timeline
{
    [DataContract]
    public class TimelineSequence
    {
        public List<TimelineTrackBase> Tracks { get; } = new List<TimelineTrackBase>();

        public bool IsLooping { get; set; }

        private TimeSpan _maxEndTime;
        public TimeSpan Duration => _maxEndTime;

        public bool IsPlaying { get; private set; }

        private TimeSpan _currentTime;
        public TimeSpan CurrentTime => _currentTime;

        public void CalculateTimelineDuration()
        {
            // Determine the duration of the timeline based on the longest end time of the tracks
            float maxEndTimeInSeconds = (Tracks.Count > 0) ? Tracks.Max(x => x.TimelineEndTimeInSeconds) : 0;
            _maxEndTime = TimeSpan.FromSeconds(maxEndTimeInSeconds);
        }

        public void RetargetTrackBinding<T>(string bindingName, T newTargetBinding)
        {
            foreach (var track in Tracks)
            {
                track.RetargetTrackBinding(bindingName, newTargetBinding);
            }
        }

        public void Play()
        {
            if (!IsPlaying)
            {
                CalculateTimelineDuration();
            }
            IsPlaying = true;
            foreach (var track in Tracks)
            {
                if (!track.IsTrackMuted)
                {
                    track.OnPlay(_currentTime.TotalSeconds);
                }
            }
        }

        public void Pause()
        {
            IsPlaying = false;
            foreach (var track in Tracks)
            {
                if (!track.IsTrackMuted)
                {
                    track.OnPause(_currentTime.TotalSeconds);
                }
            }
        }

        public void Stop()
        {
            IsPlaying = false;
            SetTime(TimeSpan.Zero);
        }

        public void SetTime(TimeSpan currentTime)
        {
            _currentTime = currentTime;

            bool isPaused = !IsPlaying;
            var timelineTime = new TimelineTime(currentTime, timeElapsed: TimeSpan.Zero);
            foreach (var track in Tracks)
            {
                if (!track.IsTrackMuted)
                {
                    track.UpdateTrack(timelineTime, isPaused);
                }
            }
        }

        public void Update(TimeSpan timeElapsed)
        {
            bool isAtEndOfTimeline = _currentTime >= _maxEndTime;
            if (isAtEndOfTimeline && IsLooping)
            {
                // For debugging reasons, we do not reset _currentTime immediately after processing the timeline, and
                // do it at the beginning of the next update, since it'll be harder to determine what actually
                // happened in the update if we are looking at this object elsewhere after it had been processed.
                _currentTime = TimeSpan.Zero;
            }
            else if (isAtEndOfTimeline && !IsLooping)
            {
               // Debug.Fail($"{nameof(UpdateTimeline)} should not be called because it has already finished.");
                return;
            }

            var timeElapsedRemaining = timeElapsed;
            do
            {
                var timeElapsedProcessed = ProcessTimeOnce(timeElapsedRemaining);

                timeElapsedRemaining -= timeElapsedProcessed;
                bool hasReachedEndOfTimeline = _currentTime == _maxEndTime;
                if (hasReachedEndOfTimeline)
                {
                    if (!IsLooping)
                    {
                        IsPlaying = false;
                        break;
                    }
                    else if (IsLooping && timeElapsedRemaining > TimeSpan.Zero)
                    {
                        _currentTime = TimeSpan.Zero;
                    }
                }
            } while (timeElapsedRemaining > TimeSpan.Zero);
        }

        /// <summary>
        /// Process time up to the end of the timeline. Time beyond the timeline duration will not be processed.
        /// </summary>
        /// <param name="timeElapsed">Desired amount of time to process on the timeline.</param>
        /// <returns>The actual amount of time processed.</returns>
        private TimeSpan ProcessTimeOnce(TimeSpan timeElapsed)
        {
            var timeElapsedToProcess = timeElapsed;

            var newCurrentTime = _currentTime + timeElapsed;
            if (newCurrentTime > _maxEndTime)
            {
                newCurrentTime = _maxEndTime;
                timeElapsedToProcess = _maxEndTime - _currentTime;
            }

            bool isPaused = !IsPlaying;
            var timelineTime = new TimelineTime(newCurrentTime, timeElapsedToProcess);
            foreach (var track in Tracks)
            {
                if (!track.IsTrackMuted)
                {
                    track.UpdateTrack(timelineTime, isPaused);
                }
            }

            _currentTime = newCurrentTime;

            return timeElapsedToProcess;
        }
    }

    public readonly struct TimelineTime
    {
        public readonly TimeSpan CurrentTime;
        public readonly TimeSpan TimeElapsed;

        public TimelineTime(TimeSpan currentTime, TimeSpan timeElapsed)
        {
            CurrentTime = currentTime;
            TimeElapsed = timeElapsed;
        }
    }
}
