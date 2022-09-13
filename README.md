# Stride Proof of Concepts

This repository houses a few basic projects to test capabilities of the Stride Game Engine (formerly called Xenko).

The projects are currently built with Stride version **4.1.0.1734**, and have only been created with the Windows projects (apologies to any non-Windows developers!).
Note that some projects were originally created with Xenko, then upgraded to Stride through its launcher, however some things do not get upgraded (eg. Xenko assets & icons), so some things may differ if you have created a new project from the Stride templates.

---

These projects are aimed more towards intermediate to advanced programmers, and may require further investigation into the Stride source code, or search online, for more information on how some of the applied code works, as some of the some techniques may not be officially what the Stride developers intended for us to use.

This repository contains the following projects:
* [BepuPhysicsExample](BepuPhysicsExample): A rough attempt at integrating Bepu2 physics engine into Stride (this is not completed and has been abandoned).
* [CutsceneTimelineExample](CutsceneTimelineExample): Playing and previewing a simple cutscene in Stride and Game Studio.
* [EntityProcessorExample](EntityProcessorExample): Processor-centric code rather than "Script"/Component-centric code.
* [GameScreenManagerExample](GameScreenManagerExample): Game screen navigation via a `GameScreenManager` entity.
* [LevelEditorExtensionExample](LevelEditorExtensionExample): Custom component & processor running in the editor to extend the Game Studio's functionality.
* [MultiplayerExample](MultiplayerExample): Multiplayer project with a client application and a server application, both running with Stride code.
* [ObjectInfoRenderTargetExample](ObjectInfoRenderTargetExample): Render entity information onto a `Texture` and then read this in a subsequent shader.
* [ScreenSpaceDecalExample](ScreenSpaceDecalExample): Use a cube projector and renders a given `Texture` onto any surfaces within the projector.
* [ScreenSpaceDecalRootRendererExample](ScreenSpaceDecalRootRendererExample): Similar to `ScreenSpaceDecalExample`, but uses a `RootRenderFeature` instead of making a processor fake a `RenderMesh`.
* [UINavigationExample](UINavigationExample): Example on extending existing UI controls to add the ability to traverse between UI via keyboard/gamepad.

---
The only project worth showing a screenshot of for this front-end readme file is the `ScreenSpaceDecalExample`:

![Render Stage](ScreenSpaceDecalExample/images/scene.png)

Note that the `ScreenSpaceDecalExample` project can be seen as an accumulation of applying the techniques from both the `EntityProcessorExample` and `ObjectInfoRenderTargetExample` projects, so it may be worthwhile understanding those projects before trying to look at the `ScreenSpaceDecalExample` project.
