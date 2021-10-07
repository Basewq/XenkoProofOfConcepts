using Stride.Core;
using Stride.UI.Controls;
using System.ComponentModel;

namespace UINavigationExample.UI
{
    [DataContract]
    public class EditTextExt : EditText, INavigatableControl
    {
        [DataMember]
        [DefaultValue(true)]
        [Display(null, "Behavior")]
        public bool IsSelectable { get; set; } = true;

        private bool _isSelected = false;
        [DataMemberIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                IsSelectionActive = value;
            }
        }

        bool INavigatableControl.HasFocusExternally { get; set; }

        bool INavigatableControl.CanNavigateToControl()
        {
            if (!IsEnabled || !IsVisible)
            {
                return false;
            }
            var parentElem = Parent;
            while (parentElem != null)
            {
                if (!parentElem.IsEnabled || !parentElem.IsVisible)
                {
                    return false;
                }
                parentElem = parentElem.Parent;
            }
            return true;
        }

        bool INavigatableControl.OnNavigationMovement(UINavigationInputMovement inputMovement)
        {
            if (inputMovement.InputType == UINavigationInputType.Input)
            {
                return true;    // Don't allow arrow movement, since the textbox uses it to move the caret
            }

            return false;
        }

        void INavigatableControl.OnNavigationCommitSelection()
        {
            // Do nothing?
            //RaiseEvent(new RoutedEventArgs(TextChangedEvent));
        }

        protected override void OnTouchUp(Stride.UI.TouchEventArgs args)
        {
            base.OnTouchUp(args);
            if (IsSelectionActive)
            {
                ((INavigatableControl)this).HasFocusExternally = true;
            }
        }
    }
}
