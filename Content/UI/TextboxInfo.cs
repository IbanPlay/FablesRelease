using Terraria.Localization;

namespace CalamityFables.Content.UI
{
    /// <summary>
    /// Represents all the data needed to construct a functional button UI element
    /// </summary>
    public class ButtonInfo
    {
        public bool closeOnClick;
        public Action clickEvent;
        public TextSnippet label;
        public LocalizedText localizedLabel;
        public float labelSize;
        public Color labelColor;
        public CharacterDisplacementDelegate letterDisplace;

        public static readonly List<ButtonInfo> localizedButtonLabels = new();

        public ButtonInfo(string localizationKey, Action clickEvent, bool closesOnClick, float labelSize = 1f, Color? labelColor = null, CharacterDisplacementDelegate labelDisplacement = null)
        {
            this.localizedLabel = CalamityFables.Instance.GetLocalization(localizationKey);
            this.clickEvent = clickEvent;
            this.closeOnClick = closesOnClick;
            this.labelColor = labelColor ?? Color.White;
            this.labelSize = labelSize;
            this.letterDisplace = labelDisplacement ?? CharacterDisplacements.NoDisplacement;

            localizedButtonLabels.Add(this);
        }

        ~ButtonInfo()
        {
            if (localizedLabel != null)
                localizedButtonLabels.Remove(this);
        }

        public void UpdateLocalization()
        {

        }

        /// <summary>
        /// Instantiates a button UI object from the associated info, including the label and on click events, but without the styling <br/>
        /// This happens when a new textbox is set
        /// </summary>
        /// <returns></returns>
        public QuadrilateralButtonUI ConstructButton()
        {
            QuadrilateralButtonUI button = new QuadrilateralButtonUI();

            button.Label = localizedLabel.Value;
            button.labelColor = labelColor;
            button.labelScale = labelSize;
            button.labelDisplacement = letterDisplace;
            button.CloseUIOnClick = closeOnClick;
            button.clickEvent = clickEvent;

            button.OnLeftMouseDown += CoolDialogueUI.OnClickDialogueButton;

            return button;
        }
    }

    /// <summary>
    /// Represents all the data needed to construct a full textbox for NPC dialogue (textbox in this context meaning a single panel of dialogue with buttons)
    /// </summary>
    public class TextboxInfo
    {
        public DialoguePortrait portrait;
        public PortraitAnimationMovement portraitMovement;
        public AwesomeSentence setence;

        public bool closeOnClick;
        public Action clickEvent;

        public List<ButtonInfo> buttons = new List<ButtonInfo>();

        /// <summary>
        /// Constructor for a basic textbox. If no action override is set, it will automatically close when clicked on
        /// </summary>
        public TextboxInfo(AwesomeSentence setence, DialoguePortrait portrait, PortraitAnimationMovement portraitMovement = null)
        {
            this.setence = setence;
            this.portrait = portrait;
            this.portraitMovement = portraitMovement;

            closeOnClick = true;
            clickEvent = null;
        }

        /// <summary>
        /// Constructor for a basic textbox, with the potential to specify an on-click action
        /// </summary>
        public TextboxInfo(AwesomeSentence setence, DialoguePortrait portrait, Action clickAction, bool closeOnClick = false, PortraitAnimationMovement portraitMovement = null)
        {
            this.setence = setence;
            this.portrait = portrait;
            this.portraitMovement = portraitMovement;

            this.closeOnClick = closeOnClick;
            clickEvent = clickAction;
        }

        /// <summary>
        /// Sets up a click event for the textbox to transition into another specified textbox
        /// </summary>
        /// <param name="nextTextbox">The textbox to chain to</param>
        /// <param name="closeOnClick">Why is this here??? If we chain into a textbox we dont care about closing on next click...</param>
        public void ChainToTextbox(TextboxInfo nextTextbox, bool closeOnClick = false)
        {
            this.closeOnClick = closeOnClick;
            clickEvent = delegate () { CoolDialogueUIManager.theUI.SetTextbox(nextTextbox); };
        }

        /// <summary>
        /// Sets up a click event for the textbox to transition into another textbox out of a pool of options
        /// </summary>
        /// <param name="nextTextboxPool">The random pool of textboxes to chain to</param>
        /// <param name="closeOnClick">Why is this here??? If we chain into a textbox we dont care about closing on next click...</param>
        public void ChainToRandomTextbox(IEnumerable<TextboxInfo> nextTextboxPool, bool closeOnClick = false)
        {
            this.closeOnClick = closeOnClick;
            clickEvent = delegate () { CoolDialogueUIManager.theUI.SetTextbox(Main.rand.Next(nextTextboxPool)); };
        }

        /// <summary>
        /// Adds a button to this textbox based on the provided <see cref="ButtonInfo"/>
        /// </summary>
        public TextboxInfo AddButton(ButtonInfo info)
        {
            buttons.Add(info);
            return this;
        }

        /*
        /// <summary>
        /// Constructs a <see cref="ButtonInfo"/> out of the given data, then appends it to this textbox
        /// </summary>
        public TextboxInfo AddButton(TextSnippet label, Action buttonAction, bool closeOnClick)
        {
            buttons.Add(new ButtonInfo(label, buttonAction, closeOnClick));
            return this;
        }

        /// <summary>
        /// Constructs a <see cref="ButtonInfo"/> out of the given data, then appends it to this textbox <br/>
        /// This overload makes the button transition the dialogue to the specified textbox on click
        /// </summary>
        public TextboxInfo AddButton(TextSnippet label, TextboxInfo nextTextbox, bool closeOnClick = false)
        {
            return AddButton(label, delegate () { CoolDialogueUIManager.theUI.SetTextbox(nextTextbox); }, closeOnClick); ;
        }

        /// <summary>
        /// Constructs a <see cref="ButtonInfo"/> out of the given data, then appends it to this textbox <br/>
        /// This overload makes the button transition the dialogue to a random textbox from a pool of options on click
        /// </summary>
        public TextboxInfo AddButton(TextSnippet label, IEnumerable<TextboxInfo> nextTextboxPool, bool closeOnClick = false)
        {
            return AddButton(label, delegate () { CoolDialogueUIManager.theUI.SetTextbox(Main.rand.Next(nextTextboxPool)); }, closeOnClick);
        }
        */
    }
}
