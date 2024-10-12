using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;
using System.Collections.Generic;

namespace SceneEditorExtensionExample.SharedData
{
    /**
     * This is the custom data as seen at run-time.
     */
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<FoliagePlacement>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<FoliagePlacement>), Profile = "Content")]
    public class FoliagePlacement
    {
        public List<ModelPlacement> ModelPlacements { get; set; } = new();
    }

    [DataContract]
    public class ModelPlacement
    {
        public UrlReference<Model> ModelUrl { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 SurfaceNormalModelSpace { get; set; }
    }
}
