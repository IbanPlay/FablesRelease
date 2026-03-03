using CalamityFables.Content.UI;

namespace CalamityFables.Core
{
    /// <summary>
    /// Interface that contains hooks necessary to provide the UI with the necessary styling for the textbox and buttons, alongside deciding what textbox to display first. <br/>
    /// Subsequent textboxes naturally emerge from this by chaining the textboxes together
    /// </summary>
    public interface ICoolDialogueHandler
    {
        /// <summary>
        /// Does everything necessary to set the theme of the UI to match the character
        /// </summary>
        public void TurnOnUITheme();

        /// <summary>
        /// Sets the style for a specific button
        /// </summary>
        /// <param name="buttonIndex">Index of the button to style. Use this if you want to style buttons differently</param>
        public void StyleButton(QuadrilateralButtonUI button, int buttonIndex);

        /// <summary>
        /// Gets the first textbox that should be used when the UI is opened
        /// </summary>
        public TextboxInfo GetFirstTextbox();

    }
}
