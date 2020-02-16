using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace CustomAssetExample.SharedData
{
    /**
     * This is the custom data as seen at run-time.
     */
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<LocalizationStringDefinition>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<LocalizationStringDefinition>), Profile = "Content")]
    public class LocalizationStringDefinition
    {
        public string English { get; set; }
        public string French { get; set; }
        public string German { get; set; }
    }
}
