using Terraria.DataStructures;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class SporedCorpse : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;
        public override void Load()
        {
            FablesPlayer.PreKillEvent += SporifyCorpses;
        }

        public static readonly SoundStyle DeathSound = new SoundStyle(SoundDirectory.Crabulon + "CrabulonFungusKill");
        public static readonly SoundStyle VineHookSound = new SoundStyle(SoundDirectory.Crabulon + "MyceliumTakeover", 3) { Volume = 0.4f };

        private bool SporifyCorpses(Player player, double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (player.HasBuff<CrabulonDOT>())
            {
                playSound = false;
                genGore = false;

                if (Main.myPlayer == player.whoAmI)
                {
                    float sidewaysDisplacement = Main.rand.NextFloat(40f, 330f);
                    if (Main.rand.NextBool())
                        sidewaysDisplacement *= -1;

                    Projectile.NewProjectile(player.GetSource_Misc("PlayerDeath_TombStone"), player.Center, player.velocity * 0.3f, Type, 0, 0, Main.myPlayer, Main.myPlayer, sidewaysDisplacement);
                }
            }

            return true;
        }

        public int CorpsePlayer => (int)Projectile.ai[0];
        public Texture2D corpseTexture;
        public List<SporedCorpseRoot> roots;
        public float rootExpansion;
        public Vector2 offset;
        public bool initializationEffects = false;

        public bool ShouldDeathCheck => Projectile.ai[2] == 0;

        public List<PrimitiveTrail> vineTrails = new List<PrimitiveTrail>();
        public List<VerletPoint> segments;
        public List<VerletStick> sticks;

        public static int segmentCount = 20;
        public Vector2 HookOriginPosition(int index)
        {
            return index switch
            {
                0 => Projectile.Center - new Vector2(Projectile.ai[1], 900f),
                1 => Projectile.Center - new Vector2(-Projectile.ai[1] * 1.6f, 800f),
                _ => Projectile.Center - new Vector2(Projectile.ai[1] * 0.4f, 1300f),
            };
        }

        public override void SetDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
        }

        public static readonly int[] vineTimers = new int[] { 140, 200, 225 };

        public override void AI()
        {
            if (Projectile.timeLeft == 500 && Main.myPlayer == CorpsePlayer && ShouldDeathCheck && (!Main.player[CorpsePlayer].active || !Main.player[CorpsePlayer].dead))
            {
                //deactivates it in mp for the syncing
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new DeactivateProjectilePacket(Projectile).Send(-1, -1, false);
                Projectile.active = false;
                return;
            }

            if (!Main.dedServ)
            {
                if (corpseTexture == null)
                {
                    //If the player somehow isnt active while generating the texture
                    if (!Main.player[CorpsePlayer].active)
                    {
                        CalamityFables.Instance.Logger.Debug("Spored corpse found inactive player before we could generate a player capture. Despawning projectile");
                        Projectile.active = false;
                        return;
                    }

                    PlayerCapture.CapturePlayer(Main.player[CorpsePlayer], StorePlayerTarget, 5, 5);
                }
                else
                    PlayerCapture.TrackCapture(corpseTexture);

                roots = roots ?? new List<SporedCorpseRoot>();
                while (roots.Count < 6)
                    roots.Add(new SporedCorpseRoot(14f));

                offset = Main.rand.NextVector2Circular(1f, 1f) * Math.Max(1 - rootExpansion / 100f, 0f) * 1.4f;
                Projectile.rotation += Main.rand.NextFloatDirection() * Math.Max(1 - rootExpansion / 100f, 0f) * 0.04f;

                if (!initializationEffects)
                {
                    SoundEngine.PlaySound(DeathSound, Projectile.Center);

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 goreOffset = Main.rand.NextVector2Circular(20f, 20f);
                        Gore g = Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center + goreOffset, goreOffset * Main.rand.NextFloat(0.3f, 2f), GoreID.TreeLeaf_Mushroom);
                        g.velocity = goreOffset * Main.rand.NextFloat(0.3f, 2f);
                    }

                    for (int i = 0; i < 13; i++)
                    {
                        Vector2 goreOffset = Main.rand.NextVector2Circular(30f, 30f);
                        Gore g = Gore.NewGorePerfect(Projectile.GetSource_FromThis(), Projectile.Center + goreOffset - Vector2.One * 5f, Vector2.Zero, Main.rand.Next(375, 378));
                        g.velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(0.5f, 4f);
                    }

                    initializationEffects = true;
                }

                //Blood gushing out as the roots emerge
                if (ChildSafety.Disabled)
                {
                    foreach (SporedCorpseRoot root in roots)
                    {
                        if (rootExpansion - root.expansionDelay <= 14 && rootExpansion - root.expansionDelay >= 0)
                        {
                            Vector2 rootPosition = Projectile.Center + (root.rotation + Projectile.rotation).ToRotationVector2() * root.distance;
                            rootPosition += Main.rand.NextVector2Circular(3f, 3f);
                            Vector2 bloodSpeed = (root.rotation + Projectile.rotation + Main.rand.NextFloat(-0.23f, 0.21f)).ToRotationVector2() * Main.rand.NextFloat(2f, 13f);

                            Dust.NewDustPerfect(rootPosition, DustID.Blood, bloodSpeed, Scale: Main.rand.NextFloat(0.8f, 1.7f));
                        }
                    }
                }
            }

            //Spore dust
            if (Main.rand.NextBool((int)(2 + Math.Min(1, rootExpansion / 100f) * 8)))
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(22f, 22f);
                Dust.NewDustPerfect(position, DustID.GlowingMushroom, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(0.7f, 1.1f));
            }

            rootExpansion++;

            if (rootExpansion >= 140 && !Main.dedServ)
            {
                for (int i = 0; i < vineTimers.Length; i++)
                {
                    if (rootExpansion == vineTimers[i])
                        CreateTrail(HookOriginPosition(i));
                }

                //Simulate the vines
                VerletPoint.ComplexSimulation(segments, sticks, 10, 0.58f);
                Projectile.Center = segments[0].position;

                while (vineTrails.Count < ((segments.Count - 1) / segmentCount))
                    vineTrails.Add(new PrimitiveTrail(segmentCount + 1, VineWidth, VineColor));

                for (int i = 0; i < vineTrails.Count; i++)
                {
                    IEnumerable<Vector2> points = segments.Skip(1 + i * segmentCount).Take(segmentCount).Select(x => x.position);
                    points = points.Append(Projectile.Center);
                    vineTrails[i].SetPositions(points, FablesUtils.SmoothBezierPointRetreivalFunction);
                }

                //Go up
                if (rootExpansion > 260)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float reelSpeed = 8.5f;
                        if (i == 2)
                            reelSpeed = 10f;
                        if (i == 1)
                            reelSpeed = 7f;

                        segments[1 + i * segmentCount].position.Y -= Math.Min((float)Math.Pow((rootExpansion - 260) / 120f, 2), 1f) * reelSpeed;
                    }
                }
            }
            else
                Projectile.velocity *= 0.9f;
        }

        public static Color VineLightColor;
        public float VineWidth(float progress) => 9f;
        public Color VineColor(float progress) => VineLightColor * progress;


        public void StorePlayerTarget(PlayerCapture.PlayerTargetHolder target) => corpseTexture = target.Target;
        public void CreateTrail(Vector2 vineOrigin)
        {
            if (segments is null)
                segments = new List<VerletPoint>();
            if (sticks is null)
                sticks = new List<VerletStick>();

            //Add the corpses segment if it doesnt exist already
            if (segments.Count == 0)
                segments.Add(new VerletPoint(Projectile.Center));

            //Add the vine
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 position = Vector2.Lerp(vineOrigin, Projectile.Center, i / (float)(segmentCount));
                VerletPoint segment = new VerletPoint(position);
                segments.Add(segment);

                //Lock the first segment of the vine chain
                if (i == 0)
                    segment.locked = true;
                //Create sticks to link up the vine
                else
                    sticks.Add(new VerletStick(segments[^2], segment));
            }

            //Connect the last segment with the corpse
            sticks.Add(new VerletStick(segments[^1], segments[0]));

            //Blood gushing out as the corpse is skewered
            for (int d = 0; d < 23; d++)
            {
                Vector2 direction = segments[^1].position.DirectionTo(Projectile.Center).RotatedByRandom(0.2f);
                Vector2 bloodPosition = Projectile.Center + direction * Main.rand.NextFloat(0f, 18f);
                int dustType = (!ChildSafety.Disabled || Main.rand.NextBool(4)) ? DustID.GlowingMushroom : DustID.Blood;

                Dust.NewDustPerfect(bloodPosition, dustType, direction * Main.rand.NextFloat(5f, 12f), Scale: Main.rand.NextFloat(0.8f, 1.7f));
            }


            Vector2 impaleNormal = segments[^1].position.DirectionTo(Projectile.Center);
            int vineNumber = (segments.Count - 1) / segmentCount - 1;

            float screenshakeStrenght = 4f;
            //More screenshake on first spearing
            if (vineNumber == 0)
                screenshakeStrenght *= 3f;

            if (Main.myPlayer == Projectile.owner && Main.LocalPlayer.Distance(Projectile.Center) < 1000)
                CameraManager.AddCameraEffect(new DirectionalCameraTug(impaleNormal * screenshakeStrenght, 3f, 20, uniqueIdentity: "myceliumVines" + vineNumber.ToString()));

            SoundEngine.PlaySound(VineHookSound with { Pitch = vineNumber * 0.2f, Volume = VineHookSound.Volume * (0.7f - vineNumber * 0.1f) }, Projectile.Center);

            //Add recoil to the corpse
            segments[0].oldPosition = segments[0].oldPosition + impaleNormal * 10f;
            segments[0].position = segments[0].position + impaleNormal * 20f;
        }


        public static readonly Vector3[] mushGradientMapColors = new Vector3[] {
                new Vector3(44, 40, 28) / 255f,
                new Vector3(97, 98, 67) / 255f,
                new Vector3(89, 142, 245) / 255f,
                new Vector3(150, 143, 110) / 255f,
                new Vector3(208, 204, 164) / 255f,
                Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };
        public static readonly float[] mushGradientMapBrightness = new float[] { 0, 0.28f, 0.58f, 0.67f, 0.89f, 1, 1, 1, 1, 1 };

        public override bool PreDraw(ref Color lightColor)
        {
            if (corpseTexture == null)
                return false;

            if (Projectile.timeLeft < 60f)
                lightColor *= Projectile.timeLeft / 60f;

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.DodgerBlue with { A = 0 } * 0.14f, 0, bloom.Size() / 2f, 1.2f, 0, 0);

            DrawVines(lightColor);

            Texture2D rootTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MushroomInfestationRoots").Value;
            if (roots != null)
                DrawRoots(rootTex, false, lightColor);

            DrawRottingCorpse(lightColor);

            if (roots != null)
                DrawRoots(rootTex, true, lightColor);

            return false;
        }

        public void DrawVines(Color lightColor)
        {
            VineLightColor = lightColor;
            int i = 0;

            foreach (PrimitiveTrail vine in vineTrails)
            {
                float timeExpanded = rootExpansion - vineTimers[i];

                Effect effect = AssetDirectory.PrimShaders.TextureMap;
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(i * 0.33f + 0.3f * (float)Math.Pow(Math.Max(0, 1 - timeExpanded / 15f), 2f));
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumVine").Value);

                vine.Render(effect, -Main.screenPosition);
                i++;
            }
        }

        public void DrawRottingCorpse(Color lightColor)
        {
            Effect gradientMap = Scene["BasicGradientMap"].GetShader().Shader;
            gradientMap.Parameters["effectOpacity"].SetValue(Math.Min(rootExpansion / 120f, 1f) * 0.8f);
            gradientMap.Parameters["segments"].SetValue(5);
            gradientMap.Parameters["brightnesses"].SetValue(mushGradientMapBrightness);
            gradientMap.Parameters["colors"].SetValue(mushGradientMapColors);
            gradientMap.Parameters["lastColor"].SetValue(mushGradientMapColors[4]);
            gradientMap.Parameters["lightColor"].SetValue(lightColor.ToVector4());
            gradientMap.Parameters["useLuminance"].SetValue(false);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, gradientMap, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(corpseTexture, Projectile.Center + offset - Main.screenPosition, null, lightColor, Projectile.rotation, corpseTexture.Size() / 2f + Vector2.UnitY * 3f, 1f, 0, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public void DrawRoots(Texture2D rootTex, bool abovePlayer, Color lightColor)
        {
            foreach (SporedCorpseRoot root in roots)
            {
                if (root.aboveCorpse != abovePlayer)
                    continue;

                if (rootExpansion <= root.expansionDelay)
                    continue;

                float expansion = Math.Min(1f, (rootExpansion - root.expansionDelay) / root.expansionTime * root.stretch);
                Vector2 scale = new Vector2(1f, 1f * (float)Math.Pow(expansion, 0.7f));

                Vector2 gorePosition = Projectile.Center + (root.rotation + Projectile.rotation).ToRotationVector2() * root.distance;
                Rectangle frame = rootTex.Frame(1, 7, 0, root.variant, 0, -2);
                Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
                float rotation = Projectile.rotation + root.rotation + root.tilt + MathHelper.PiOver2;
                rotation += (float)Math.Sin((rootExpansion + root.rotation * 400f) * 0.1f * root.wiggleSpeed) * 0.1f;

                SpriteEffects effects = root.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.EntitySpriteDraw(rootTex, gorePosition + offset - Main.screenPosition, frame, lightColor, rotation, origin, scale * Projectile.scale, effects, 0);
            }
        }

        public override void OnKill(int timeLeft)
        {
            //corpseTexture?.Dispose();
        }

        public struct SporedCorpseRoot
        {
            public int variant;
            public bool flip;
            public float rotation;
            public float distance;
            public float tilt;
            public float expansionTime;
            public bool aboveCorpse;
            public float stretch;
            public float expansionDelay;
            public float wiggleSpeed;

            public SporedCorpseRoot(float radius)
            {
                variant = Main.rand.Next(7);
                flip = Main.rand.NextBool();
                rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                tilt = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4) * 0.4f;
                distance = Main.rand.NextFloat(0.15f, 1f) * radius;
                expansionTime = Main.rand.NextFloat(12f, 62f);
                aboveCorpse = Main.rand.NextBool();
                stretch = Main.rand.NextFloat(1f, 1.6f);
                expansionDelay = Main.rand.Next(0, 32);
                wiggleSpeed = Main.rand.NextFloat(0.5f, 1f);
            }
        }
    }
}
