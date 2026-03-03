using ReLogic.Graphics;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace CalamityFables.Content.UI
{
    public delegate Vector2 PortraitAnimationMovement(float timer);

    /// <summary>
    /// A box with text inside that has wobbly vertices, a portrait, and a nameplate for the talking character below the portrait.
    /// </summary>
    public class QuadrilateralBoxWithPortraitUI : QuadrilateralBoxUI
    {
        private PrimitiveQuadrilateral namePlateDrawer;
        public DialoguePortrait Portrait { get; set; }
        public DialoguePortrait FrontFacingPortrait { get; set; }

        #region portrait animation
        /// <summary>
        /// Timer that is increased whenever the portrait is visible. Used to drive the portrait's animation and more <br/>
        /// Timer resets to zero when a new textbox is selected, so displacement will happen multiple times when progressing through textboxes
        /// </summary>
        public float portraitAnimationTimer;

        public static Vector2 StaticPortrait(float timer) => Vector2.Zero;
        public static Vector2 ShakyPortrait(float timer) => timer >= 0.75f ? Vector2.Zero : -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * (float)Math.Pow(0.75f - timer, 2) * Main.rand.NextFloat(6f, 18f);


        private PortraitAnimationMovement _portraitAnimation;
        public PortraitAnimationMovement PortraitAnimation {
            get => _portraitAnimation ?? StaticPortrait;
            set => _portraitAnimation = value;
        }
        #endregion

        private LocalizedText _characterName;

        public LocalizedText CharacterName {
            get => _characterName;

            set {
                _characterName = value;
                RecalculateNameplateSize();
            }
        }

        private Vector2 NameSize;
        private float NameApparitionTimer { get; set; }

        public void Reset()
        {
            NameApparitionTimer = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            portraitAnimationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (NameSize == Vector2.Zero && CharacterName.Value != "")
                RecalculateNameplateSize();

            if (namePlateDrawer == null)
            {
                namePlateDrawer = new PrimitiveQuadrilateral(base.OutlineColor with { A = 255 });
            }

            SetNamePlateVertices();
        }

        public bool HoverSpecialVisuals => IsMouseHovering && ((Setence == null || Setence.maxProgress > SetenceTimer) || CoolDialogueUIManager.theUI.clickEvent != null);

        public override void OnActivate()
        {
            base.OnActivate();
            Reset();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            Reset();
        }

        public void RecalculateNameplateSize()
        {
            NameSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, CharacterName.Value, Vector2.One);
        }

        public void SetNamePlateVertices()
        {
            Vector2[] namePlateVertices = new Vector2[4];
            Vector2 left = DisplacedVertices[0];
            Vector2 right = DisplacedVertices[1];
            float leftToRightProgress = (SideWeight + 1) / 2f;
            Vector2 center = Vector2.Lerp(left, right, 0.2f + 0.6f * leftToRightProgress);

            float topRotation = Vertices[0].AngleTo(Vertices[1]);

            float width = (NameSize.X / 2f + 14) * CoolDialogueUI.uiScale;
            Vector2 unitVectorX = ((topRotation).ToRotationVector2() * width);
            Vector2 unitVectorY = (topRotation + MathHelper.PiOver2).ToRotationVector2();

            //Expansion
            unitVectorX *= 0.5f + 0.5f * FablesUtils.EaseInOutBack(Utils.GetLerpValue(0f, 0.7f, FablesUtils.SineInOutEasing(ExpansionTimer, 1), true), 1);
            unitVectorY *= 0.3f + 0.7f * FablesUtils.EaseInOutBack(Utils.GetLerpValue(0.3f, 1f, FablesUtils.SineInOutEasing(ExpansionTimer, 1), true), 1);

            int count = 0;

            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    float t = Utils.GetLerpValue(-j, j, SideWeight, true);
                    float multiplier = MathHelper.Lerp(1f, MathHelper.Lerp(OtherSideScaleUp, 1f, 0.3f), t);

                    if (i == -1)
                        namePlateVertices[count] = (center + unitVectorX * j + unitVectorY * 6f * i * CoolDialogueUI.uiScale);

                    else
                        namePlateVertices[count] = (center + unitVectorX * j + unitVectorY * (NameSize.Y) * i * multiplier * CoolDialogueUI.uiScale);


                    count++;
                }
            }

            namePlateDrawer.Vertices = namePlateVertices;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Timer == 0)
                return;
            spriteBatch.Draw(CrazyUIDrawingSystem.MainRenderTarget, Vector2.Zero, null, Color.White, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0);
        }

        #region drawing
        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (Timer == 0)
                return;

            if (drawer == null)
            {
                drawer = new PrimitiveQuadrilateral();
                drawer.Vertices = DisplacedVertices.ToArray();
            }

            if (outlineDrawerSquared == null && UsesDoubleOutline)
            {
                outlineDrawerSquared = new PrimitivePolygonOutline(4, DoubleOutlineThickness, OutlineColor, FablesUtils.UpscalePolygonByCombiningPerpendiculars);
                outlineDrawerSquared.Vertices = OutlineVertices.ToArray();
            }

            Color outlineColor = OutlineColor;
            Color backgroundColor = BackgroundColor;
            if (HoverSpecialVisuals)
            {
                outlineColor = HoveredOutlineColor;
                backgroundColor = HoveredBackgroundColor;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

            Effect effect = Scene["OutlinedBackgroundPrimitive"].GetShader().Shader;
            effect.Parameters["resolution"].SetValue(Dimensions);
            effect.Parameters["thickness"].SetValue((4f + 3f * FablesUtils.PolyInOutEasing(0.5f + 0.5f * (float)Math.Sin(Timer * 2f), 4)) * CoolDialogueUI.uiScale);
            effect.Parameters["backgroundColor"].SetValue(backgroundColor.ToVector4());
            effect.Parameters["outlineColor"].SetValue(outlineColor.ToVector4());

            effect.Parameters["time"].SetValue(Timer * 0.3f);
            effect.Parameters["outlineNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "IbansBrush").Value);

            //Actually it automatically uses the ui matrix???
            drawer.RenderWithView(Matrix.Identity, effect, Matrix.CreateTranslation(Vector3.Zero));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

            if (UsesDoubleOutline)
            {
                outlineDrawerSquared.color = OutlineColor;
                if (HoverSpecialVisuals)
                    outlineDrawerSquared.color = HoveredDoubleOutlineColor;

                outlineDrawerSquared.RenderWithView(Matrix.Identity, null, Matrix.CreateTranslation(Vector3.Zero));
            }

            if (namePlateDrawer != null && CharacterName != null && CharacterName.Value != "")
            {
                namePlateDrawer.color = outlineColor;
                namePlateDrawer.RenderWithView(Matrix.Identity, null, Matrix.CreateTranslation(Vector3.Zero));
            }
        }

        public void DrawUnpixelatedBackground(SpriteBatch spriteBatch)
        {
            Color outlineColor = OutlineColor;
            if (HoverSpecialVisuals)
                outlineColor = HoveredOutlineColor;

            if (Portrait != null)
            {
                Vector2 left = DisplacedVertices[0];
                Vector2 right = DisplacedVertices[1];
                float leftToRightProgress = (SideWeight + 1) / 2f;

                Vector2 portraitBase = Vector2.Lerp(left, right, 0.2f + 0.6f * leftToRightProgress) + PortraitAnimation(portraitAnimationTimer).RotatedBy(Rotation) * CoolDialogueUI.uiScale;
                float normal = left.DirectionTo(right).ToRotation();
                SpriteEffects flip = SideWeight.NonZeroSign() == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Texture2D portrait = Portrait.basePortait.Value;

                //Use the custom flipped sprite if we have any
                if (flip == SpriteEffects.FlipHorizontally && Portrait.basePortaitFlip != null)
                    portrait = Portrait.basePortaitFlip.Value;

                //use the front facing portrait if it exists and the textbox is centered enough
                if (Math.Abs(SideWeight) < 0.6f && FrontFacingPortrait != null)
                    portrait = FrontFacingPortrait.basePortait.Value;

                Vector2 origin = new Vector2(portrait.Width / 2, portrait.Height);

                if (drawer != null)
                {
                    Effect effect = Scene["Silouetteify"].GetShader().Shader;
                    effect.Parameters["recolor"].SetValue(outlineColor.ToVector4());

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect);

                    Vector2 displacement = (left.DirectionTo(right) * SideWeight * 5f + left.DirectionTo(right).RotatedBy(MathHelper.PiOver2) * 3f) * CoolDialogueUI.uiScale;
                    Main.spriteBatch.Draw(portrait, portraitBase + displacement, null, Color.White, normal, origin, 1.5f * CoolDialogueUI.uiScale, flip, 0);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
                }

                spriteBatch.Draw(portrait, portraitBase, null, Color.White, normal, origin, 1.5f * CoolDialogueUI.uiScale, flip, 0);

                //Draw any extra elements that don't have the silouette effect
                if (Portrait.nonSilouettedPortrait != null)
                {
                    portrait = Portrait.nonSilouettedPortrait.Value;

                    //Use the custom flipped sprite if we have any
                    if (flip == SpriteEffects.FlipHorizontally && Portrait.nonSilouettedPortraitFlip != null)
                        portrait = Portrait.nonSilouettedPortraitFlip.Value;

                    origin = new Vector2(portrait.Width / 2, portrait.Height);
                    spriteBatch.Draw(portrait, portraitBase, null, Color.White, normal, origin, 1.5f * CoolDialogueUI.uiScale, flip, 0);
                }
            }
        }

        public void DrawUnpixelatedForeground(SpriteBatch spriteBatch)
        {
            if (Timer == 0)
                return;

            if (Setence != null && ExpansionTimer >= 1)
            {
                Setence.Draw(SetenceTimer, Origin + new Vector2(-Dimensions.X / 2 + Wobbliness * 2f, -Dimensions.Y / 2 + Wobbliness * 2f).RotatedBy(Rotation) + Vector2.UnitY.RotatedBy(Rotation) * 10f, Rotation, CoolDialogueUI.uiScale);

                if (DialogueOverIcon != null && SetenceTimer >= Setence.maxProgress && CoolDialogueUIManager.theUI.clickEvent != null)
                {
                    Texture2D icon = DialogueOverIcon.Value;
                    Rectangle frame = new Rectangle(0, 0, icon.Width, icon.Height / 2 - 2);
                    if (IsMouseHovering)
                        frame.Y += icon.Height / 2;

                    Vector2 origin = frame.Size() / 2;

                    Vector2 topAlignment = Vector2.Lerp(Vertices[0], Vertices[1], 0.94f);
                    Vector2 botAlignment = Vector2.Lerp(Vertices[2], Vertices[3], 0.94f);

                    Vector2 drawPosition = Vector2.Lerp(topAlignment, botAlignment, 0.93f);

                    drawPosition.Y -= 10f * FablesUtils.SineInOutEasing((float)Math.Sin(Timer * 4f), 1) * CoolDialogueUI.uiScale;

                    spriteBatch.Draw(icon, drawPosition, frame, Color.White, Rotation, origin, 1f * CoolDialogueUI.uiScale, 0, 0);
                }
            }


            if (namePlateDrawer != null && CharacterName != null && CharacterName.Value != "")
            {
                if (ExpansionTimer >= 1)
                {
                    NameApparitionTimer += 0.04f;
                    if (NameApparitionTimer > 1f)
                        NameApparitionTimer = 1f;

                    Vector2 middleLeft = Vector2.Lerp(namePlateDrawer.Vertices[0], namePlateDrawer.Vertices[2], 0.5f);
                    Vector2 middleRight = Vector2.Lerp(namePlateDrawer.Vertices[1], namePlateDrawer.Vertices[3], 0.5f);

                    float rotation = middleLeft.AngleTo(middleRight);

                    Vector2 origin = Vector2.Lerp(namePlateDrawer.Vertices[0], namePlateDrawer.Vertices[2], 0.6f);
                    origin += rotation.ToRotationVector2() * 8f * CoolDialogueUI.uiScale;
                    origin += (rotation - MathHelper.PiOver2).ToRotationVector2() * NameSize.Y * 0.5f * CoolDialogueUI.uiScale;

                    Color nameColor = Color.Black;
                    if (namePlateDrawer.color.GetBrightness() < 0.3f)
                        nameColor = Color.White;

                    DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, CharacterName.Value, origin, nameColor * NameApparitionTimer, rotation, default, 1f * CoolDialogueUI.uiScale, SpriteEffects.None, 0f);
                }
            }
        }
        #endregion
    }
}
