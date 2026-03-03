using Terraria.Localization;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class KeepsakeRackUI : SmartUIState
    {
        public static readonly SoundStyle InteractWithKeepsakeSound = new("CalamityFables/Sounds/KeepsakeMenuInteract") { PitchVariance = 0.4f };
        public static readonly SoundStyle ObtainNewKeepsakeSound = new("CalamityFables/Sounds/KeepsakeGetNew");

        public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory")) + 1;

        public static LocalizedText tabName;
        public static LocalizedText closeTabName;

        public override bool Visible {
            get {
                bool visible = Main.playerInventory && Main.EquipPage == 2;

                if (visible && Main.LocalPlayer.CollectedKeepsakes().Count == 0)
                    visible = false;

                if (!visible && Rack != null)
                {
                    Rack.Open = false;
                    Rack.ChangeTimer = 0;

                    Rack.Left.Set(-162, 1);
                    Rack.Width.Set(20, 0f);
                    Rack.Recalculate();
                }

                return visible;
            }
        }

        private KeepsakeRack Rack;
        private static float RackTop
        {
            get
            {
                float value = 176;
                if (Main.ShouldPVPDraw)
                    value += 130;
                //Shift to account for barrier dye
                else if (CalamityFables.SLREnabled)
                    value += 48;

                //Shift to account for backpack slot
                if (CalamityFables.SpiritEnabled)
                    value += 50;

                return value;
            }
        }
        public const float IndividualKeepsakeWidth = 33f;

        public static bool NewKeepsakeCheckItOut = false;

        public override void OnInitialize()
        {
            Rack = new KeepsakeRack();

            Rack.Left.Set(-162, 1);
            Rack.Top.Set(RackTop, 0);

            Rack.Height.Set(90, 0f);
            Rack.Width.Set(20, 0f);
            Append(Rack);

            Rack.Bracket = new KeepsakeRackOpeningBracket();
            Rack.Bracket.Width.Set(20, 0f);
            Rack.Bracket.Height.Set(90, 0f);
            Rack.Append(Rack.Bracket);

            tabName = CalamityFables.Instance.GetLocalization("Keepsakes.TabNames.Open");
            closeTabName = CalamityFables.Instance.GetLocalization("Keepsakes.TabNames.Close");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Rack.IsMouseHovering)
            {
                Main.LocalPlayer.cursorItemIconEnabled = false;
                Main.LocalPlayer.mouseInterface = true;
            }

            if (Rack.ChangeTimer > 0)
                Rack.ChangeTimer--;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;

            //Horizontal stuff
            int keepsakesCount = Rack.displayedKeepsakes == null ? player.CollectedKeepsakes().Count : Rack.displayedKeepsakes.Count;
            float fullWidth = keepsakesCount * IndividualKeepsakeWidth + 10;

            float expandedRackWidth = Math.Max(90, fullWidth); //Have a minimum width
            float expansion = (int)(expandedRackWidth * Rack.SlideProgress);
            Rack.Left.Set(-162 - expansion, 1);
            Rack.Width.Set(20 + expansion, 0f);

            //Vertical stuff.
            Rack.Top.Set(RackTop + AutoUISystem.MapHeight, 0);
            Rack.Height.Set(90, 0f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (Rack.Bracket.IsMouseHovering && Rack.ChangeTimer == 0)
            {
                string text = Rack.Open ? closeTabName.Value : tabName.Value;
                Vector2 pos = Main.MouseScreen + Vector2.One * 16;
                pos.X = Math.Min(Main.screenWidth - FontAssets.MouseText.Value.MeasureString(text).X - 6, pos.X);
                Utils.DrawBorderString(spriteBatch, text, pos, Main.MouseTextColorReal);
            }

            Recalculate();
        }
    }

    public struct KeepsakeDisplayData
    {
        public Vector2 velocity;
        public float rotation;

        public KeepsakeDisplayData(Vector2 velocity, float rotation)
        {
            this.velocity = velocity;
            this.rotation = rotation;
        }
    }

    internal class KeepsakeRack : UIElement
    {
        public KeepsakeRackOpeningBracket Bracket;
        public Dictionary<string, KeepsakeDisplayData> displayedKeepsakes;

        public bool Open;
        public int ChangeTimer;
        public const int ChangeTime = 10;

        public float SlideProgress {
            get {
                if (Open)
                    return 1 - FablesUtils.PolyInEasing(ChangeTimer / (float)ChangeTime, 2.5f);
                else
                    return FablesUtils.PolyInEasing(ChangeTimer / (float)ChangeTime, 2.5f);
            }
        }


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (displayedKeepsakes == null)
                ResetDisplayedKeepsakes();

            //If the player needs to check out a new keepsake and the menu is open, it means they DID check out the new keepsake
            if (Open && KeepsakeRackUI.NewKeepsakeCheckItOut)
                KeepsakeRackUI.NewKeepsakeCheckItOut = false;

            Rectangle dimensions = GetDimensions().ToRectangle();
            Texture2D bracketTex = ModContent.Request<Texture2D>(AssetDirectory.Keepsakes + "KeepsakesBracket").Value;
            Texture2D bracketHighlighTex = ModContent.Request<Texture2D>(AssetDirectory.Keepsakes + "KeepsakesBracketLight").Value;
            Texture2D barEndTex = ModContent.Request<Texture2D>(AssetDirectory.Keepsakes + "KeepsakesBracketEnd").Value;
            Texture2D barTex = ModContent.Request<Texture2D>(AssetDirectory.Keepsakes + "KeepsakesRack").Value;

            Vector2 bracketPosition = dimensions.TopLeft();
            string hoveredKeepsake = "";

            if (SlideProgress > 0)
            {
                DrawKeepsakes(spriteBatch, bracketPosition, bracketTex.Width / 2 - 2, out _, true);
                DrawSlidingBar(spriteBatch, bracketPosition, bracketTex.Width / 2 - 2, barEndTex, barTex);
                DrawKeepsakes(spriteBatch, bracketPosition, bracketTex.Width / 2 - 2, out hoveredKeepsake);

                if (ChangeTimer == 0)
                    SimulateDisplayedKeepsakes(Vector2.UnitX * 1f);
            }

            //Draw the main opening/closing bracket
            DrawMainButton(spriteBatch, bracketPosition, bracketTex, bracketHighlighTex);

            if (hoveredKeepsake != "")
            {
                string itemName = (string)Language.GetText("Mods.CalamityFables.Keepsakes.Names." + hoveredKeepsake);

                string textToDisplay = "[c/" + Language.GetText("Mods.CalamityFables.Keepsakes.NameColors." + hoveredKeepsake) + ":" + itemName + "]";
                textToDisplay += "\n" + Language.GetText("Mods.CalamityFables.Keepsakes.Descriptions." + hoveredKeepsake);

                FablesUtils.DrawTextInBox(spriteBatch, textToDisplay);
            }
        }

        public void DrawSlidingBar(SpriteBatch spriteBatch, Vector2 position, float mainButtonWidth, Texture2D barEndTex, Texture2D barTex)
        {
            position += Vector2.UnitX * mainButtonWidth;
            //6 pixels lower than the button
            position += Vector2.UnitY * 6;

            float width = Math.Max(0, GetDimensions().ToRectangle().Width - mainButtonWidth);

            //Non-shadowed frame
            Rectangle barFrame = barTex.Frame(2, 1, 1);
            spriteBatch.Draw(barTex, position, barFrame, Color.White, 0, Vector2.Zero, new Vector2(width / 2f, 1), SpriteEffects.None, 0);
            //Shadowed frame
            barFrame = barTex.Frame(2, 1, 0);
            spriteBatch.Draw(barTex, position, barFrame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            //Bar end
            Vector2 barEndPosition = GetDimensions().ToRectangle().TopRight() + Vector2.UnitY * 6 - Vector2.UnitX * barEndTex.Width;

            spriteBatch.Draw(barEndTex, barEndPosition, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
        }

        public void DrawKeepsakes(SpriteBatch spriteBatch, Vector2 position, float mainButtonWidth, out string hoveredKeepsake, bool backLayer = false)
        {
            hoveredKeepsake = "";

            //Prevents the keepsake from being drawn right into the button.
            float horizontalOffsetFromLeftEdge = mainButtonWidth + KeepsakeRackUI.IndividualKeepsakeWidth * 0.5f;

            //Hangs a bit below.
            float yPosition = position.Y + 18f;
            float xPosition = position.X;
            float xPositionOffset = horizontalOffsetFromLeftEdge;

            float keepsakeOpacityMult = 1f;
            if (SlideProgress < 0.2f)
                keepsakeOpacityMult = SlideProgress * 5f;

            foreach (string key in displayedKeepsakes.Keys)
            {
                Texture2D keepsakeTex = ModContent.Request<Texture2D>(AssetDirectory.Keepsakes + key).Value;
                Rectangle frame = keepsakeTex.Frame(4, 1, backLayer ? 1 : 0, sizeOffsetX: -2);
                Vector2 keepsakePosition = new Vector2(xPosition + xPositionOffset * SlideProgress, yPosition);
                float keepsakeRotation = displayedKeepsakes[key].rotation;

                //Draw keepsake
                spriteBatch.Draw(keepsakeTex, keepsakePosition, frame, Color.White * keepsakeOpacityMult, keepsakeRotation, new Vector2(frame.Width / 2, 2), 1, SpriteEffects.None, 0);

                //Hovered stuff
                Rectangle keepsakeHitbox = new Rectangle((int)(keepsakePosition.X - frame.Width / 2), (int)(keepsakePosition.Y - 2), frame.Width, frame.Height);
                if (!backLayer && ChangeTimer == 0 && keepsakeHitbox.Contains(Main.MouseScreen.ToPoint()))
                {
                    hoveredKeepsake = key;

                    frame = keepsakeTex.Frame(4, 1, 3, sizeOffsetX: -2);
                    spriteBatch.Draw(keepsakeTex, keepsakePosition, frame, Main.OurFavoriteColor * keepsakeOpacityMult, keepsakeRotation, new Vector2(frame.Width / 2, 2), 1, SpriteEffects.None, 0);

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        KeepsakeDisplayData data = displayedKeepsakes[key];
                        data.velocity = Vector2.UnitY * 4.7f * Main.rand.NextFloatDirection().NonZeroSign();
                        displayedKeepsakes[key] = data;
                        SoundEngine.PlaySound(KeepsakeRackUI.InteractWithKeepsakeSound);
                    }
                }

                xPositionOffset += KeepsakeRackUI.IndividualKeepsakeWidth;
            }
        }

        public void DrawMainButton(SpriteBatch spriteBatch, Vector2 position, Texture2D bracketTex, Texture2D bracketHighlightTex)
        {
            Rectangle bracketFrame = bracketTex.Frame(2, 2, Open ? 1 : 0, Bracket.IsMouseHovering ? 1 : 0, -2, -2);
            spriteBatch.Draw(bracketTex, position, bracketFrame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            //Draw a highlight over the button if the player has a new keepsake to check out
            if (KeepsakeRackUI.NewKeepsakeCheckItOut)
            {
                Color glowColor = new Color(71, 41, 0, 0);
                glowColor *= 0.75f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly);

                glowColor *= 1f + (float)Math.Pow(Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.5 + 0.5, 3f);

                spriteBatch.Draw(bracketHighlightTex, position, null, glowColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }
        }

        public void ResetDisplayedKeepsakes()
        {
            displayedKeepsakes = new Dictionary<string, KeepsakeDisplayData>();
            foreach (string key in Main.LocalPlayer.CollectedKeepsakes())
            {
                displayedKeepsakes[key] = new KeepsakeDisplayData(Vector2.Zero, -Main.rand.NextFloat(0.35f, 0.6f));
            }
        }

        public void SimulateDisplayedKeepsakes(Vector2 gravity)
        {
            foreach (string key in displayedKeepsakes.Keys)
            {
                KeepsakeDisplayData displayData = displayedKeepsakes[key];

                Vector2 endPoint = displayData.rotation.ToRotationVector2() * 60f;

                displayData.velocity += gravity;
                Vector2 newEndPoint = endPoint + displayData.velocity;
                displayData.rotation = newEndPoint.ToRotation();

                newEndPoint = displayData.rotation.ToRotationVector2() * 60f;

                displayData.velocity = (newEndPoint - endPoint) * 0.965f;

                displayedKeepsakes[key] = displayData;
            }
        }
    }

    internal class KeepsakeRackOpeningBracket : UIElement
    {
        public KeepsakeRack Rack => Parent as KeepsakeRack;

        public override void LeftClick(UIMouseEvent evt)
        {
            KeepsakeRack rack = Rack;

            if (rack.ChangeTimer != 0)
                return;

            SoundEngine.PlaySound(rack.Open ? SoundID.MenuClose : SoundID.MenuOpen);
            if (!rack.Open)
                rack.ResetDisplayedKeepsakes();

            rack.Open = !rack.Open;
            rack.ChangeTimer = KeepsakeRack.ChangeTime;
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            if (Rack.ChangeTimer != 0)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = SoundID.MenuTick.Volume * 0.5f });
            base.MouseOver(evt);
        }
    }
}
