// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Input;

namespace UINavigationExample
{
    public class SplashScript : UISceneBase
    {
        public UrlReference<Scene> NextSceneUrl { get; set; }

        protected override void LoadScene()
        {
            // Allow user to resize the window with the mouse.
            Game.Window.AllowUserResizing = true;
        }

        protected override void UpdateScene()
        {
            bool hasInput = Input.PointerEvents.Any(e => e.EventType == PointerEventType.Pressed);
            hasInput = hasInput || Input.IsKeyPressed(Keys.Space) || Input.IsKeyPressed(Keys.Enter);
            if (Input.HasGamePad && Input.DefaultGamePad != null)
            {
                var gamePad = Input.DefaultGamePad;
                hasInput = hasInput
                    || gamePad.IsButtonPressed(GamePadButton.A)
                    || gamePad.IsButtonPressed(GamePadButton.B)
                    || gamePad.IsButtonPressed(GamePadButton.X)
                    || gamePad.IsButtonPressed(GamePadButton.Y);
            }
            if (hasInput)
            {
                // Next scene
                SceneSystem.SceneInstance.RootScene = Content.Load(NextSceneUrl);
                Cancel();
            }
        }
    }
}
