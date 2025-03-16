#if GAME_EDITOR
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using System;

namespace SceneEditorExtensionExample.StrideEditorExt;

static class SceneEditorExtensions
{
    // Code from Stride.Assets.Presentation.AssetEditors.GameEditor.Game.EditorGameHelper
    public static Ray CalculateRayFromMousePosition([NotNull] CameraComponent camera, Vector2 normalisedMousePosition, Matrix worldView)
    {
        // determine the mouse position normalized, centered and correctly oriented
        var screenPosition = new Vector2(2f * (normalisedMousePosition.X - 0.5f), -2f * (normalisedMousePosition.Y - 0.5f));

        if (camera.Projection == CameraProjectionMode.Perspective)
        {
            // calculate the ray direction corresponding to the click in the view space
            var verticalFov = MathUtil.DegreesToRadians(camera.VerticalFieldOfView);
            var rayDirectionView = Vector3.Normalize(new Vector3(camera.AspectRatio * screenPosition.X, screenPosition.Y, -1 / MathF.Tan(verticalFov / 2f)));

            // calculate the direction of the ray in the gizmo space
            var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(rayDirectionView, worldView));

            return new Ray(worldView.TranslationVector, rayDirectionGizmo);
        }
        else
        {
            // calculate the direction of the ray in the gizmo space
            var rayDirectionGizmo = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, worldView));

            // calculate the position of the ray in the gizmo space
            var halfSize = camera.OrthographicSize / 2f;
            var rayOriginOffset = new Vector3(screenPosition.X * camera.AspectRatio * halfSize, screenPosition.Y * halfSize, 0);
            var rayOrigin = Vector3.TransformCoordinate(rayOriginOffset, worldView);

            return new Ray(rayOrigin, rayDirectionGizmo);
        }
    }

    public static Vector3 GetPositionInScene(CameraComponent cameraComponent, in Vector2 normalisedMousePosition, float planePositionY = 0)
    {
        const float limitAngle = 7.5f * MathUtil.Pi / 180f;
        const float randomDistance = 20f;

        Matrix.Invert(ref cameraComponent.ViewMatrix, out var worldMatrix);
        var ray = CalculateRayFromMousePosition(cameraComponent, normalisedMousePosition, worldMatrix);
        var plane = new Plane(Vector3.UnitY, d: -planePositionY);

        // Ensures a ray angle with projection plane of at least 'limitAngle' to avoid the object to go to infinity.
        var dotProductValue = Vector3.Dot(ray.Direction, plane.Normal);
        var comparisonSign = Math.Sign(Vector3.Dot(ray.Position, plane.Normal) + plane.D);
        if (comparisonSign * (MathF.Acos(dotProductValue) - MathUtil.PiOverTwo) < limitAngle || !plane.Intersects(in ray, out Vector3 scenePosition))
        {
            scenePosition = ray.Position + randomDistance * ray.Direction;
        }

        return scenePosition;
    }
}
#endif
