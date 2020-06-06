# Game Screen Manager Example [GSM]

This project includes code and assets for game screen management.

There are two types of screens, `IGameScreen` and `ISubScreen`.
In the root of each scene, there must be an Entity that contains either an `IGameScreen` or `ISubScreen`, and it is the presence of `IGameScreen`/`ISubScreen` that we use to determine if the scene is treated as a game screen or a sub-screen.

`IGameScreen`s are used for fully independent "screens", eg. Splash Screen, Title Screen, In-Game Screen. Only one `IGameScreen` can be active at any time.

`ISubScreen`s are used for additively adding scenes to the root scene, which are tracked on a stack in the `GameScreenManager`, usually for UI navigation purposes.

**Warning:** When adding an entity with `IGameScreen` or `ISubScreen`, it is advised that only ONE of these exists per scene, otherwise you may encounter odd bugs when navigation screens or sub-screens.

After deciding whether you want a full game screen or a sub-screen, create a new C# file that derived from either `GameScreenBase` or `SubScreenBase`. These abstract classes hold common methods/entity accessors that a screen may use, and provide screen size change event to handle UI size changes (if you allow for screen resizing).

For convinence, there are two prefabs in `Assets/GameScreenPrefabs` which can be used added to new scenes, depending on the type of screen the scene is.
When one of the two prefabs are added to a scene, select the `GameScreenController` or `SubScreenController` entity and change the Game Screen/Sub-Screen property in the `GameScreenController` or `SubScreenController` component.

**Warning:** Do not change or override the entity names of these prefabs, as the underlying screen management code relies on these names.

There is one important scene that should not be modified, the `RootScene`.
This houses the globally persistent entities `GameManager` (currently unused), `GameScreenManager`, `RootMainCamera` and `RootUICamera`.

From a coding perspective, adding/removing scenes must be done through `GameScreenManager` rather than modifying the scene collection anywhere else. It is especially important to not reassign `SceneSystem.SceneInstance.RootScene` since this will effectively remove the main managers.

### Screen Navigation

Basic diagram of the screen navigation in the project:
```
                              ------------------------------------------------------------
                              v                                                          |
[Splash Screen] --> [Title Screen] --> [In-Game Screen] -- [In-Game Sub-Screen] <--> [In-Game Options Sub-Screen] --> (Quit)
                      ^                   ^
                      |                   |
                      |                   |
                      --> [Load Game Sub-Screen] 
                      |
                      --> [Options Sub-Screen] 
                      |
                      --> (Quit)

```

Some example screenshots of some of the screens:

Splash Screen

![Splash Screen](images/screen_nav1.png)

Title Screen

![Title Screen](images/screen_nav2.png)

In-Game Screen

![In-Game Screen](images/screen_nav3.png)

In-Game Options Sub-Screen

![In-Game Options Sub-Screen](images/screen_nav4.png)

**Additional Setup:**
By default, Stride renders everything in the same pipeline, which includes post-processing effects. Generally, you do not want UI to be using the same post-processing effects (if any), so the `GraphicsCompositor` has been changed so that `RenderGroup31` is rendered separately and the UI is only rendered in this group.
The UI is provided its own Camera Slot (named UI), and this Camera is added to the root scene.

![GraphicsCompositor setup](images/gfxcomp_setup1.png)

Main camera rendering all except `Group31` (Left), UI camera only rendering UI entities in `Group31` (Right)

![GraphicsCompositor setup](images/gfxcomp_setup2.png)

**Other Notes:**
The following is beyond the scope of this example:
Game pause when in options sub-screen.
Properly ignoring input in when a sub-screen is not at the top of the stack (or perhaps allowing some input is still ok).
Properly stop game sounds/music when changing screens.
Transferring game data between screens (this can probably be done with the `GameManager` entity).
Properly change the main camera when changing screens.
