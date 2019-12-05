# Screen Space Decal Root Renderer Example [SSDR]

This projects adds a screen space projected decal system which uses a custom root renderer to read the `DecalComponent`, and render it out ourselves.

Note that because this uses a separate rendering system, none of the existing effect parameters get automatically set.
This is because the existing rendering system has additional logic to determine/feed the effect parameters for the materials.

For the system to work, remember to include it in the 'Render features' in the Graphics Compositor.

In the DecalRootRenderFeature property grid, also add the `DecalRenderStatgeSelector` in the Render Stage Selectors section, and select `Transparent` in the Render Stage property.

Note that `Screen Space Decal Example` project also contains a feature that allows decals to ignore specific Render Groups, which has not been implemented in this project, however it is possible to implement those changes into this project.

**Important Game Studio Notes:**
* The entity with the `DecalComponent` is not selectable in the Game Studio scene editor, and must be selected by the entity tree sub-window.

---
In the future, it may be possible to make the entity selectable in the scene editor.
This will require creating a `GizmoComponent`, which is part of the `Xenko.Assets.Presentation` library, however referencing this library stops the game from compiling.

---
