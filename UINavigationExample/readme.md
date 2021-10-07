# UI Navigation Example [UNE]

This project includes code and assets for proof of concept for keyboard/controller UI navigation.

Only two screens have implemented navigation:
1. The initial 'Welcome popup' (can traverse between the name textbox and the two buttons)
2. The main screen, which appears after closing the 'Welcome popup' (can traverse between the ship selection button, the four ship option buttons and the quit button).

The example is hardcoded to listen to the following inputs:
* Keyboard arrow keys
* Keyboard tab (and shift + tab)
* Keyboard space key
* Keyboard enter key
* Gamepad direction pad
* Gamepad left/right shoulder buttons (treated as shift + tab & tab, respectively)


The primary UI traversal logic (and input handling) is in the `UINavigationProcessor` class.
The traversal logic is based on two basic rules, as seen in the below image, depending on whether this is horizontal traversal or vertical traversal:

* When traversing left/right (or tabbing), we prioritize vertical ordering, then horizontal ordering, eg. if traversing right, we pick the top most, then if the vertical is equal then the left most, as seen in the left side arrow of the image.
* When traversing up/down, we prioritize horizontal ordering, then vertical ordering, eg. if traversing down, we pick the left most, then if the vertical is equal then the bottom most, as seen in the right side arrow of the image.

Also note the position used for sorting is the top-left corner of the UI, rather than the center. This is to account for different sized UI.

UI Traversal Ordering

![UI Traversal Order](images/traversal_order.png)


For `UINavigationProcessor` to detect which controls can be navigatable, we need to extend existing controls (or create new controls that co-exist with Stride controls) and implement `INavigatableControl` interface.
For this proof of concept, only the existing `Button` and `EditText` controls have been extended (as `ButtonExt` and `EditTextExt`, respectively).

`ButtonExt` needed extra visual logic to show it being selected by the user's input/gamepad, so `ButtonExt.NavigatableSelectedImage` is added for the additional visual representation, `ButtonExtRenderer` is used so the correct image is rendered, `GameUIRendererFactory` is how `ButtonExtRenderer` is used for rendering.

`GameStartupScript` is required for registering `GameUIRendererFactory` and `IUINavigationManager`.

---

### How to extend controls for Stride and use in Stride Game Studio:

As of 2021-10-07, Stride Game Studio does not automatically detect user-defined controls.
These are the steps used to get it working for this project:

1. Extend an existing control (eg. `public class ButtonExt : Button`)
2. Ensure it has public visibility and `[DataContract]` attribute on the class.
3. In Stride Game Studio, create a new UI Library asset (name it eg. `ControlsExt`).
4. Open the new UI Library asset, and add a standard library control into it (eg. `Button`). [Drag `Button` into the Visual Tree panel]
5. Save the asset and close Stride Game Studio (if you don't close it, you'll need to restart it)

**Warning: You should save everything now with a source control system (eg. Git), to ensure you don't lose/corrupt the project** 

6.  Locate and open `ControlsExt.sduilib` in a text editor (depending on the project, it's probably in `\Assets\Shared` folder).
7. Locate the lines with `!Button`
8. Change these to your extended control, in the form `!Namespace.ClassName,AssemblyName` (in this project, this is `!UINavigationExample.UI.ButtonExt,UINavigationExample.Game`
9. Once saved, you can open Stride Game Studio, and it should now appear in the UI library panel.

---

**Other Notes:**

The following is beyond the scope of this example:
* Customized user input for navigation (this can probably be done by changing the hardcoded button checks with VirtualButtons)
* Override the traversal ordering (can probably add addtional fields to `INavigatableControl` which `UINavigationProcessor` can use for additional logic, or redo the entire logic to suit your purpose)
