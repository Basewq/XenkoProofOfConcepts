using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using System.ComponentModel;

namespace UINavigationExample.UI
{
    [DataContract]
    public class ButtonExt : Button, INavigatableControl
    {
        [DataMember]
        [DefaultValue(null)]
        [Display(null, "Appearance")]
        public ISpriteProvider NavigatableSelectedImage { get; set; }

        [DataMember]
        [DefaultValue(true)]
        [Display(null, "Behavior")]
        public bool IsSelectable { get; set; } = true;

        [DataMemberIgnore]
        public bool IsSelected { get; set; }

        internal ISpriteProvider ButtonImageProvider
        {
            get
            {
                if (IsPressed)
                {
                    return PressedImage;
                }
                if (IsSelected && NavigatableSelectedImage != null)
                {
                    return NavigatableSelectedImage;
                }
                else if (MouseOverState == MouseOverState.MouseOverElement && MouseOverImage != null)
                {
                    return MouseOverImage;
                }
                else
                {
                    return NotPressedImage;
                }
            }
        }

        internal Sprite ButtonImage => ButtonImageProvider?.GetSprite();

        bool INavigatableControl.HasFocusExternally { get; set; }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            return SizeToContent
                ? base.ArrangeOverride(finalSizeWithoutMargins)
                : ImageSizeHelper.CalculateImageSizeFromAvailable(ButtonImage, finalSizeWithoutMargins, ImageStretchType, ImageStretchDirection, isMeasuring: false);
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return SizeToContent
                ? base.MeasureOverride(availableSizeWithoutMargins)
                : ImageSizeHelper.CalculateImageSizeFromAvailable(ButtonImage, availableSizeWithoutMargins, ImageStretchType, ImageStretchDirection, isMeasuring: true);
        }

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

        bool INavigatableControl.OnNavigationMovement(UINavigationInputMovement inputMovement) => false;

        void INavigatableControl.OnNavigationCommitSelection()
        {
            // Treat as if we manually clicked the button
            RaiseEvent(new RoutedEventArgs(ClickEvent));
        }
    }
}
