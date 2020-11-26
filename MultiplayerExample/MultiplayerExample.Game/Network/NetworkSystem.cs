using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MultiplayerExample.Network
{
    partial class NetworkSystem : GameSystemBase, IGameNetworkService
    {
        private const int ConnectionTimeoutInMilliseconds = 30 * 1000;

        public const string ServerConnectionAppName = "MultiplayerExample";
        public const ushort ServerPortNumber = 60000;   // TODO: should probably be a config setting

        private ServerNetworkHandler _serverNetworkHandler;
        private ClientNetworkHandler _clientNetworkHandler;

        private SceneSystem _sceneSystem;
        private SceneInstance _currentSceneInstance;
        private NetworkEntityProcessor _networkEntityProcessor;
        private GameClockManager _gameClockManager;

        public NetworkGameMode NetworkGameMode { get; private set; }

        public bool IsGameHost => NetworkGameMode == NetworkGameMode.ListenServer || NetworkGameMode == NetworkGameMode.DedicatedServer;

        public NetworkSystem([NotNull] IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Services.AddService<IGameNetworkService>(this);
            _serverNetworkHandler = new ServerNetworkHandler(this);
            _clientNetworkHandler = new ClientNetworkHandler(this);
        }

        public override void Initialize()
        {
            _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            _gameClockManager = Services.GetSafeServiceAs<GameClockManager>();
        }

        /// <summary>
        /// Start this game with only a local client.
        /// </summary>
        void IGameNetworkService.StartLocalGame()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.Local;
            // TODO: Need to implement this for local only game.
            throw new Exception("Not implemented");
        }

        /// <summary>
        /// Join a remote game.
        /// </summary>
        Task<ConnectResult> IGameNetworkService.BeginConnectToServer(string serverIp, ushort serverPortNumber)
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.RemoteClient;
            return _clientNetworkHandler.Connect(serverIp, serverPortNumber);
        }

        /// <summary>
        /// Start this game with a local client and accepts remote clients.
        /// </summary>
        IGameNetworkServerHandler IGameNetworkService.StartHost()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.ListenServer;
            _serverNetworkHandler.Start();
            return this;
        }

        /// <summary>
        /// Start this game as a dedicated server.
        /// </summary>
        IGameNetworkServerHandler IGameNetworkService.StartDedicatedServer()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.NotSet);
            NetworkGameMode = NetworkGameMode.DedicatedServer;
            _serverNetworkHandler.Start();
            return this;
        }

        IGameNetworkClientHandler IGameNetworkService.GetClientHandler()
        {
            Debug.Assert(NetworkGameMode == NetworkGameMode.RemoteClient, "Network service is not set as a client.");
            return this;
        }

        IGameNetworkServerHandler IGameNetworkService.GetServerHandler()
        {
            Debug.Assert(IsGameHost, "Network service is not set as a server.");
            return this;
        }

        public override void Update(GameTime gameTime)
        {
            if (_currentSceneInstance != _sceneSystem?.SceneInstance)
            {
                UpdateCurrentSceneInstance(_sceneSystem.SceneInstance);
            }

            if (NetworkGameMode == NetworkGameMode.NotSet)
            {
                // Do nothing
                return;
            }

            switch (NetworkGameMode)
            {
                case NetworkGameMode.Local:
                    //??_serverNetworkHandler.Update();
                    break;
                case NetworkGameMode.RemoteClient:
                    _clientNetworkHandler.Update(gameTime);
                    break;
                case NetworkGameMode.ListenServer:
                    _serverNetworkHandler.Update();
                    _clientNetworkHandler.Update(gameTime);
                    break;
                case NetworkGameMode.DedicatedServer:
                    _serverNetworkHandler.Update();
                    break;

                default:
                    Debug.Fail($"Unhandled NetworkGameMode: {NetworkGameMode}");
                    break;
            }
        }

        private void UpdateCurrentSceneInstance(SceneInstance newSceneInstance)
        {
            if (newSceneInstance == null)
            {
                _networkEntityProcessor = null;
            }

            // Set the current scene
            _currentSceneInstance = newSceneInstance;

            if (_currentSceneInstance != null)
            {
                _networkEntityProcessor ??= _currentSceneInstance.Processors.FirstOrDefault(x => x is NetworkEntityProcessor) as NetworkEntityProcessor;
                if (_networkEntityProcessor == null)
                {
                    // Create this NetworkEntityProcessor if it doesn't exist
                    _networkEntityProcessor = new NetworkEntityProcessor();
                    _currentSceneInstance.Processors.Add(_networkEntityProcessor);
                }
            }
        }

        [Conditional("DEBUG")]
        private void DebugWriteLine(string message)
        {
            message = $"[{nameof(NetworkSystem)}] " + message;
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
