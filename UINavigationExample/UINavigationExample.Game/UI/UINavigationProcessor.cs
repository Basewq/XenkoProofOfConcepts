using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UINavigationExample.UI
{
    // Using an EntityProcessor means Stride will automatically tell us when a new UIComponent is added/removed
    // via OnEntityComponentAdding/OnEntityComponentRemoved.
    class UINavigationProcessor : EntityProcessor<UIComponent>, IUINavigationManager
    {
        private readonly List<UIComponent> _uiComponents = new List<UIComponent>();
        private InputManager _inputManager;

        private UINavControl? _selectedControl = null;

        private readonly List<UINavControl> _allActiveControls = new List<UINavControl>();
        private bool _rebuildActiveControlsList = true;

        protected override void OnSystemAdd()
        {
            _inputManager = Services.GetService<InputManager>();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] UIComponent component, [NotNull] UIComponent data)
        {
            _uiComponents.Add(component);
            _rebuildActiveControlsList = true;
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] UIComponent component, [NotNull] UIComponent data)
        {
            _uiComponents.Remove(component);
            if (_selectedControl?.UIComponent == component)
            {
                UnsetSelectedControl();
            }
            _rebuildActiveControlsList = true;
        }

        public override void Update(GameTime time)
        {
            var selectedNavCtrl = _selectedControl?.Control;

            if (_rebuildActiveControlsList)
            {
                _allActiveControls.Clear();
                // Find all active navigatable controls
                foreach (var uiComp in _uiComponents)
                {
                    if (!uiComp.Enabled || uiComp.Page == null)
                    {
                        continue;
                    }
                    var rootElement = uiComp.Page.RootElement;
                    var navControls = FindAllNavigatableControls(rootElement);
                    var activeNavControls = navControls.Where(x => x.IsSelectable && x.CanNavigateToControl() && x != selectedNavCtrl);

                    _allActiveControls.AddRange(activeNavControls.Select(x => new UINavControl(uiComp, x)));
                }
                _rebuildActiveControlsList = false;
            }

            bool selectionRequestedHandled = false;
            if (selectedNavCtrl?.HasFocusExternally ?? false)
            {
                selectedNavCtrl.HasFocusExternally = false;      // Already selected
            }
            foreach (var activeCtrl in _allActiveControls)
            {
                if (activeCtrl.Control == selectedNavCtrl)
                {
                    continue;
                }
                if (activeCtrl.Control.HasFocusExternally)
                {
                    UnsetSelectedControl();
                    SetControlAsSelected(activeCtrl);
                    activeCtrl.Control.HasFocusExternally = false;
                    selectionRequestedHandled = true;
                }
                // Don't break for-loop, because if multiple external focuses occur, we need to accept them all
                // (which will immediately unset the previous one, since we can't reject the previous)
            }

            if (!selectionRequestedHandled)
            {
                HandleInputMovement(_allActiveControls);
                bool isCommittingSelection = _inputManager.IsKeyPressed(Keys.Enter) || _inputManager.IsKeyPressed(Keys.Space);
                if (_inputManager.HasGamePad && _inputManager.DefaultGamePad != null)
                {
                    var gamePad = _inputManager.DefaultGamePad;
                    isCommittingSelection = isCommittingSelection
                        || gamePad.IsButtonPressed(GamePadButton.A)
                        || gamePad.IsButtonPressed(GamePadButton.X);
                }
                if (isCommittingSelection && selectedNavCtrl != null)
                {
                    selectedNavCtrl.OnNavigationCommitSelection();
                }
            }
        }

        private void HandleInputMovement(List<UINavControl> allActiveControls)
        {
            var selectedNavCtrl = _selectedControl?.Control;

            bool isTabPressed = _inputManager.IsKeyPressed(Keys.Tab);
            bool isShiftKeyDown = _inputManager.IsKeyDown(Keys.LeftShift) || _inputManager.IsKeyDown(Keys.RightShift);
            if (_inputManager.HasGamePad && _inputManager.DefaultGamePad != null)
            {
                var gamePad = _inputManager.DefaultGamePad;
                if (gamePad.IsButtonPressed(GamePadButton.LeftShoulder))
                {
                    // Treat as shift + tab
                    isTabPressed = true;
                    isShiftKeyDown = true;
                }
                else if (gamePad.IsButtonPressed(GamePadButton.RightShoulder))
                {
                    // Treat as tab
                    isTabPressed = true;
                }
            }
            if (selectedNavCtrl != null && isTabPressed)
            {
                var navMmnt = new UINavigationInputMovement
                {
                    InputType = isShiftKeyDown ? UINavigationInputType.ShiftTab : UINavigationInputType.Tab,
                    InputDirection = Vector2.Zero
                };
                if (selectedNavCtrl.OnNavigationMovement(navMmnt))
                {
                    // Current control has handled the input
                    return;
                }
            }

            // UI is in screen space where origin is top left corner of the screen, x-axis pointing left, y-axis pointing down
            var inputDir = Vector2.Zero;        // Vector2 is used since in the future we could possibly implement thumbstick controller input.
            inputDir.X += _inputManager.IsKeyPressed(Keys.Left) ? -1 : 0;
            inputDir.X += _inputManager.IsKeyPressed(Keys.Right) ? 1 : 0;
            inputDir.Y += _inputManager.IsKeyPressed(Keys.Up) ? -1 : 0;
            inputDir.Y += _inputManager.IsKeyPressed(Keys.Down) ? 1 : 0;

            if (_inputManager.HasGamePad && _inputManager.DefaultGamePad != null)
            {
                var gamePad = _inputManager.DefaultGamePad;
                inputDir.X += gamePad.IsButtonPressed(GamePadButton.PadLeft) ? -1 : 0;
                inputDir.X += gamePad.IsButtonPressed(GamePadButton.PadRight) ? 1 : 0;
                inputDir.Y += gamePad.IsButtonPressed(GamePadButton.PadUp) ? -1 : 0;
                inputDir.Y += gamePad.IsButtonPressed(GamePadButton.PadDown) ? 1 : 0;
            }

            if (inputDir.LengthSquared() > 0)
            {
                inputDir.Normalize();
            }
            if (selectedNavCtrl != null && inputDir.LengthSquared() > 0)
            {
                var navMmnt = new UINavigationInputMovement
                {
                    InputType = UINavigationInputType.Input,
                    InputDirection = inputDir
                };
                if (selectedNavCtrl.OnNavigationMovement(navMmnt))
                {
                    // Current control has handled the input
                    return;
                }
            }

            if (isTabPressed)
            {
                // Treat tabbing as left/right movement
                float shiftValue = isShiftKeyDown ? -1 : 1;
                inputDir.X += shiftValue * 1;
                if (inputDir.LengthSquared() > 0)
                {
                    inputDir.Normalize();
                }
            }

            if (inputDir.LengthSquared() > 0)
            {
                if (!_selectedControl.HasValue)
                {
                    // Just pick the first control
                    if (allActiveControls.Count > 0)
                    {
                        // Sort top to bottom, left to right
                        allActiveControls.Sort(TopDownThenLeftRightSorter);
                        SetControlAsSelected(allActiveControls[0]);
                    }
                    //else  // Nothing to select
                }
                else if (_selectedControl.HasValue && allActiveControls.Count > 1)
                {
                    // Navigate to next control
                    var selectedCtrl = _selectedControl.Value;
                    var uiWorldMatrix = selectedCtrl.UIElement.WorldMatrix;
                    var originUIPosition = uiWorldMatrix.TranslationVector.XY();
                    if (Math.Abs(inputDir.X) >= Math.Abs(inputDir.Y))
                    {
                        // Sort top to bottom, left to right
                        allActiveControls.Sort(TopDownThenLeftRightSorter);
                        int curSelectedIndex = allActiveControls.IndexOf(x => x.Control == selectedCtrl.Control);
                        int nextSelectedIndex = curSelectedIndex + (inputDir.X > 0 ? 1 : -1);
                        if (nextSelectedIndex < 0)
                        {
                            nextSelectedIndex = allActiveControls.Count - 1;
                        }
                        else if (nextSelectedIndex == allActiveControls.Count)
                        {
                            nextSelectedIndex = 0;
                        }
                        if (nextSelectedIndex != curSelectedIndex)
                        {
                            SetControlAsSelected(allActiveControls[nextSelectedIndex]);
                        }
                    }
                    else
                    {
                        // Sort left to right, top to bottom
                        allActiveControls.Sort(LeftRightThenTopDownSorter);
                        int curSelectedIndex = allActiveControls.IndexOf(x => x.Control == selectedCtrl.Control);
                        int nextSelectedIndex = curSelectedIndex + (inputDir.Y > 0 ? 1 : -1);
                        if (nextSelectedIndex < 0)
                        {
                            nextSelectedIndex = allActiveControls.Count - 1;
                        }
                        else if (nextSelectedIndex == allActiveControls.Count)
                        {
                            nextSelectedIndex = 0;
                        }
                        if (nextSelectedIndex != curSelectedIndex)
                        {
                            SetControlAsSelected(allActiveControls[nextSelectedIndex]);
                        }
                    }
                }
            }
        }

        private static readonly Comparison<UINavControl> TopDownThenLeftRightSorter = (UINavControl ctrl1, UINavControl ctrl2) =>
        {
            // Use the top-left corner of the UI as the sorting position, as this should
            // account for different sized UI
            (float ctrl1Left, float ctrl1Top) = GetUIScreenPosition(ctrl1.UIElement);
            (float ctrl2Left, float ctrl2Top) = GetUIScreenPosition(ctrl2.UIElement);
            if (ctrl1Top < ctrl2Top)
            {
                return -1;
            }
            else if (ctrl1Top > ctrl2Top)
            {
                return 1;
            }
            else
            {
                return ctrl1Left.CompareTo(ctrl2Left);
            }
        };

        private static readonly Comparison<UINavControl> LeftRightThenTopDownSorter = (UINavControl ctrl1, UINavControl ctrl2) =>
        {
            // Use the top-left corner of the UI as the sorting position, as this should
            // account for different sized UI
            (float ctrl1Left, float ctrl1Top) = GetUIScreenPosition(ctrl1.UIElement);
            (float ctrl2Left, float ctrl2Top) = GetUIScreenPosition(ctrl2.UIElement);
            if (ctrl1Left < ctrl2Left)
            {
                return -1;
            }
            else if (ctrl1Left > ctrl2Left)
            {
                return 1;
            }
            else
            {
                return ctrl1Top.CompareTo(ctrl1Top);
            }
        };

        private static (float left, float top) GetUIScreenPosition(UIElement uiElement)
        {
            var uiPos = uiElement.WorldMatrix.TranslationVector;
            var uiRenderSize = uiElement.RenderSize;
            float left = uiPos.X - (uiRenderSize.X * 0.5f);
            float top = uiPos.Y - (uiRenderSize.Y * 0.5f);

            return (left, top);
        }

        private static IEnumerable<INavigatableControl> FindAllNavigatableControls(UIElement source)
        {
            if (source is INavigatableControl navControl)
            {
                yield return navControl;
            }

            foreach (var subChild in FindAllNavigatableChildren(source, e => e.VisualChildren.Count, (e, i) => e.VisualChildren[i]).NotNull())
            {
                yield return subChild;
            }
        }

        private static IEnumerable<INavigatableControl> FindAllNavigatableChildren(UIElement source, Func<UIElement, int> getChildrenCountFunc, Func<UIElement, int, UIElement> getChildFunc)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (getChildrenCountFunc == null) throw new ArgumentNullException(nameof(getChildrenCountFunc));
            if (getChildFunc == null) throw new ArgumentNullException(nameof(getChildFunc));

            var childCount = getChildrenCountFunc(source);
            for (var i = 0; i < childCount; i++)
            {
                var child = getChildFunc(source, i);
                if (child != null)
                {
                    if (child is INavigatableControl navControl)
                        yield return navControl;

                    foreach (var subChild in FindAllNavigatableChildren(child, getChildrenCountFunc, getChildFunc).NotNull())
                    {
                        yield return subChild;
                    }
                }
            }
        }

        void IUINavigationManager.OnControlStatesUpdated()
        {
            _rebuildActiveControlsList = true;

            if (!_selectedControl.HasValue)
            {
                return;
            }
            var selectedCtrl = _selectedControl.Value;
            if (!selectedCtrl.UIComponent.Enabled
                || !selectedCtrl.Control.IsSelectable || !selectedCtrl.Control.CanNavigateToControl())
            {
                UnsetSelectedControl();
            }
        }

        private void SetControlAsSelected(UINavControl uiNavControl)
        {
            UnsetSelectedControl();
            _selectedControl = uiNavControl;
            uiNavControl.Control.IsSelected = true;
        }

        private void UnsetSelectedControl()
        {
            if (_selectedControl.HasValue)
            {
                _selectedControl.Value.Control.IsSelected = false;
            }
            _selectedControl = default;
        }

        private struct UINavControl
        {
            public UIComponent UIComponent;
            public UIElement UIElement;
            public INavigatableControl Control;

            public UINavControl(UIComponent uiComponent, INavigatableControl control)
            {
                UIComponent = uiComponent;
                UIElement = (UIElement)control;     // Convenience field to avoid recasting
                Control = control;
            }
        }
    }
}
