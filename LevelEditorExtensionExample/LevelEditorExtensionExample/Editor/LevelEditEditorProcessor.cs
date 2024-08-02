using LevelEditorExtensionExample.UI;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.UI.Controls;
using Stride.UI.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LevelEditorExtensionExample.Editor
{
    class LevelEditEditorProcessor : EntityProcessor<LevelEditComponent, LevelEditEditorProcessor.AssociatedData>
    {
        internal static readonly UIElementKey<Button> CreatePrefabButton = new UIElementKey<Button>("CreatePrefabButton");
        internal static readonly UIElementKey<Button> ResetPrefabNextYPositionButton = new UIElementKey<Button>("ResetPrefabNextYPositionButton");
        internal static readonly UIElementKey<Button> UpdateScaleButton = new UIElementKey<Button>("UpdateScaleButton");
        internal static readonly UIElementKey<Button> ResetScaleButton = new UIElementKey<Button>("ResetScaleButton");
        internal static readonly UIElementKey<Button> UpdateInternalDataButton = new UIElementKey<Button>("UpdateInternalDataButton");
        internal static readonly UIElementKey<Button> ResetInternalDataButton = new UIElementKey<Button>("ResetInternalDataButton");

        public LevelEditEditorProcessor() : base(typeof(UIComponent))
        {

        }

        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] LevelEditComponent component)
        {
            return new AssociatedData
            {
                UIComponent = entity.Get<UIComponent>(),
            };
        }

