using Stride.Core.Mathematics;
using Stride.Engine;
using System;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage;

[ComponentCategory("Environment")]
public class FoliageInteractableTest : SyncScript
{
    private Vector3 _initialPosition;
    private float _currentAngle = 0;

    public float RotationSpeedDegrees { get; set; } = 60;

    public override void Start()
    {
        _initialPosition = Entity.Transform.Position;
    }

    private bool _isRunning = true;
    public override void Update()
    {
        if (Input.HasKeyboard)
        {
            if (Input.IsKeyPressed(Stride.Input.Keys.Z))
            {
                _isRunning = !_isRunning;
            }
            if (Input.IsKeyPressed(Stride.Input.Keys.X))
            {
                var modelComp = Entity.Get<ModelComponent>();
                if (modelComp is not null)
                {
                    modelComp.Enabled = !modelComp.Enabled;
                }
            }
        }
        if (!_isRunning)
        {
            return;
        }
        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        _currentAngle += dt * MathUtil.DegreesToRadians(RotationSpeedDegrees);
        while (_currentAngle > MathUtil.TwoPi)
        {
            _currentAngle -= MathUtil.TwoPi;
        }
        (var sin, var cos) = MathF.SinCos(_currentAngle);
        var nextPos = _initialPosition;
        nextPos.X += sin * 2;
        nextPos.Z += cos * 2;
        Entity.Transform.Position = nextPos;
    }
}
