using CalamityFables.Cooldowns;
using Terraria.Localization;
using Terraria.Map;
using Terraria.UI;
using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using CalamityFables.Content.Items.Cursed;
using static CalamityFables.Helpers.FablesUtils;
using Terraria.ModLoader.IO;
using Humanizer;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class SporePod : ModItem
    {
        public static readonly SoundStyle WarpPodBloomSound = new SoundStyle(SoundDirectory.CrabulonDrops + "WarpPodBeacon");
        public static readonly SoundStyle WarpPodStretchSound = new SoundStyle(SoundDirectory.CrabulonDrops + "WarpPodStretch");
        public static readonly SoundStyle WarpPodOpenSound = new SoundStyle(SoundDirectory.CrabulonDrops + "WarpPodOpen");

        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public const float WARPCOOLDOWN = 60f * 60f * 1.5f;

        public const int CAMERA_LOCK_TIME = (int)(60 * 6.5f);
        public const int BLOOM_TIME = (int)(60 * 1.5f);
        public const int PRE_BLOOMTIME = (int)(60 * 1.8f);

        public const int TELEPORT_STYLE = 102;

        public static int PlayerRespawnAnimationTime => PRE_BLOOMTIME + BLOOM_TIME;
        public static int FleshWeavingTime => CAMERA_LOCK_TIME + PRE_BLOOMTIME + BLOOM_TIME;

        public override void Load()
        {
            FablesGeneralSystemHooks.CustomTeleportationEffectEvent += CustomWarpVisuals;
            FablesPlayer.ImmuneToEvent += ImmuneWhileFleshWeaving;
            FablesPlayer.SetControlsEvent += ImmobilizeWhileFleshWeaving;
            FablesDrawLayers.ModifyPlayerDrawingEvent += GrowOutFromBloom_ScaleUp;

            //Necessary or else the capture will have scale of 0
            PlayerCapture.CacheVariablesBeforeCaptureEvent += CacheFleshWeavingTime;
            PlayerCapture.RestoreCachedVariablesAfterCaptureEvent += RestoreFleshWeavingTime;
        }


        public static int cachedFleshWeavingTime;
        private void CacheFleshWeavingTime(Player player)
        {
            cachedFleshWeavingTime = player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime;
            player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime = 0;
        }
        private void RestoreFleshWeavingTime(Player player)
        {
            player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime = cachedFleshWeavingTime;
        }

        private void ImmobilizeWhileFleshWeaving(Player player)
        {
            if (player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime > 0)
            {
                // Disable inputs
                player.controlLeft = false;
                player.controlRight = false;
                player.controlUp = false;
                player.controlDown = false;
                player.controlJump = false;
                player.controlHook = false;
                player.controlInv = false;
                player.controlUseItem = false;
                player.controlUseTile = false;
            }
        }

        private bool ImmuneWhileFleshWeaving(Player player, PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            if (player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime > 0)
                return true;
            return false;
        }

        private bool CustomWarpVisuals(Rectangle effectRect, int Style, int extraInfo, float dustCountMult, TeleportationSide side, Vector2 otherPosition)
        {
            if (Style != TELEPORT_STYLE)
                return false;

            return true;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Spore Pod");
            Tooltip.SetDefault("Places down a warp pod you can teleport back to anytime\n" +
                "Click the warp pod on the fullscreen map to teleport\n" +
                "The warp pod withers when teleported to or when you die");
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 22;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.value = Item.sellPrice(silver: 5);
            Item.rare = ItemRarityID.Green;

            Item.shootSpeed = 6;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.shoot = ModContent.ProjectileType<WarpPod>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            damage = 0;
            velocity.Y = -5;
            velocity.X *= 0.4f;
            position = player.Top;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                    p.Kill();
            }

            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        private void GrowOutFromBloom_ScaleUp(Player player, ref Vector2 position, ref float rotation, ref float scale, ref float shadow)
        {
            int fleshWeavingTime = player.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime;
            if (fleshWeavingTime > 0)
            {
                if (fleshWeavingTime < BLOOM_TIME - 34)
                {
                    shadow = float.Epsilon;
                    float scalar = Utils.GetLerpValue(BLOOM_TIME - 34 , 0, fleshWeavingTime, true);
                    scale *= PlayerScaleUp(scalar);
                    position.Y -= (1 - (float)Math.Pow(scalar, 3f)) * 15f;
                    //position.Y -= MathF.Sin(scalar) * 10f;
                }
                else
                    scale = 0;
            }
        }

        public float PlayerScaleUp(float progress) => PiecewiseAnimation(progress,
            new CurveSegment(PolyInEasing, 0, 0, 1.1f, 1.4f),
            new CurveSegment(PolyInOutEasing, 0.30f, 1.1f, -0.1f, 2f)
            //new CurveSegment(SineOutEasing, 0.5f, 1f, -0.15f),
            //new CurveSegment(PolyOutEasing, 0.65f, 0.85f, 0.15f)
            );
    }

    public class SporeWarpPlayer : ModPlayer
    {
        public Vector2? warpSporePosition;
        public Dictionary<int, Vector2> worldToWarpPos = new();

        public int fleshWeavingTime;
        public int warpBudIndex;

        public override void ResetEffects()
        {
            warpBudIndex = -1;
        }

        public override void PreUpdate()
        {
            int fleshWeavingIndex = Player.FindBuffIndex(ModContent.BuffType<WarpPodFleshWeaving>());
            if (fleshWeavingIndex == -1)
                fleshWeavingTime = 0;
            else
            {
                Dust.QuickDust(Player.Center - Vector2.UnitY * fleshWeavingTime / 30f, Color.Red);
                fleshWeavingTime = Player.buffTime[fleshWeavingIndex];
                if (fleshWeavingTime == SporePod.BLOOM_TIME - 40)
                    ParticleHandler.SpawnParticle(new CircularPulseShine(Player.Center, Color.RoyalBlue, 1f));
            }

            if (warpSporePosition.HasValue && warpBudIndex == -1 && Player.whoAmI == Main.myPlayer)
            {
                Vector2 projectilePosition = warpSporePosition.Value - Vector2.UnitY * 8f + ModContent.GetInstance<WarpPod>().Projectile.Size / 2f;

                Projectile.NewProjectile(Player.GetSource_FromThis(), projectilePosition, Vector2.Zero, ModContent.ProjectileType<WarpPod>(), 0, 0, Main.myPlayer, 0, 1);
            }
        }

        public override void OnEnterWorld()
        {
            if (worldToWarpPos != null && worldToWarpPos.ContainsKey(Main.worldID))
            {
                warpSporePosition = worldToWarpPos[Main.worldID];
            }
        }

        public override void SaveData(TagCompound tag)
        {
            TagCompound sporePodPositionTag = new TagCompound();

                //Register or erase warp pos as necessary
            if (warpSporePosition.HasValue)
                worldToWarpPos[Main.worldID] = warpSporePosition.Value;
            else if (worldToWarpPos.ContainsKey(Main.worldID))
                worldToWarpPos.Remove(Main.worldID);

            tag["WarpPodWorldCount"] = worldToWarpPos.Count;

            int i = 0;
            foreach (var item in worldToWarpPos)
            {
                sporePodPositionTag.Add("SporePodWorld" + i.ToString(), item.Key);
                sporePodPositionTag.Add("SporePodPosition" + i.ToString(), item.Value);
                i++;
            }

            tag["WarpPodData"] = sporePodPositionTag;
        }

        public override void LoadData(TagCompound tag)
        {
            worldToWarpPos.Clear();
            if (!tag.TryGet<int>("WarpPodWorldCount", out int warpPodCount) || !tag.TryGet<TagCompound>("WarpPodData", out TagCompound warpPodData))
                return;

;           for (int i = 0; i < warpPodCount; i++)
            {
                int worldID = -1;
                Vector2 sporePos = Vector2.Zero;

                if (!warpPodData.TryGet<int>("SporePodWorld" + i.ToString(), out worldID))
                    continue;
                if (!warpPodData.TryGet<Vector2>("SporePodPosition" + i.ToString(), out sporePos))
                    continue;

                worldToWarpPos.Add(worldID, sporePos);
            }

            //I HAVE TO DO THIS OTHERWISE IT GETS CLEARED BECAUSE SAVEDATA GETS CALLED RIGHT AFTERWARDS???
            if (worldToWarpPos != null && worldToWarpPos.ContainsKey(Main.worldID))
                warpSporePosition = worldToWarpPos[Main.worldID];
        }
    }

    public class WarpPodMapMarker : ModMapLayer
    {
        public static Asset<Texture2D> MapIcon;
        public static bool MouseOver = false;


        public static LocalizedText warpPodLabel;
        public override void Load()
        {
            warpPodLabel = Mod.GetLocalization("Extras.MapIcons.WarpPod");
        }


        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            SporeWarpPlayer mp = Main.LocalPlayer.GetModPlayer<SporeWarpPlayer>();
            if (!mp.warpSporePosition.HasValue)
                return;
            Vector2 warpPosition = mp.warpSporePosition.Value;

            bool clickFunction = Main.mapFullscreen && (!Main.LocalPlayer.HasCooldown(WarpPodCooldown.ID));

            if (MapIcon == null)
                MapIcon = ModContent.Request<Texture2D>(AssetDirectory.UI + "SporePodWarp");
            Texture2D icon = MapIcon.Value;
            float sizeWhenHovered = clickFunction ? 1.15f : 1f;

            if (context.Draw(icon, warpPosition / 16, Color.White, new(1, 1, 0, 0), 1f, sizeWhenHovered, Alignment.Center).IsMouseOver && clickFunction)
            {
                if (!MouseOver)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    MouseOver = true;
                }

                text = Language.GetTextValue("Game.TeleportTo", warpPodLabel.Value);
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    Main.mapFullscreen = false;
                    Main.LocalPlayer.AddCooldown(WarpPodCooldown.ID, (int)SporePod.WARPCOOLDOWN);
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<WarpPodFleshWeaving>(), SporePod.FleshWeavingTime, false);
                    mp.warpSporePosition = null;

                    //Spawns a visual corpse
                    float sidewaysDisplacement = Main.rand.NextFloat(40f, 330f);
                    if (Main.rand.NextBool())
                        sidewaysDisplacement *= -1;
                    Projectile.NewProjectile(Main.LocalPlayer.GetSource_Misc("Spore Pod Warp"), Main.LocalPlayer.Center, Vector2.Zero, ModContent.ProjectileType<SporedCorpse>(), 0, 0, Main.myPlayer, Main.myPlayer, sidewaysDisplacement, 1f);

                    int bulbIndex = Main.LocalPlayer.GetModPlayer<SporeWarpPlayer>().warpBudIndex;
                    if (bulbIndex >= 0)
                    {
                        Main.projectile[bulbIndex].ai[0] = 1;
                        Main.projectile[bulbIndex].timeLeft = SporePod.FleshWeavingTime;
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, bulbIndex);
                    }

                    //Locks the camera for a few seconds
                    CameraManager.LockOn.SetLockPosition(Main.LocalPlayer.Center, SporePod.CAMERA_LOCK_TIME, 50);
                    Main.LocalPlayer.CustomTeleport(warpPosition, SporePod.TELEPORT_STYLE);

                    //This is done in case of the player ending up super far from the corpse and not haering the sound get played from the projectiles perspective
                    SoundEngine.PlaySound(SporedCorpse.DeathSound with { MaxInstances = 1 });
                }
            }

            else
                MouseOver = false;
        }
    }

    public class WarpPodFleshWeaving : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            DisplayName.SetDefault("Flesh Weaving");
            Description.SetDefault("Flesh to spores, spores to flesh");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.breath = player.breathMax;
            player.noKnockback = true;
            player.noFallDmg = true;
            player.noThrow = 255;
            player.velocity = -Vector2.UnitY * player.gravity;
            if (player.mount.Active)
                player.mount.Dismount(player);
        }
    }

    public class WarpPod : ModProjectile
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name + "Bulb";
        public static Asset<Texture2D> FlowerTexture;
        public float budRotation = 0f;
        public int budRotationTimer = 0;

        public Asset<Texture2D> StrandTexture;
        public Asset<Texture2D> SeedTexture;
        public VerletNet strands;

        public bool Blooming {
            get => Projectile.ai[0] == 1;
            set => Projectile.ai[0] = value ? 1 : 0;
        }

        public bool Anchored {
            get => Projectile.ai[1] == 1;
            set => Projectile.ai[1] = value ? 1 : 0;
        }

        public Player Owner => Main.player[Projectile.owner];
        public float Timer => SporePod.FleshWeavingTime - Projectile.timeLeft;

        public Vector2 CenterOfMass => Projectile.Bottom - Vector2.UnitY * Projectile.height * 0.25f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Warp Pod");
        }

        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 20;
            Projectile.height = 50;
            Projectile.aiStyle = -1;

            Projectile.netImportant = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 120;
        }

        public bool playedPlantSound = false;

        public bool playedBloomSound = false;
        public bool playedStretchSound = false;
        public bool playedOpenSound = false;

        public override void AI()
        {
            if (Anchored)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = 0;

                //Breaks if no tile below itself
                if (!Blooming && !SolidCollisionFix(Projectile.BottomLeft, Projectile.width, 16, true))
                {
                    Projectile.Kill();
                    return;
                }

                if (!playedPlantSound)
                {
                    playedPlantSound = true;
                    SoundEngine.PlaySound(Crabulon.SporeMortarLandSound, Projectile.Center);
                }
            }

            //Simply fall
            else
            {
                if (!Main.dedServ)
                {
                    if (strands == null)
                        InitializeStrands();
                    UpdateStrandAttachPoints();
                    strands.Update(7, -Projectile.velocity.SafeNormalize(Vector2.UnitY) * 1.2f);
                }

                Projectile.rotation += Projectile.velocity.Y * 0.03f + Projectile.velocity.X * 0.04f;
                Projectile.velocity.X *= 0.98f;
                Projectile.velocity.Y += 0.2f;
                return;
            }

            if (!Blooming)
            {
                Projectile.rotation = 0;
                //Stay alive
                if (Projectile.timeLeft < 2)
                    Projectile.timeLeft = 2;

                AnimateIdle();

                //Grow in size
                Projectile.scale = (float)Math.Pow(Math.Min(Timer / 12f, 1f), 0.4f);
                if (Timer == 0)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                        Dust d = Dust.NewDustPerfect(CenterOfMass + direction * 12f, DustID.GemSapphire, direction * 1.4f, 0);
                        d.noGravity = true;
                    }
                }

                SporeWarpPlayer mp = Owner.GetModPlayer<SporeWarpPlayer>();
                mp.warpSporePosition = Projectile.TopLeft + Vector2.UnitY * 8f ;
                mp.warpBudIndex = Projectile.whoAmI;
            }

            else
            {
                Projectile.scale = 1f;

                if (Projectile.timeLeft > SporePod.BLOOM_TIME)
                {
                    AnimateIdle();

                    if (Projectile.timeLeft < SporePod.BLOOM_TIME + SporePod.PRE_BLOOMTIME)
                    {
                        if (!playedBloomSound)
                        {
                            SoundEngine.PlaySound(SporePod.WarpPodBloomSound, Projectile.Center);
                            playedBloomSound = true;
                        }

                        Vector2 dustOrigin = Projectile.Bottom + (budRotation * 0.1f - MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(50f, 90f);
                        if (Main.rand.NextBool(9))
                        {
                            Dust d = Dust.NewDustPerfect(dustOrigin + Main.rand.NextVector2Circular(5f, 5f), DustID.ShimmerSpark, (budRotation * 0.1f - MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1.6f));
                            d.noGravity = true;
                            d.noLight = true;
                            d.noLightEmittence = true;
                        }
                    }

                }

                else
                {
                    float progress = 1 - Projectile.timeLeft / (float)SporePod.BLOOM_TIME;
                    Projectile.frame = 10 + (int)(progress * 12);

                    if (Projectile.frame >= 15 && Projectile.frame < 19 && Main.rand.NextBool())
                        BloomVisuals();

                    if (!playedStretchSound)
                    {
                        SoundEngine.PlaySound(SporePod.WarpPodStretchSound, Projectile.Center);
                        playedStretchSound = true;
                    }
                    if (!playedOpenSound && progress > 0.45f)
                    {
                        SoundEngine.PlaySound(SporePod.WarpPodOpenSound, Projectile.Center);
                        playedOpenSound = true;
                    }
                }
            }

            if (Owner.dead && !Blooming)
                Projectile.Kill();

            budRotationTimer++;
            budRotation += (float)Math.Sin(budRotationTimer * 0.04f) * 0.019f;
            Lighting.AddLight(Projectile.Center, new Color(30, 27, 176).ToVector3() * 3);
        }

        public void AnimateIdle()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 5)
            {
                Projectile.frame++;
                if (Projectile.frame >= 10)
                    Projectile.frame = 0;

                Projectile.frameCounter = 0;
            }

            if (Main.rand.NextBool(24))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.7f, DustID.GlowingMushroom);
                d.scale = Main.rand.NextFloat(0.8f, 1.1f);
            }

            if (Main.rand.NextBool(12))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 3f, Projectile.height) * 0.7f, DustID.ShimmerTorch);
                d.scale = Main.rand.NextFloat(0.8f, 1.1f);
                d.velocity.X *= 0.3f;
                d.velocity.Y = -Main.rand.NextFloat(0.3f, 1f);

                d.noGravity = true;
                d.noLightEmittence = true;
                d.noLight = true;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.Y > 0 && Projectile.velocity.Y == 0)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Anchored = true;

                Projectile.netUpdate = true;
                Projectile.timeLeft = SporePod.FleshWeavingTime;
            }

            return false;
        }

        public float WidthFunction(float completion) => 6f;

        public static Color PrimsLightColor;
        public Color ColorFunction(float completion) => PrimsLightColor;

        public void InitializeStrands()
        {
            strands = new VerletNet();

            VerletPoint[] mainAttachPoints = new VerletPoint[3];
            for (int i = 0; i < 3; i++)
            {
                //Do three attach points
                mainAttachPoints[i] = new VerletPoint(Projectile.Center + (i / 3f * MathHelper.TwoPi).ToRotationVector2() * 6f, true);
            }

            VerletPoint[] loops = new VerletPoint[10];
            for (int i = 0; i < 3; i++)
            {
                float loopRotation = (i / 3f * MathHelper.TwoPi + MathHelper.TwoPi / 6f);
                Vector2 loopCenter = Projectile.Center + loopRotation.ToRotationVector2() * 14f;
                loops[0] = mainAttachPoints[i];
                loops[9] = mainAttachPoints[(i + 1) % 3];

                for (int j = 1; j < 9; j++)
                {
                    loops[j] = new VerletPoint(loopCenter + ((j / 10f) * MathHelper.TwoPi * 0.8f - MathHelper.TwoPi * 0.4f + loopRotation).ToRotationVector2() * 18f);
                }

                strands.AddChain(WidthFunction, ColorFunction, loops);
            }

            Vector2 away = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                //long strands
                float distance = i == 0 ? 60f : 24;
                strands.AddChain(mainAttachPoints[i], new VerletPoint(mainAttachPoints[i].position + away * distance), 10, WidthFunction, ColorFunction);
            }
        }

        public void UpdateStrandAttachPoints()
        {
            for (int i = 0; i < 3; i++)
            {
                //Do three attach points
                strands.extremities[i].position = Projectile.Center + (i / 3f * MathHelper.TwoPi + Projectile.rotation).ToRotationVector2() * 6f;
            }
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    float direction = (i / 3f + 1 / 6f) * MathHelper.TwoPi + Projectile.rotation;
                    direction += (j / 10f) * MathHelper.TwoPi * 0.8f - MathHelper.TwoPi * 0.4f;

                    strands.chains[i][j].customGravity = Vector2.Lerp(-Projectile.velocity.SafeNormalize(Vector2.UnitY) * 2f, direction.ToRotationVector2() * 2f, 0.6f);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (timeLeft == 0)
                return;

            SoundEngine.PlaySound(Crabulon.SporeMortarLandSound with { Volume = Crabulon.SporeMortarLandSound.Volume * 0.5f, Pitch = 0.5f }, Projectile.Center);
            Owner.GetModPlayer<SporeWarpPlayer>().warpSporePosition = null;

            if (!Anchored)
                return;

            if (Main.myPlayer == Projectile.owner)
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Bottom, Vector2.Zero, ModContent.ProjectileType<SporePodSplort>(), 0, 0, Main.myPlayer);

            //Bloom a lot
            for (int i = 0; i < 20; i++)
                BloomVisuals();
        }

        public void BloomVisuals()
        {
            float smokeSize = Main.rand.NextFloat(1.4f, 2.6f);
            Vector2 gushDirection = Main.rand.NextFloat(-MathHelper.Pi, 0).ToRotationVector2();

            Vector2 origin = CenterOfMass - Vector2.UnitY * 6f;
            Vector2 smokeCenter = origin + gushDirection * 22f * Main.rand.NextFloat(0.2f, 0.55f);

            Vector2 velocity = gushDirection * Main.rand.NextFloat(0.9f, 2.8f);
            velocity.X *= 0.66f;
            Particle smoke = new SporeGas(smokeCenter, velocity, origin, 92f, smokeSize, 0.01f);
            smoke.FrontLayer = Main.rand.NextBool();

            ParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(4))
            {
                gushDirection = Main.rand.NextFloat(-MathHelper.Pi, 0).ToRotationVector2();
                Vector2 dustPosition = CenterOfMass + gushDirection * 22f * Main.rand.NextFloat(0.1f, 0.6f);
                velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.6f, 3.6f);
                Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                d.noLightEmittence = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Anchored)
            {
                SeedTexture = SeedTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporePodProjectile");
                StrandTexture = StrandTexture ?? ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumStrand");

                PrimsLightColor = lightColor;
                strands?.Render(StrandTexture.Value, -Main.screenPosition);
                Texture2D sporePod = SeedTexture.Value;
                Main.EntitySpriteDraw(sporePod, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, sporePod.Size() / 2f, Projectile.scale, 0, 0);
                return false;
            }

            FlowerTexture = FlowerTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + Name + "Flower");
            Texture2D flowerBase = FlowerTexture.Value;

            Vector2 basePos = Projectile.Bottom;
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 size = new Vector2(1f + 0.2f * (float)(0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 3f + Projectile.whoAmI / 200f * 0.2f) + 0.5));
            size.Y = 2 - size.X;

            Rectangle bulbFrame = tex.Frame(1, 10, 0, Projectile.frame, 0, -2);
            Vector2 origin = new Vector2(bulbFrame.Width * 0.5f, bulbFrame.Height);

            Rectangle flowerFrame = flowerBase.Frame(1, 22, 0, Projectile.frame, 0, -2);
            Vector2 flowerOrigin = new Vector2(flowerFrame.Width / 2f - 2, flowerFrame.Height - 14);
            Vector2 flowerSize = Vector2.Lerp(Vector2.One, size, (size.X - 1f) / 0.2f);

            if (Blooming && Projectile.timeLeft < 20f)
                lightColor *= Projectile.timeLeft / 20f;

            if (Projectile.frame < 10)
                Main.EntitySpriteDraw(tex, basePos + Vector2.UnitY * 10f * Projectile.scale - Main.screenPosition, bulbFrame, lightColor, budRotation * 0.1f, origin, size * Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(flowerBase, basePos - Main.screenPosition, flowerFrame, lightColor, 0f, flowerOrigin, flowerSize * (float)Math.Pow(Projectile.scale, 0.5f), 0, 0);
            
            if (Blooming && Projectile.timeLeft <= SporePod.BLOOM_TIME + SporePod.PRE_BLOOMTIME && Projectile.timeLeft > SporePod.BLOOM_TIME)
            {
                float beamOpacity = 1f;
                float progress = (Projectile.timeLeft - SporePod.BLOOM_TIME) / (float)SporePod.PRE_BLOOMTIME;
                if (progress < 0.35f)
                    beamOpacity *= progress / 0.35f;
                if (progress > 0.8f)
                    beamOpacity = MathF.Sin(MathHelper.PiOver2 + MathHelper.PiOver2 * Utils.GetLerpValue(0.8f, 1f, progress, true));

                Texture2D beamTex = ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + Name + "GlowBeam").Value;
                Color beamGlow = new Color(80, 50, 255, 0);
                Vector2 beamOrigin = new Vector2(beamTex.Width / 2f, beamTex.Height);
                Vector2 beamOffset = (budRotation * 0.1f - MathHelper.PiOver2).ToRotationVector2() * 42f * size.Y;

                Main.EntitySpriteDraw(beamTex, basePos + beamOffset - Main.screenPosition, null, beamGlow * beamOpacity, budRotation * 0.1f, beamOrigin, flowerSize * (float)Math.Pow(Projectile.scale, 0.5f), 0, 0);

            }

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }
    }

    public class SporePodSplort : PeculiarPotTarBlot, IDrawOverTileMask
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "SporeSplat";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Splort");
        }

        public override void DrawOverMask(SpriteBatch spriteBatch, bool solidLayer)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float opacity = (float)Math.Pow(Projectile.timeLeft / 60f, 0.5f);
            Color color = Lighting.GetColor(Projectile.Top.ToTileCoordinates()) * opacity;

            Vector2 size = new Vector2(1 + SmearAmount * 0.6f, 1f);

            if (Projectile.timeLeft > 55)
                size *= (float)Math.Pow(Utils.GetLerpValue(60, 55, Projectile.timeLeft, true), 0.2f);
            else
                size.Y += (float)Math.Pow(Utils.GetLerpValue(40, 0, Projectile.timeLeft, true), 1.2f) * 0.2f;

            spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color, SplotchRotation, tex.Size() / 2, size, 0, 0);
        }
    }
}
