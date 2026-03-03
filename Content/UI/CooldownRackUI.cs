using CalamityFables.Cooldowns;
using MonoMod.Cil;
using System.Reflection;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using static CalamityFables.Content.UI.CooldownRackUI;
using static Mono.Cecil.Cil.OpCodes;
using Terraria.ModLoader.IO;
using Terraria.Localization;

namespace CalamityFables.Content.UI
{
    public class CooldownRackUI : SmartUIState, ILoadable
    {
        #region Adding a setting option
        public void Load(Mod mod)
        {
            IL_IngameOptions.Draw += AddCooldownDisplaySetting;
            FablesGeneralSystemHooks.ClearWorldEvent += SaveSettingsChanges;
            FablesGeneralSystemHooks.SaveWorldDataEvent += SaveSettingsChanges;
        }


        public static MethodInfo SaveConfigMethod = typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic );

        private void AddCooldownDisplaySetting(ILContext il)
        {
            #region Increase the category size
            ILCursor cursor = new ILCursor(il);
            FieldInfo categoryField = typeof(IngameOptions).GetField("category", BindingFlags.Static | BindingFlags.Public);

            //SWITCH STATEMENT MOMENT (sob)
            /*
            ILLabel[] rightSideInitializationSwitchLabels = null;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(categoryField),
                i => i.MatchStloc(out int switchValue),
                i => i.MatchLdloc(switchValue),
                i => i.MatchSwitch(out rightSideInitializationSwitchLabels)
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the switch(category) statement");
                return;
            }

            //Category 1 = interface
            cursor.GotoLabel(rightSideInitializationSwitchLabels[1]);
            */

            int categorySizeLoc = 0;

            /*
            //Anchor point to identify the category size local var
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out int categorySizeLoc),
                i => i.MatchStsfld(typeof(UILinkPointNavigator.Shortcuts).GetField("INGAMEOPTIONS_BUTTONS_RIGHT"))
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the INGAMEOPTIONS_BUTTONS_RIGHT setting");
                return;
            }
            */

            //Initial setting of the category size to 11
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(11),
                i => i.MatchStloc(out categorySizeLoc)
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the  loading of 11");
                return;
            }

