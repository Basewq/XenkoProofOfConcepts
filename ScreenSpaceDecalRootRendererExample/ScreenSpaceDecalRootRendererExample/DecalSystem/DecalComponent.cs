using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Graphics;
using Stride.Rendering;

namespace ScreenSpaceDecalExample.DecalSystem
{
    [ComponentCategory("Model")]
    [DataContract]
    //[DefaultEntityComponentProcessor(typeof(DecalProcessor))]
    [DefaultEntityComponentRenderer(typeof(DecalProcessor))]
    public class DecalComponent : ActivableEntityComponent
    {
        [DataMember(10)]
        [Display("Texture")]
        public Texture Texture { get; set; }

        [DataMember(20)]
        [Display("Base Color")]
        public Color4 Color { get; set; } = Color4.White;

        [DataMember(30)]
        [DefaultValue(1f)]
        public float TextureScale { get; set; } = 1f;

        [DataMember(40)]
        [Display("Render group")]
        public RenderGroup RenderGroup { get; set; }
    }
}
