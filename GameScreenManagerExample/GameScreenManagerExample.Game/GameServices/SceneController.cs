using GameScreenManagerExample.GameScreens;
using GameScreenManagerExample.GameServices.SceneHandlers;
using Stride.Core;
using Stride.Engine;
using System.Diagnostics;

namespace GameScreenManagerExample.GameServices
{
    public class SceneController : SyncScript
    {
        public const string EntityName = "SceneController";

        public SceneHandlerBase SceneHandler;

        [DataMemberIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Called once after this screen has been created, but before it has been added to the scene tree.
        /// If the screen has been deactivated but not deinitialized/destroyed, this will not be called again.
        /// </summary>
        public void Initialize(SceneManager sceneManager, GameManager gameManager, UIManager uiManager)
        {
            Debug.Assert(SceneHandler != null, $"{nameof(SceneHandler)} has not been set.");
            SceneHandler.Initialize(Entity, sceneManager, gameManager, uiManager);
            IsInitialized = true;
        }

        //public override void Start()
        //{
        //}

        public override void Cancel()
        {
            SceneHandler.Deinitialize();
        }

        public override void Update()
        {
            SceneHandler.Update();
        }

        /// <summary>
        /// Called immediately after the <see cref="Scene"/> is added to the scene tree.
        /// </summary>
        public void OnActivate()
        {
            SceneHandler.OnActivate();
        }

        /// <summary>
        /// Called just before the <see cref="Scene"/> removed from the scene tree.
        /// </summary>
        public void OnDeactivate()
        {
            SceneHandler.OnDeactivate();
        }
    }
}
