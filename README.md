# XenkoProofOfConcepts

This repository houses a few basic projects to test capabilities of the Xenko Game Engine.

The projects currently require built on Xenko version **3.1.0.1**, and have only been created with the Windows projects (apologies to any non-Windows developers!).

These projects are aimed more towards intermediate to advanced programmers, and may require further investigation into the Xenko source code, or search online, for more information on how some of the applied code works, as some of the some techniques may not be officially what the Xenko developers intended for us to use.

Currently there are four projects:
* `EntityProcessorExample`: Processor-centric code rather than "Script"/Component-centric code.
* `ObjectInfoRenderTargetExample`: Render entity information onto a `Texture` and then read this in a subsequent shader.
* `ScreenSpaceDecalExample`: Use a cube projector and renders a given `Texture` onto any surfaces within the projector.
* `ScreenSpaceDecalRootRendererExample`: Similar to `ScreenSpaceDecalExample`, but uses a `RootRenderFeature` instead of making a processor fake a `RenderMesh`.

---
The only project worth showing a screenshot of for this front-end readme file is the `ScreenSpaceDecalExample`:

![Render Stage](ScreenSpaceDecalExample/images/scene.png)

Note that the `ScreenSpaceDecalExample` project can be seen as an accumulation of applying the techniques from both the `EntityProcessorExample` and `ObjectInfoRenderTargetExample` projects, so it may be worthwhile understanding those projects before trying to look at the `ScreenSpaceDecalExample` project.
