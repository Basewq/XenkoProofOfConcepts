// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Linq;

namespace MultiplayerExample.Core
{
    public static class Utils
    {
        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera, Vector3 upVector)
        {
            var inverseView = Matrix.Invert(camera.ViewMatrix);

            var forward = Vector3.Cross(upVector, inverseView.Right);
            forward.Normalize();

            var right = Vector3.Cross(forward, upVector);
            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            worldDirection.Normalize();
            return worldDirection;
        }

        public static void MergeSceneTo(this Scene sourceScene, Scene destinationScene)
        {
            var srcEntities = sourceScene.Entities;
            while (srcEntities.Count > 0)
            {
                srcEntities[0].Scene = destinationScene;
            }
        }
    }
}
