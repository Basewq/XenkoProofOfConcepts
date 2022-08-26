using CutsceneTimelineExample.Timeline;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CutsceneTimelineExample
{
    public class PlayCutsceneScript : SyncScript
    {
        private Prefab _loadedTimelinePrefab;
        private List<Entity> _loadedTimelinePrefabEntities = new List<Entity>();
        private TimelineController _prefabTimelineController;
        private List<Entity> _loadedRetargetTimelinePrefabEntities = new List<Entity>();
        private TimelineController _prefabRetargetTimelineController;

        public TimelineController TimelineController { get; set; }

        public UrlReference<Prefab> TimelinePrefab { get; set; }
        public TransformComponent RetargetWaypoint1 { get; set; }
        public TransformComponent RetargetWaypoint2 { get; set; }

        public override void Start()
        {
            SetupUI();

            TimelineController?.Timeline.Stop();
        }

        public override void Update()
        {
            // Do nothing
        }

        private void SetupUI()
        {
            var uiComp = Entity.Get<UIComponent>();
            if (uiComp == null)
            {
                return;
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PlayCutscene1") as Button;
                if (btn != null)
                {
                    // Play the timeline of the TimelineController in the scene
                    btn.Click += (sender, ev) =>
                    {
                        TimelineController?.Timeline.Play();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PlayCutscene2") as Button;
                if (btn != null)
                {
                    btn.Click += async (sender, ev) =>
                    {
                        if (_prefabRetargetTimelineController != null)
                        {
                            _prefabTimelineController.Timeline.Play();
                            return;
                        }
                        // Play the timeline of the TimelineController from the prefab
                        lock (_loadedTimelinePrefabEntities)
                        {
                            foreach (var ent in _loadedTimelinePrefabEntities)
                            {
                                ent.Scene = null;
                            }
                            _loadedTimelinePrefabEntities.Clear();
                        }
                        if (_loadedTimelinePrefab == null)
                        {
                            _loadedTimelinePrefab = await Content.LoadAsync(TimelinePrefab);
                        }
                        var timelineEntities = _loadedTimelinePrefab.Instantiate();
                        lock (_loadedTimelinePrefabEntities)
                        {
                            _loadedTimelinePrefabEntities.AddRange(timelineEntities);

                            Entity.Scene.Entities.AddRange(timelineEntities);
                        }
                        var prefabTimelineController = timelineEntities.FirstOrDefault(x => x.Get<TimelineController>() != null)?.Get<TimelineController>();
                        if (prefabTimelineController != null)
                        {
                            prefabTimelineController.Timeline.Stop();
                            prefabTimelineController.Timeline.Play();
                        }
                        _prefabTimelineController = prefabTimelineController;
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PlayCutscene3") as Button;
                if (btn != null)
                {
                    btn.Click += async (sender, ev) =>
                    {
                        if (_prefabRetargetTimelineController != null)
                        {
                            _prefabRetargetTimelineController.Timeline.Play();
                            return;
                        }
                        // Play the timeline of the TimelineController from the prefab, but change the waypoints/target
                        lock (_loadedRetargetTimelinePrefabEntities)
                        {
                            foreach (var ent in _loadedRetargetTimelinePrefabEntities)
                            {
                                ent.Scene = null;
                            }
                            _loadedRetargetTimelinePrefabEntities.Clear();
                        }
                        if (_loadedTimelinePrefab == null)
                        {
                            _loadedTimelinePrefab = await Content.LoadAsync(TimelinePrefab);
                        }
                        var timelineEntities = _loadedTimelinePrefab.Instantiate();
                        lock (_loadedRetargetTimelinePrefabEntities)
                        {
                            _loadedRetargetTimelinePrefabEntities.AddRange(timelineEntities);

                            Entity.Scene.Entities.AddRange(timelineEntities);
                        }
                        var prefabTimelineController = timelineEntities.FirstOrDefault(x => x.Get<TimelineController>() != null)?.Get<TimelineController>();
                        if (prefabTimelineController != null)
                        {
                            prefabTimelineController.Timeline.RetargetTrackBinding("Waypoint1", RetargetWaypoint1);
                            prefabTimelineController.Timeline.RetargetTrackBinding("Waypoint2", RetargetWaypoint2);

                            prefabTimelineController.Timeline.Stop();
                            prefabTimelineController.Timeline.Play();
                        }
                        _prefabRetargetTimelineController = prefabTimelineController;
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PauseCutscene1") as Button;
                if (btn != null)
                {
                    // Pause the timeline of the TimelineController in the scene
                    btn.Click += (sender, ev) =>
                    {
                        TimelineController?.Timeline.Pause();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PauseCutscene2") as Button;
                if (btn != null)
                {
                    // Pause the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        _prefabTimelineController?.Timeline.Pause();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("PauseCutscene3") as Button;
                if (btn != null)
                {
                    // Pause the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        _prefabRetargetTimelineController?.Timeline.Pause();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("StopCutscene1") as Button;
                if (btn != null)
                {
                    // Stop the timeline of the TimelineController in the scene
                    btn.Click += (sender, ev) =>
                    {
                        TimelineController?.Timeline.Stop();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("StopCutscene2") as Button;
                if (btn != null)
                {
                    // Stop the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        _prefabTimelineController?.Timeline.Stop();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("StopCutscene3") as Button;
                if (btn != null)
                {
                    // Stop the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        _prefabRetargetTimelineController?.Timeline.Stop();
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("ClearCutscene2") as Button;
                if (btn != null)
                {
                    // Clear the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        lock (_loadedTimelinePrefabEntities)
                        {
                            foreach (var ent in _loadedTimelinePrefabEntities)
                            {
                                ent.Scene = null;
                            }
                            _loadedTimelinePrefabEntities.Clear();
                        }
                        _prefabTimelineController = null;
                    };
                }
            }
            {
                var btn = uiComp.Page.RootElement.FindName("ClearCutscene3") as Button;
                if (btn != null)
                {
                    // Clear the timeline of the TimelineController from the prefab
                    btn.Click += (sender, ev) =>
                    {
                        lock (_loadedRetargetTimelinePrefabEntities)
                        {
                            foreach (var ent in _loadedRetargetTimelinePrefabEntities)
                            {
                                ent.Scene = null;
                            }
                            _loadedRetargetTimelinePrefabEntities.Clear();
                        }
                        _prefabRetargetTimelineController = null;
                    };
                }
            }
        }
    }
}
