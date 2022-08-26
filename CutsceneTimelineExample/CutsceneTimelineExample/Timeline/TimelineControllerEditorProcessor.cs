using Stride.Animations;
using Stride.Engine;
using Stride.Games;

namespace CutsceneTimelineExample.Timeline
{
    class TimelineControllerEditorProcessor : EntityProcessor<TimelineController>
    {
#if GAME_EDITOR
        protected override void OnSystemAdd()
        {
            base.OnSystemAdd();

            // By default AnimationProcessor is not enabled in the editor.
            // This and AnimationAssetEditorGameCompilerExt ensures we trick the editor
            // to run the animation in the editor
            var animationProcessor = EntityManager.GetProcessor<AnimationProcessor>();
            if (animationProcessor == null)
            {
                animationProcessor = new AnimationProcessor();
                EntityManager.Processors.Add(animationProcessor);
            }
        }

        public override void Update(GameTime gameTime)
        {
            var timeElapsed = gameTime.Elapsed;
            foreach (var kv in ComponentDatas)
            {
                var tlcComp = kv.Key;
                tlcComp.EditorControl.Update(timeElapsed);
            }
        }
#endif
    }
}
