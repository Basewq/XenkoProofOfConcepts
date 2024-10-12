#if GAME_EDITOR
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Core.Annotations;
using Stride.Editor.EditorGame.Game;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SceneEditorExtensionExample.WorldTerrain.Foliage.Editor;

// HACK: This class is required so our painter has exclusive control over the mouse.
// Refer to FoliagePainterProcessor.OnSystemAdd & ProcessEditor to see how it is used.
class FoliagePainterEditorMouseService : EditorGameMouseServiceBase
{
    public override bool IsControllingMouse { get; protected set; }

    protected override Task<bool> Initialize([NotNull] EditorServiceGame editorGame)
    {
        Debug.WriteLine("FoliagePainterEditorMouseService Initialize");
        return Task.FromResult(true);
    }

    public void SetIsControllingMouse(bool isControllingMouse)
    {
        IsControllingMouse = isControllingMouse;
    }
}
#endif
