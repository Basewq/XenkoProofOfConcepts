using Stride.Engine;
using Stride.Games;

namespace CutsceneTimelineExample.Timeline
{
    class TimelineControllerProcessor : EntityProcessor<TimelineController>
    {
        public override void Update(GameTime gameTime)
        {
            var timeElapsed = gameTime.Elapsed;
            foreach (var kv in ComponentDatas)
            {
                var tlcComp = kv.Key;
                tlcComp.Update(timeElapsed);
            }
        }
    }
}
