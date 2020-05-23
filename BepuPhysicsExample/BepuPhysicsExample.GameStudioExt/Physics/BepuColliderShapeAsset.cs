using BepuPhysicsExample.BepuPhysicsIntegration;
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;

namespace BepuPhysicsExample.GameStudioExt.Physics
{
    [Display(800, "Collider shape (Bepu)")]        //(int)AssetDisplayPriority.Physics
    [DataContract("BepuColliderShapeAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(BepuPhysicsColliderShape))]
    [AssetFormatVersion(BepuPhysicsExampleConfig.PackageName, CurrentVersion, "1.0.0.0")]
    public partial class BepuColliderShapeAsset : Asset
    {
        private const string CurrentVersion = "1.0.0.0";

        public const string FileExtension = ".bpphy";

        /// <userdoc>
        /// The collection of shapes in this asset, a collection shapes will automatically generate a compound shape.
        /// </userdoc>
        [DataMember(10)]
        [Category]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public List<IBepuAssetColliderShapeDesc> ColliderShapes { get; } = new List<IBepuAssetColliderShapeDesc>();
    }
}
