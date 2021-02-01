# Bepu2 Physics Engine Example [BPE]

### WARNING: THIS NOT A COMPLETED PROJECT AND HAS BEEN ABANDONED

The purpose of this example is test integrating Bepu2 into Stride.

All the relevant physics code is contained within the `BepuPhysicsExample.BepuPhysicsIntegration` folder.
`BepuPhysicsExample.Game` was an attempt to create the original Physics (Bullet) sample with Bepu.
`BepuPhysicsExample.GameStudioExt` makes the Bepu2 physics component appear in Stride Game Studio (NOTE: this trick may not work in future Stride versions). Be aware due to this project being referenced by the game project, compiling the game project copies some of the editor related libraries.

Currently, only the raycast shooting example works. The remaining examples require collider constraints, but this has not been implemented.
