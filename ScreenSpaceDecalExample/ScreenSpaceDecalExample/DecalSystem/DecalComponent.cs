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
        [Display("Decal Color")]
        public Color4 Color { get; set; } = Color4.White;

        [DataMember(30)]
        [DefaultValue(1f)]
        public float DecalScale { get; set; } = 1f;

        [DataMember(40)]
        public RenderGroup RenderGroup { get; set; }
    }
}