#if GAME_EDITOR
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] LevelEditComponent component, [NotNull] AssociatedData data)
        {
            // Ensure we unregister all event handlers
            if (data.CreatePrefabButton != null)
            {
                data.CreatePrefabButton.Click -= OnCreatePrefabButtonClicked;
            }
            if (data.ResetPrefabNextYPositionButton != null)
            {
                data.ResetPrefabNextYPositionButton.Click -= OnResetPrefabNextYPositionButtonClicked;
            }
            if (data.UpdateScaleButton != null)
            {
                data.UpdateScaleButton.Click -= OnUpdateScaleButtonClicked;
            }
            if (data.ResetScaleButton != null)
            {
                data.ResetScaleButton.Click -= OnResetScaleButtonClicked;
            }
            if (data.UpdateInternalDataButton != null)
            {
                data.UpdateInternalDataButton.Click -= OnUpdateInternalDataButtonClicked;
            }
            if (data.ResetInternalDataButton != null)
            {
                data.ResetInternalDataButton.Click -= OnResetInternalDataButtonClicked;
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var kv in ComponentDatas)
            {
                var assocData = kv.Value;
                var uiComp = assocData.UIComponent;
                if (uiComp?.Page?.RootElement == null)
                {
                    continue;
                }

                var levelEditComp = kv.Key;

                if (assocData.CreatePrefabButton == null && uiComp.TryGetUI(CreatePrefabButton, out var button))
                {
                    button.Click += OnCreatePrefabButtonClicked;
                    assocData.CreatePrefabButton = button;
                }
                if (assocData.ResetPrefabNextYPositionButton == null && uiComp.TryGetUI(ResetPrefabNextYPositionButton, out button))
                {
                    button.Click += OnResetPrefabNextYPositionButtonClicked;
                    assocData.ResetPrefabNextYPositionButton = button;
                }
                if (assocData.UpdateScaleButton == null && uiComp.TryGetUI(UpdateScaleButton, out button))
                {
                    button.Click += OnUpdateScaleButtonClicked;
                    assocData.UpdateScaleButton = button;
                }
                if (assocData.ResetScaleButton == null && uiComp.TryGetUI(ResetScaleButton, out button))
                {
                    button.Click += OnResetScaleButtonClicked;
                    assocData.ResetScaleButton = button;
                }
                if (assocData.UpdateInternalDataButton == null && uiComp.TryGetUI(UpdateInternalDataButton, out button))
                {
                    button.Click += OnUpdateInternalDataButtonClicked;
                    assocData.UpdateInternalDataButton = button;
                }
                if (assocData.ResetInternalDataButton == null && uiComp.TryGetUI(ResetInternalDataButton, out button))
                {
                    button.Click += OnResetInternalDataButtonClicked;
                    assocData.ResetInternalDataButton = button;
                }
            }
        }

        private void OnCreatePrefabButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                var sceneVm = sceneEditorVm?.Asset;
                if (sceneEditorVm != null)
                {
                    //var sp = sceneVm?.ServiceProvider;
                    var package = sceneVm.AssetItem.Package;
                    var boxPrefabPath = levelEditComp.BoxPrefab;
                    var boxPrefabAssetItem = package.Assets.FirstOrDefault(x => string.Equals(x.Location.FullPath, boxPrefabPath.Url));
                    var boxPrefabAsset = boxPrefabAssetItem.Asset as Stride.Assets.Entities.PrefabAsset;
                    var prefabVmRaw = sceneEditorVm.Session.GetAssetById(boxPrefabAsset.Id);
                    var prefabVm = prefabVmRaw as Stride.Assets.Presentation.ViewModel.PrefabViewModel;
                    var addChildMod = Stride.Core.Assets.Editor.ViewModel.AddChildModifiers.Alt;    // Create without a container entity

                    /*
                    // This adds the prefab to the root at origin, but can't be modified further
                    {
                        var sceneRootVm = sceneEditorVm.RootPart as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneRootViewModel;
                        var sceneRootAddChildViewModel = (Stride.Core.Assets.Editor.ViewModel.IAddChildViewModel)sceneRootVm;
                        if (sceneRootAddChildViewModel.CanAddChildren(new[] { prefabVm }, addChildMod, out string msg))
                        {
                            sceneRootAddChildViewModel.AddChildren(new[] { prefabVm }, addChildMod);
                        }
                    }
                    */

                    var sceneRootVm = sceneEditorVm.RootPart as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneRootViewModel;
                    // SceneRootViewModel has an internal method called AddEntitiesFromAssets which will be used to add a prefab into the scene
                    var sceneRoot_AddEntitiesFromAssets_MethodInfo = sceneRootVm.GetType().GetMethod("AddEntitiesFromAssets", BindingFlags.NonPublic | BindingFlags.Instance);

                    var root = sceneEditorVm.HierarchyRoot;
                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        var param_Assets = new[] { prefabVm };
                        var param_Index = root.Asset.Asset.Hierarchy.RootParts.Count;   // Add to the end
                        var param_Modifiers = addChildMod;
                        var param_RootPosition = new Vector3(0, levelEditComp.PrefabNextYPosition, 0);
                        var methodParams = new object[] { param_Assets, param_Index, param_Modifiers, param_RootPosition };
                        var methodReturnValue = sceneRoot_AddEntitiesFromAssets_MethodInfo.Invoke(sceneRootVm, methodParams);
                        var entitiesViewModels = methodReturnValue as IReadOnlyCollection<Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels.EntityViewModel>;
                        // Can look at the entities created from the prefab in entitiesViewModels

                        var levelEditorEntity = levelEditComp.Entity;
                        var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                        var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;      // This is the entity on the 'master' version.
                        var vmLevelEditComp = vmLevelEditorEntity.Get<LevelEditComponent>();

                        var levelEditCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditComp);
                        var nextYPosNodeRaw = levelEditCompNode[nameof(LevelEditComponent.PrefabNextYPosition)];
                        var nextYPosNode = nextYPosNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                        nextYPosNode.Update(levelEditComp.PrefabNextYPosition + 1);     // Increment & update LevelEditComponent.PrefabNextYPosition

                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor create prefab");
                    }
                }
            });
        }

        private void OnResetPrefabNextYPositionButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                if (sceneEditorVm != null)
                {
                    var levelEditorEntity = levelEditComp.Entity;

                    var root = sceneEditorVm.HierarchyRoot;
                    var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                    var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;

                    var vmLevelEditComp = vmLevelEditorEntity.Get<LevelEditComponent>();

                    var levelEditCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditComp);
                    var nextYPosNodeRaw = levelEditCompNode[nameof(LevelEditComponent.PrefabNextYPosition)];
                    var nextYPosNode = nextYPosNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        nextYPosNode.Update(0);
                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor Reset prefab next Y position");
                    }
                }
            });
        }

        private void OnUpdateScaleButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                if (sceneEditorVm != null)
                {
                    var levelEditorEntity = levelEditComp.Entity;

                    // The Game Studio holds a master version of the scene, whereas the processor (which we're currently in) is running on
                    // a separate duplicate copy of the scene.
                    // We need to find the matching Id (which should be unique) of the entity from the master version, then
                    // find the associated asset node which will allow us to update an entity's property on the master version
                    // and this will then propagate this back to our copy of the scene.
                    var root = sceneEditorVm.HierarchyRoot;
                    var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                    var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;

                    var levelEditorEntityTransformNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditorEntity.Transform);
                    var scaleNodeRaw = levelEditorEntityTransformNode[nameof(TransformComponent.Scale)];
                    var scaleNode = scaleNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                    var newScale = levelEditorEntity.Transform.Scale;
                    newScale.X += 1;

                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        scaleNode.Update(newScale);
                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor Update transformation");
                    }
                }
            });
        }

        private void OnResetScaleButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                if (sceneEditorVm != null)
                {
                    var levelEditorEntity = levelEditComp.Entity;

                    var root = sceneEditorVm.HierarchyRoot;
                    var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                    var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;

                    var levelEditorEntityTransformNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditorEntity.Transform);
                    var scaleNodeRaw = levelEditorEntityTransformNode[nameof(TransformComponent.Scale)];
                    var scaleNode = scaleNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                    var newScale = levelEditorEntity.Transform.Scale;
                    newScale.X = 1;

                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        scaleNode.Update(newScale);
                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor Reset transformation");
                    }
                }
            });
        }

        private void OnUpdateInternalDataButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                if (sceneEditorVm != null)
                {
                    var levelEditorEntity = levelEditComp.Entity;

                    var root = sceneEditorVm.HierarchyRoot;
                    var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                    var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;

                    var vmLevelEditComp = vmLevelEditorEntity.Get<LevelEditComponent>();

                    var levelEditCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditComp);
                    var intlDataNodeRaw = levelEditCompNode[nameof(LevelEditComponent.InternalData)];
                    var intlDataNode = intlDataNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                    var intlDataArray = intlDataNode.MemberDescriptor.Get(intlDataNode.Parent.Retrieve()) as int[];     // This is LevelEditComponent.InternalData
                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        var arrayObjectNode = intlDataNode.Target as Stride.Core.Quantum.ObjectNode;
                        for (int i = 0; i < intlDataArray.Length; i++)
                        {
                            int newValue = intlDataArray[i] + 1;    // Increment each array item
                            arrayObjectNode.Update(newValue, new Stride.Core.Quantum.NodeIndex(i));     // Update the value of the array (NodeIndex is the node equivalent of the array index)
                        }
                        /*
                        // Alternative way is to just create a new array and replace the entire array, but may allocate large chunks of memory
                        // which could be bad for performance
                        {
                            var newArrayData = (int[])intlDataArray.Clone();
                            for (int i = 0; i < newArrayData.Length; i++)
                            {
                                newArrayData[i]++;
                            }
                            intlDataNode.Update(newArrayData);
                        }
                        */
                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor Update internal data");
                    }
                }
            });
        }

        private void OnResetInternalDataButtonClicked(object sender, RoutedEventArgs e)
        {
            var kv = ComponentDatas.FirstOrDefault();
            var levelEditComp = kv.Key;

            var editorVm = Stride.Core.Assets.Editor.ViewModel.EditorViewModel.Instance;
            var gsVm = Stride.GameStudio.ViewModels.GameStudioViewModel.GameStudio;
            gsVm.StrideAssets.Dispatcher.Invoke(() =>
            {
                // Application.Current must be accessed on the UI thread
                var window = System.Windows.Application.Current.MainWindow as Stride.GameStudio.View.GameStudioWindow;
                var sceneEditorView = window.GetChildOfType<Stride.Assets.Presentation.AssetEditors.SceneEditor.Views.SceneEditorView>();
                var sceneEditorVm = sceneEditorView?.DataContext as Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels.SceneEditorViewModel;
                if (sceneEditorVm != null)
                {
                    var levelEditorEntity = levelEditComp.Entity;

                    var root = sceneEditorVm.HierarchyRoot;
                    var levelEditorEntityAssetPart = root.Asset.Asset.Hierarchy.Parts.FirstOrDefault(x => x.Value.Entity.Id == levelEditorEntity.Id);
                    var vmLevelEditorEntity = levelEditorEntityAssetPart.Value.Entity;

                    var vmLevelEditComp = vmLevelEditorEntity.Get<LevelEditComponent>();

                    var levelEditCompNode = sceneEditorVm.Session.AssetNodeContainer.GetNode(vmLevelEditComp);
                    var intlDataNodeRaw = levelEditCompNode[nameof(LevelEditComponent.InternalData)];
                    var intlDataNode = intlDataNodeRaw as Stride.Core.Assets.Quantum.IAssetMemberNode;

                    var intlDataArray = intlDataNode.MemberDescriptor.Get(intlDataNode.Parent.Retrieve()) as int[];     // This is LevelEditComponent.InternalData
                    using (var transaction = sceneEditorVm.UndoRedoService.CreateTransaction())
                    {
                        var arrayObjectNode = intlDataNode.Target as Stride.Core.Quantum.ObjectNode;
                        for (int i = 0; i < intlDataArray.Length; i++)
                        {
                            int newValue = i + 1;
                            arrayObjectNode.Update(newValue, new Stride.Core.Quantum.NodeIndex(i));     // Update the value of the array (NodeIndex is the node equivalent of the array index)
                        }
                        sceneEditorVm.UndoRedoService.SetName(transaction, "Level Editor Reset internal data");
                    }
                }
            });
        }
#endif
        internal class AssociatedData
        {
            public UIComponent UIComponent;

            public Button CreatePrefabButton;
            public Button ResetPrefabNextYPositionButton;
            public Button UpdateScaleButton;
            public Button ResetScaleButton;
            public Button UpdateInternalDataButton;
            public Button ResetInternalDataButton;
        }
    }

#if GAME_EDITOR
    static class WpfExt
    {
        public static void GetChildrenOfType<T>(this System.Windows.DependencyObject depObj, List<T> foundChildren)
            where T : System.Windows.DependencyObject
        {
            if (depObj == null) return;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);

                if (child is T matchedChild)
                {
                    foundChildren.Add(matchedChild);
                }
                GetChildrenOfType(child, foundChildren);
            }
        }

        public static T GetChildOfType<T>(this System.Windows.DependencyObject depObj)
            where T : System.Windows.DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
#endif
}
