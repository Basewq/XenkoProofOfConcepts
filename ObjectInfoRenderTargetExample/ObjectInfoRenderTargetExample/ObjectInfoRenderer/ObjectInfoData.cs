namespace ObjectInfoRenderTargetExample.ObjectInfoRenderer
{
    struct ObjectInfoData
    {
        private const float MaxMaterialIndex = 1024;

        public float ModelComponentId;
        /// <summary>
        /// MeshIndex is stored as the integer part, MaterialIndex is stored as fractional which can
        /// be recovered by multiplying the fraction by 1024.
        /// </summary>
        public float MeshIndexAndMaterialIndex;

        public ObjectInfoData(float modelComponentId, float meshIndex, float materialIndex)
        {
            ModelComponentId = modelComponentId;
            MeshIndexAndMaterialIndex = meshIndex + (materialIndex / MaxMaterialIndex);
        }
    }
}
