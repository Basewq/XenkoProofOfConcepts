# Screen Space Decal Example [SSD]

This projects adds a screen space projected decal system which uses a custom processor to read the `DecalComponent` to feed a `RenderMesh` with our custom shader material to be consumed by the standard rendering system.
This is a hacky solution as it relies on copying the existing rendering system's data format.

Currently, we generate a new material object per `DecalComponent` (ie. per *entity*).
This probably needs to be changed to allow reuse of materials, however be aware if the `DecalComponent`'s fields change at run-time this will affect *all* entities using the same material.

**Important Game Studio Notes:** 
* The entity with the `DecalComponent` is not selectable in the Game Studio scene editor, and must be selected by the entity tree sub-window.

---
In the future, it may be possible to make the entity selectable in the scene editor.
This will require creating a `GizmoComponent`, which is part of the `Xenko.Assets.Presentation` library, however referencing this library stops the game from compiling.

---