            //Addition of 1 by tml
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(categorySizeLoc),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(categorySizeLoc)
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the setting of the category size, addition by tml");
                return;
            }


            //Increase category size by one
            cursor.Emit(Ldloc, categorySizeLoc);
            cursor.Emit(Ldc_I4, 1);
            cursor.Emit(Add);
            cursor.Emit(Stloc, categorySizeLoc);
            #endregion


            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<Main>("mouseLeft"),
                i => i.MatchBrtrue(out _),
                i => i.MatchLdcI4(-1),
                i => i.MatchStsfld(typeof(IngameOptions).GetField("rightLock", BindingFlags.Static | BindingFlags.Public))
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the rightlock statement ");
                return;
            }

            ILLabel skipCategory1Label = null;
            if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdsfld(categoryField),
                i => i.MatchLdcI4(1),
                i => i.MatchBneUn(out skipCategory1Label)
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the category == 1 statement");
                return;
            }

            cursor.GotoLabel(skipCategory1Label, MoveType.AfterLabel);

            int optionsIndexVariable = 36;
            int anchorPositionVariable = 0;
            int offsetVariable = 0;
            int num4 = 0;
            int num5 = 0; //idk what these 2 variables are meant to be tbh
            int defaultColorVar = 0;

            //now we should be just after the optionsindex was increased. This is where we want to make our edit, but first we need to go back to retrieve variables

            if (!cursor.TryGotoPrev(MoveType.Before,
                i => i.MatchLdloc(out optionsIndexVariable),
                i => i.MatchLdcI4(1),
                i => i.MatchAdd(),
                i => i.MatchStloc(optionsIndexVariable)
                ))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the optionsIndex increase");
                return;
            }

            //THE BIG ONE
            if (!cursor.TryGotoPrev(MoveType.Before,
                i => i.MatchLdloc(optionsIndexVariable),
                i => i.MatchLdloc(out anchorPositionVariable),
                i => i.MatchLdloc(out offsetVariable),
                i => i.MatchLdsfld(typeof(IngameOptions).GetField("rightScale", BindingFlags.Static | BindingFlags.Public)),
                i => i.MatchLdloc(optionsIndexVariable),
                i => i.MatchLdelemR4(),
                i => i.MatchLdsfld(typeof(IngameOptions).GetField("rightScale", BindingFlags.Static | BindingFlags.Public)),
                i => i.MatchLdloc(optionsIndexVariable),
                i => i.MatchLdelemR4(),
                i => i.MatchLdloc(out num4),
                i => i.MatchSub(),
                i => i.MatchLdloc(out num5),
                i => i.MatchLdloc(num4),
                i => i.MatchSub(),
                i => i.MatchDiv(),
                i => i.MatchLdloca(out defaultColorVar),
                i => i.MatchInitobj<Color>(),
                i => i.MatchLdloc(defaultColorVar),
                i => i.MatchCall(typeof(IngameOptions).GetMethod("DrawRightSide", BindingFlags.Static | BindingFlags.Public))))
            {
                FablesUtils.LogILEpicFail("Add cooldown display to settings", "Could not locate the DrawRightSide statement");
                return;
            }
            /*
             * 
                
             */

            ILLabel skipHoverLabel = cursor.DefineLabel();

            //We go back to where we were before, and now we call DrwRightside
            cursor.GotoLabel(skipCategory1Label, MoveType.Before);
            cursor.Emit(Ldarg_1);
            cursor.EmitDelegate(GetSettingOptionName);
            cursor.Emit(Ldloc, optionsIndexVariable);
            cursor.Emit(Ldloc, anchorPositionVariable);
            cursor.Emit(Ldloc, offsetVariable);
            cursor.Emit(Ldsfld, typeof(IngameOptions).GetField("rightScale", BindingFlags.Static | BindingFlags.Public));
            cursor.Emit(Ldloc, optionsIndexVariable);
            cursor.Emit(Ldelem_R4);
            cursor.Emit(Ldsfld, typeof(IngameOptions).GetField("rightScale", BindingFlags.Static | BindingFlags.Public));
            cursor.Emit(Ldloc, optionsIndexVariable);
            cursor.Emit(Ldelem_R4);
            cursor.Emit(Ldloc, num4);
            cursor.Emit(Sub);
            cursor.Emit(Ldloc, num5);
            cursor.Emit(Ldloc, num4);
            cursor.Emit(Sub);
            cursor.Emit(Div);
            cursor.Emit(Ldloca, defaultColorVar);
            cursor.Emit(Initobj, typeof(Color));
            cursor.Emit(Ldloc, defaultColorVar);
            cursor.Emit(Call, typeof(IngameOptions).GetMethod("DrawRightSide", BindingFlags.Static | BindingFlags.Public));
            cursor.Emit(Brfalse_S, skipHoverLabel);

            cursor.Emit(Ldloc, optionsIndexVariable);
            cursor.Emit(Stsfld, typeof(IngameOptions).GetField("rightHover"));
            cursor.EmitDelegate(OnSettingHover);

            skipHoverLabel.Target = cursor.Next;
            cursor.Emit(Ldloc, optionsIndexVariable);
            cursor.Emit(Ldc_I4, 1);
            cursor.Emit(Add);
            cursor.Emit(Stloc, optionsIndexVariable);
        }

        public static string GetSettingOptionName()
        {
            string display = "";
            switch ((int)((FablesConfig.Instance.CooldownDisplay - 1) / 2))
            {
                case 0:
                    display = Language.GetTextValue("Mods.CalamityFables.Cooldowns.SettingOption.Fancy");
                    break;
                case 1:
                    display = Language.GetTextValue("Mods.CalamityFables.Cooldowns.SettingOption.Square");
                    break;
                case 2:
                    display = Language.GetTextValue("Mods.CalamityFables.Cooldowns.SettingOption.Compact");
                    break;
            }

            if (!ShowTimers)
                display += " 2";

            if (FablesConfig.Instance.CooldownDisplay == 0)
                display = Language.GetTextValue("Mods.CalamityFables.Cooldowns.SettingOption.Disabled");

            return Language.GetTextValue("Mods.CalamityFables.Cooldowns.SettingOption.Label") + display;
        }

        public static bool changedSettings = false;
        public static void OnSettingHover()
        {
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                FablesConfig.Instance.CooldownDisplay++;
                if (FablesConfig.Instance.CooldownDisplay >= 7)
                    FablesConfig.Instance.CooldownDisplay = 0;
                changedSettings = true;
            }
        }


        private void SaveSettingsChanges(TagCompound tag) => SaveSettingsChanges();
        private void SaveSettingsChanges()
        {
            if (changedSettings && SaveConfigMethod != null)
                SaveConfigMethod.Invoke(null, new object[] { FablesConfig.Instance });
        }


        public static bool ShowTimers => FablesConfig.Instance.CooldownDisplay % 2 == 1;
        #endregion

        public enum DisplayStyle
        {
            Fancy,
            Square,
            Compact,
            Hidden
        }

        public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));

        public override bool Visible => !Main.gameMenu && !Main.playerInventory && FablesConfig.Instance.CooldownDisplay > 0;

        private readonly CooldownRack Rack = new CooldownRack();

        /// <summary>
        /// The maximum number of cooldowns which can be drawn in expanded mode before the rack auto-switches to compact mode.
        /// </summary>
        public static int MaxLargeIcons = 10;
        public static bool CompactIcons {
            get {
                // Option 1: Always use compact icons if configured to do so.
                if (FablesConfig.Instance.CooldownDisplay > 4)
                    return true;

                // Option 2: If there are too many cooldowns, auto switch to compact mode.
                return Main.LocalPlayer.GetDisplayedCooldowns().Count > MaxLargeIcons;
            }
        }

        public static DisplayStyle SelectedStyle => FablesConfig.Instance.CooldownDisplay == 0 ? DisplayStyle.Hidden : CompactIcons ? DisplayStyle.Compact : (DisplayStyle)((int)((FablesConfig.Instance.CooldownDisplay - 1) / 2));

        public static DisplayStyle PreviousStyle;

        public static bool DebugFullDisplay = false;
        public static float DebugForceCompletion = 0f;

        public static float CooldownIconWidth => CompactIcons ? 36 : 56;
        public static float CooldownIconHeight {
            get {
                if (CompactIcons)
                    return 38;
                switch (SelectedStyle)
                {
                    case DisplayStyle.Fancy:
                        return 64;
                    default:
                        return 58;
                }
            }
        }
        public static float CooldownIconSpacing {
            get {
                if (CompactIcons)
                    return -2;
                switch (SelectedStyle)
                {
                    case DisplayStyle.Fancy:
                        return 2;
                    default:
                        return 10;
                }
            }
        }
        public static float CooldownRackTopSpacing {
            get {
                switch (SelectedStyle)
                {
                    case DisplayStyle.Fancy:
                        return 86;
                    default:
                        return 80;
                }
            }
        }



        public override void OnInitialize()
        {
            Rack.Left.Set(20, 0);
            Rack.Top.Set(100, 0);

            Rack.Height.Set(60, 0f);
            Rack.Width.Set(60, 0f);
            Append(Rack);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (PreviousStyle != SelectedStyle)
            {
                foreach (UIElement child in Rack.Children)
                {
                    child.Width.Set(CooldownIconWidth, 0);
                    child.Height.Set(CooldownIconHeight, 0);
                }
            }

            PreviousStyle = SelectedStyle;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;

            //Horizontal stuff
            float fullWidth = Rack.Children.Count() == 0 ? 0 : Rack.Children.Last().Left.Pixels + CooldownIconWidth;
            Rack.Left.Set(20, 0f);
            Rack.Width.Set(fullWidth, 0f);

            //Vertical stuff.
            int buffCount = Main.LocalPlayer.CountBuffs();
            if (FablesConfig.Instance.VanillaCooldownDisplay)
            {
                if (player.HasBuff(BuffID.PotionSickness))
                    buffCount--;
                if (player.HasBuff(BuffID.ChaosState))
                    buffCount--;
            }

            int buffRows = buffCount / 11 + (buffCount > 0 ? 1 : 0);

            Rack.Top.Set(CooldownRackTopSpacing + buffRows * 45, 0);
            Rack.Height.Set(60, 0f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            Recalculate();
        }
    }

    internal class CooldownRack : UIElement
    {
        public override void OnActivate()
        {
            base.OnActivate();
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public int RightmostDisplayIndex {
            get {
                for (int i = Children.Count() - 1; i >= 0; i--)
                {
                    if (Children.ElementAt(i) is CooldownDisplay display && !display.dissapearing)
                        return i;
                }

                return 0;
            }
        }
        public float IdealOffsetAtIndex(int displayIndex) => displayIndex * (CooldownIconWidth + CooldownIconSpacing);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            IList<CooldownInstance> cooldownsToDraw = Main.LocalPlayer.GetDisplayedCooldowns();
            foreach (CooldownInstance cooldown in cooldownsToDraw)
            {
                if (!Children.Any(i => i is CooldownDisplay d && d.handler.instance == cooldown))
                {
                    CooldownDisplay newDisplay = new CooldownDisplay(cooldown);
                    newDisplay.Left.Set(IdealOffsetAtIndex(Children.Count()), 0f);
                    Append(newDisplay);
                    newDisplay.Initialize();
                }
            }

            for (int i = 0; i < Children.Count(); i++)
            {
                if (Children.ElementAt(i) is CooldownDisplay display)
                {
                    if (display.dissapearOpacity <= 0)
                    {
                        display.Remove();
                        i--;
                    }
                    else
                        display.idealLeft = IdealOffsetAtIndex(display.previousIndex);
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            CooldownDisplay hoveredDisplay;
            if (CompactIcons)
                DrawCompact(spriteBatch, out hoveredDisplay);
            else
                DrawFull(spriteBatch, out hoveredDisplay);

            if (hoveredDisplay != null)
            {
                Color boxColor = Color.Lerp(new Color(25, 20, 55), hoveredDisplay.handler.HighlightColor, 0.2f) * 0.75f;
                FablesUtils.DrawTextInBox(spriteBatch, Language.GetTextValue("Mods.CalamityFables.Cooldowns." + hoveredDisplay.handler.LocalizationKey), boxColor);
            }
        }

        public void DrawFull(SpriteBatch spriteBatch, out CooldownDisplay hoveredDisplay)
        {
            hoveredDisplay = null;

            //Draw bgs
            foreach (UIElement child in Children)
            {
                if (child is CooldownDisplay display)
                {
                    display.DrawBackground(spriteBatch);

                }
            }

            Effect barShaderEffect = Scene["CooldownCompletion"].GetShader().Shader;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, barShaderEffect, Main.UIScaleMatrix);

            foreach (UIElement child in Children)
            {
                if (child is CooldownDisplay display)
                {
                    display.DrawCompletion(spriteBatch, barShaderEffect);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            foreach (UIElement child in Children)
            {
                if (child is CooldownDisplay display)
                {
                    display.DrawIcon(spriteBatch);

                    if (child.IsMouseHovering)
                        hoveredDisplay = display;
                }
            }

            if (CooldownRackUI.ShowTimers)
            {
                foreach (UIElement child in Children)
                {
                    if (child is CooldownDisplay display)
                    {
                        if (display.handler.DrawTimer)
                            display.DrawText(spriteBatch, false);
                    }
                }
            }
        }

        public void DrawCompact(SpriteBatch spriteBatch, out CooldownDisplay hoveredDisplay)
        {
            hoveredDisplay = null;

            //Draw everything at once
            foreach (UIElement child in Children)
            {
                if (child is CooldownDisplay display)
                {
                    display.DrawCompact(spriteBatch);

                    if (child.IsMouseHovering)
                        hoveredDisplay = display;


                }
            }

            if (CooldownRackUI.ShowTimers)
            {
                foreach (UIElement child in Children)
                {
                    if (child is CooldownDisplay display)
                    {
                        if (display.handler.DrawTimer)
                            display.DrawText(spriteBatch, true);
                    }
                }
            }
        }
    }

    internal class CooldownDisplay : UIElement
    {
        public CooldownHandler handler;

        public bool dissapearing = false;
        public float dissapearOpacity = 1;

        public float opacity = 1f;
        public float DrawOpacity => opacity * dissapearOpacity;

        #region Textures
        public static Asset<Texture2D> FrameTexture;
        public static Asset<Texture2D> BackgroundTexture;
        public static Asset<Texture2D> BackgroundEdgeTexture;
        public static Asset<Texture2D> CompletionTexture;

        //Compact
        public static Asset<Texture2D> CompactFrameTexture;
        public static Asset<Texture2D> CompactConnectedFrameTexture;
        public static Asset<Texture2D> CompactBackgroundTexture;
        public static Asset<Texture2D> CompactBackgroundEdgeTexture;

        //Fancy
        public static Asset<Texture2D> FancyBackgroundTexture;
        public static Asset<Texture2D> FancyBackgroundEdgeTexture;
        public static Asset<Texture2D> FancyCompletionTexture;
        public static Asset<Texture2D> FancyFrameStartTexture;
        public static Asset<Texture2D> FancyFrameLeftTexture;
        public static Asset<Texture2D> FancyFrameRightTexture;
        public static Asset<Texture2D> FancyFrameEndTexture;
        #endregion

        public float motionTimer = 0f;
        public int previousIndex;
        public float previousLeft;
        public float idealLeft;

        public bool previouslyFancyRight;
        public bool previouslyFancyLeft;

        public CooldownDisplay(CooldownInstance cooldown)
        {
            handler = cooldown.handler;
        }

        public int Index {
            get {
                if (Parent == null)
                    return 0;

                int index = 0;
                foreach (UIElement child in Parent.Children)
                {
                    if (child is CooldownDisplay display)
                    {
                        if (display == this)
                            return index;

                        if (!display.dissapearing)
                            index++;
                    }
                }
                return index;
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();
        }

        public override void OnInitialize()
        {
            Height.Set(CooldownIconHeight, 0);
            Width.Set(CooldownIconWidth, 0);
            previousIndex = Index;
            opacity = 0.8f;
            Recalculate();
        }

        public int IndexFromLeft => (int)(Left.Pixels / (CooldownIconWidth + CooldownIconSpacing));

        public override void Update(GameTime gameTime)
        {
            //Fading away
            if (dissapearing)
            {
                dissapearOpacity -= 1 / (60f * 0.6f);
                return;
            }

            //If the handler is done drawing, fade off
            if (handler.instance.timeLeft < 0 ||
                !Main.LocalPlayer.GetModPlayer<CooldownsPlayer>().cooldowns.Values.Contains(handler.instance) ||
                !handler.ShouldDisplay)
            {
                dissapearing = true;
                return;
            }

            if (IsMouseHovering)
                opacity = 1f;
            else
                opacity = Math.Max(0.8f, opacity - 0.02f);


            motionTimer = Math.Max(0, motionTimer - 1 / (60 * 0.4f));
            if (motionTimer > 0)
            {
                Left.Set(MathHelper.Lerp(previousLeft, idealLeft, FablesUtils.PolyOutEasing(1 - motionTimer, 2.5f)), 0f);
            }

            int index = Index;
            if (Left.Pixels != idealLeft && motionTimer == 0)
            {
                motionTimer = 1f;
                previousLeft = Left.Pixels;
            }
            previousIndex = index;
            previouslyFancyLeft = index == 0;
            previouslyFancyRight = false;

            for (int i = Parent.Children.Count() - 1; i >= 0; i--)
            {
                if (Parent.Children.ElementAt(i) is CooldownDisplay display && !display.dissapearing)
                {
                    previouslyFancyRight = display == this;
                    break;
                }
            }
        }


        public Vector2 Corner => GetDimensions().ToRectangle().TopLeft();
        public Vector2 Center {
            get {
                Rectangle rect = GetDimensions().ToRectangle();
                return rect.TopLeft() + rect.Size() / 2;
            }
        }


        #region Drawing
        public void DrawCompact(SpriteBatch spriteBatch)
        {
            Vector2 position = Corner;

            //Draw background
            CompactFrameTexture = CompactFrameTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotFrameCompact");
            CompactConnectedFrameTexture = CompactConnectedFrameTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotFrameCompactConnected");
            CompactBackgroundTexture = CompactBackgroundTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotBackgroundCompact");
            CompactBackgroundEdgeTexture = CompactBackgroundEdgeTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotBackgroundOutlineCompact");

            Texture2D frame = CompactFrameTexture.Value;
            if (previousIndex != 0)
                frame = CompactConnectedFrameTexture.Value;

            spriteBatch.Draw(frame, position, null, Color.White * DrawOpacity, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.Draw(CompactBackgroundTexture.Value, position, null, handler.BackgroundColor * DrawOpacity, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            spriteBatch.Draw(CompactBackgroundEdgeTexture.Value, position, null, handler.BackgroundEdgeColor * DrawOpacity, 0, Vector2.Zero, 1, SpriteEffects.None, 0);


            Texture2D sprite = ModContent.Request<Texture2D>(handler.Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(handler.OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(handler.OverlayTexture).Value;

            position += new Vector2(frame.Width / 2, frame.Width / 2 + 4);
            spriteBatch.Draw(sprite, position, null, Color.White * DrawOpacity, 0, sprite.Size() * 0.5f, 1f, SpriteEffects.None, 0f);


            int lostHeight = (int)Math.Ceiling(overlay.Height * (handler.AdjustedCompletion));
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            Vector2 drawOrigin = new Vector2(overlay.Width / 2, crop.Height);
            float overlayOpacity = handler.AdjustedCompletion * 0.2f + 0.5f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly);
            spriteBatch.Draw(overlay, position + overlay.Height / 2 * Vector2.UnitY, crop, handler.OutlineColor * DrawOpacity * overlayOpacity, 0, drawOrigin, 1f, SpriteEffects.None, 0f);


            if (handler.AdjustedCompletion == 0)
                spriteBatch.Draw(outline, position, null, handler.OutlineColor * DrawOpacity, 0, outline.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            else if (handler.AdjustedCompletion > 0)
            {
                crop = new Rectangle(0, 2 + lostHeight, outline.Width, overlay.Height - lostHeight + 2);
                drawOrigin = new Vector2(outline.Width / 2, crop.Height + 2);
                spriteBatch.Draw(outline, position + (overlay.Height / 2 + 4) * Vector2.UnitY, crop, handler.OutlineColor * DrawOpacity, 0, drawOrigin, 1f, SpriteEffects.None, 0f);
            }

            handler.PostDraw(spriteBatch, position, DrawOpacity, true);
        }

        public static void LoadTextures()
        {
            if (SelectedStyle == DisplayStyle.Square)
            {
                FrameTexture = FrameTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotFrame", AssetRequestMode.ImmediateLoad);
                BackgroundTexture = BackgroundTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotBackground", AssetRequestMode.ImmediateLoad);
                BackgroundEdgeTexture = BackgroundEdgeTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotBackgroundOutline", AssetRequestMode.ImmediateLoad);
                CompletionTexture = CompletionTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCompletion", AssetRequestMode.ImmediateLoad);
            }
            else if (SelectedStyle == DisplayStyle.Fancy)
            {
                FancyBackgroundTexture = FancyBackgroundTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleBackground", AssetRequestMode.ImmediateLoad);
                FancyBackgroundEdgeTexture = FancyBackgroundEdgeTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleBackgroundOutline", AssetRequestMode.ImmediateLoad);
                FancyCompletionTexture = FancyCompletionTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleCompletion", AssetRequestMode.ImmediateLoad);

                FancyFrameStartTexture = FancyFrameStartTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleFrame_LeftStart", AssetRequestMode.ImmediateLoad);
                FancyFrameLeftTexture = FancyFrameLeftTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleFrame_Left", AssetRequestMode.ImmediateLoad);
                FancyFrameRightTexture = FancyFrameRightTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleFrame_Right", AssetRequestMode.ImmediateLoad);
                FancyFrameEndTexture = FancyFrameEndTexture ?? ModContent.Request<Texture2D>(AssetDirectory.UI + "CooldownRack/CooldownSlotCircleFrame_RightEnd", AssetRequestMode.ImmediateLoad);
            }
        }

        public void DrawBackground(SpriteBatch spriteBatch)
        {
            Vector2 position = Center;
            LoadTextures();

            if (SelectedStyle == DisplayStyle.Square)
            {
                spriteBatch.Draw(FrameTexture.Value, position, null, Color.White * DrawOpacity, 0, FrameTexture.Value.Size() / 2, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(BackgroundTexture.Value, position, null, handler.BackgroundColor * DrawOpacity, 0, BackgroundTexture.Value.Size() / 2, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(BackgroundEdgeTexture.Value, position, null, handler.BackgroundEdgeColor * DrawOpacity, 0, BackgroundEdgeTexture.Value.Size() / 2, 1, SpriteEffects.None, 0);
            }
            else if (SelectedStyle == DisplayStyle.Fancy)
            {
                spriteBatch.Draw(FancyBackgroundTexture.Value, position, null, handler.BackgroundColor * DrawOpacity, 0, FancyBackgroundTexture.Size() / 2f, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(FancyBackgroundEdgeTexture.Value, position, null, handler.BackgroundEdgeColor * DrawOpacity, 0, FancyBackgroundEdgeTexture.Size() / 2f, 1, SpriteEffects.None, 0);

                Texture2D leftHalf = previouslyFancyLeft ? FancyFrameStartTexture.Value : FancyFrameLeftTexture.Value;
                Texture2D rightHalf = previouslyFancyRight ? FancyFrameEndTexture.Value : FancyFrameRightTexture.Value;

                //This is drawn with the icon
                if (!previouslyFancyLeft)
                    spriteBatch.Draw(leftHalf, position, null, Color.White * DrawOpacity, 0, leftHalf.Size() / 2, 1, SpriteEffects.None, 0);
                spriteBatch.Draw(rightHalf, position, null, Color.White * DrawOpacity, 0, rightHalf.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }

        public void DrawCompletion(SpriteBatch spriteBatch, Effect effect)
        {
            Vector2 position = Center;
            Texture2D completionTexture = SelectedStyle == DisplayStyle.Square ? CompletionTexture.Value : FancyCompletionTexture.Value;

            effect.Parameters["opacity"].SetValue(DrawOpacity);
            effect.Parameters["resolution"].SetValue(completionTexture.Size() * 0.5f);
            handler.UpdateShaderParameters(effect);


            spriteBatch.Draw(completionTexture, position, null, Color.White, 0, completionTexture.Size() / 2f, 1, SpriteEffects.None, 0);
        }

        public void DrawIcon(SpriteBatch spriteBatch)
        {
            Vector2 position = Center;

            //Draw the big left overlay on top of the rest
            if (SelectedStyle == DisplayStyle.Fancy)
            {
                if (previouslyFancyLeft)
                    spriteBatch.Draw(FancyFrameStartTexture.Value, position, null, Color.White * DrawOpacity, 0, FancyFrameStartTexture.Value.Size() / 2, 1, SpriteEffects.None, 0);
                position.Y += 1;
            }

            Texture2D sprite = ModContent.Request<Texture2D>(handler.Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(handler.OutlineTexture).Value;

            spriteBatch.Draw(outline, position, null, handler.OutlineColor * DrawOpacity, 0, outline.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(sprite, position, null, Color.White * DrawOpacity, 0, sprite.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            handler.PostDraw(spriteBatch, position, DrawOpacity);
        }

        public void DrawText(SpriteBatch spriteBatch, bool compact)
        {
            Vector2 position = Corner;
            position += compact ? CompactFrameTexture.Size() : GetDimensions().ToRectangle().Size();
            position.X -= 8;

            int minute = 60 * 60;
            string time = ((handler.instance.timeLeft % minute) / 60).ToString();

            if (handler.instance.timeLeft >= minute)
            {
                string minutesString = (handler.instance.timeLeft / minute).ToString();
                if (time == "0")
                    time = minutesString + "m";
                else
                {
                    if (time.Length == 1)
                        time = "0" + time;
                    time = minutesString + "m" + time;
                }
            }

            Color drawColor = Color.White;
            float scale = compact ? 0.8f : 1.2f;

            handler.ModifyTextDrawn(ref time, ref position, ref drawColor, ref scale);

            Vector2 stringSize = FontAssets.MouseText.Value.MeasureString(time);
            Vector2 outlineOffsetVertical = Vector2.UnitY * 3f;

            if (compact)
            {
                ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position - (Vector2.UnitX * 1.2f + outlineOffsetVertical * 0.5f) * scale, Color.Black * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));
                ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position - (Vector2.UnitX * -2.5f + outlineOffsetVertical * 0.5f) * scale, Color.Black * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));
            }

            ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position + outlineOffsetVertical * scale, Color.Black * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));
            ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position + (Vector2.UnitX * 1f + outlineOffsetVertical) * scale, Color.Black * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));


            ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position, drawColor * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));
            ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, time, position + Vector2.UnitX * 0.5f * scale, drawColor * DrawOpacity, -0f, new Vector2(0.5f, 0.5f) * stringSize, new Vector2(scale));
        }
        #endregion
    }

    public class CooldownStyleConfigElement : ConfigElement
    {
        public CooldownStyleConfigElement()
        {
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
             //NO Drawing!
        }
    }
}
