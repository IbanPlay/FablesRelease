using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class CoolDialogueUIManager : ModSystem
    {
        internal static UserInterface userInterface;
        internal static CoolDialogueUI theUI;

        /// <summary>
        /// Portrait shown on the current textbox. use this if you want to match NPC animations to dialogue portraits
        /// </summary>
        public static DialoguePortrait CurrentPortrait => theUI.mainBox.Portrait;
        /// <summary>
        /// Is the dialogue handler displaying a textbox right now?
        /// </summary>
        public static bool Active => _dialogueHandler != null;

        internal static ICoolDialogueHandler _dialogueHandler;

        /// <summary>
        /// Dialogue handler that acts as the initial impulse for the dialogue to appear <br/>
        /// Setting this to null will close the textbox. <br/>
        /// Setting this to a value different than the current one we have will close the previous one we had open and do nothing  <br/>
        /// Setting this to a valid dialogue handler will automatically open the dialogue menu and do the initialization for you behind the scenes<br/>
        /// <br/>
        /// Dialogue handler controls the visual theme of the dialogue used, and is used by the UI to know which textbox to open with
        /// </summary>
        public static ICoolDialogueHandler DialogueHandler {
            get => _dialogueHandler;
            set {
                if (value == null)
                {
                    _dialogueHandler = null;
                    return;
                }

                //IF we already had another menu open , close it instead
                if (_dialogueHandler != value && _dialogueHandler != null)
                {
                    _dialogueHandler = null;
                    HideDialogueMenu();
                    return;
                }


                if (_dialogueHandler == null)
                {
                    value.TurnOnUITheme();
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }

                _dialogueHandler = value;
            }
        }

        internal static float panelCloseTimer;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                userInterface = new UserInterface();

                theUI = new CoolDialogueUI();
                theUI.Activate();
            }
        }

        public override void Unload()
        {
            theUI = null;
        }

        private static GameTime _lastUpdateUIGameTime;

        public override void UpdateUI(GameTime gameTime)
        {
            // Initialize the user interface if we have a dialogue handler assigned but the UI is toggled off. Grabs the initial textbox from the dialogue handler interface
            if (userInterface?.CurrentState == null && DialogueHandler != null)
            {
                ShowDialogueMenu();
                theUI.SetTextbox(DialogueHandler.GetFirstTextbox());
            }

            _lastUpdateUIGameTime = gameTime;
            if (userInterface?.CurrentState != null)
                userInterface.Update(gameTime);

            //Tick down the panel dissapear timer
            if (panelCloseTimer > 0)
                panelCloseTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            CoolDialogueUI.previousPreviousMouseLeft = Main.mouseLeft;
            CoolDialogueUI.uiScale = 1;
            float screenHeight = PlayerInput.RealScreenHeight;
            if (screenHeight > 1080f)
                CoolDialogueUI.uiScale *= 1 + Utils.GetLerpValue(1080f, 2160, screenHeight); //Higher offset the higher the screen height resolution is
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("Fables: Cool Ass Dialogue", delegate () {

                    if (userInterface?.CurrentState != null && _lastUpdateUIGameTime != null)
                        userInterface.Draw(Main.spriteBatch, _lastUpdateUIGameTime);

                    else if (panelCloseTimer > 0 && CrazyUIDrawingSystem.MainRenderTarget != null)
                        DrawDissapearingTextBox();

                    return true;
                }, InterfaceScaleType.None));
            }
        }

        /// <summary>
        /// Draws the last captured dialogue box RT fading in opacity and flying upwards as it dissapears
        /// </summary>
        public void DrawDissapearingTextBox()
        {
            Vector2 position = Vector2.Zero - Vector2.UnitY * 400f * (float)Math.Pow(1 - panelCloseTimer / 0.5f, 1.7f) * CoolDialogueUI.uiScale;
            Color fade = Color.White * (float)Math.Pow(panelCloseTimer / 0.5f, 2f);

            Main.spriteBatch.Draw(CrazyUIDrawingSystem.MainRenderTarget, position, null, fade, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Toggles on the ui layer with the dialogue
        /// </summary>
        internal static void ShowDialogueMenu()
        { userInterface?.SetState(theUI); }

        /// <summary>
        /// Closes the dialogue menu, and sets it up to fade away upwards if it was fully open
        /// </summary>
        internal static void HideDialogueMenu()
        {
            panelCloseTimer = 0f;
            if (theUI.mainBox.ExpansionTimer >= 1)
                panelCloseTimer = 0.5f;

            if (!Main.gameMenu)
                SoundEngine.PlaySound(SoundID.MenuClose);

            userInterface?.SetState(null);
            DialogueHandler = null;
        }

        public override void OnLocalizationsLoaded()
        {
            if (Main.dedServ)
                return;

            //Shouldn't happen because the player is in the main menu when changing languages lol
            if (theUI.mainBox.CharacterName != null && theUI.mainBox.CharacterName.Value != "")
                theUI.mainBox.RecalculateNameplateSize();

            foreach (AwesomeSentence localizedSetence in AwesomeSentence.localizedSetences)
                localizedSetence.UpdateLocalization();
            foreach (ButtonInfo localizedButton in ButtonInfo.localizedButtonLabels)
                localizedButton.UpdateLocalization();
        }
    }

    public class CoolDialogueUI : UIState
    {
        public QuadrilateralBoxWithPortraitUI mainBox;
        public NPC anchorNPC;
        internal static float uiScale = 1f;

        internal bool closesOnClick;
        internal Action clickEvent;
        internal static bool previousPreviousMouseLeft;

        /// <summary>
        /// Does the UI have buttons? Calculated from the amount of child element the UI state contains
        /// </summary>
        public bool HasButtons => Elements.Count > 1;
        internal float totalButtonWidth = 0;
        /// <summary>
        /// Width of the buttons on the rack without any spacing inbetween, trimming off potential overflow on the sides <br/>
        /// Used to calculate the free space we have between buttons
        /// </summary>
        internal float buttonRackInnerWidth = 0;
        /// <summary>
        /// Width of the buttons on the rack including spacing, trimming off potential overflow on the sides <br/>
        /// Only used if this exceeds the default spacing for buttons
        /// </summary>
        internal float buttonRackInnerWidthWithPadding = 0;
        internal int buttonCount = 0;

        /// <summary>
        /// Maximum button half width before the buttons are pulled closer together to avoid the button spilling out the sides <br/>
        /// If buttons, even at maximum compactness, still overflow, the buttons may still spill out the sides of the textbox
        /// </summary>
        internal const float BUTTON_SIDE_OVERFLOW_MAX = 50f;
        /// <summary>
        /// Miniumum padding between buttons if the buttons are too wide that they would otherwise overlap <br/>
        /// </summary>
        internal const float BUTTON_MIN_PADDING = 20f;


        public override void OnInitialize()
        {
            mainBox = new QuadrilateralBoxWithPortraitUI();
            mainBox.Origin = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            mainBox.OtherSideScaleUp = 1.7f;
            mainBox.OnLeftMouseDown += OnClickDialogueBox;

            mainBox.Wobbliness = 5.6f;
            mainBox.Dimensions = new Vector2(500, 100);

            mainBox.UsesDoubleOutline = true;
            mainBox.DoubleOutlineDistance = 5f;
            mainBox.DoubleOutlineThickness = 2f;
            mainBox.OutlineUpscalingAlgorithm = FablesUtils.UpscalePolygonByCombiningPerpendiculars;

            mainBox.AutoAdjustSizeToSetence = true;
            Append(mainBox);
        }

        /// <summary>
        /// Sets all the variables on the main textbox to what's provided in the textbox info, including setting up the portrait, the setence and click events <br/>
        /// Clears the previous buttons from the UI and adds the new ones if the textbox has any.
        /// </summary>
        /// <param name="info"></param>
        public void SetTextbox(TextboxInfo info)
        {
            //Portrait
            mainBox.Portrait = info.portrait;
            mainBox.PortraitAnimation = info.portraitMovement;
            mainBox.portraitAnimationTimer = 0f;

            //Setence
            mainBox.Setence = info.setence;
            mainBox.SetenceTimer = 0f;
            mainBox.SetenceSoundTimer = 0f;

            closesOnClick = info.closeOnClick;
            clickEvent = info.clickEvent;

            ResetButtons();
            if (info.buttons != null && info.buttons.Count > 0)
            {
                buttonCount = 0;

                foreach (ButtonInfo buttonInfo in info.buttons)
                {
                    QuadrilateralButtonUI button = buttonInfo.ConstructButton();
                    CoolDialogueUIManager._dialogueHandler.StyleButton(button, buttonCount);

                    Append(button);
                    totalButtonWidth += button.Dimensions.X / uiScale;

                    if (buttonCount == 0 || buttonCount == info.buttons.Count - 1) //Outer buttons can get a redution of half their width, with a cap on it
                        buttonRackInnerWidth += (button.Dimensions.X - Math.Min(button.Dimensions.X / 2f, BUTTON_SIDE_OVERFLOW_MAX)) / uiScale;
                    else
                        buttonRackInnerWidth += button.Dimensions.X / uiScale; // Inner buttons use their full width (duh)

                    buttonCount++;
                }

                totalButtonWidth += (buttonCount - 1) * BUTTON_MIN_PADDING;
                buttonRackInnerWidthWithPadding = buttonRackInnerWidth + (buttonCount - 1) * BUTTON_MIN_PADDING;
            }
        }

        /// <summary>
        /// Removes the button UI elements. Called when changing textboxes with <see cref="SetTextbox(TextboxInfo)"/> before the new ones are added
        /// </summary>
        public void ResetButtons()
        {
            totalButtonWidth = 0f;
            buttonRackInnerWidthWithPadding = 0f;
            buttonRackInnerWidth = 0f;

            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                if (Elements[i] is QuadrilateralButtonUI)
                {
                    Elements.RemoveAt(i);
                }
            }
        }


        /// <summary>
        /// Makes the buttons on the current dialogue menu recalculate their dimensions based on the text they're displaying. <br/> 
        /// Use this if you changed the text on the buttons. <br/>
        /// This is currently called right before <see cref="SetTextbox(TextboxInfo)"/> which clears them right afterwards... what?
        /// </summary>
        public void RecalculateButtons()
        {
            foreach (UIElement element in Elements)
            {
                if (element is QuadrilateralButtonUI button)
                    button.Recalculate();
            }
        }


        /// <summary>
        /// Makes the things happen when clicking on the main dialogue box itself. <br/>
        /// If the textbox's text hasn't fully scrolled by, automatically finish scrolling the text and make the buttons pop in at full opacity immediately <br/>
        /// Otherwise, call the click events or closes the textbox depending on what the current <see cref="TextboxInfo"/> says
        /// </summary>
        internal void OnClickDialogueBox(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);

            //Fast forward
            if (mainBox.Setence != null && mainBox.SetenceTimer < mainBox.Setence.maxProgress)
            {
                mainBox.SetenceTimer = mainBox.Setence.maxProgress;
                if (mainBox.Setence.voice != null)
                    SoundEngine.PlaySound(mainBox.Setence.voice.endSyllables);

                //Instantly make all buttons appear
                if (HasButtons)
                {
                    foreach (UIElement element in Elements)
                    {
                        if (element is QuadrilateralButtonUI button)
                            button.Opacity = 1f;
                    }
                }
                return;
            }

            bool closesOnClickBackup = closesOnClick;

            if (clickEvent != null)
                clickEvent();

            if (closesOnClickBackup)
                CoolDialogueUIManager.HideDialogueMenu();
        }

        /// <summary>
        /// Makes the things happen when clicking on the dialogue button. <br/>
        /// Calls the click events or closes the textbox depending on what the associated <see cref="ButtonInfo"/> says
        /// </summary>
        internal static void OnClickDialogueButton(UIMouseEvent evt, UIElement listeningElement)
        {
            if (listeningElement is not QuadrilateralButtonUI button)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);

            bool closesOnClickBackup = button.CloseUIOnClick;

            if (button.clickEvent != null)
                button.clickEvent();

            previousPreviousMouseLeft = true;

            if (closesOnClickBackup)
                CoolDialogueUIManager.HideDialogueMenu();
        }

        public override void OnActivate()
        {
            MoveTextboxAboveAnchorNPC();
            base.OnActivate();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //Aligns the buttons with the buttom edge of the textbox
            if (HasButtons)
            {
                float upRotation = mainBox.DisplacedVertices[2].DirectionTo(mainBox.DisplacedVertices[3]).ToRotation();
                

                Vector2 tangent = mainBox.DisplacedVertices[2].DirectionTo(mainBox.DisplacedVertices[3]);
                Vector2 edgeCenter = Vector2.Lerp(mainBox.DisplacedVertices[2], mainBox.DisplacedVertices[3], 0.5f);

                //Single button is centered, no biggie
                if (buttonCount == 1 && Elements[1] is QuadrilateralButtonUI singleButton)
                    UpdateButton(singleButton, upRotation, edgeCenter);

                else
                {
                    // Width of the bottom of the UI
                    float buttonRackWidth = mainBox.DisplacedVertices[2].Distance(mainBox.DisplacedVertices[3]) * 0.5f;
                    // If our buttons don't fit inside, we have to widen the rack to fit in all the buttons
                    float usedRackWidth = Math.Max(buttonRackWidth, buttonRackInnerWidthWithPadding * uiScale);


                    float emptySpaceLeft = usedRackWidth - buttonRackInnerWidth * uiScale; // Space we have leftover after the buttons are placed
                    float buttonSpacing = Math.Max(BUTTON_MIN_PADDING * uiScale, emptySpaceLeft / (buttonCount - 1));
                    Vector2 edgeLeft = edgeCenter - tangent * usedRackWidth / 2f;
                    Vector2 buttonPos = edgeLeft;
                    int index = 0;

                    foreach (UIElement element in Elements)
                    {
                        if (element is QuadrilateralButtonUI button)
                        {
                            // Adjust for overflow being restricted if too large. If the half-width is lower than the max overflow, this doesn't have any effect
                            if (index == 0)
                                buttonPos += tangent * Math.Max(0, button.Dimensions.X / 2f - BUTTON_SIDE_OVERFLOW_MAX * uiScale);

                            // Center button origin
                            else
                                buttonPos += tangent * button.Dimensions.X / 2f;

                            UpdateButton(button, upRotation, buttonPos);

                            buttonPos += tangent * (button.Dimensions.X / 2f + buttonSpacing);
                            index++;
                        }
                    }

                }
            }

            bool anyElementHovered = false;

            //Check for hover
            foreach (UIElement element in Elements)
            {
                if (element.IsMouseHovering)
                {
                    Main.LocalPlayer.cursorItemIconEnabled = false;
                    Main.LocalPlayer.mouseInterface = true;
                    anyElementHovered = true;
                    break;
                }
            }

            // Close menu if the player clicks off the UI
            if (!anyElementHovered && Main.mouseLeft && !Main.playerInventory && !previousPreviousMouseLeft)
            {
                CoolDialogueUIManager.HideDialogueMenu();
                return;
            }

            // Update position
            MoveTextboxAboveAnchorNPC();
        }

        public void UpdateButton(QuadrilateralButtonUI button, float rotation, Vector2 origin)
        {
            button.Origin = origin;
            button.Rotation = rotation;
            button.Origin += (rotation + MathHelper.PiOver2).ToRotationVector2() * 8f; //Shift button down a little

            //Fade the button progressively in if the text has been fully read through
            if (mainBox.ExpansionTimer >= 1 && (mainBox.Setence == null || (mainBox.SetenceTimer >= mainBox.Setence.maxProgress)))
                button.Opacity += 0.05f;
        }

        /// <summary>
        /// Handles the inworld positionning of the textbox, alongside the textbox rotation and keeping track of the player's side to help the UI quadrilateral box get its swaggy smooth trapezoidal deformation <br/>
        /// Updates the textbox's position to stay floating over the anchor NPC, and dissapears if the NPC dies or dissapears for any reason<br/>
        /// Closes the textbox if the player walks too far away
        /// </summary>
        public void MoveTextboxAboveAnchorNPC()
        {
            //Close if the npc is dead or null somehow
            if (anchorNPC == null || !anchorNPC.active)
            {
                anchorNPC = null;
                CoolDialogueUIManager.HideDialogueMenu();
                return;
            }
            
            //Close if player too far away
            if (Main.LocalPlayer.Distance(anchorNPC.Center) > 500)
            {
                anchorNPC = null;
                CoolDialogueUIManager.HideDialogueMenu();
                return;
            }

            float offsetAboveNPC = 160f * uiScale;
            Vector2 textboxCenterPosition = anchorNPC.Center - Vector2.UnitY * offsetAboveNPC;

            //Shifts the dialogue box opposite to the player depending on their relative position
            float progressLeftRight = FablesUtils.PolyInOutEasing((100f + Math.Clamp(anchorNPC.Center.X - Main.LocalPlayer.Center.X, -100f, 100f)) / 200f, 5f);
            textboxCenterPosition += MathHelper.Lerp(-100, 100, progressLeftRight) * Vector2.UnitX;

            Vector2 toTextBoxCenter = anchorNPC.Center.DirectionTo(textboxCenterPosition);
            toTextBoxCenter.X *= -1;
            mainBox.Rotation = (toTextBoxCenter.ToRotation() + MathHelper.PiOver2) * 0.2f;
            mainBox.PlayerSide = (anchorNPC.Center.X - Main.LocalPlayer.Center.X).NonZeroSign();

            mainBox.Origin = textboxCenterPosition - Main.screenPosition;

            if (Main.LocalPlayer.gravDir == -1)
            {
                mainBox.Origin = new Vector2(mainBox.Origin.X, Main.screenPosition.Y + (float)Main.screenHeight - (mainBox.Origin.Y + Main.screenPosition.Y));
            }
        }

        public Color SelectedButtonColor {
            get {
                if (!HasButtons)
                    return Color.Transparent;

                foreach (UIElement element in Elements)
                    if (element.IsMouseHovering && element is QuadrilateralButtonUI button)
                        return button.HoveredMainColor;

                return Color.Transparent;
            }
        }

        public void DrawButtonsPixelated(SpriteBatch spriteBatch)
        {
            foreach (UIElement element in Elements)
            {
                if (element is QuadrilateralButtonUI button)
                {
                    button.DrawPixelated(spriteBatch);
                }
            }
        }

        public void DrawButtonsLabels(SpriteBatch spriteBatch)
        {
            foreach (UIElement element in Elements)
            {
                if (element is QuadrilateralButtonUI button)
                {
                    button.DrawLabel(spriteBatch);
                }
            }
        }

        public void DrawButtonsDecals(SpriteBatch spriteBatch)
        {
            foreach (UIElement element in Elements)
            {
                if (element is QuadrilateralButtonUI button)
                {
                    button.DrawDecal(spriteBatch);
                }
            }
        }
    }
}
