using DialogueTextControlExample.UI;
using DialogueTextControlExample.UI.Controls;
using Stride.Engine;
using Stride.UI.Controls;

namespace DialogueTextControlExample
{
    public class DialogueStartupController : StartupScript
    {
        private static readonly UIElementKey<Button> ShowDialogueButton1 = new("ShowDialogueButton1");
        private static readonly UIElementKey<Button> ShowDialogueButton2 = new("ShowDialogueButton2");
        private static readonly UIElementKey<Button> ShowDialogueButton3 = new("ShowDialogueButton3");
        private static readonly UIElementKey<DialogueText> DialogueText = new("DialogueText");
        private static readonly UIElementKey<TextBlock> DialogueTextBlock = new("DialogueTextBlock");
        private static readonly UIElementKey<EditText> DialogueEditText = new("DialogueEditText");
        private static readonly UIElementKey<Button> NextDialogueButton = new("NextDialogueButton");

        private DialogueText _dialogueText;
        private TextBlock _dialogueTextBlock;
        private EditText _dialogueEditText;

        public override void Start()
        {
            // Initialization of the script.
            var uiComp = Entity.Get<UIComponent>();
            _dialogueText = uiComp.GetUI(DialogueText);
            //_dialogueText.TextCharacterAppeared += glyph => Debug.WriteLine($"Char appeared: '{glyph.Character}' - {DateTime.Now:HH:mm:ss.ffff}");
            _dialogueTextBlock = uiComp.GetUI(DialogueTextBlock);
            _dialogueEditText = uiComp.GetUI(DialogueEditText);
            _dialogueEditText.TextChanged += (_, _) => ShowEditText();
            uiComp.GetUI(ShowDialogueButton1).Click += (_, _) => ShowText1();
            uiComp.GetUI(ShowDialogueButton2).Click += (_, _) => ShowText2();
            uiComp.GetUI(ShowDialogueButton3).Click += (_, _) => ShowText3();
            uiComp.GetUI(NextDialogueButton).Click += (_, _) => OnNextButton();
        }

        private void ShowEditText()
        {
            string newText = _dialogueEditText.Text;
            _dialogueTextBlock.Text = newText;
            _dialogueText.Text = newText;
            _dialogueText.PlayTextDisplay();
        }

        private void ShowText1()
        {
            _dialogueText.Text = @"This is <color=red>red text!!!</color>
Now the next line <wave>has wavy</wave> text
Third line's <wave amp=0.75 freq=2><color=green>wave</color> is faster</wave>
Fourth line is <b><heatwave per=2>feeling the <color=red>heat.</color></heatwave></b>
";
            _dialogueTextBlock.Text = _dialogueText.Text;
            _dialogueText.PlayTextDisplay();
        }

        private void ShowText2()
        {
            _dialogueText.Text = @"This is dialogue 2<pause skip1=false>...</pause>
This dialogue <pause=0.25>message</pause> has a line message that will be word-wrapped even though it's <i>supposed</i> to be a <b>single</b> line.
";
            _dialogueTextBlock.Text = _dialogueText.Text;
            _dialogueText.PlayTextDisplay();
        }

        private void ShowText3()
        {
            _dialogueText.Text = @"This text <b>is <i>displayed</b> immediately</i>.";
            _dialogueTextBlock.Text = _dialogueText.Text;
            _dialogueText.DisplayAllText();
        }

        private void OnNextButton()
        {
            _dialogueText.DisplayAllText();
        }
    }
}
