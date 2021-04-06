# Multiplayer Example [MPE]

This project includes code and assets for a proof of concept for a client/server multiplayer game.

It is important to note that this project contains a lot of hacky code, and generally bypasses the standard way of how Stride wants to work.

Also, it should be noted that the gameplay & physics code is run at fixed time intervals, currently set at 30 updates per second (see `GameConfig.PhysicsFixedTimeStep`).

For development, it is best to set the VS solution to run multiple projects (ie. right click the solution -> Set Startup Projects... -> Multiple startup projects -> Start MultiplayerExample.Server and MultiplayerExample.Windows).

### Limitations of this proof of concept include:
- `GameTimeExt` derives from Stride's `GameTime` and uses reflection in order to access the internal `Update` & `Reset` methods. Since those methods are not exposed publically, they may potentially change on version updates so keep this in mind as the app may crash if the methods change.
- The standard `ScriptComponent` classes (ie. `StartupScript`/`SyncScript`/`AsyncScript`) **cannot** be used in any server side entities, and will crash the server side application if you try to load the entity into the scene. The reason they cannot be used is because `ScriptComponent`s enforce retrieving systems that have been cut out from the server engine, eg. audio, input, graphics related systems.
- Each visible networked entity comes in a pair, the actual networked entity and the network 'view' entity (whose position is always set to the appropriate render interpolation position). This doubling of entities may be inefficient memory/processing-wise if too many pairs are created.
- While a `IGraphicsDeviceService` is required to be registered even for the server engine, a 'headless' version is implemented to essentially do nothing. This hasn't been fully tested, so there's no guarantee the headless server application is truly 'headless'.
- Some usage of IL generation is done (eg. `BulletPhysicsExt` methods), potentially making it not usable on other platforms (eg. iOS?), however these are mainly used to provide access to internal/private properties & fields under the assumption you can't modify the Stride source code (or you're too lazy to modify the source code and just want to use the official libraries, like this project). If you can modify the Stride source code (it's open source, so it is possible!), it is probably advised to do it that way.


## Implementation Details
- The client screen flow architecture follows code similar to that in the *Game Screen Manager Example*.
- Entity components/processors are used instead of the `SyncScript`/`AsyncScript`, as seen in the *Entity Component/Processor System Example*.
- The networking library used is `LiteNetLib`. The source code/project is directly referenced because the debugging options (ie. latency & packet loss simulation) are not available in the Nuget version. Some effort (rather minimal) has been made to reduce the number of direct references to the `LiteNetLib` library, eg. the lightweight wrapper structs `NetworkConnection`, `NetworkMessageReader` & `NetworkMessageWriter`, though depending on your choice of networking library, these wrappers may not be applicable.
- In the resimulation step, reflection is used to call the underlying Bullet physics engine api, since there's no direct access in Stride, in order to apply simulation to a single entity rather than the entire physics world. This is not a perfect resimulation since no rewinding is applied to the nearby entities.
- By default, dragging the window freezes the game, however a WinForms hack is implemented (adapted from MonoGame source code, see `GameAppClient.OnWindowCreated`) to allow the game to continue running. No cross-platform implementation is done in this project, so it is unknown whether those platform will require similar hacks, as well.


**Some important classes to note:**
- `GameEngineClient` & `GameEngineServer`: While Stride handles all the 'systems' in the `Game` class, our application does not use those systems and manually recreates the relevant systems depending on whether the engine is a client or server engine.

- `GameClockManager`: Stride's `GameTime` is only used for Stride related systems that we have minimal control over (eg. audio, physics, model/UI animations) though our engine classes have been made to control the actual time values of the `GameTime`. For our gameplay code, we should use `GameClockManager.SimulationClock`, as this keeps track of the time and `SimulationTickNumber` of the gameplay scenes. `GameClockManager.NetworkServerSimulationClock` also exist for the purpose of nudging the `GameClockManager.SimulationClock` and `GameTime` so it doesn't get too far ahead or behind the server's clock.

- `NetworkAssetDefinitions` holds three different player entity prefabs which are loaded depending on the context of the running game. `ServerPlayer` holds the player prefab to be loaded on the server, `LocalPlayer` is the player loaded for the locally owned client, and `RemotePlayer` is the prefab loaded when other players connect to the server which are visible to the local player.

- `NetworkAssetDatabase` is an optimization class that maps game asset file paths to GUIDs. This is only network optimization implemented in this project, so that when the server tells the client to load an entity, it only needs to send a GUID instead of an entire file path string.

---

### This proof of concept is still a work in progress, with the following issues:
- Client side prediction somewhat implemented. There are still bugs, eg. jumping near stairs/cliffs has animation issues.
- Minor jittering seems to still occur on local and remote players, despite rendering interpolation, so further investigation is required.
- Over-engineered code, and lots of debug code lying around. Requires some general cleanup.


### Issues beyond the scope this project:
- `DynamicNavigationMeshSystem` not added to the server engine. This is because this is untested in this projected.
- The standalone server does not have any game instancing management code, it just loads the game scene immediately, listens for any clients, and drops them all in the same scene. A more appropriate way would be to run the server application on demand and maybe not run the game scene until there is at least one client. Also, the server is hardcoded to cap at four players (see `NetworkSystem.ServerNetworkHandler.OnConnectionRequest` for `const int MaxPlayers`).
- Optimization (network packet, memory allocations, etc).
- Server rollback not implemented. Past position data is stored in `MovementSnapshotsComponent` so it should be simple to add.
- Gracefully handle client/server timeouts and rejoining existing game.
- Minor warning that appears on the client game about the camera slot not set.
- Lower latency players having input advantage over high latency players.
