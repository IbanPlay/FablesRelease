namespace CalamityFables.Cooldowns
{
    public class CooldownHandler
    {
        public CooldownInstance instance;

        #region Gameplay Behavior
        /// <summary>
        /// This method runs once every frame while the cooldown instance is active.
        /// </summary>
        public virtual void Tick() { }

        /// <summary>
        /// This method runs when the cooldown instance ends naturally.<br/>
        /// It is not called if the cooldown instance is deleted because the player died.
        /// </summary>
        public virtual void OnCompleted() { }

        /// <summary>
        /// Determines whether the cooldown instance can currently tick down.<br/>
        /// For example, this is useful for cooldowns that don't tick down if there are any bosses alive.<br/>
        /// You can also use it so that cooldowns which persist through death do not tick down while the player is dead.
        /// </summary>
        public virtual bool CanTickDown => true;

        /// <summary>
        /// Set this to true to make this cooldown remain even when the player dies.<br/>
        /// All cooldowns with PersistsThroughDeath set to false disappear immediately when the player dies.
        /// </summary>
        public virtual bool PersistsThroughDeath => false;

        /// <summary>
        /// Set this to true to make this cooldown persist through saves and loads.<br/>
        /// All cooldowns with SavedWithPlayer set to true are serialized into the modded player file.
        /// </summary>
        public virtual bool SavedWithPlayer => true;

        /// <summary>
        /// If this cooldown should be synced to all clients in multiplayer
        /// </summary>
        public virtual bool MultiplayerSynced => true;

        /// <summary>
        /// When the cooldown instance ends, this sound is played. Leave at <b>null</b> for no sound.
        /// </summary>
        public virtual SoundStyle? EndSound => null;
        #endregion

        #region Display & Rendering
        /// <summary>
        /// The name of the cooldown instance, appears when the player hovers over the indicator
        /// </summary>
        public virtual string LocalizationKey => "";

        /// <summary>
        /// Whether or not this cooldown instance should appear in the cooldown rack UI.
        /// </summary>
        public virtual bool ShouldDisplay => true;

        /// <summary>
        /// The texture of the cooldown indicator.<br/>
        /// <b>These must be 20x20 pixels when at 2x2 scale.</b>
        /// </summary>
        public virtual string Texture => "";
        /// <summary>
        /// This texture is overlaid atop the cooldown when the cooldown rack is rendering in compact mode.
        /// </summary>
        public virtual string OverlayTexture => $"{Texture}Overlay";
        /// <summary>
        /// This outline texture is rendered around the base icon texture.
        /// </summary>
        public virtual string OutlineTexture => $"{Texture}Outline";

        internal static string DefaultChargeBarTexture = "CalamityFables/Cooldowns/BarBase";

        public virtual float AdjustedCompletion => 1 - instance.Completion;

        /// <summary>
        /// The color used for the background of the cooldown slot
        /// </summary>
        public virtual Color BackgroundColor => new Color(57, 4, 7);
        /// <summary>
        /// The color used for edge of the background of the cooldown slot
        /// </summary>
        public virtual Color BackgroundEdgeColor => new Color(85, 27, 28);

        /// <summary>
        /// The color used for the icon outline when rendering in expanded mode.<br/>
        /// In compact mode, this color is used for the overlay that goes above the icon.
        /// </summary>
        public virtual Color OutlineColor => Color.White;
        /// <summary>
        /// The color used as the top of the gradient look.<br/>
        /// This color is only used when drawing in expanded mode and is ignored if a charge bar texture is provided.
        /// </summary>
        public virtual Color GradientTopColor => Color.Gray;
        /// <summary>
        /// The color used for the end of the circular cooldown timer rendered by shaders.<br/>
        /// This color is only used when drawing in expanded mode and is ignored if a charge bar texture is provided.
        /// </summary>
        public virtual Color GradientBotColor => Color.White;

        /// <summary>
        /// The color used as the top of the gradient look.<br/>
        /// This color is only used when drawing in expanded mode and is ignored if a charge bar texture is provided.
        /// </summary>
        public virtual Color HighlightColor => Color.White;
        /// <summary>
        /// The color used for the end of the circular cooldown timer rendered by shaders.<br/>
        /// This color is only used when drawing in expanded mode and is ignored if a charge bar texture is provided.
        /// </summary>
        public virtual Color GradientOutlineColor => Color.DarkGray;

        public virtual void PostDraw(SpriteBatch spriteBatch, Vector2 centerPosition, float opacity, bool compact = false) { }

        public virtual bool DrawTimer => true;

        /// <summary>
        /// This method is called to render the cooldown when the cooldown rack is in compact mode.
        /// </summary>
        public virtual void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;
            Color outlineColor = OutlineColor;

            //Draw the outline
            spriteBatch.Draw(outline, position, null, outlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            //Draw the icon
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            //Draw the small overlay
            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, outlineColor * opacity * 0.9f, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Renders the circular cooldown timer with a radial shader.<br/>
        /// If the charge bar texture is defined, it is used. Otherwise it renders a flat ring which slides from the start color to the end color.
        /// </summary>
        public virtual void UpdateShaderParameters(Effect effect)
        {
            effect.Parameters["completion"].SetValue(AdjustedCompletion);
            effect.Parameters["outlineColor"].SetValue(GradientOutlineColor.ToVector3());
            effect.Parameters["highlightColor"].SetValue(HighlightColor.ToVector3());
            effect.Parameters["gradientTopColor"].SetValue(GradientTopColor.ToVector3());
            effect.Parameters["gradientBottomColor"].SetValue(GradientBotColor.ToVector3());
        }

        public virtual void ModifyTextDrawn(ref string text, ref Vector2 position, ref Color textColor, ref float scale) { }
        #endregion
    }
}
