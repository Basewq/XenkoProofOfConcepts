using Stride.Core.Mathematics;

namespace UINavigationExample.UI
{
    // Note that C# limitations doesn't allow multiple class inheritance, all our
    // extended controls must implement this interface separately.
    // But be aware that all INavigatableControls are also UIElement, and UINavigationProcessor
    // will assume this is the case.
    interface INavigatableControl
    {
        /// <summary>
        /// Is true if this is the control in focus.
        /// </summary>
        bool IsSelectable { get; set; }

        /// <summary>
        /// Is true if this is the control in focus.
        /// </summary>
        bool IsSelected { get; set; }

        /// <remarks>
        /// Minor hack, where user can focus on a control outside the <see cref="IUINavigationManager"/> (eg. EditText via mouse click),
        /// and the <see cref="IUINavigationManager"/> needs to change its selected control to this control.
        /// </remarks>
        bool HasFocusExternally { get; set; }

        /// <summary>
        /// Returns true if this control can be selected.
        /// It traverses the visual hierarchy to check if it is enabled and visible on all levels of the visual tree.
        /// </summary>
        bool CanNavigateToControl();

        /// <summary>
        /// Returns true if this this control has handled the movement. This is only called if the user
        /// is making navigation movement AND this control <see cref="IsSelected"/> is true.
        /// </summary>
        bool OnNavigationMovement(UINavigationInputMovement inputMovement);

        /// <summary>
        /// Called when user presses Enter or Space key.
        /// This can be seen as eg. 'clicking' on a button.
        /// </summary>
        void OnNavigationCommitSelection();
    }

    struct UINavigationInputMovement
    {
        public UINavigationInputType InputType;
        /// <summary>
        /// Normalized direction. Non-zero when <see cref="InputType"/> is <see cref="UINavigationInputType.Input"/>.
        /// </summary>
        public Vector2 InputDirection;
    }

    enum UINavigationInputType
    {
        Input,
        Tab,
        ShiftTab
    }
}
