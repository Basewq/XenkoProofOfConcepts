using MultiplayerExample.GameServices;
using MultiplayerExample.Network;
using MultiplayerExample.UI;
using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MultiplayerExample.GameScreens.PageHandlers
{
    public class InGameScreenPageHandler : PageHandlerBase
    {
        internal static readonly UIElementKey<TextBlock> PlayerNameText = new UIElementKey<TextBlock>("PlayerNameText");
        internal static readonly UIElementKey<TextBlock> ClockTimeText = new UIElementKey<TextBlock>("ClockTimeText");

        private TextBlock[] _playerNamesUI;
        private TextBlock _clockTimeUI;

        private InputManager _inputManager;
        private bool _ignoreInputEvents;

        private IGameNetworkService _networkService;
        private GameManager _gameManager;
        private GameClockManager _gameClockManager;

        private List<(SerializableGuid PlayerId, string PlayerName)> _uiDisplayForPlayer = new List<(SerializableGuid, string)>();

        protected override void OnInitialize()
        {
            _inputManager = GameManager.Services.GetService<InputManager>();

            _networkService = GameManager.Services.GetSafeServiceAs<IGameNetworkService>();
            _gameClockManager = GameManager.Services.GetService<GameClockManager>();
        }

        public async override void OnActivate()
        {
            _ignoreInputEvents = false;

            _playerNamesUI = UIComponent.GetAllUI(PlayerNameText).ToArray();
            _clockTimeUI = UIComponent.GetUI(ClockTimeText);

            var sceneSystem = GameManager.Services.GetSafeServiceAs<SceneSystem>();

            _gameManager = sceneSystem.GetGameManagerFromRootScene();
            _gameManager.PlayerAdded += OnPlayerAdded;
            _gameManager.PlayerRemoved += OnPlayerRemoved;

            var networkClientHandler = _networkService.GetClientHandler();
            var readyTask = networkClientHandler.SendClientInGameReady();
            var readyResult = await readyTask;
            if (!readyResult.IsOk)
            {
                //ShowErrorMessage(readyResult.ErrorMessage);
                // TODO: return to previous screen?
                return;
            }
        }

        public override void OnDeactivate()
        {
            _gameManager.PlayerAdded -= OnPlayerAdded;
            _gameManager.PlayerRemoved -= OnPlayerRemoved;
        }

        private void OnPlayerAdded(Entity e)
        {
            var networkEntityComp = e.Get<NetworkEntityComponent>();
            var networkPlayerComp = e.Get<NetworkPlayerComponent>();
            Debug.Assert(networkEntityComp != null);
            Debug.Assert(networkPlayerComp != null);

            _playerNamesUI[_uiDisplayForPlayer.Count].Text = $"Player: {networkPlayerComp.PlayerName}";
            _uiDisplayForPlayer.Add((networkEntityComp.NetworkEntityId, networkPlayerComp.PlayerName));     // NetworkEntityId is the same as the PlayerId for the main entity
        }

        private void OnPlayerRemoved(Entity e)
        {
            var networkEntityComp = e.Get<NetworkEntityComponent>();
            var networkPlayerComp = e.Get<NetworkPlayerComponent>();
            Debug.Assert(networkEntityComp != null);
            Debug.Assert(networkPlayerComp != null);

            int index = _uiDisplayForPlayer.FindIndex(x => x.PlayerId == networkEntityComp.NetworkEntityId);
            Debug.Assert(index >= 0);
            _uiDisplayForPlayer.RemoveAt(index);
            for (int i = index; i < _uiDisplayForPlayer.Count; i++)
            {
                // Shift down existing player names
                _playerNamesUI[i].Text = $"Player: {_uiDisplayForPlayer[i].PlayerName}";
            }
            for (int i = _uiDisplayForPlayer.Count; i < _playerNamesUI.Length; i++)
            {
                // Unassign remaining names
                _playerNamesUI[i].Text = $"Player: -";
            }
        }

        protected override void OnIsTopMostScreenChanged(bool newIsTopMostScreen)
        {
            if (newIsTopMostScreen)
            {
                _ignoreInputEvents = false;
            }
        }

        public override void Update()
        {
            var networkClientHandler = _networkService.GetClientHandler();
            _clockTimeUI.Text = @$"LocTime: {_gameClockManager.SimulationClock.TotalTime:hh\:mm\:ss\.ff} - TickNo: {_gameClockManager.SimulationClock.SimulationTickNumber}
NetTime: {_gameClockManager.NetworkServerSimulationClock.TargetTotalTime:hh\:mm\:ss\.ff} - TickNo: {_gameClockManager.NetworkServerSimulationClock.LastServerSimulationTickNumber}
Latency: {networkClientHandler.AverageNetworkLatency.TotalMilliseconds} ms
";
            //if (_ignoreInputEvents || !IsTopMostScreen)
            //{
            //    return;
            //}
            //if (_inputManager.HasKeyboard)
            //{
            //    if (_inputManager.IsKeyPressed(Keys.Escape))
            //    {
            //        SceneManager.LoadNextMainScene(InGameOptionsSubScreenSceneUrl, onLoadCompleted: scene =>
            //        {
            //            SceneManager.PushSubScreen(scene);
            //        });
            //    }
            //}
        }
    }
}
