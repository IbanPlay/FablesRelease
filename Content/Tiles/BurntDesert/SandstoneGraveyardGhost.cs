using CalamityFables.Content.Tiles.Graves;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.IO;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class SandstoneGraveyardGhost : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.BurntDesert + "GraveyardGhosts";
        public DrawhookLayer layer => DrawhookLayer.AboveTiles;

        public static Asset<Texture2D> DisplaceNoiseTex;
        public static Asset<Texture2D> OutlineTex;
        public static Asset<Texture2D> EmoteTex;

        public const int GHOST_VARIANT_COUNT = 8;

        public int Variant => (int)Projectile.ai[0];
        public ref float Opacity => ref Projectile.localAI[0];
        public ref float AnchorX => ref Projectile.ai[1];
        public ref float OriginalAnchorX => ref Projectile.localAI[1];
        public ref float FindNewSpotTimer => ref Projectile.ai[2];
        public ref float CaptivatedBy => ref Projectile.localAI[2];

        public bool reachedCurrentSpot;


        public static List<Projectile> LoadedGhosts = new List<Projectile>();
        public static int GhostType;

        public override void Load()
        {
            FablesGeneralSystemHooks.PreUpdateProjectilesEvent += ClearLoadedGhosts;
        }

        private void ClearLoadedGhosts()
        {
            for (int i = LoadedGhosts.Count - 1; i >= 0; i--)
            {
                Projectile ghost = LoadedGhosts[i];
                if (!ghost.active)
                    LoadedGhosts.RemoveAt(i);
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost");
            GhostType = Type;
        }

        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.timeLeft = 60 * 25;
            Projectile.manualDirectionChange = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            if (AnchorX == 0)
            {
                AnchorX = Projectile.Center.X;
                OriginalAnchorX = AnchorX;
            }

            CheckForCaptivatedAttention();
            UpdateEmote();

            if (CaptivatedBy >= 0)
            {
                Player targetOfAttention = Main.player[(int)CaptivatedBy];
                Projectile.direction = (targetOfAttention.Center.X - Projectile.Center.X).NonZeroSign();

                //Slows down
                Projectile.velocity.X *= 0.6f;
                FindNewSpotTimer = 220;

                //cant despawn if the player is near
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 120);
            }
            else
                Projectile.direction = Projectile.velocity.X.NonZeroSign();


            //randomly choose a new spot
            if (FindNewSpotTimer >= 290)
            {
                FindNewSpotTimer = 0;
                FindNewHangoutSpot(50f, 220f, 30);
            }
            if (Math.Abs(Projectile.Center.X - AnchorX) < 5f)
                reachedCurrentSpot = true;

            #region X Movement
            float hoverTargetX;
            float maxXSpeed = 1.6f;

            if (!reachedCurrentSpot)
                hoverTargetX = AnchorX;
            else
            {
                hoverTargetX = Projectile.Center.X + Projectile.direction * 4f;
                maxXSpeed = 0.1f;
                Projectile.velocity.X *= 0.98f;
            }

            float velocityTargetX = Math.Clamp(hoverTargetX - Projectile.Center.X, - maxXSpeed, maxXSpeed);
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, velocityTargetX, 0.01f);
            #endregion

            #region Y movement
            int floorDistance = -1;
            Point checkPos = Projectile.Bottom.ToTileCoordinates();
            for (int i = 0; i < 2; i++)
            {
                Tile t = Main.tile[checkPos];
                if (WorldGen.SolidTile(t) || t.LiquidAmount > 140 || (Main.tileSolidTop[t.TileType] && t.TileFrameY == 0 && t.HasUnactuatedTile))
                {
                    floorDistance = i;
                    break;
                }
                checkPos.Y += 1;
            }
            if (floorDistance == -1)
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 2f, 0.02f); //Too far from ground
            else if (floorDistance < 1f)
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, -2f, 0.05f); //Too close to ground
            else
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 0f, 0.05f); //Just far enough
            #endregion


            if (Main.rand.NextBool(40))
            {
                Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(Projectile.Hitbox), ModContent.DustType<SpectralWaterDustEmbers>(), -Vector2.UnitY, 15, Color.White, Main.rand.NextFloat(0.6f, 1.3f));
            }

            if (Projectile.timeLeft > 60)
                Opacity = MathHelper.Lerp(Opacity, 1f, 0.04f);
            else
                Opacity -= 0.014f;


            Lighting.AddLight(Projectile.Center, 0.1f, 0.2f, 0.3f);
            FindNewSpotTimer++;

            if (!LoadedGhosts.Contains(Projectile))
                LoadedGhosts.Add(Projectile);
        }

        public void CheckForCaptivatedAttention()
        {
            float lastCaptivatedBy = CaptivatedBy;
            CaptivatedBy = -1;

            Player nearest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (nearest.active && nearest.WithinRange(Projectile.Center, 160))
            {
                foreach (Projectile p in LoadedGhosts)
                {
                    if (p.whoAmI == Projectile.whoAmI)
                        continue;

                    //Won't share attention to the same player as another ghost
                    int otherGhostAttention = (int)p.localAI[2];
                    if (otherGhostAttention == nearest.whoAmI)
                    {
                        return;
                    }
                }

                CaptivatedBy = nearest.whoAmI;
                
            }

            //Choose a new emote if talking to a new ghost
            if (lastCaptivatedBy == -1 && CaptivatedBy >= 0 && !Main.rand.NextBool(3))
                PickEmote();
        }

        public void FindNewHangoutSpot(float minDistance, float maxDistance, int maxAttempts)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float newX = Main.rand.NextFloat(minDistance, maxDistance) * (Main.rand.NextBool() ? 1 : -1) + AnchorX;

                if (Math.Abs(newX - OriginalAnchorX) < 1400f && Collision.CanHitLine(Projectile.Top, 1, 10, new Vector2(newX, Projectile.position.Y), 0, 10))
                {
                    //Check for standable ground
                    bool ground = false;
                    for (int j = -4; j < 14; j++)
                    {
                        Point tilePos = new Vector2(newX, Projectile.position.Y).ToTileCoordinates() + new Point(0, j);
                        Tile t = Main.tile[tilePos];

                        if (WorldGen.SolidTile(t) || t.LiquidAmount > 140)
                        {
                            ground = true;
                            break;
                        }
                    }

                    if (!ground)
                        continue;

                    bool closeToOtherGhost = false;
                    foreach (Projectile p in LoadedGhosts)
                    {
                        if (p.whoAmI == Projectile.whoAmI)
                            continue;

                        float otherAnchorX = p.ai[0];
                        if (Math.Abs(otherAnchorX - newX) < 60f)
                        {
                            closeToOtherGhost = true;
                            break;
                        }

                        //Also avoid being too close to captivated ghosts
                        if (p.localAI[2] >= 0 && Math.Abs(p.Center.X - newX) < 60f)
                        {
                            closeToOtherGhost = true;
                            break;
                        }
                    }
                    if (closeToOtherGhost)
                        continue;

                    AnchorX = newX;
                    reachedCurrentSpot = false;
                    return;
                }
            }
        }

        #region emotes
        public enum EmoteType
        {
            None,
            EmptyBubble,
            Gibberish,
            Ocean,
            OceanJumpscare,
            FireOcean
        }
        public EmoteType chosenEmote = EmoteType.None;
        public int emoteFrameCounter;
        public int emoteFrame;
        public float emoteTimer = 0f;

        public void PickEmote()
        {
            if (emoteTimer <= 0)
            {
                //3 in 4 chance to get an emtpy textbox
                if (!Main.rand.NextBool(4))
                    chosenEmote = EmoteType.EmptyBubble;
                //1 in 8 chance to get gibberish
                else if (Main.rand.NextBool())
                    chosenEmote = EmoteType.Gibberish;
                //1 in 16 chance for either ocean or fire ocean
                else if (Main.rand.NextBool())
                    chosenEmote = EmoteType.Ocean;
                else
                    chosenEmote = EmoteType.OceanJumpscare;
            }
        }

        public void UpdateEmote()
        {
            if (CaptivatedBy == -1 && chosenEmote != EmoteType.None && emoteTimer < 0.95f)
            {
                emoteTimer = 0.95f;
            }

            if (chosenEmote != EmoteType.None)
            {
                emoteTimer += 1 / (60f * 5f);
                if (emoteTimer >= 1f)
                    chosenEmote = EmoteType.None;
            }
            else if (emoteTimer > 0)
                emoteTimer -= 1 / (60f * 6f);

            emoteFrameCounter++;

            if (chosenEmote == EmoteType.Gibberish)
            {
                if (emoteFrameCounter >= 4)
                {
                    emoteFrameCounter = 0;
                    emoteFrame = Main.rand.Next(0, 8);
                }
            }
            else if (chosenEmote == EmoteType.OceanJumpscare)
            {
                if (emoteFrameCounter % 8 == 0)
                {
                    emoteFrame++;
                    if (emoteFrame >= 2)
                        emoteFrame = 0;

                    if (emoteFrameCounter > 60)
                        chosenEmote = EmoteType.FireOcean;
                }
            }
            else if (emoteFrameCounter >= 8)
            {
                emoteFrameCounter = 0;
                emoteFrame++;
                if (emoteFrame >= 2)
                    emoteFrame = 0;
            }
        }

        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((ushort)chosenEmote);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            chosenEmote = (EmoteType)reader.ReadUInt16();
        }

        #region Drawing
        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            float finalFade = Utils.GetLerpValue(60, 0, Projectile.timeLeft, true);
            float fadeNear = Utils.GetLerpValue(200f, 60f, Main.LocalPlayer.Center.Distance(Projectile.Center), true);
            Color glowColor = FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly + Projectile.whoAmI)), Color.Cyan, Color.Turquoise, Color.MediumSpringGreen, Color.DeepSkyBlue);


            DisplaceNoiseTex ??= ModContent.Request<Texture2D>(AssetDirectory.Noise + "DisplaceNoise3");
            OutlineTex ??= ModContent.Request<Texture2D>(Texture + "Outline");

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Bottom - Main.screenPosition;
            drawPosition.Y += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.8f + Projectile.whoAmI) * 4.4f;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            spriteBatch.Draw(bloom, (Projectile.Center - Main.screenPosition) * 0.5f, null, glowColor with { A = 0 } * Opacity * 0.06f, 0f, bloom.Size() / 2f, 0.5f, 0, 0);


            Rectangle frame = tex.Frame(1, GHOST_VARIANT_COUNT, 0, Variant, 0, -2);
            switch (Variant)
            {
                case 0:
                    frame.Y += 22;
                    frame.Height -= 22;
                    break;
                case 6:
                    frame.Y += 12;
                    frame.Height -= 12;
                    break;
            }

            float drawOpacity = Opacity * (0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.67f + Projectile.whoAmI));
            drawOpacity *= 0.7f + fadeNear * 0.3f;


            Effect ghostEffect = Scene["DesertGraveGhost"].GetShader().Shader;
            ghostEffect.Parameters["sourceRect"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
            ghostEffect.Parameters["resolution"].SetValue(tex.Size());
            ghostEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.06f);
            ghostEffect.Parameters["noise"].SetValue(DisplaceNoiseTex.Value);
            ghostEffect.Parameters["idOffset"].SetValue(Projectile.whoAmI);
            ghostEffect.Parameters["fadePercent"].SetValue(finalFade);
            ghostEffect.Parameters["screenLayerColor"].SetValue(glowColor.ToVector3());

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, ghostEffect);


            SpriteEffects effects = Projectile.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            glowColor = Color.Lerp(glowColor * 0.7f, Color.White, fadeNear * 0.5f);

            spriteBatch.Draw(OutlineTex.Value, drawPosition / 2f, frame, glowColor * drawOpacity, 0f, new Vector2(frame.Width / 2f, frame.Height), 0.5f, effects, 0);
            spriteBatch.Draw(tex, drawPosition / 2f, frame, Color.White * drawOpacity, 0f, new Vector2(frame.Width / 2f, frame.Height), 0.5f, effects, 0);

            if (chosenEmote != EmoteType.None)
                DrawSpeech(spriteBatch, drawPosition, drawOpacity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);
        }

        public void DrawSpeech(SpriteBatch spriteBatch, Vector2 ghostDrawPosition, float drawOpacity)
        {
            EmoteTex ??= ModContent.Request<Texture2D>(Texture + "Speech");
            SpriteEffects effects = Projectile.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Rectangle frame;

            if (emoteTimer <= 0.02f)
                frame = EmoteTex.Frame(14, 1, 0, 0, -2, 0); //Tiny bubble
            else if (chosenEmote == EmoteType.EmptyBubble)
                frame = EmoteTex.Frame(14, 1, 1, 0, -2, 0); 
            else
            {
                switch (chosenEmote)
                {
                    case EmoteType.Gibberish:
                        frame = EmoteTex.Frame(14, 1, emoteFrame + 2, 0, -2, 0);
                        break;
                    case EmoteType.Ocean:
                    case EmoteType.OceanJumpscare:
                        frame = EmoteTex.Frame(14, 1, emoteFrame + 10, 0, -2, 0);
                        break;
                    default:
                        frame = EmoteTex.Frame(14, 1,emoteFrame + 12, 0, -2, 0);
                        break;
                }
            }

            ghostDrawPosition += Vector2.UnitX * Projectile.direction * 35f - Vector2.UnitY * 30f;
            drawOpacity *= Utils.GetLerpValue(1f, 0.95f, emoteTimer, true);

            Effect ghostEffect = Scene["DesertGraveGhost"].GetShader().Shader;
            ghostEffect.Parameters["sourceRect"].SetValue(new Vector4(frame.X, frame.Y, frame.Width, frame.Height));
            ghostEffect.Parameters["resolution"].SetValue(EmoteTex.Size());

            spriteBatch.Draw(EmoteTex.Value, ghostDrawPosition / 2f, frame, Color.White * drawOpacity, 0f, new Vector2(frame.Width / 2f, frame.Height), 0.5f, effects, 0);

        }

        #endregion
    }
}
