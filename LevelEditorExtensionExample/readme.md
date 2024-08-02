# Level Editor Extension Example [LEE]

This project includes code and assets for a proof of concept for extending editing capabilities in Stride Game Studio using a user defined Entity Component & Processor.

**Important Notes**
- This requires advance knowledge in how Stride Game Studio works, on a programming level, as Game Studio does not currently officially support extending the editor.
- Visual Studio is required in order to run the standalone exe (debug/release builds), due to different compilation symbols required when running in Game Studio or the standalone exe.
- As of version `4.2.0.2149`, you can only build/run the standalone exe in DebugExGameEditor or Release builds, because there is a bug with the CompilerApp which cannot load WPF libraries.

---

This project shows five buttons with the following action:
- Add a prefab to the scene
- Increase an entity's scale
- Reset an entity's scale
- Increase an entity's 'internal' data (an array of ints)
- Reset an entity's 'internal' data

![Editor buttons](images/editor_buttons.gif)

---

## Implementation Details
- You will need to reference `Stride.Assets.Presentation` and `Stride.GameStudio` to gain access to the Game Studio editor classes. The compilation symbol `GAME_EDITOR` is also required and must be **disabled** when running the standalone exe due to loading issues with including these libraries.
- A `LevelEditComponent` is required to be added to an entity, and its corresponding processor, `LevelEditProcessor`, is used to handle the logic for the editing capabilities. A `UIComponent` is also required to be attached to the entity with the `LevelEditComponent` since `LevelEditProcessor` will use the UI from `UIComponent` (ie. the buttons that the user will click on).
- `LevelEditProcessor` must unregister the button click event handler in `LevelEditProcessor.OnEntityComponentRemoved`. This is because when Game Studio reloads for any code changes, the entities in the scene are removed and readded, but the UI persists. If not removed will you get multiple registered click event handlers.
- Modifying entity data is complicated. This is because Game Studio holds a 'master version' of the game scene in a different data structure, which are used for loading & saving the data (as an asset file). The entities that `LevelEditProcessor` see are a *copy* of the scene (in fact it is the run-time version which you see on screen).
- Any data manipulation in the scene by the entity processor will **not** be reflected back to the master version, so you need to find the 'manager' of the data, which is done by getting the instance of `GameStudioWindow`, finding `SceneEditorViewModel` and using the relevant objects from that. In the case of editing entity data, you must match `Entity.Id` of the run-time version to the master version via `SceneEditorViewModel.HierarchyRoot`, then find the `IAssetMemberNode` from the `SceneEditorViewModel.Session.AssetNodeContainer` which you use to update the value in both the asset file and the run-time copy.

---

### Additional notes
- Do not store stateful data in `LevelEditProcessor`. This is because when Game Studio reloads for any code changes, the processor may be destroyed and recreated, and any stateful data will not carry over. It is best to store such data within the master version of an entity component.
- Game Studio sometimes may not reload UI changes fully on code reload (eg. if you try reloading when your code isn't compilable). If the UI click event handler doesn't appear working, consider closing and re-opening the scene asset, as this will recreate the scene (and the entity processors).
- Make sure the compilation symbol `GAME_EDITOR` is on the **Debug build**, as this is the build that Game Studio uses when using the code in the editor scene. The DebugExGameEditor build should be used when trying to build and run the game as an exe (or Release), which excludes the `GAME_EDITOR` symbol, because the exe will not run when `Stride.Assets.Presentation` and `Stride.GameStudio` libraries are referenced in the build. This also means you cannot use the Run button within Game Studio to run the standalone exe, because Game Studio doesn't support different build configurations - you must use Visual Studio (or some alternative C# editor).
- Due to the scene in the editor having dynamic resolution, the UI may resize itself as you resize the Game Studio window.
