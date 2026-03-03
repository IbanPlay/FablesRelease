using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public class PlayerCapture : ModSystem
    {
        public override void Load()
        {
            On_Main.CheckMonoliths += DrawTargets;
            On_PlayerDrawSet.BoringSetup_End += HalfTransparentizeAlpha;
            FablesPlayer.DrawEffectsEvent += GoFullBright;
        }

        private void GoFullBright(Player player, PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (drawingTargets)
                fullBright = true;
        }

        /// <summary>
        /// Use this to cache and clear some variables before doing a player capture
        /// </summary>
        public static event FablesPlayer.PlayerActionDelegate CacheVariablesBeforeCaptureEvent;

        /// <summary>
        /// Use this to restore the cached variables after drawing the player capture
        /// </summary>
        public static event FablesPlayer.PlayerActionDelegate RestoreCachedVariablesAfterCaptureEvent;

        private void HalfTransparentizeAlpha(On_PlayerDrawSet.orig_BoringSetup_End orig, ref PlayerDrawSet self)
        {
            //We want shadow to be 0 at the start to get the skin to not be half transparent.
            //However, afterwards during the drawing, we want it to be counted as an afterimage.
            if (drawingTargets)
                self.shadow = float.Epsilon;
            orig(ref self);
        }

        private static List<PlayerTargetHolder> _requests = new List<PlayerTargetHolder>();
        public delegate void OnCompletedPlayerRenderTargetRequest(PlayerTargetHolder target);
        private static bool drawingTargets;

        private void DrawTargets(On_Main.orig_CheckMonoliths orig)
        {
            if (_requests.Count != 0)
            {
                drawingTargets = true;
                for (int i = 0; i < _requests.Count; i++)
                {
                    _requests[i].PrepareTexture();
                    TrackCapture(_requests[i].Target);
                }
                _requests.Clear();

                drawingTargets = false;
            }

            orig();
        }

        public static void CapturePlayer(Player player, OnCompletedPlayerRenderTargetRequest onCompleted, int? bodyFrame = null, int? legFrame = null)
        {
            if (Main.netMode == NetmodeID.Server)
                return;
            _requests.Add(new PlayerTargetHolder(player, onCompleted, bodyFrame, legFrame));
        }


        public static Dictionary<Texture2D, int> managedCaptures = new Dictionary<Texture2D, int>();
        private static List<Texture2D> capturesToClear = new List<Texture2D>();
        public override void PostUpdateEverything()
        {
            capturesToClear.Clear();
            foreach (Texture2D texture in managedCaptures.Keys)
            {
                if (!texture.IsDisposed)
                {
                    managedCaptures[texture]--;
                    if (managedCaptures[texture] < 0)
                    {
                        Main.QueueMainThreadAction(() => texture.Dispose());
                        capturesToClear.Add(texture);
                    }
                }

                else
                    capturesToClear.Add(texture);
            }


            managedCaptures.RemoveAll(tex => capturesToClear.Contains(tex));
        }

        public static void TrackCapture(Texture2D capture, int refreshTime = 4)
        {
            managedCaptures[capture] = refreshTime;
        }

        public override void Unload()
        {
            foreach (Texture2D tex in managedCaptures.Keys)
                tex.Dispose();
            managedCaptures.Clear();
            capturesToClear.Clear();
        }

        //Ripped off mostly from TilePaintSystemV2's ARenderTargetHolder
        public class PlayerTargetHolder
        {
            public Player myPlayer;
            public RenderTarget2D Target;
            private OnCompletedPlayerRenderTargetRequest OnCompleted;
            public int? bodyFrame;
            public int? legFrame;

            public PlayerTargetHolder(Player player, OnCompletedPlayerRenderTargetRequest onCompleted, int? bodyFrame, int? legFrame)
            {
                myPlayer = player;
                OnCompleted = onCompleted;
                this.bodyFrame = bodyFrame;
                this.legFrame = legFrame;
            }

            public void Clear()
            {
                if (Target != null && !Target.IsDisposed)
                    Target.Dispose();
            }

            public void PrepareTexture()
            {
                if (Target == null || Target.IsContentLost)
                {
                    Main instance = Main.instance;

                    Target = new RenderTarget2D(instance.GraphicsDevice, 300, 300);
                    instance.GraphicsDevice.SetRenderTarget(Target);
                    instance.GraphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

                    //Idk wtf the dye check is doing frankly
                    if (myPlayer.active && myPlayer.dye.Length > 0)
                    {
                        //Cache variables
                        Vector2 cachedScreenPosition = Main.screenPosition;
                        int cachedHeldProj = myPlayer.heldProj;
                        bool oldDead = myPlayer.dead;
                        bool oldMount = myPlayer.mount._active;
                        int oldImmuneAlpha = myPlayer.immuneAlpha;

                        Rectangle oldHeadFrame = myPlayer.headFrame;
                        Rectangle oldBodyFrame = myPlayer.bodyFrame;
                        Rectangle oldLegFrame = myPlayer.legFrame;

                        //When the player dies, these variables move around
                        Vector2 oldheadPosition = myPlayer.headPosition;
                        Vector2 oldBodyPosition = myPlayer.bodyPosition;
                        Vector2 oldLegsPosition = myPlayer.legPosition;

                        myPlayer.headPosition = Vector2.Zero;
                        myPlayer.bodyPosition = Vector2.Zero;
                        myPlayer.legPosition = Vector2.Zero;

                        //Tweak variables
                        myPlayer.dead = false;
                        myPlayer.heldProj = -1;
                        myPlayer.mount._active = false;
                        myPlayer.immuneAlpha = 0;
                        Main.screenPosition = Vector2.Zero;

                        if (bodyFrame.HasValue)
                        {
                            myPlayer.bodyFrame = new Rectangle(0, 56 * bodyFrame.Value, 40, 56);
                            myPlayer.headFrame = new Rectangle(0, 56 * bodyFrame.Value, 40, 56);
                        }
                        if (legFrame.HasValue)
                        {
                            myPlayer.legFrame = new Rectangle(0, 56 * legFrame.Value, 40, 56);
                        }

                        CacheVariablesBeforeCaptureEvent?.Invoke(myPlayer);

                        Main.PlayerRenderer.DrawPlayer(Main.Camera, myPlayer, Target.Size() * 0.5f - myPlayer.Size * 0.5f, 0, Vector2.Zero, 0);

                        //Restore cached variables
                        RestoreCachedVariablesAfterCaptureEvent?.Invoke(myPlayer);

                        Main.screenPosition = cachedScreenPosition;
                        myPlayer.heldProj = cachedHeldProj;
                        myPlayer.dead = oldDead;
                        myPlayer.mount._active = oldMount;
                        myPlayer.immuneAlpha = oldImmuneAlpha;

                        myPlayer.headFrame = oldHeadFrame;
                        myPlayer.bodyFrame = oldBodyFrame;
                        myPlayer.legFrame = oldLegFrame;

                        myPlayer.headPosition = oldheadPosition;
                        myPlayer.bodyPosition = oldBodyPosition;
                        myPlayer.legPosition = oldLegsPosition;
                    }

                    Main.spriteBatch.End();
                    instance.GraphicsDevice.SetRenderTarget(null);

                    if (OnCompleted != null)
                        OnCompleted(this);
                }
            }
        }

        #region Lighting overrides
        private Vector3 OverrideLegacyColor(Terraria.Graphics.Light.On_LegacyLighting.orig_GetColor orig, Terraria.Graphics.Light.LegacyLighting self, int x, int y)
        {
            if (!drawingTargets)
                return orig(self, x, y);
            return Vector3.One;
        }

        private Vector3 OverrideColor(Terraria.Graphics.Light.On_LightingEngine.orig_GetColor orig, Terraria.Graphics.Light.LightingEngine self, int x, int y)
        {
            if (!drawingTargets)
                return orig(self, x, y);
            return Vector3.One;
        }
        #endregion
    }
}