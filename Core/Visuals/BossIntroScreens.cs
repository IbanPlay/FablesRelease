using ReLogic.Graphics;
using System.IO;
using Terraria.Localization;

namespace CalamityFables.Core
{
    public class BossIntroScreens : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ)
                return;

            FablesGeneralSystemHooks.DrawOverInterface += DrawIntroCards;
            FablesNPC.PostAIEvent += CheckForIntroCard;
        }


        public static BossIntroCard currentCard;

        public void DrawIntroCards()
        {
            if (debug_drawnCard != null)
                Debug_SaveIntroCardImages();

            if (currentCard != null)
            {
                Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default);
                currentCard.DrawCard();
                Main.spriteBatch.End();
            }
        }


        public static BossIntroCard debug_drawnCard;
        public static int debug_frameCounter = 0;
        public static RenderTarget2D debug_captureTarget;
        public void Debug_SaveIntroCardImages()
        {
            if (debug_drawnCard == null)
                return;

            //Create rendertarget
            if (debug_captureTarget is null)
                Main.QueueMainThreadAction(() => { debug_captureTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight); });

            if (debug_captureTarget is null || !RenderTargetsManager.SwitchToRenderTarget(debug_captureTarget))
                return;

            float previousGlobalTime = Main.GlobalTimeWrappedHourly;

            for (int i = 0; i < debug_drawnCard.maxTime; i++)
            {
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default);
                debug_drawnCard.DrawCard();
                Main.spriteBatch.End();

                string path = $"{Main.SavePath}/GreenScreen";
                Stream saveStream = File.OpenWrite(path + "/GreenScreen" + debug_drawnCard.bossNameFunction() + i.ToString() + ".png");

                debug_captureTarget.SaveAsPng(saveStream, Main.screenWidth, Main.screenHeight);
                saveStream.Dispose();

                debug_drawnCard.time++;
                Main.GlobalTimeWrappedHourly += (1 / 60f);
            }


            Main.GlobalTimeWrappedHourly = previousGlobalTime;
            debug_drawnCard = null;
            Main.graphics.GraphicsDevice.SetRenderTargets(null);
        }


        private void CheckForIntroCard(NPC npc)
        {
            if (!FablesConfig.Instance.BossIntroCardsActivated)
                return;

            //Plays intro card if the NPC has one and it hasnt played it already
            if (currentCard == null && npc.ModNPC != null && npc.ModNPC is IIntroCardBoss bossCardHaver && bossCardHaver.ShouldPlayIntroCard && !bossCardHaver.PlayedIntroCard)
            {
                currentCard = bossCardHaver.GetIntroCard;
                bossCardHaver.PlayedIntroCard = true;
            }
        }

        public override void PostUpdateEverything()
        {
            if (currentCard != null)
            {
                currentCard.time++;
                if (currentCard.time >= currentCard.maxTime)
                    currentCard = null;
            }
        }
    }

    public interface IIntroCardBoss
    {
        /// <summary>
        /// A setter/getter to know if the intro card has been played already
        /// Automatically set to true when the card is played
        /// </summary>
        public bool PlayedIntroCard { get; set; }

        /// <summary>
        /// The intro card of the boss
        /// </summary>
        public BossIntroCard GetIntroCard { get; }

        /// <summary>
        /// Should the intro card play?
        /// Used to delay the intro card when the boss spawns
        /// </summary>
        public bool ShouldPlayIntroCard => true;
    }

    public class BossIntroCard
    {
        public Func<string> bossNameFunction;
        public string bossTitle;
        public MusicTrackInfo? music;

        public int time;
        public int maxTime;
        public float Completion => time / (float)maxTime;

        public bool flipped;
        public float slant = 0.3f; //Slantedness of the bars
        public float shiftDown = 0.08f;

        public Color edgeColor;
        public Color titleColor;
        public Color nameColorChroma1;
        public Color nameColorChroma2;

        public DynamicSpriteFont font;
        public PrimitiveQuadrilateral kinoBars;


        public virtual void DrawCard()
        {
            //Points at top left and top right of the screen that go upwards
            Vector2 bottomLeftCorner = new Vector2(0, Main.screenHeight * slant);
            Vector2 bottomRightCorner = new Vector2(Main.screenWidth, Main.screenHeight * shiftDown);

            //Gets the base units to make up the rotated rectangle for the top bar
            Vector2 tangent = bottomLeftCorner.DirectionTo(bottomRightCorner);
            Vector2 perpendicular = tangent.RotatedBy(-MathHelper.PiOver2);

            //We need to push the bottom right corner (top right of the screen) further right, otherwise a small triangle on the top right corner of the screen will be left empty by the rect mesh
            //    |        /--------O  |
            // O--+-------/__________\_| 
            //  \ |                   \|  <- HERE
            //   \|         /----------O
            //    O--------/           |
            //    |                    |

            bottomRightCorner += tangent * Main.screenHeight * shiftDown;

            //Finsh the rectangle by adding the top parts
            Vector2 topLeftCorner = bottomLeftCorner + perpendicular * Main.screenHeight * 0.5f;
            Vector2 topRightCorner = bottomRightCorner + perpendicular * Main.screenHeight * 0.5f;
            Vector2[] corners = [topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner];

            //Flip the corners
            if (flipped)
            {
                FlipVerticesHorizontally(ref corners);
                tangent.Y *= -1;
                perpendicular.X *= -1;
            }
            kinoBars.Vertices = corners;

            //Slick appear and dissapear effect wowzers!
            float fadeInPercent = MathF.Pow(Utils.GetLerpValue(25, 0, time, true), 3f);
            float fadeOutPercent = MathF.Pow(Utils.GetLerpValue(maxTime - 35, maxTime, time, true), 4f);
            float totalFade = 1 - fadeInPercent - fadeOutPercent;

            //Make text scrolled more on smaller screens to hopefully let it take up a bit more space
            float defaultTextScrollPercent = 0.3f + Utils.GetLerpValue(1900, 1000, Main.screenWidth, true) * 0.05f;
            float textScrollPercent = defaultTextScrollPercent;
            textScrollPercent -= 0.5f * MathF.Pow(Utils.GetLerpValue(10f, 0f, time, true), 3f);
            textScrollPercent += 2.5f * MathF.Pow(Utils.GetLerpValue(maxTime - 15, maxTime, time, true), 3f);

            Effect effect = InitializeEffect(fadeInPercent, fadeOutPercent);

            //Upper half
            DrawKinoBarWithEdge(effect, 0.1f);
            if (music != null)
            {
                float scrollOffset = flipped ? 0.035f : 0.055f;
                Vector2 musicTextCenter = GetTextCenter(corners, textScrollPercent - scrollOffset, true);
                DrawOSTDetails(musicTextCenter, tangent, perpendicular, totalFade, textScrollPercent == defaultTextScrollPercent ? musicTextCenter : GetTextCenter(corners, defaultTextScrollPercent - scrollOffset, true));

            }

            //Lower half
            RotateVertices180(ref corners);
            DrawKinoBarWithEdge(effect, -0.1f);
            Vector2 textCenter = GetTextCenter(corners, textScrollPercent, false);
            DrawTitleAndName(textCenter, tangent, perpendicular, totalFade, textScrollPercent == defaultTextScrollPercent ? textCenter : GetTextCenter(corners, defaultTextScrollPercent, false));
        }

        public void DrawKinoBarWithEdge(Effect shader, float screenHeightPercent)
        {
            //Render the bar twice, once with an offset that makes it draw a little down so we get the cool colored edge behind the black part
            kinoBars.color = edgeColor;
            shader.Parameters["texturePercent"].SetValue(0.2f);
            kinoBars.RenderWithView(Matrix.Identity, shader, Vector2.UnitY * Main.screenHeight * screenHeightPercent);

            kinoBars.color = Color.Black;
            shader.Parameters["texturePercent"].SetValue(0.7f);
            kinoBars.RenderWithView(Matrix.Identity, shader, Vector2.Zero);
        }


        //https://media.discordapp.net/attachments/802291445360623686/1145178834371608576/image.png
        public Vector2 GetTextCenter(Vector2[] corners, float textScrollPercent, bool musicText)
        {
            Vector2 horizontalVector = corners[0] - corners[1];
            //Vector2 verticalMiddle = Vector2.Lerp(corners[2], corners[0], 0.1f); // <- Old code to find the vertical position of the text

            // 54 comes from 0.1 of the distance between the top and bottom of the bar, at my (iban's) scuffed windowed resolution
            // Since the bars are as tall as half the screen height, in 1920x1017, the bars are 508.5 pixels tall, and so 10% of that is 50.85

            // 51.408f Comes from the reference title size used in the previous code to offset the boss name down -> (titleSize.Y * 1.7f * fontScaling)
            // 81f Comes from the reference title size used to offset the song title -> (110f - titleSize.Y / titleScaleMultiplier * fontScaling * 3f * (1 - titleScaleMultiplier))
            //Actually lets move it higher

            float pixelsFromBarEdge = 50.85f + (musicText ? 130f : 51.408f);
            Vector2 verticalMiddle = corners[2].MoveTowards(corners[0], pixelsFromBarEdge);

            return verticalMiddle - horizontalVector * textScrollPercent;
        }

        public void DrawTitleAndName(Vector2 textCenter, Vector2 tangent, Vector2 perpendicular, float fade, Vector2 defaultTextOrigin)
        {
            float nameScaleMult = 6f * fontScaling;
            float titleScaleMult = 2f * fontScaling;
            string bossName = bossNameFunction();

            //Increase size on bigger monitors. 
            //Using screen height instead of width to account for ultra wide? idk
            if (Main.screenHeight > 1080)
            {
                nameScaleMult *= (Main.screenHeight / 1080f);
                titleScaleMult *= (Main.screenHeight / 1080f);
            }

            //padding from screen width so smaller screens have less padding
            float padding = 25f - Utils.GetLerpValue(1900, 1000, Main.screenWidth, true) * 15f;
            //move text a bit upwards on smaller screens to avoid it hugging the bottom edge of the screen
            textCenter.Y -= Utils.GetLerpValue(1900, 1000, Main.screenWidth, true) * 15f;

            float rotation = tangent.ToRotation();
            Vector2 namePosition = textCenter; //Name is below the title
            Vector2 titlePosition = textCenter + tangent * 23; //Align the title to be sliiiightly not perfectly aligned with the name. 23 taken from previous version that scaled it off boss name size

            Vector2 rawTitleSize = font.MeasureString(bossTitle);
            Vector2 titleSize = rawTitleSize * titleScaleMult;
            Vector2 rawNameSize = font.MeasureString(bossName);
            Vector2 nameSize = rawNameSize * nameScaleMult;

            // Title is partly overlapping with the boss name otherwise the spacing looks too odd. Might fuck up if its a title with no descender-characters (but that feels rare)
            Vector2 titleOrigin = new Vector2(0, rawTitleSize.Y * 0.85f);
            Vector2 nameOrigin = new Vector2(0, 0);

            //Draw text from the right if flipped
            //Doesn't apply to title because the title is matched to the right of the boss's name
            if (flipped)
                nameOrigin.X += rawNameSize.X;

            // The name is drawn from the top left, so find the bottom left (based on the default position)
            // The title is drawn from 85% of the way down, so find the bottom left from that (based on the default position)
            Vector2 nameBottomLeftCorner = defaultTextOrigin - perpendicular * nameSize.Y;
            Vector2 titleBottomLeftCorner = defaultTextOrigin - perpendicular * titleSize * 0.15f + tangent * 23;
            float tangentSlope = tangent.Y / tangent.X;

            if (!flipped)
            {
                //Use cringe math to figure out the Y coordinate at which the perpendicular crosses over the right edge of the screen so we know the max length of the name and title
                float tangentNameYintercept = nameBottomLeftCorner.Y - tangentSlope * nameBottomLeftCorner.X;
                float screenRightNameIntersectionY = tangentSlope * Main.screenWidth + tangentNameYintercept;

                float tangentTitleYintercept = titleBottomLeftCorner.Y - tangentSlope * nameBottomLeftCorner.X;
                float screenRightTitleIntersectionY = tangentSlope * Main.screenWidth + tangentTitleYintercept;

                //Max width is the distance from the bottom left of the text to the right edge of the screen, minus some padding. This way we can shrink it and guarantee it wont overflow
                float maxNameWidth = nameBottomLeftCorner.Distance(new Vector2(Main.screenWidth, screenRightNameIntersectionY)) - padding;
                if (nameSize.X > maxNameWidth)
                    nameScaleMult *= maxNameWidth / nameSize.X;

                //Idem for the title
                float maxTitleWidth = titleBottomLeftCorner.Distance(new Vector2(Main.screenWidth, screenRightTitleIntersectionY)) - padding;
                if (titleSize.X > maxTitleWidth)
                    titleScaleMult *= maxTitleWidth / titleSize.X;
            }
            else
            {
                //Use cringe math to figure out the Y coordinate at which the perpendicular crosses over the right edge of the screen so we know the max length of the name and title
                float tangentNameYintercept = nameBottomLeftCorner.Y - tangentSlope * nameBottomLeftCorner.X;
                float screennLeftNameIntersectionY = tangentNameYintercept;

                //Max width is the distance from the bottom right of the text to the left edge of the screen, minus some padding. This way we can shrink it and guarantee it wont overflow
                float maxNameWidth = nameBottomLeftCorner.Distance(new Vector2(0, screennLeftNameIntersectionY)) - padding;
                if (nameSize.X > maxNameWidth)
                    nameScaleMult *= maxNameWidth / nameSize.X;

                //This isn't actually the bottom left when flipped its the bottom right, but oh well!
                titlePosition = textCenter - tangent * (rawNameSize.X * nameScaleMult - 23);
                titleBottomLeftCorner = defaultTextOrigin - perpendicular * titleSize * 0.15f + tangent * (rawNameSize.X * nameScaleMult - 23);

                float tangentTitleYintercept = titleBottomLeftCorner.Y - tangentSlope * titleBottomLeftCorner.X;
                float screenLeftTitleIntersectionY = tangentTitleYintercept;

                //Idem for the title
                float maxTitleWidth = nameBottomLeftCorner.Distance(new Vector2(0, screenLeftTitleIntersectionY)) - padding;
                if (titleSize.X > maxTitleWidth)
                    titleScaleMult *= maxTitleWidth / titleSize.X;
            }

            //nameScaleMultiplier *= 1f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4);
            //titleScaleMultiplier *= 1f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4);

            for (int i = 0; i < 8; i++)
                Main.spriteBatch.DrawString(font, bossTitle, titlePosition + Vector2.UnitY.RotatedBy(rotation + i / 8f * MathHelper.TwoPi) * 4f, Color.Lerp(titleColor, Color.Black, 0.95f) * 0.3f * fade, rotation, titleOrigin, titleScaleMult, 0, 0);
            Main.spriteBatch.DrawString(font, bossTitle, titlePosition, titleColor * fade, rotation, titleOrigin, titleScaleMult, 0, 0);

            DrawTextWithBlur(bossName, namePosition, Color.White * fade, nameColorChroma1 * fade, nameColorChroma2 * fade, tangent, nameOrigin, nameScaleMult, 6, 40f);
        }

        public void DrawOSTDetails(Vector2 textCenter, Vector2 tangent, Vector2 perpendicular, float fade, Vector2 defaultTextOrigin)
        {
            Texture2D note = ModContent.Request<Texture2D>(AssetDirectory.UI + "BossIntroCardNote").Value;
            string songTitle = music.Value.songTitle;
            string composerName = "-" + music.Value.composer;
            float titleScaleMult = 2f * fontScaling;
            float nameScaleMult = 1.2f * fontScaling;
            float noteScaleMult = 1.8f - Utils.GetLerpValue(1000, 600, Main.screenHeight, true); //Shrink the note on smaller screens

            //Increase size on bigger monitors. 
            //Using screen height instead of width to account for ultra wide? idk
            if (Main.screenHeight > 1080)
            {
                nameScaleMult *= (Main.screenHeight / 1080f);
                titleScaleMult *= (Main.screenHeight / 1080f);
                noteScaleMult *= (Main.screenHeight / 1080f);
            }

            Vector2 rawTitleSize = font.MeasureString(songTitle);
            Vector2 titleSize = rawTitleSize * titleScaleMult;
            Vector2 rawNameSize = font.MeasureString(composerName);
            Vector2 nameSize = rawNameSize * nameScaleMult;

            //padding from screen width so smaller screens have less padding
            float padding = 40f;
            padding -= Utils.GetLerpValue(1900, 1000, Main.screenWidth, true) * 15f;
            float notePadding = 15f;
            //move text a bit downwards on smaller screens to avoid it hugging the bottom edge of the screen
            textCenter.Y += Utils.GetLerpValue(1000, 760f, Main.screenHeight, true) * 35f;

            //Rotation and slope from tangent
            float rotation = tangent.ToRotation();
            float tangentSlope = tangent.Y / tangent.X;

            //We squeeze the title and author less close together if theres descenders
            float authorOriginHeight = 0.35f;
            char[] hangers = "ypqgj".ToCharArray();
            if (songTitle.Any(c => hangers.Contains(c)))
                authorOriginHeight = 0.55f;

            //Title is moved to the left of the note so it doesnt overlap, with some extra padding, when drawn not flipped (from left to right)
            Vector2 titlePosition = textCenter;
            if (flipped)
                titlePosition += tangent * (note.Width * noteScaleMult + notePadding);

            // Title is drawn from approx the middle, to align with the center of the note
            Vector2 titleOrigin = new Vector2(0, rawTitleSize.Y * 0.35f);
            Vector2 nameOrigin = new Vector2(0, 0);

            //Draw text from the right if not flipped
            //Doesn't apply to name credits because the credits are matched to the right of the track's title
            if (!flipped)
                titleOrigin.X += rawTitleSize.X;


            //Same code as the bottom text, but we dont care about the bottom part of the text, instead about the top part (since that's what will actually collide with the border first 
            Vector2 titleDefaultPosition = defaultTextOrigin;

            if (flipped)
            {
                //Since we draw the text from the left when not flipped, we gotta push the text sideways to account for the note taking up space to our left
                titleDefaultPosition += tangent * (note.Width * noteScaleMult + notePadding);

                //Use cringe math to figure out the Y coordinate at which the perpendicular crosses over the right edge of the screen so we know the max length of the name and title
                float tangentTitleYintercept = titleDefaultPosition.Y - tangentSlope * titleDefaultPosition.X;
                float screenRightTitleIntersectionY = tangentSlope * Main.screenWidth + tangentTitleYintercept;

                //Max width is the distance from the bottom left of the text to the right edge of the screen, minus some padding. This way we can shrink it and guarantee it wont overflow
                float maxTitleWidth = titleDefaultPosition.Distance(new Vector2(Main.screenWidth, screenRightTitleIntersectionY)) - padding;
                if (titleSize.X > maxTitleWidth)
                    titleScaleMult *= maxTitleWidth / titleSize.X;

                //We gotta set it here after adjusting the title since the name is offset relative to the title
                Vector2 nameDefaultPosition = titleDefaultPosition - perpendicular * (rawTitleSize.Y * titleScaleMult * authorOriginHeight);
                float tangentNameYintercept = nameDefaultPosition.Y - tangentSlope * nameDefaultPosition.X;
                float screenRightNameIntersectionY = tangentSlope * Main.screenWidth + tangentNameYintercept;

                //Idem for the title
                float maxNameWidth = nameDefaultPosition.Distance(new Vector2(Main.screenWidth, screenRightNameIntersectionY)) - padding;
                if (nameSize.X > maxNameWidth)
                    nameSize *= maxNameWidth / nameSize.X;
            }
            else
            {
                //Use cringe math to figure out the Y coordinate at which the perpendicular crosses over the right edge of the screen so we know the max length of the name and title
                float tangentTitleYintercept = titleDefaultPosition.Y - tangentSlope * titleDefaultPosition.X;
                float screenLeftTitleIntersectionY = tangentTitleYintercept;

                //Max width is the distance from the bottom right of the text to the left edge of the screen, minus some padding. This way we can shrink it and guarantee it wont overflow
                float maxTitleWidth = titleDefaultPosition.Distance(new Vector2(0, screenLeftTitleIntersectionY)) - (padding + note.Width * noteScaleMult + notePadding);
                if (titleSize.X > maxTitleWidth)
                    titleScaleMult *= maxTitleWidth / titleSize.X;


                Vector2 nameDefaultPosition = titleDefaultPosition - perpendicular * (rawTitleSize.Y * titleScaleMult * authorOriginHeight);
                nameDefaultPosition -= tangent * (rawTitleSize.X * titleScaleMult);
                //For this one we measure from the top of the screen
                float tangentNameYintercept = nameDefaultPosition.Y - tangentSlope * nameDefaultPosition.X;
                float screenTopNameIntersectionX = -tangentNameYintercept / tangentSlope;

                //Idem for the title
                float maxNameWidth = nameDefaultPosition.Distance(new Vector2(screenTopNameIntersectionX, 0)) - padding;
                if (nameSize.X > maxNameWidth)
                    nameScaleMult *= maxNameWidth / nameSize.X;
            }


            //Set the name position at the end after having adjusted the title's size so we can align below it
            Vector2 namePosition = titlePosition - perpendicular * (rawTitleSize.Y * titleScaleMult * authorOriginHeight);
            if (!flipped)
                namePosition -= tangent * (rawTitleSize.X * titleScaleMult);

            Main.spriteBatch.DrawString(font, songTitle, titlePosition, Color.White * fade, rotation, titleOrigin, titleScaleMult, 0, 0);
            Main.spriteBatch.DrawString(font, composerName, namePosition, Color.Gray * fade, rotation, nameOrigin, nameScaleMult, 0, 0);

            Vector2 notePosition = textCenter;
            Vector2 noteOrigin = new Vector2(0f, note.Height * 0.4f);
            if (!flipped)
            {
                notePosition -= tangent * (rawTitleSize.X * titleScaleMult + notePadding);
                noteOrigin.X = note.Width;
            }

            //Lower the note on smaller screensizes
            noteOrigin.Y -= note.Height * (0.2f * Utils.GetLerpValue(1000, 600, Main.screenHeight, true));

            Main.spriteBatch.Draw(note, notePosition + Vector2.One * 3f, null, Color.Lerp(Color.Black, nameColorChroma1, 0.4f) * fade, rotation, noteOrigin, noteScaleMult, 0, 0);
            Main.spriteBatch.Draw(note, notePosition, null, Color.White * fade, rotation, noteOrigin, noteScaleMult, 0, 0);
        }

        public void DrawTextWithBlur(string text, Vector2 position, Color textColor, Color fadeColor, Color fadeColor2, Vector2 tangent, Vector2 origin, float scale, int blurAmount, float blurWidth)
        {
            fadeColor = fadeColor with { A = 0 };
            fadeColor2 = fadeColor2 with { A = 0 };

            float rotation = tangent.ToRotation();

            for (int i = 0; i < blurAmount; i++)
            {
                float blurProgress = 1 - i / (float)blurAmount;

                Vector2 displace = tangent * MathF.Pow(blurProgress, 3f) * blurWidth;
                float opacity = MathF.Pow(1 - blurProgress, 1f);

                Main.spriteBatch.DrawString(font, text, position + displace, fadeColor * opacity, rotation, origin, scale, 0, 0);
                Main.spriteBatch.DrawString(font, text, position - displace, fadeColor2 * opacity, rotation, origin, scale, 0, 0);
            }
            Main.spriteBatch.DrawString(font, text, position, textColor, rotation, origin, scale, 0, 0);
        }

        public Effect InitializeEffect(float fadeInPercent, float fadeOutPercent)
        {
            Effect effect = Scene["BossIntroKinoBars"].GetShader().Shader;
            Vector2 scroll = new Vector2(-Main.GlobalTimeWrappedHourly, Main.GlobalTimeWrappedHourly * 3f);
            Vector2 stretch = new Vector2(0.04f, 0.07f);
            effect.Parameters["scroll"].SetValue(scroll * 0.5f);
            effect.Parameters["horizontalRepeats"].SetValue(stretch);

            effect.Parameters["tireScratch"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TireScratch").Value);
            effect.Parameters["fadeNoise"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "MilkyBlobNoise").Value);
            effect.Parameters["fadeOutPercent"].SetValue(fadeOutPercent * 2f);
            effect.Parameters["fadeInPercent"].SetValue(fadeInPercent * 2f);

            effect.Parameters["fadeStretch"].SetValue(new Vector2(0.3f, 0.9f));
            effect.Parameters["fadeScroll"].SetValue(new Vector2(-Main.GlobalTimeWrappedHourly, 0f));
            return effect;
        }

        public void FlipVerticesHorizontally(ref Vector2[] corners)
        {
            for (int i = 0; i < 4; i++)
                corners[i].X = Main.screenWidth - corners[i].X;
        }

        public void RotateVertices180(ref Vector2[] corners)
        {
            //Flip it all around
            for (int i = 0; i < 4; i++)
            {
                corners[i].X = Main.screenWidth - corners[i].X;
                corners[i].Y = Main.screenHeight - corners[i].Y;
            }
            kinoBars.Vertices = corners;
        }

        public float fontScaling;

        /// <param name="name">The boss's localization key</param>
        /// <param name="title">The boss's title</param>
        /// <param name="duration">How long does the effect last</param>
        /// <param name="flipped">Should the effect be flipped horizontally</param>
        /// <param name="edgeColor">Color of the edge of the black tire marks</param>
        /// <param name="titleColor">Color of the boss's title</param>
        /// <param name="nameColorChroma1">Color to the right of the boss's name</param>
        /// <param name="nameColorChroma2">Color to the left of the boss's name</param>
        public BossIntroCard(string localizationKey, int duration, bool flipped,
            Color edgeColor, Color titleColor, Color nameColorChroma1, Color nameColorChroma2)
        {
            bossNameFunction = () => Language.GetText("Mods.CalamityFables.BossIntroCards." + localizationKey + ".Name").Value;
            this.bossTitle = Language.GetText("Mods.CalamityFables.BossIntroCards." + localizationKey + ".Title").Value;

            kinoBars = new PrimitiveQuadrilateral(Color.Black);
            time = 0;
            maxTime = duration;

            this.flipped = flipped;

            this.edgeColor = edgeColor;
            this.titleColor = titleColor;
            this.nameColorChroma1 = nameColorChroma1;
            this.nameColorChroma2 = nameColorChroma2;
            font = FontAssets.DeathText.Value;
            fontScaling = 0.42f;
        }

        /// <param name="duration">How long does the effect last</param>
        /// <param name="flipped">Should the effect be flipped horizontally</param>
        /// <param name="edgeColor">Color of the edge of the black tire marks</param>
        /// <param name="titleColor">Color of the boss's title</param>
        /// <param name="nameColorChroma1">Color to the right of the boss's name</param>
        /// <param name="nameColorChroma2">Color to the left of the boss's name</param>
        public BossIntroCard(Func<string> bossNameFunction, string bossTitle, int duration, bool flipped,
            Color edgeColor, Color titleColor, Color nameColorChroma1, Color nameColorChroma2)
        {
            this.bossNameFunction = bossNameFunction;
            this.bossTitle = bossTitle;

            kinoBars = new PrimitiveQuadrilateral(Color.Black);
            time = 0;
            maxTime = duration;

            this.flipped = flipped;

            this.edgeColor = edgeColor;
            this.titleColor = titleColor;
            this.nameColorChroma1 = nameColorChroma1;
            this.nameColorChroma2 = nameColorChroma2;
            font = FontAssets.DeathText.Value;
            fontScaling = 0.42f;
        }
    }

    public struct MusicTrackInfo
    {
        public string songTitle;
        public string composer;

        public MusicTrackInfo(string title, string musician)
        {
            songTitle = title;
            composer = musician;
        }
    }
}