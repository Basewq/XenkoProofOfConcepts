using SceneEditorExtensionExample.Rendering;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using System;

namespace SceneEditorExtensionExample.WorldTerrain.EnvironmentInteractions.WindSources;

public class EnvironmentWindDirectional : EnvironmentWindSourceBase
{
    private bool _isWindActive = true;      // Since _windUptimeRemaining is initially zero, it'll immediately flip to false on the first update
    private TimeSpan _windDowntimeRemaining = TimeSpan.Zero;
    private TimeSpan _windUptimeRemaining = TimeSpan.Zero;
    private float _currentWindStrength = 0;

    [Display("Wind Direction XZ")]
    public Vector2 WindDirectionXZ { get; set; } = new Vector2(1, 1);
    public float WindMaxSpeed { get; set; } = 10;
    [Display("Downtime Duration (s)")]
    public float WindDowntimeDurationSeconds { get; set; } = 5;
    [Display("Uptime Duration (s)")]
    public float WindUptimeDurationSeconds { get; set; } = 7;

    public override void AddData(ref WindSourcesPerViewData windSourcesPerViewData)
    {
        var interactorData = new EnvironmentWindDirectionalData
        {
            WindDirectionXZ = WindDirectionXZ,
            WindMaxSpeed = WindMaxSpeed,
            WindCurrentStrength = _currentWindStrength,
        };
        windSourcesPerViewData.WindDirectionalDataList.Add(interactorData);
    }

    public override void Update(GameTime time)
    {
        // Update wind
        if (!_isWindActive)
        {
            _windDowntimeRemaining -= time.Elapsed;
            if (_windDowntimeRemaining <= TimeSpan.Zero)
            {
                _isWindActive = true;
                _windUptimeRemaining = TimeSpan.FromSeconds(WindUptimeDurationSeconds) + _windDowntimeRemaining;
                _currentWindStrength = 0;
            }
        }
        else
        {
            _windUptimeRemaining -= time.Elapsed;
            if (_windUptimeRemaining <= TimeSpan.Zero)
            {
                _isWindActive = false;
                _windDowntimeRemaining = TimeSpan.FromSeconds(WindDowntimeDurationSeconds);    // Reset

                //System.Diagnostics.Debug.WriteLine($"WndEnd-----");
            }
            else
            {
                float activeTimePercentageDecimal = (float)(_windUptimeRemaining.TotalSeconds / WindUptimeDurationSeconds);
                float strength;
                if (activeTimePercentageDecimal >= 0.5f)
                {
                    strength = (activeTimePercentageDecimal - 0.5f) * 2;    // Change from [0.5, 1] to [0, 1]
                    strength = 1 - strength;                                // Change from [0, 1] to [1, 0]
                }
                else
                {
                    strength = 2 * activeTimePercentageDecimal;             // Change from [0, 0.5] to [0, 1]
                }
                //strength = MathUtil.SmoothStep(strength);
                _currentWindStrength = strength;
                //System.Diagnostics.Debug.WriteLine($"WndStr: {_currentWindStrength}");
            }
        }
    }
}
