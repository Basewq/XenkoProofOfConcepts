namespace UINavigationExample.UI
{
    interface IUINavigationManager
    {
        /// <summary>
        /// Should call if any UI's state has been changed that affects
        /// the selectability of the control (eg. IsVisible, IsEnabled, UI tree removed, etc).
        /// </summary>
        void OnControlStatesUpdated();
    }
}
