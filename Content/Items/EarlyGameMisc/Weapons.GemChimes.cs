using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Content.Items.EarlyGameMisc.GemChimes;
using static CalamityFables.Content.Items.EarlyGameMisc.GemChimesNPCDebuff;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class GemChimes : ModItem, ICustomHeldDraw
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public static Asset<Texture2D> HeldTexture;
        public static Asset<Texture2D> ChimesTexture;

        public static readonly SoundStyle UseEmerald = new(SoundDirectory.Sounds + "GemChimeEmerald");
        public static readonly SoundStyle UseAmethyst = new(SoundDirectory.Sounds + "GemChimeAmethyst");
        public static readonly SoundStyle UseDiamond = new(SoundDirectory.Sounds + "GemChimeDiamond");
        public static readonly SoundStyle UseRuby = new(SoundDirectory.Sounds + "GemChimeRuby");
        public static readonly SoundStyle Resonate = new(SoundDirectory.Sounds + "GemChimesResonate") { PitchVariance = 0.05f, Volume = 0.75f, MaxInstances = 0 };

        public static LocalizedText ResonanceDamageText;

        // Each value is associated with a color
        // 0 is Emerald, Green
        // 1 is Amethyst, Purple
        // 2 is Diamond, Cyan
        // 3 is Ruby, Red
        private int AttackMode = 0;

        // Tracks attackmode on the player, this is for the held animation
        public class GemChimesAttackMode : CustomGlobalData
        {
            public int AttackMode;
        }

        public static float SOUNDWAVE_MIN_RADIUS = 15f;
        public static float SOUNDWAVE_MAX_RADIUS = 80f;
        public static float SOUNDWAVE_MIN_RANGE = 200f;
        public static float SOUNDWAVE_MAX_RANGE = 700f;
        public static float SOUNDWAVE_DECELERATION_POWER = 1f;

        public static float RESONANCE_DAMAGE_MULT = 4f;
        public static float RESONANCE_MAX_DISTANCE = 800f;
        public static int RESONANCE_LENGTH = 75;   // Length of the resonance animation
        public static int RESONANCE_DURATION = 480;

        public static readonly int DefaultResonanceDamage = (int)(20 * RESONANCE_DAMAGE_MULT);

        public override void SetStaticDefaults()
        {
            FablesSets.HasCustomHeldDrawing[Type] = true;
            ResonanceDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.ResonanceDamage");
        }

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.DamageType = DamageClass.Magic;
            Item.width = 13;
            Item.height = 19;
            Item.useTime = 28;
            Item.useAnimation = 28;
            Item.useStyle = ItemUseStyleID.RaiseLamp;
            Item.holdStyle = ItemHoldStyleID.HoldLamp;
            Item.noMelee = true;
            Item.knockBack = 3;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<GemChimeSoundwave>();
            Item.autoReuse = true;
            Item.shootSpeed = 20;
            Item.mana = 7;

            Item.noUseGraphic = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Offset the projectile a bit
            FablesUtils.ShiftShootPositionAhead(ref position, velocity.SafeNormalize(Vector2.Zero), 20);

            // Find the distance the soundwave should travel
            Vector2 mouseWorld = Main.LocalPlayer.MouseWorld();
            float distance = Math.Clamp(Vector2.Distance(mouseWorld, position), SOUNDWAVE_MIN_RANGE, SOUNDWAVE_MAX_RANGE);

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, AttackMode, velocity.Length(), distance);
            return false;
        }

        #region Animation
        public override void UseAnimation(Player player)
        {
            // Changes attack mode at the beginning of every usage
            AttackMode++;
            if (AttackMode > 3)
                AttackMode = 0;

            // Writes AttackMode to the player
            // The value will not update while the item is in use, so the animation will not change as it should
            if (!player.GetPlayerData(out GemChimesAttackMode data))
            {
                data = new GemChimesAttackMode();
                player.SetPlayerData(data);
            }
            data.AttackMode = AttackMode;
        }

        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin)
        {
            HeldTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "GemChimes_Held");
            ChimesTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "GemChimes_Chimes");
            Texture2D bloomFlare = AssetDirectory.CommonTextures.BloomStreak.Value;

            Player player = drawInfo.drawPlayer;
            int direction = (drawInfo.playerEffect & SpriteEffects.FlipHorizontally) != 0 ? 1 : -1;
            FablesPlayer modPlayer = player.GetModPlayer<FablesPlayer>();

            // Offset the position so it lines up with the user's hand
            position += new Vector2(8 * player.direction, -18 * player.gravDir);

            // Chime offset calculated here to apply physics animation
            Vector2 chimeOffset = Vector2.Zero;
            chimeOffset.Y -= Math.Clamp(modPlayer.hairLikeVelocityTracker.value.Y * 0.1f, -2, 2);

            // Rotation calculated ehre to apply physics animation. Changed later on to apply variance to each chime
            float chimeRotation = modPlayer.springyVelocityTracker.value.X * -0.05f;
            chimeRotation = Math.Clamp(chimeRotation * direction, -0.05f, 0.1f) * -direction;

            // Draw the four chimes individually so they can be animated
            Vector2 chimesOrigin = new(3, 1);

            // Make some grav adjustments for the chimes
            if (player.gravDir == -1)
            {
                chimeOffset.Y -= 4f;
                chimesOrigin.Y = 23;
            }

            for (int i = 0; i < 4; i++)
            {
                // Frame depending on loop iteration
                Rectangle chimeFrame = ChimesTexture.Frame(4, 2, i, 0);

                // Find the final position of the chime, depending on the loop iteration
                float xPos = (4 * i - 8) * direction + (direction == -1 ? -2 : 0);
                Vector2 chimePosition = position + new Vector2(xPos, 2) + chimeOffset;

                // Give chime rotation some variation
                float finalRotation = rotation + chimeRotation + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f + Math.PI + i);

                // Use animation if the chime is the same chime thats being used to attack
                if (player.ItemAnimationActive && player.GetPlayerData(out GemChimesAttackMode data) && i == data.AttackMode)
                {
                    // Add animation rotation to final rotation using anim progress
                    float animProgress = 1 - player.itemAnimation / (float)player.itemAnimationMax;
                    finalRotation += MathF.Sin(3 * MathHelper.Pi * animProgress) * MathF.Exp(-2 * animProgress) * direction;

                    // Draw chime before chime glow
                    DrawData chime = new(ChimesTexture.Value, chimePosition, chimeFrame, color, finalRotation, chimesOrigin, scale, drawInfo.itemEffect);
                    drawInfo.DrawDataCache.Add(chime);

                    // Draw glow and have it fade as the animation ends
                    float glowFactor = MathF.Pow(1 - animProgress, 0.7f);
                    // Color determined by loop iteration to match chime color
                    Color glowColor = i switch
                    {
                        0 => Color.Green,
                        1 => Color.Magenta,
                        2 => Color.Cyan,
                        _ => Color.Red
                    };
                    // Color for the chime flashing
                    Color flashColor = Color.Lerp(Color.White, glowColor, animProgress) with { A = 0 } * glowFactor;

                    DrawData chimeGlow = new(ChimesTexture.Value, chimePosition, ChimesTexture.Frame(4, 2, i, 1), flashColor, finalRotation, chimesOrigin, scale, drawInfo.itemEffect);
                    drawInfo.DrawDataCache.Add(chimeGlow);

                    // Draw bloom last
                    Vector2 bloomPosition = chimePosition + new Vector2(0, 8 * player.gravDir);

                    // Scale changes depending on the size of the chime
                    float scaleMult = -0.25f * MathF.Abs(i - 2) + 1;
                    Vector2 bloomScale = new Vector2(MathF.Pow(glowFactor, 1.8f) * 0.75f, MathF.Pow(1 - glowFactor, 0.4f) * 1.2f) * scaleMult;

                    DrawData bloom = new(bloomFlare, bloomPosition, null, glowColor with { A = 0 }, 0, bloomFlare.Size() / 2, bloomScale, SpriteEffects.None);
                    drawInfo.DrawDataCache.Add(bloom);
                }
                // Just draw the chime otherwise
                else
                {
                    DrawData chime = new(ChimesTexture.Value, chimePosition, chimeFrame, color, finalRotation, chimesOrigin, scale, drawInfo.itemEffect);
                    drawInfo.DrawDataCache.Add(chime);
                }
            }
            // Draw the frame of the chimes last
            DrawData item = new(HeldTexture.Value, position, HeldTexture.Frame(), color, rotation, HeldTexture.Size() / 2, scale, drawInfo.itemEffect);
            drawInfo.DrawDataCache.Add(item);
        }
        #endregion

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");
            int resonanceDamage = (int)(Main.LocalPlayer.GetWeaponDamage(Item, true) * RESONANCE_DAMAGE_MULT);
            TooltipLine resonanceDamageLine = new TooltipLine(Mod, "CalamityFables:SlimelingDamage", ResonanceDamageText.Format(resonanceDamage));

            // Rotate between all four gem colors
            float globalTime = ((Main.GlobalTimeWrappedHourly / MathHelper.Pi) + 0.25f) % 4;
            Color gemColor = (int)globalTime switch
            {
                0 => new(67, 255, 185),
                1 => new Color(255, 128, 246),
                2 => new Color(87, 217, 236),
                _ => new Color(255, 100, 88)
            };
            resonanceDamageLine.OverrideColor = Color.Lerp(Color.White, gemColor, (float)Math.Pow(0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));

            tooltips.Insert(damageIndex + 1, resonanceDamageLine);
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddRecipeGroup(FablesRecipes.LowTierGemGroup, 10).
                AddRecipeGroup(FablesRecipes.MidTierGemGroup, 5).
                AddRecipeGroup(FablesRecipes.HighTierGemGroup, 3).
                AddTile(TileID.Anvils).
                Register();
        }

        public override void NetSend(BinaryWriter writer) => writer.Write7BitEncodedInt(AttackMode);

        public override void NetReceive(BinaryReader reader) => AttackMode = reader.Read7BitEncodedInt();
    }

    public class GemChimeSoundwave : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Invisible;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;

        private PrimitiveClosedLoop Ring;
        private List<Vector2> Cache;
        private float BaseRadius;

        public int AttackMode => (int)Projectile.ai[0];
        public ref float InitialSpeed => ref Projectile.ai[1];
        public ref float Range => ref Projectile.ai[2];

        public bool Lingering => Projectile.penetrate == -1;    // Dont need to use an AI state for this since it doesn't really change AI
        public float Progress => Utils.GetLerpValue(0, Range, DistanceTraveled);

        private float DistanceTraveled = 0;
        private bool IncreasedPen = false;
        private bool PlaySound = true;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (PlaySound)
            {
                // Pitched shifted shoot sound
                float pitchLerp = Utils.GetLerpValue(SOUNDWAVE_MIN_RANGE, SOUNDWAVE_MAX_RANGE, Range);
                int semitoneOffset = (int)Math.Round(MathHelper.Lerp(-3, 3, pitchLerp));
                float pitch = Math.Sign(semitoneOffset) * (MathF.Pow(2, Math.Abs(semitoneOffset) / 12f) - 1);
                //Main.NewText(semitoneOffset + ", " + pitch);

                SoundStyle chimeSound = AttackMode switch
                {
                    0 => UseEmerald,
                    1 => UseAmethyst,
                    2 => UseDiamond,
                    _ => UseRuby
                };

                SoundEngine.PlaySound(chimeSound with { Pitch = pitch }, Projectile.Center);
                PlaySound = false;
            }

            // Kill once the projectile is too slow
            if (Projectile.velocity.Length() < 0.08f)
                Projectile.Kill();

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Set base radius based on the progress of the soundwave. This affects the hitbox and visuals
            BaseRadius = MathHelper.Lerp(SOUNDWAVE_MIN_RADIUS, SOUNDWAVE_MAX_RADIUS, MathF.Pow(Progress, 0.3f));

            // Switch to lingering upon hitting a tile
            if (!Lingering && Collision.SolidCollision(Projectile.Center + Projectile.velocity, Projectile.width, Projectile.height))
                StartLingering();

            if (!Lingering && Progress > 0.5f && !IncreasedPen)
            {
                Projectile.penetrate++;
                IncreasedPen = true;
            }

            // Deceleration and fade
            float speedPower = SOUNDWAVE_DECELERATION_POWER * MathHelper.Lerp(1.2f, 0.8f, (Range - SOUNDWAVE_MIN_RANGE) / (SOUNDWAVE_MAX_RANGE - SOUNDWAVE_MIN_RANGE));
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * InitialSpeed * MathF.Pow(1 - Progress, speedPower);
            Projectile.Opacity = MathF.Min(MathHelper.Lerp(5, 0, Progress), 1);

            // Twinkly Particles
            float spawnChance = Projectile.Opacity * 0.05f;  
            if (Main.rand.NextFloat() <= spawnChance)
            {
                // Finds a point along the edge of the VFX and spawns the particle
                float rand = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 unitVectorX = new Vector2(1f, 0).RotatedBy(Projectile.rotation);
                Vector2 unitVectorY = new Vector2(0f, 0.5f).RotatedBy(Projectile.rotation);
                Vector2 unit = unitVectorX * (float)Math.Cos(rand) + unitVectorY * (float)Math.Sin(rand + Projectile.rotation);
                Vector2 particlePosition = unit * (BaseRadius * 0.95f) + Projectile.Center;

                Vector2 particleVelocity = Projectile.velocity * Main.rand.NextFloat(0.2f, 0.5f);

                Particle sparkle = new TwinklySparkle(particlePosition, particleVelocity, BaseColor((int)AttackMode), 0.3f, Projectile.Opacity) { Pixelated = true };
                ParticleHandler.SpawnParticle(sparkle);
            }

            // Track distance traveled last
            DistanceTraveled += Projectile.velocity.Length();

            ManageCache();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Lingering) 
            {
                if (CanApplyMode(target, AttackMode))
                {
                    // Add to penetrate if target has buff
                    if (target.GetNPCFlag("GemChimesNPCDebuff"))
                        Projectile.penetrate++;

                    // Add debuff
                    target.AddBuff(ModContent.BuffType<GemChimesNPCDebuff>(), RESONANCE_DURATION);

                    // New NPC data instance even if the NPC already has the data, applies new values in constructor
                    target.SetNPCData(new GemChimesResonanceData(Projectile, target));
                    target.SyncNPCData<GemChimesResonanceData, GemChimesResonanceDataModule>();
                }
            }

            // Enemies dying from the hit won't take up a pierce
            if (target.life < damageDone)
                Projectile.penetrate++;

            // Start lingering once penetrate drops too low
            if (Projectile.penetrate <= 1)
                StartLingering();
        }

        /// <summary>
        /// Checks if this NPC can receive the NPC debuff depending on mode. <br/>
        /// NPCs without the debuff can have it applied, unless they're a segmented NPC with a whitelist applied by a segment. <br/>
        /// Otherwise, checks if the current whitelist allows the mode to be applied. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        private static bool CanApplyMode(NPC target, int mode)
        {
            bool hasParent = target.GetIFramesProvider(out int iframesProvider);
            NPC realTarget = hasParent ? Main.npc[iframesProvider] : target;

            return !realTarget.GetNPCData<GemChimesResonanceData>(out var data) || (data.CanApply == -1 && !target.GetNPCFlag("GemChimesNPCDebuff")) || data.CanApply == mode;
        }

        // Cannot deal damage while lingering
        public override bool? CanDamage() => Lingering ? false : null;

        // Custom collision, allows the actual hitbox to be much smaller to prevent tile collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => FablesUtils.AABBvCircle(targetHitbox, Projectile.Center, BaseRadius) && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1);

        private void StartLingering()
        {
            // Allows the projectile to persist, but makes it's range to travel shorter
            Projectile.penetrate = -1;
            Range = MathF.Min(DistanceTraveled + Range * 0.3f, Range);
            Projectile.netUpdate = true;
        }

        #region Visuals
        public void ManageCache()
        {
            // Tracks position to draw multiple soundwave rings
            Vector2 position = Projectile.Center + Projectile.velocity;

            Cache ??= [];
            if (Cache.Count == 0)
                for (int i = 0; i < 16; i++)
                    Cache.Add(position);

            Cache.Add(position);
            while (Cache.Count > 16)
                Cache.RemoveAt(0);
        }

        private Color ColorFunction(float completionRatio)
        {
            // Fades at the far end of the ring to give it perspective
            float fadeFunction = MathF.Sin(MathHelper.TwoPi * completionRatio + MathHelper.Pi) / 2f + 0.5f;
            float perspectiveFade = MathHelper.Lerp(1, 0.5f, fadeFunction);

            return Color.White * perspectiveFade * Projectile.Opacity;
        }

        public static Color HighlightColor(int mode) => mode switch
        {
            0 => new Color(0, 205, 113),
            1 => new Color(255, 128, 246),
            2 => new Color(87, 217, 236),
            _ => new Color(255, 152, 128)
        };

        public static Color BaseColor(int mode) => mode switch
        {
            0 => new Color(10, 143, 93),
            1 => new Color(192, 48, 245),
            2 => new Color(89, 166, 255),
            _ => new Color(238, 51, 53)
        };

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            const int cylceLength = 60;
            const int Afterimages = 4;

            for (int i = 1; i <= Afterimages; i++)
            {
                // Finds cache index based on loop iterations
                int index = (int)((Cache.Count - 1) * Utils.GetLerpValue(0, Afterimages, i));
                Vector2 position = Cache[index];

                // Opacity and size of the afterimage rings
                float afterimageProgress = index / (float)(Cache.Count - 1);
                float afterimageScale = MathHelper.Lerp(0.6f, 1, MathF.Pow(afterimageProgress, 2));
                float afterimageOpacity = MathHelper.Lerp(0.1f, 1, MathF.Pow(afterimageProgress, 2.5f));

                // Make em wobble. Basically just X displacement that oscillates
                float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 5 + index) * 5;
                position += Vector2.UnitX.RotatedBy(Projectile.rotation) * wobble;

                // Two rings in the same cycle
                for (float j = 0; j < 1; j += 0.5f)
                {
                    // Anim offset based on both loop's iteration
                    float animOffset = j + (Afterimages - i) * 0.05f;

                    // Tracks progress of the animation, based on timeLeft
                    float cycle = Utils.GetLerpValue(cylceLength, 0, (Projectile.timeLeft + cylceLength * animOffset) % cylceLength);

                    // Radius grows and rapidly shrinks as anim progresses
                    float radiusFunction = cycle < 0.5f ? MathHelper.Lerp(0.7f, 1.3f, cycle) : MathHelper.Lerp(1.9f, 0.1f, cycle);
                    float radius = BaseRadius * radiusFunction * afterimageScale;

                    // Width shrinks with anim progress. Minimum is always 13. 5 width for the ring and 8 for the amplitude of it's waves
                    float width = 13 + 10 * radiusFunction * (1 - Progress) * afterimageProgress;

                    // Y Displacement dependant on anim progress and velocity. Makes the wave go forward as it grows and back as it shrinks
                    float maxDist = 60 * Projectile.velocity.Length() / InitialSpeed;
                    position += Projectile.velocity.SafeNormalize(Vector2.Zero) * maxDist * (1 - 8 * MathF.Pow(0.5f - cycle, 2));

                    // Fades at start and end of cycle, plus afterimage opacity
                    float opacity = MathF.Min(2.5f - 5 * Math.Abs(cycle - 0.5f), 1) * afterimageOpacity;

                    Ring ??= new PrimitiveClosedLoop(20, f => width, ColorFunction);
                    Ring.SetPositionsEllipse(position, radius, 0, 0.5f, Projectile.rotation);

                    Effect effect = Scene["SoundwaveRing"].GetShader().Shader;
                    effect.Parameters["Width"].SetValue(width);
                    effect.Parameters["Amplitude"].SetValue(4);
                    effect.Parameters["Distortion"].SetValue(0.65f);
                    effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly * 6.5f);
                    effect.Parameters["Repeats"].SetValue(2);
                    effect.Parameters["Voronoi"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

                    // Optional second layer of distortion
                    effect.Parameters["Amplitude2"].SetValue(4 * 2);
                    effect.Parameters["Distortion2"].SetValue(2);

                    Color highlight = HighlightColor((int)AttackMode) * opacity;
                    Color baseColor = BaseColor((int)AttackMode) * opacity;
                    // White fades to the highlight color with opacity
                    Color white = Color.Lerp(highlight, Color.White, opacity);

                    effect.Parameters["White"].SetValue(white.ToVector4());
                    effect.Parameters["HighlightColor"].SetValue(highlight.ToVector4());
                    effect.Parameters["MidrangeColor"].SetValue(baseColor.ToVector4());
                    effect.Parameters["ShadowColor"].SetValue(baseColor.ToVector4() * 0.5f);

                    Ring?.Render(effect, -Main.screenPosition);
                }
            }

            /*
             * Original Style
            for (int i = 0; i < Math.Max(Cache.Count, 0); i += 2)
            {
                Vector2 position = Projectile.Center;
                float distance = DistanceTraveled;

                position = Cache[i];
                distance -= position.Distance(Projectile.Center);

                float progressThruCache = i / (float)Cache.Count;

                // Amplitude of the distortion
                float amplitude = MathHelper.Lerp(2, 6, progressThruCache);

                // Width ranges from 0 to 12 and decreases with the after image. Minimum width is always 5 and double the amplitude is added
                Width = MathHelper.Lerp(12, 0, Progress) * progressThruCache + amplitude * 2 + 5;
                float baseRadius = (GemChimes.SOUNDWAVE_MAX_RADIUS - GemChimes.SOUNDWAVE_MIN_RADIUS) * MathF.Pow(Progress, 2) + GemChimes.SOUNDWAVE_MIN_RADIUS;
                Radius = baseRadius * MathHelper.Lerp(0.25f, 1, MathF.Pow(progressThruCache, 2)) - Width / 2;

                Ring ??= new PrimitiveClosedLoop(40, WidthFunction, ColorFunction);
                Ring.SetPositionsEllipse(position, Radius, 0, 0.5f, Projectile.rotation);

                Effect effect = Scene["SoundwaveRing"].GetShader().Shader;
                effect.Parameters["Width"].SetValue(Width);
                effect.Parameters["Amplitude"].SetValue(amplitude);
                effect.Parameters["Distortion"].SetValue(0.65f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly * 6.5f + i);
                effect.Parameters["Repeats"].SetValue(2);
                effect.Parameters["Voronoi"].SetValue(AssetDirectory.NoiseTextures.Voronoi.Value);

                // Optional second layer of distortion
                effect.Parameters["Amplitude2"].SetValue(amplitude * 2);
                effect.Parameters["Distortion2"].SetValue(2);

                Color highlight = AttackMode switch
                {
                    0 => new Color(0, 205, 113),
                    1 => new Color(255, 128, 246),
                    2 => new Color(87, 217, 236),
                    _ => new Color(255, 152, 128)
                } * progressThruCache;

                Color baseColor = AttackMode switch
                {
                    0 => new Color(10, 143, 93),
                    1 => new Color(192, 48, 245),
                    2 => new Color(89, 166, 255),
                    _ => new Color(238, 51, 53)
                } * progressThruCache;

                Color white = Color.Lerp(Color.White, highlight, 1 - progressThruCache);

                effect.Parameters["White"].SetValue(white.ToVector4());
                effect.Parameters["HighlightColor"].SetValue(highlight.ToVector4());
                effect.Parameters["MidrangeColor"].SetValue(baseColor.ToVector4());
                effect.Parameters["ShadowColor"].SetValue(baseColor.ToVector4() * 0.5f);

                Ring?.Render(effect, -Main.screenPosition);
            }
            */
        }
        #endregion
    }

    [Serializable]
    public class GemChimesResonanceDataModule(NPC npc, GemChimesResonanceData data) : FablesNPC.SyncNPCMiscData<GemChimesResonanceData>(npc, data) { }

    public class GemChimesNPCDebuff : ModBuff
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        #region NPC Data
        /// <summary>
        /// Track all data related to <see cref="GemChimesNPCDebuff"/>.
        /// </summary>
        [Serializable]
        public class GemChimesResonanceData : CustomGlobalData
        {
            public int ResonanceDamage = DefaultResonanceDamage;
            public int Mode;
            public bool Resonating = false;
            public int ResonanceTime = RESONANCE_LENGTH;
            public int PulseTimer = 0;

            public int CanApply = -1;   // Designed for segmented enemies, but also affects which buff can be re-applied

            public GemChimesResonanceData(Projectile proj, NPC target)
            {
                ResonanceDamage = (int)(proj.damage * RESONANCE_DAMAGE_MULT);
                Mode = (int)proj.ai[0];
                SetModeWhitelist(target, this);  // Set in constructor to instantly apply whitelist
            }

            public override void Reset() => CanApply = -1;
        }

        #endregion

        public override void Load() => FablesNPC.PostAIEvent += PostAI;

        public override void Unload() => FablesNPC.PostAIEvent -= PostAI;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Resonating");
            Description.SetDefault("Vibrating at a dangerous frequency");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Removes debuff if it's associated data cannot be found
            if (!npc.GetNPCData<GemChimesResonanceData>(out var data))
            {
                npc.RequestBuffRemoval(ModContent.BuffType<GemChimesNPCDebuff>());
                return;
            }

            npc.SetNPCFlag(Name);
            SetModeWhitelist(npc, data);

            // Keeps buff active while resonating
            if (data.Resonating)
                npc.buffTime[buffIndex]++;

            PassiveVisuals(npc, data);

            // End visuals on last frame
            if (data.ResonanceTime == 1)
                ResonanceExplosion(npc, data);
        }

        private static void SetModeWhitelist(NPC npc, GemChimesResonanceData data)
        {
            data.CanApply = data.Mode;

            // Propogate data to main segment for segmented enemies
            if (npc.GetIFramesProvider(out int iframesProvider))
            {
                NPC immunityProvider = Main.npc[iframesProvider];
                if (!immunityProvider.GetNPCData<GemChimesResonanceData>(out var parentData))
                {
                    parentData = data;
                    immunityProvider.SetNPCData(parentData);
                }

                parentData.CanApply = data.Mode;
            }
        }

        private void PostAI(NPC npc)
        {
            // Runs if the target NPC has the debuff
            if (!npc.GetNPCFlag(Name))
                return;

            // Removes debuff if it's associated data cannot be found
            if (!npc.GetNPCData<GemChimesResonanceData>(out var data))
            {
                npc.RequestBuffRemoval(ModContent.BuffType<GemChimesNPCDebuff>());
                return;
            }
            int mode = data.Mode;
            int resonanceTime = data.ResonanceTime;
            bool resonating = data.Resonating;

            // Start countdown while resonating
            if (resonating)
                data.ResonanceTime--;

            // Find another NPC to resonate with
            if (!resonating)
            {
                foreach (NPC target in Main.ActiveNPCs)
                {
                    // Only check NPCs within range
                    if (npc.Distance(target.Center) > RESONANCE_MAX_DISTANCE)
                        continue;

                    // If the NPC with the debuff is a dummy, make it only able to resonate with other dummies
                    // Still allows for damage testing but no funky cheese sorry guys
                    if (target.type == NPCID.TargetDummy && npc.type != NPCID.TargetDummy)
                        continue;

                    if (target.whoAmI != npc.whoAmI && target.HasBuff<GemChimesNPCDebuff>() && target.GetNPCData<GemChimesResonanceData>(out var targetData) && targetData.Mode == mode)
                    {
                        resonating = true;

                        // Create connecting particle and play sound only once when resonance starts
                        if (!targetData.Resonating || targetData.ResonanceTime < RESONANCE_LENGTH) {
                            SoundEngine.PlaySound(Resonate, npc.Center);

                            ConnectingLineParticle line = new SoundwaveConnectingLine(GemChimeSoundwave.HighlightColor(mode), GemChimeSoundwave.BaseColor(mode), 21, 30, 6, 0.8f, npc.Center);
                            line.Attach(npc, target);
                            ParticleHandler.SpawnParticle(line);
                        }
                        break;
                    }
                }
            }

            // Set pulse timer to 0 if just started resonating
            // This stops the pulses from being synced with the global timer
            if (resonating && !data.Resonating)
                data.PulseTimer = 0;
            data.Resonating = resonating;

            if (resonanceTime <= 0)
            {
                // Damage affected NPC and remove buff
                npc.SimpleStrikeNPC(data.ResonanceDamage, 0, damageType: DamageClass.Magic, damageVariation: true);
                npc.RequestBuffRemoval(ModContent.BuffType<GemChimesNPCDebuff>());
            }
        }

        #region Visuals
        private static void PassiveVisuals(NPC npc, GemChimesResonanceData data)
        {
            int mode = data.Mode;
            bool resonating = data.Resonating;

            // Values initialized before either visual since it's used in both
            Color baseColor = GemChimeSoundwave.BaseColor(mode);
            Color highlightColor = GemChimeSoundwave.HighlightColor(mode);
            float radius = GetPulseRadius((npc.width + npc.height) / 2f);
            float resonanceCompletion = data.ResonanceTime / (float)RESONANCE_LENGTH;

            // Pulses at a synced rate when not resonating
            if (!resonating)
                data.PulseTimer = (int)(Main.GlobalTimeWrappedHourly * 60) % 30;
            // Decrement pulse timer when resonating
            else
                data.PulseTimer--;

            if (data.PulseTimer <= 0)
            {
                // Time between pulses decreases while resonating
                int pulseTime = data.PulseTimer = (int)MathHelper.Lerp(8, 30, resonanceCompletion);
                if (resonating)
                    data.PulseTimer = pulseTime;

                // Normally draw one ring, draw two while resonating
                for (int i = 0; i < (resonating ? 2 : 1); i++)
                {
                    // Upscaled further while resonating, or this is the second wave
                    float pulseRadius = radius;
                    pulseRadius *= MathHelper.Lerp(1.5f, 1.2f, resonanceCompletion);
                    if (i > 0)
                        pulseRadius *= MathHelper.Lerp(1.5f, 1.2f, resonanceCompletion);

                    // Ampltide, width, and opacity changes while resonating or this is the second pulse
                    float amplitude = MathHelper.Lerp(6, 4, resonanceCompletion);
                    float width = 5 + amplitude * 2 + (7 * i);
                    float opacity = 0.8f - 0.3f * i;

                    // Spawn particle, attach it to the target, and change easing
                    PrimitiveRingParticle ring = new SoundwaveRing(npc.Center, Vector2.Zero, highlightColor, baseColor, pulseRadius, width, amplitude, opacity, (int)(pulseTime * 1.2f));
                    ring.ModifyEasings(radiusEasingDegree: 3).Attach(npc, true);
                    ParticleHandler.SpawnParticle(ring);
                }
            }

            // Spawn sparkles within radius
            int spawnChance = (int)MathHelper.Lerp(20, 40, resonanceCompletion);
            if (Main.rand.NextBool(spawnChance))
            {
                Vector2 particlePosition = npc.Center + Main.rand.NextVector2Circular(radius * 1.25f, radius * 1.25f);
                Vector2 particleVelocity = npc.velocity * Main.rand.NextFloat(0.2f, 0.5f);

                Particle sparkle = new TwinklySparkle(particlePosition, particleVelocity, baseColor, 0.3f) { Pixelated = true };
                ParticleHandler.SpawnParticle(sparkle);
            }
        }

        private static void ResonanceExplosion(NPC npc, GemChimesResonanceData data)
        {
            // Values initialized before visuals
            Color baseColor = GemChimeSoundwave.BaseColor(data.Mode);
            Color highlightColor = GemChimeSoundwave.HighlightColor(data.Mode);
            float radius = GetPulseRadius((npc.width + npc.height) / 2f);

            // Create a unique soundwave particle
            float pulseRadius = radius * 2.5f;

            // Spawn particle, attach it to the target, and change easing
            PrimitiveRingParticle ring = new SoundwaveRing(npc.Center, Vector2.Zero, highlightColor, baseColor, pulseRadius, 27, 6, 0.8f, 40);
            ring.ModifyEasings(radiusEasingDegree: 3, widthEasing: FablesUtils.PolyInEasing, widthEasingDegree: 1.2f, invertWidthEasing: true).Attach(npc, true);
            ParticleHandler.SpawnParticle(ring);

            // Shine particle centered on the target
            float shineScale = MathF.Min(radius / 84f * 1.5f, 1.5f);
            ShineFlashParticle shine = new ShineFlashParticle(npc.Center, Vector2.Zero, highlightColor, shineScale) { Pixelated = true };
            ParticleHandler.SpawnParticle(shine);

            // Sparkle particles that spread out from the target center
            float particleSpeed = radius / 8f;
            for (int i = 0; i < Main.rand.NextFloat(5, 10); i++)
            {
                Vector2 particlePosition = npc.Center;
                Vector2 particleVelocity = Main.rand.NextVector2Unit() * particleSpeed * Main.rand.NextFloat(0.5f, 1);

                Particle sparkle = new TwinklySparkle(particlePosition, particleVelocity, baseColor, 0.3f) { Pixelated = true };
                ParticleHandler.SpawnParticle(sparkle);
            }
        }

        /// <summary>
        /// Reduces radius slightly as it gets larger. Helps prevent super large rings.
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        private static float GetPulseRadius(float radius) => radius > 50 ? MathF.Pow(radius - 50f, 0.9f) + 50f : radius;

        #endregion
    }
}