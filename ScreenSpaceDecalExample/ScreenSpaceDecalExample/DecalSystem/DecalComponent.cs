using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Graphics;
using Xenko.Rendering;

namespace ScreenSpaceDecalExample.DecalSystem
{
    [ComponentCategory("Model")]
    [DataContract]
    [DefaultEntityComponentRenderer(typeof(DecalProcessor))]
    public class DecalComponent : ActivableEntityComponent
    {
        [DataMember(10)]
        [Display("Decal Texture")]
        public Texture DecalTexture { get; set; }

        [DataMember(20)]
        [Display("Tint Color")]
        public Color4 Color { get; set; } = Color4.White;

        [DataMember(30)]
        [DefaultValue(1f)]
        public float DecalScale { get; set; } = 1f;

        /// <summary>
        /// The RenderGroup this decal belongs to.
        /// </summary>
        // This is a bit arbitrary, but we must place the decals on a separate RenderGroup
        // because we do not want it to appear on the 'ObjectInfo' texture.
        [DataMember(40)]
        public RenderGroup DecalRenderGroup { get; set; } = RenderGroup.Group10;

        [DataMember(50)]
        public RenderGroupMask IgnoreRenderGroups { get; set; }

        [DataMember(60)]
        [DefaultValue(true)]
        public bool IsAffectedByShadow { get; set; } = true;
    }
}
