namespace ObjectInfoRenderTargetExample.ObjectInfoRenderer
{
    // The generated shader key will be public, so this struct must also be public.
    public struct ObjectInfoData
    {
        public uint ModelComponentId;
        /// <summary>
        /// MeshIndex is stored in the upper 16 bits, MaterialIndex is stored in the lower 16 bits.
        /// </summary>
        public uint MeshIndexAndMaterialIndex;

        public ObjectInfoData(uint modelComponentId, ushort meshIndex, ushort materialIndex)
        {
            ModelComponentId = modelComponentId;
            MeshIndexAndMaterialIndex = ((uint)meshIndex << 16) | materialIndex;
        }
    }
}
