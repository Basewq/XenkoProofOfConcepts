using MultiplayerExample.GameScreens;
using MultiplayerExample.GameServices.SceneHandlers;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using System.Diagnostics;

namespace MultiplayerExample.GameServices
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(SceneControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    public class SceneController : EntityComponent
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

        //public void Start()
        //{
        //}

        public void Deinitialize()
        {
            SceneHandler.Deinitialize();
        }

        public void Update()
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
