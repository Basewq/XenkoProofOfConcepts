using MultiplayerExample.GameScreens.PageHandlers;
using MultiplayerExample.GameServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;

namespace MultiplayerExample.GameScreens
{
    public class PageController : SyncScript
    {
        public const string EntityName = "PageController";

        private UIComponent _uiComponent;

        public PageHandlerBase PageHandler;

        [DataMemberIgnore]
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Called once after this screen has been created, but before it has been added to the scene tree.
        /// If the screen has been deactivated but not deinitialized/destroyed, this will not be called again.
        /// </summary>
        public void Initialize(UIManager uiManager, GameManager gameManager, SceneManager sceneManager)
        {
            PageHandler.Initialize(Entity, uiManager, gameManager, sceneManager);
            IsInitialized = true;
        }

        public override void Start()
        {
            Game.Window.ClientSizeChanged += AdjustVirtualResolution;
        }

        // Sealed since we don't want GameScreens to use this method
        public override void Cancel()
        {
            Game.Window.ClientSizeChanged -= AdjustVirtualResolution;
            PageHandler.Deinitialize();
        }

        private void AdjustVirtualResolution(object sender, EventArgs e)
        {
            var backBufferSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
            var uiComp = GetUIComponent();
            if (uiComp != null)
            {
                uiComp.Resolution = new Vector3(backBufferSize, 1000);
            }
            PageHandler.OnScreenSizeChanged(backBufferSize);
        }

        private UIComponent GetUIComponent()
        {
            _uiComponent ??= Entity.Get<UIComponent>();
            return _uiComponent;
        }

        public override void Update()
        {
            PageHandler.Update();
        }

        /// <summary>
        /// Called immediately after this screen is added to the scene tree.
        /// </summary>
        public void OnActivate()
        {
            PageHandler.OnActivate();
        }

        /// <summary>
        /// Called just before this screen removed from the scene tree.
        /// </summary>
        public void OnDeactivate()
        {
            PageHandler.OnDeactivate();
        }
    }
}
