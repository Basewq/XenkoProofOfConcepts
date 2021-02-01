using System.Collections.Generic;
using Stride.Animations;
using Stride.Core;
using Stride.Engine;

namespace ObjectInfoRenderTargetExample
{
    [DataContract("PlayAnimation")]
    public class PlayAnimation
    {
        public AnimationClip Clip;
        public AnimationBlendOperation BlendOperation = AnimationBlendOperation.LinearBlend;
        public double StartTime = 0;
    }

    /// <summary>
    /// Script which starts a few animations on its entity
    /// </summary>
    public class MannequinAnimationStart : StartupScript
    {
        /// <summary>
        /// Al list of animations to be loaded when the script starts
        /// </summary>
        public readonly List<PlayAnimation> Animations = new List<PlayAnimation>();

        public override void Start()
        {
            var animComponent = Entity.GetOrCreate<AnimationComponent>();

            if (animComponent != null)
                PlayAnimations(animComponent);

            // Destroy this script since it's no longer needed
            Entity.Remove(this);
        }

        private void PlayAnimations(AnimationComponent animComponent)
        {
            foreach (var anim in Animations)
            {
                if (anim.Clip != null)
                    animComponent.Add(anim.Clip, anim.StartTime, anim.BlendOperation);
            }

            Animations.Clear();
        }
    }
}
