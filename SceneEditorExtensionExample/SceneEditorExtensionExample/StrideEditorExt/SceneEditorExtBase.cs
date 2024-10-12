using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SceneEditorExtensionExample.StrideEditorExt;

[ComponentCategory("Scene Editors")]
[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(SceneEditorExtProcessor), ExecutionMode = ExecutionMode.Editor)]
public abstract class SceneEditorExtBase : EntityComponent, INotifyPropertyChanged
{
#if GAME_EDITOR
    [DataMemberIgnore]
    public IStrideEditorService StrideEditorService { get; private set; } = default!;
    protected internal UIComponent? UIComponent { get; private set; } = default!;

    private Scene _rootScene;
    protected Scene RootScene
    {
        get
        {
            if (_rootScene is null)
            {
                var scene = Entity.Scene;
                while (scene.Parent is not null)
                {
                    scene = scene.Parent;
                }
                _rootScene = scene;
            }
            return _rootScene;
        }
    }

    internal bool IsInitialized { get; private set; }

    protected internal abstract void Initialize();
    protected internal abstract void Deinitialize();
    protected internal virtual void Update(GameTime gameTime) { }

    internal void Initialize(IStrideEditorService strideEditorService, UIComponent? uiComponent)
    {
        StrideEditorService = strideEditorService;
        UIComponent = uiComponent;

        Initialize();

        IsInitialized = true;
    }
#endif

    protected bool SetProperty<T>(
        ref T backingStore,
        T value,
        [CallerMemberName] string propertyName = default!,
        Action? onChanged = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        onChanged?.Invoke();
        OnPropertyChanged(propertyName);
        return true;
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = default!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}
