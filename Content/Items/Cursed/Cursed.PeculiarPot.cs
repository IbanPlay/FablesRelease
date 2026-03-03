using CalamityFables.Cooldowns;
using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Shaders;
using Terraria.Localization;

namespace CalamityFables.Content.Items.Cursed
{
    public class PeculiarPot : ModItem
    {
        public override string Texture => AssetDirectory.CursedItems + Name;

        public const int MAX_POT_HEALTH = 100;
        public const float POT_REGENERATION_TIME = 15f;
        public const float POT_REGENERATION_TIME_DELAY = 5f;

        public const float DEBUFF_TIME = 8;
        public const float DEBUFF_DAMAGE_MULTIPLIER_MAX = 0.1f;
        public const float DEBUFF_EXPONENT = 0.2f;

        public const int POT_DAMAGE_ABSORB = 12;

        public static readonly SoundStyle FormSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotForm") { Volume = 0.85f };
        public static readonly SoundStyle BreakSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotBreak") { Volume = 0.6f };
        public static readonly SoundStyle RollingSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotRollingLoop") { IsLooped = true , MaxInstances = 0 };
        public static readonly SoundStyle JumpSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotJump", 2) { Volume = 0.5f, PitchVariance = 0.2f };
        public static readonly SoundStyle ChipSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotCrack", 2);
        public static readonly SoundStyle LandingSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotBounce", 2);
        public static readonly SoundStyle RepairSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotFullHealth");
        public static readonly SoundStyle AvailableSound = new SoundStyle("CalamityFables/Sounds/PeculiarPotRegenerate");

        public override void Load()
        {
            if (!Main.dedServ)
            {
                EquipLoader.AddEquipTexture(Mod, Texture + "_Head1", EquipType.Head, this);
                EquipLoader.AddEquipTexture(Mod, Texture + "_Head2", EquipType.Head, this, "PeculiarPotChipped");
                EquipLoader.AddEquipTexture(Mod, Texture + "_Head3", EquipType.Head, this, "PeculiarPotCracked");
            }

            FablesPlayer.DisableItemHoldEvent += DisableItemHoldEffectsWhenPotted;
        }

        private bool DisableItemHoldEffectsWhenPotted(Player player, Item item, Rectangle heldItemFrame) => player.GetModPlayer<PeculiarPotPlayer>().Potted;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(POT_DAMAGE_ABSORB, MAX_POT_HEALTH);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Peculiar Pot");
            Tooltip.SetDefault("A ceramic pot containing a strange fluid\n" +
                "Allows you to turn into tar and retract into the pot, reducing damage taken by {0}\n" +
                "You're unable to attack while inside the pot, and the pot will shatter after taking more than {1} damage\n" +
                "Damage ramps back up from zero over a short period of time after exiting the pot");

            if (!Main.dedServ)
            {
                int slot = EquipLoader.GetEquipSlot(Mod, Item.Name, EquipType.Head);
                if (slot != -1)
                    ArmorIDs.Head.Sets.DrawHead[slot] = false;

                slot = EquipLoader.GetEquipSlot(Mod, "PeculiarPotChipped", EquipType.Head);
                if (slot != -1)
                    ArmorIDs.Head.Sets.DrawHead[slot] = false;

                slot = EquipLoader.GetEquipSlot(Mod, "PeculiarPotCracked", EquipType.Head);
                if (slot != -1)
                    ArmorIDs.Head.Sets.DrawHead[slot] = false;

                for (int i = 1; i < 5; i++)
                    ChildSafety.SafeGore[Mod.Find<ModGore>("PeculiarPot_Gore" + i.ToString()).Type] = true;
            }
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noUseGraphic = true;
            Item.value = Item.sellPrice(0, 1, 0, 0);
            Item.rare = ModContent.RarityType<CursedRarity>();
            Item.UseSound = null;
            Item.noMelee = true;
            Item.mountType = ModContent.MountType<PeculiarPotMount>();
        }

        public override bool CanUseItem(Player player)
        {
            return !player.GetModPlayer<PeculiarPotPlayer>().potBroken;
        }
    }

    #region Buff & Debuff
    public class Potted : ModBuff
    {
        public override string Texture => AssetDirectory.CursedItems + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            DisplayName.SetDefault("Potted");
            Description.SetDefault("Your goopified flesh is sloshing inside the cursed pot");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<PeculiarPotMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }

    public class TarsMark : ModBuff
    {
        public override string Texture => AssetDirectory.CursedItems + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;

            DisplayName.SetDefault("Tar's Mark");
            Description.SetDefault("Tar still flows through your veins...\n" +
                $"All damage reduced by ");
        }

        public float GetDamageMultiplier(Player player, int buffIndex = -1)
        {
            if (buffIndex == -1)
            {
                for (int i = 0; i < Player.MaxBuffs; i++)
                {
                    buffIndex = i;
                    if (player.buffType[i] == Type)
                        break;
                }

                if (player.buffType[buffIndex] != Type)
                    return 1;
            }

            float damageReductionTime = Utils.GetLerpValue(0, PeculiarPot.DEBUFF_TIME * 60, player.buffTime[buffIndex], true);
            return MathHelper.Lerp(1f, PeculiarPot.DEBUFF_DAMAGE_MULTIPLIER_MAX, (float)Math.Pow(damageReductionTime, PeculiarPot.DEBUFF_EXPONENT));
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) *= GetDamageMultiplier(player, buffIndex);
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int num = 100 - (int)(GetDamageMultiplier(Main.LocalPlayer) * 100f) + 1;
            tip = tip + num + "%";
        }
    }
    #endregion

    public class PeculiarPotPlayer : ModPlayer
    {
        public float unpotTime;

        public float potHealth = PeculiarPot.MAX_POT_HEALTH;
        public int PotHealth {
            get => (int)Math.Clamp(potHealth, 0, PeculiarPot.MAX_POT_HEALTH);
            set => potHealth = Math.Clamp(value, 0f, PeculiarPot.MAX_POT_HEALTH);
        }

        public float potRegenDelay = 0f;

        public bool potBroken = false;
        public bool Potted => (Player.mount.Active && Player.mount.Type == ModContent.MountType<PeculiarPotMount>());

        public override bool CanUseItem(Item item) => !Potted || item.IsPotionLike();
        public override void ResetEffects()
        {
            unpotTime -= 1 / (60f * 0.8f);
            if (unpotTime < 0)
                unpotTime = 0;

            if (potRegenDelay > 0)
                potRegenDelay -= 1 / (60f * PeculiarPot.POT_REGENERATION_TIME_DELAY);

            if (PotHealth < PeculiarPot.MAX_POT_HEALTH && potRegenDelay <= 0)
            {
                float potHealthRegen = PeculiarPot.MAX_POT_HEALTH / (60f * PeculiarPot.POT_REGENERATION_TIME);
                potHealth += potHealthRegen;

                if (PotHealth == PeculiarPot.MAX_POT_HEALTH && Player.mount.Active && Player.mount.Type == ModContent.MountType<PeculiarPotMount>())
                {
                    SoundEngine.PlaySound(PeculiarPot.RepairSound, Player.Center);
                    ParticleHandler.SpawnParticle(new TwinkleShine(Player.MountedCenter + Vector2.UnitX * 10f, Vector2.Zero, Color.White, Color.Goldenrod, 1f, Vector2.One, Vector2.One, 20, 0.02f, 3f));
                }
            }
        }

        public override void UpdateDead()
        {
            unpotTime = 0;
            potRegenDelay = 0;
            PotHealth = PeculiarPot.MAX_POT_HEALTH;
            potBroken = false;
        }

        public bool ShouldDisplayCooldown()
        {
            //Always display if in the pot
            if (Potted)
                return true;

            //Don't display if out of the pot and its fully solid
            if (PotHealth == 100)
                return false;

            //Only always display if its in the mount slot
            return Player.miscEquips[Player.miscSlotMount].type == ModContent.ItemType<PeculiarPot>();
        }

        public override void PostUpdateMiscEffects()
        {
            //Keeps track of the broken pot
            if (PotHealth <= 0)
                potBroken = true;

            if (potBroken && PotHealth == PeculiarPot.MAX_POT_HEALTH)
            {
                potBroken = false;
                SoundEngine.PlaySound(PeculiarPot.AvailableSound, Player.Center);

                if (Player.whoAmI == Main.myPlayer)
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<PeculiarPotTarRing>(), 0, 0, Main.myPlayer);
            }

            bool shouldDisplayCooldown = ShouldDisplayCooldown();

            if (!shouldDisplayCooldown)
            {
                Player.RemoveCooldown(PeculiarPotCooldown.ID);
            }
            else if (!Player.HasCooldown(PeculiarPotCooldown.ID))
            {
                CooldownInstance durabilityCooldown = Player.AddCooldown(PeculiarPotCooldown.ID, PeculiarPot.MAX_POT_HEALTH);
                durabilityCooldown.timeLeft = PotHealth;
            }
            else if (Player.FindCooldown(PeculiarPotCooldown.ID, out var cdDurability))
                cdDurability.timeLeft = PotHealth;

            if (Potted)
            {
                Player.channel = false;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (Potted)
            {
                modifiers.FinalDamage.Flat -= PeculiarPot.POT_DAMAGE_ABSORB;
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (Potted)
            {
                int previousPotHealth = PotHealth;
                PotHealth -= info.Damage;

                float thirdAmount = PeculiarPot.MAX_POT_HEALTH / 3f;
                int previousThird = (int)(previousPotHealth / thirdAmount);
                int newThird = (int)(PotHealth / thirdAmount);


                if ((info.Damage > 26 || previousThird > newThird) && !Main.dedServ)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 position = Player.MountedCenter + Main.rand.NextVector2Circular(6f, 6f) - Vector2.UnitY * 8f;
                        Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 0.7f + Player.velocity * 0.6f;
                        if (velocity.Y > 0)
                            velocity.Y *= 0.5f;

                        Gore.NewGoreDirect(Player.mount.GetProjectileSpawnSource(Player), position, velocity, Mod.Find<ModGore>("PeculiarPot_Gore" + Main.rand.Next(1, 4).ToString()).Type);
                    }

                    for (int i = 0; i < 7; i++)
                    {
                        Dust.NewDustPerfect(Player.MountedCenter - Vector2.UnitY * 8f + Main.rand.NextVector2Circular(20f, 20f), DustID.DesertPot, Main.rand.NextVector2Circular(4f, 4f));
                    }
                }
                potRegenDelay = 1f;

            }
        }

        public override void UpdateVisibleVanityAccessories()
        {
            if (Player.armor[10].headSlot < 0 && Player.miscEquips[Player.miscSlotMount].type == ModContent.ItemType<PeculiarPot>() && !potBroken)
            {
                int potFrame = (int)((1 - PotHealth / (float)PeculiarPot.MAX_POT_HEALTH) * 2.99999f);
                string[] potEquips = new string[] { "PeculiarPot", "PeculiarPotChipped", "PeculiarPotCracked" };

                Player.head = EquipLoader.GetEquipSlot(Mod, potEquips[potFrame], EquipType.Head);
                Player.cHead = Player.cMount;
                Player.SetHeadRotation(Math.Clamp(Player.velocity.X * -0.04f, -0.2f, 0.2f));
            }
        }

        //Darken with TAR (fire) and make dust fall
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (Player.dead)
                return;

            float darken = (float)Math.Pow(1 - unpotTime, 2f);
            r *= darken;
            g *= darken;
            b *= darken;

            if (unpotTime > 0 && (Main.rand.NextFloat() > darken || Main.rand.NextBool(7)))
            {
                int i = Dust.NewDust(Player.position, Player.width, Player.height / 2, DustID.Granite, 0, 0);
                Dust d = Main.dust[i];
                d.velocity.X = 0;
                d.velocity.Y = Main.rand.NextFloat(0.2f, 1.3f);
                d.scale = Main.rand.NextFloat(1, 1.1f);
                d.velocity += Player.velocity * 0.5f;

                drawInfo.DustCache.Add(i);
            }
        }

        //Hide everything but the pot
        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (Potted && !drawInfo.headOnlyRender)
            {
                foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
                {
                    if (layer.Name != nameof(PlayerDrawLayers.MountFront))
                        layer.Hide();
                }
            }
        }
    }

    #region Visuals (projectiles)
    public class PeculiarPotTarBlot : ModProjectile, IDrawOverTileMask
    {
        public override string Texture => AssetDirectory.CursedItems + "TarSplat";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tar Blot");
        }

        public float SmearAmount => Utils.GetLerpValue(2f, 8f, Math.Abs(Projectile.ai[0]), true);
        public float SplotchRotation => Math.Clamp(Projectile.ai[0] * 0.05f, -0.2f, 0.2f);

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;

        public bool UsesNonsolidMask => false;
        public virtual void DrawOverMask(SpriteBatch spriteBatch, bool solidLayer)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float opacity = (float)Math.Pow(Projectile.timeLeft / 60f, 0.3f);

            Vector2 size = new Vector2(1 + SmearAmount * 0.6f, 1f);

            if (Projectile.timeLeft > 50)
                size *= (float)Math.Pow(Utils.GetLerpValue(60, 50, Projectile.timeLeft, true), 0.2f);
            else
                size.Y += (float)Math.Pow(Utils.GetLerpValue(40, 0, Projectile.timeLeft, true), 1.2f) * 0.2f;

            spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.Black * opacity, SplotchRotation, tex.Size() / 2, size, 0, 0);
        }
    }

    public class PeculiarPotTarRing : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Noise + "FluidTrail";

        public PrimitiveClosedLoop tarRing;
        public static int LifeTime = 40;
        public bool playedAppearSound = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tar");
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTime;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            randomRotation = Main.rand.NextFloat(-0.1f, 0.1f);
        }

        public float Depletion => Projectile.timeLeft / (float)LifeTime;
        public float Completion => 1 - Projectile.timeLeft / (float)LifeTime;

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;


        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public float RingWidth(float completion)
        {
            return 26 * (float)Math.Pow(Depletion, 1.8f);
        }

        public Color drawColor;

        public Color RingColor(float completion)
        {
            return drawColor;
        }

        public float randomRotation;

        public override void PostAI()
        {
            if (!playedAppearSound)
            {
                SoundEngine.PlaySound(PeculiarPot.FormSound, Projectile.Center);
                playedAppearSound = true;
            }

            if (tarRing != null)
            {
                float outwards = (float)Math.Pow(Completion, 0.2f) * 70f;
                tarRing.SetPositionsCircle(Projectile.Center, outwards, 0f);
            }
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            drawColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            tarRing = tarRing ?? new PrimitiveClosedLoop(40, RingWidth, RingColor);

            Effect effect = Scene["TarTrail"].GetShader().Shader;
            effect.Parameters["repeats"].SetValue(4);
            effect.Parameters["outlineColor"].SetValue(new Color(12, 0, 0).ToVector4());
            effect.Parameters["fadeInColor"].SetValue(new Color(78, 50, 43).ToVector4());
            effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 1.5f);
            effect.Parameters["sampleTexture"].SetValue(TextureAssets.Projectile[Type].Value);

            tarRing.Render(effect);
        }
    }

    public class PeculiarPotTarTrail : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.Noise + "FluidTrail";
        public VerletNet trail;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tar");
        }

        public static float LifeTime = 30;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)LifeTime;
            Projectile.tileCollide = false;
            Projectile.hide = true;
        }

        public float Depletion => Projectile.timeLeft / LifeTime;
        public float Completion => 1 - Projectile.timeLeft / LifeTime;

        public Player Owner => Main.player[Projectile.owner];

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;


        public Vector2 VerletCollision(Vector2 position, Vector2 velocity)
        {
            float minDistance = 22;

            Vector2 nextPosition = position + velocity;
            float distanceToCenter = nextPosition.Distance(Projectile.Center);

            if (distanceToCenter < minDistance)
                return velocity.SafeNormalize(Vector2.Zero) * (minDistance - distanceToCenter);

            return velocity;
        }


        public override void PostAI()
        {
            Projectile.Center = Owner.MountedCenter + Vector2.UnitY * 10f;

            //We cannot kill the projectile in multiplayer, on the cases where the projectile spawn packet arrives before the mount packet.
            if (!Owner.mount.Active && Main.netMode == NetmodeID.SinglePlayer) 
            {
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ)
            {
                if (trail == null)
                {
                    trail = new VerletNet();

                    VerletPoint start = new VerletPoint(Projectile.Center, true);
                    VerletPoint end = new VerletPoint(Projectile.Center + Vector2.UnitY * 65f);
                    trail.AddChain(start, end, 14, TrailWidth, TrailColor, primResolution: 2f);
                }

                trail.extremities[0].position = Projectile.Center;

                float potLidDirection = 0;
                if (Owner.mount._mountSpecificData is PeculiarPotMount.PotMountData potData)
                    potLidDirection = (potData.rotation - MathHelper.PiOver2);

                VerletPoint.WormLikeSimulation(trail.chains[0], trail.segments[0].lenght, 1.2f, 0.4f, true, potLidDirection);
                trail.UpdateTrailPositions();
            }

            /*
            for (int i = 1; i < trail.points.Count; i++)
            {
                VerletPoint p = trail.points[i];
                if (p.position.Distance(Projectile.Center) < 40)
                    p.position = Projectile.Center + Projectile.Center.DirectionTo(p.position) * 20;
            }
            */
        }

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public float TrailWidth(float completion)
        {
            //1 = end of trail, 0 = start of trail
            return 15 * Utils.GetLerpValue(Depletion + 0.2f, Depletion, completion, true) * (float)Math.Pow(Depletion, 0.3f);
        }

        public Color drawColor;
        public Color TrailColor(float completion)
        {
            return drawColor;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (trail is null || !Owner.mount.Active | Owner.mount.Type != ModContent.MountType<PeculiarPotMount>())
                return;

            drawColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Effect effect = Scene["TarTrail"].GetShader().Shader;
            effect.Parameters["repeats"].SetValue(0.8f);
            effect.Parameters["outlineColor"].SetValue(new Color(12, 0, 0).ToVector4());
            effect.Parameters["fadeInColor"].SetValue(new Color(78, 50, 43).ToVector4());
            effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 1.5f);
            effect.Parameters["sampleTexture"].SetValue(TextureAssets.Projectile[Type].Value);

            trail.trailRenderers[0].Render(effect);
        }
    }
    #endregion

    public class PeculiarPotMount : ModMount
    {
        public override string Texture => AssetDirectory.CursedItems + Name;

        public class PotMountData
        {
            public float rotation;
            public Vector2 oldVelocity;
            public float animationTimeIn;
            public SlotId rollingSoundSlot;
            public float rollingSoundFade;

            public PotMountData()
            {
                rotation = 0;
                oldVelocity = Vector2.Zero;
                animationTimeIn = 1f;
                rollingSoundSlot = SlotId.Invalid;
                rollingSoundFade = 1f;
            }
        }

        protected static PotMountData Data(Player p) => (PotMountData)p.mount._mountSpecificData ?? new PotMountData();

        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<Potted>(); // The ID number of the buff assigned to the mount.
            MountData.heightBoost = -18; // Shrink the player down

            // Movement
            //MountData.acceleration = 0.16f; // The rate at which the mount speeds up.
            MountData.blockExtraJumps = false; // Determines whether or not you can use a double jump (like cloud in a bottle) while in the mount.
            MountData.constantJump = false; // Allows you to hold the jump button down.
            MountData.fallDamage = 0.8f; // Fall damage multiplier.
            //MountData.runSpeed = 8f;

            //NOTE: Too high acceleration and runspeed can have the player detach from slopes and take fall damage they shouldn't
            MountData.acceleration = 0.12f; // The rate at which the mount speeds up.
            MountData.runSpeed = 7.5f;

            MountData.dashSpeed = 8f; // The speed the mount moves when in the state of dashing.
            MountData.fatigueMax = 0;
            MountData.jumpHeight = 10; // How high the mount can jump.
            MountData.jumpSpeed = 7.5f;

            // Frame data and player offsets
            MountData.totalFrames = 1;
            MountData.playerYOffsets = Enumerable.Repeat(0, MountData.totalFrames).ToArray();

            MountData.xOffset = 0;
            MountData.yOffset = 0;
            MountData.playerHeadOffset = 0;
            MountData.bodyFrame = 0;
        }

        public override void UpdateEffects(Player player)
        {
            player.GetDamage(DamageClass.Generic) *= 0.01f;
            FablesUtils.SetCustomHurtSound(player, PeculiarPot.ChipSound, 10, 2f);

            if (player.GetModPlayer<PeculiarPotPlayer>().potBroken)
            {
                player.mount.Dismount(player);
                if (player.whoAmI == Main.myPlayer)
                    CameraManager.Shake += 15;
            }

            if (Data(player).animationTimeIn > 0)
                Data(player).animationTimeIn -= 1 / (60f * 0.5f);

            //if (Data(player).animationTimeIn > 0.5f && player.velocity.Y <= 0)
            //    player.maxFallSpeed *= Utils.GetLerpValue(1f, 05f, Data(player).animationTimeIn, true);

        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            player.mount._mountSpecificData = new PotMountData();
            skipDust = true;

            if (Main.myPlayer == player.whoAmI)
            {
                CameraManager.AddCameraEffect(new PeculiarPotCameraCorrection(40, MountData.heightBoost));
                Projectile.NewProjectileDirect(player.mount.GetProjectileSpawnSource(player), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<PeculiarPotTarRing>(), 0, 0, Main.myPlayer);
                Projectile.NewProjectileDirect(player.mount.GetProjectileSpawnSource(player), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<PeculiarPotTarTrail>(), 0, 0, Main.myPlayer);
            }

            if (player.velocity.Y == 0)
            {
                player.velocity.Y = -9;
                player.position.Y -= 24;
            }

            if (player.velocity.X == 0)
                player.velocity.X = 3 * player.direction;
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            SoundEngine.PlaySound(PeculiarPot.BreakSound, player.Center);
            Vector2 usedCenter = player.MountedCenter + Vector2.UnitY * 5f;


            player.AddBuff(ModContent.BuffType<TarsMark>(), (int)(PeculiarPot.DEBUFF_TIME * 60), false);
            player.GetModPlayer<PeculiarPotPlayer>().unpotTime = 1f;

            if (player.whoAmI == Main.myPlayer && player.velocity.Y == 0)
                Projectile.NewProjectileDirect(player.mount.GetProjectileSpawnSource(player), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<PeculiarPotTarBlot>(), 0, 0, Main.myPlayer, player.velocity.X);

            skipDust = true;
            if (Main.dedServ)
                return;

            ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader(player.cMount, player);

            //Eruption from the top
            for (int i = 0; i < 20; i++)
            {
                Vector2 dustPos = usedCenter + Main.rand.NextFloat(-15f, 15f) * Vector2.UnitX;
                Vector2 dustVel = (usedCenter + Vector2.UnitY * 90f).DirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 2f);
                dustVel.Y *= Main.rand.NextFloat(1f, 3f);

                Dust d = Dust.NewDustPerfect(dustPos, DustID.Granite, dustVel, Scale: Main.rand.NextFloat(1f, 1.3f));
                d.shader = dustShader;
            }

            //To the sides
            for (int i = 0; i < 20; i++)
            {
                Vector2 dustPos = usedCenter + Main.rand.NextFloat(-15f, 15f) * Vector2.UnitX;
                Vector2 dustVel = (usedCenter + Vector2.UnitY * 10f).DirectionTo(dustPos) * Main.rand.NextFloat(0.5f, 2f);
                dustVel.X *= Main.rand.NextFloat(1f, 2f);

                Dust d = Dust.NewDustPerfect(dustPos, DustID.Granite, dustVel, Scale: Main.rand.NextFloat(0.5f, 0.8f));
                d.shader = dustShader;
            }

            //To the sides
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(player.MountedCenter - Vector2.UnitY * 8f + Main.rand.NextVector2Circular(20f, 20f), DustID.DesertPot, Main.rand.NextVector2Circular(4f, 4f));
                d.shader = dustShader;
            }


            //Add gores
            int[] goreCounts = new int[] { 1, 1, 2, 3, 3, 4, 4 };

            for (int i = 0; i < goreCounts.Length; i++)
            {
                Vector2 position = player.MountedCenter + Main.rand.NextVector2Circular(6f, 6f) - Vector2.UnitY * 8f;
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 0.7f + player.velocity * 0.6f;
                if (velocity.Y > 0)
                    velocity.Y *= 0.5f;

                Gore.NewGoreDirect(player.mount.GetProjectileSpawnSource(player), position, velocity, Mod.Find<ModGore>("PeculiarPot_Gore" + goreCounts[i].ToString()).Type);
            }
        }

        public override bool UpdateFrame(Player player, int state, Vector2 velocity)
        {
            if (player.dead)
                return false;

            float rotation = velocity.X * 0.03f;
            if (velocity.Y != 0 && Math.Abs(velocity.X) < 2f && ((PotMountData)player.mount._mountSpecificData).animationTimeIn <= 0)
                rotation += player.direction * 0.03f * Utils.GetLerpValue(2f, 1f, Math.Abs(velocity.X), true);

            Vector2 oldVel = ((PotMountData)player.mount._mountSpecificData).oldVelocity;
            if (oldVel.Y == 0 && velocity.Y < 0)
            {
                SoundEngine.PlaySound(PeculiarPot.JumpSound, player.Center);
                Data(player).rollingSoundFade = 0f;
            }

            if (Data(player).rollingSoundFade <= 0f && oldVel.Y > 0 && velocity.Y == 0)
                SoundEngine.PlaySound(PeculiarPot.LandingSound, player.Center);

            if (velocity.Y == 0)
                Data(player).rollingSoundFade = 1f;
            else if (Data(player).rollingSoundFade > 0)
                Data(player).rollingSoundFade -= 1 / (60f * 0.3f);

            Data(player).rotation += rotation;
            Data(player).oldVelocity = velocity;


            if (!SoundEngine.TryGetActiveSound(Data(player).rollingSoundSlot, out var rollingSound))
            {
                Data(player).rollingSoundSlot = SoundEngine.PlaySound(PeculiarPot.RollingSound, player.Center);
                SoundEngine.TryGetActiveSound(Data(player).rollingSoundSlot, out rollingSound);
            }

            if (rollingSound != null)
            {
                rollingSound.Position = player.Center;
                rollingSound.Pitch = Utils.GetLerpValue(2f, 8f, Math.Abs(velocity.X), true) * 0.2f;
                rollingSound.Volume = 0.4f + 0.3f * Utils.GetLerpValue(0f, 6f, Math.Abs(velocity.X), true);
                rollingSound.Volume *= Utils.GetLerpValue(2f, 4f, Math.Abs(velocity.X), true);

                rollingSound.Volume *= 0.1f + 0.9f * Data(player).rollingSoundFade;

                rollingSound.Update();

                SoundHandler.TrackSoundWithFade(Data(player).rollingSoundSlot);
            }

            /*
            for (int i = 0; i < Data(player).tar.Count; i++)
            {
                TarBlob tar = Data(player).tar[i];

                tar.offset.X *= 0.94f;
                tar.offset.Y -= 0.6f;
                tar.offset.Y *= 0.99f - 0.04f * (1 - Data(player).animationTimeIn);
                tar.offset.Y -= Math.Clamp(velocity.Y, -2f, 2f);

                if (tar.offset.Y < 0)
                    tar.offset.Y = 0;
                Data(player).tar[i] = tar;
            }
            */

            if (velocity.Length() > 2f && Main.rand.NextFloat() > 0.5f + Utils.GetLerpValue(6f, 2f, velocity.Length(), true) * 0.3f)
            {
                Vector2 potLid = player.MountedCenter + Vector2.UnitY.RotatedBy(Data(player).rotation) * 10f + Vector2.UnitY * 8f;
                potLid += (Data(player).rotation).ToRotationVector2() * Main.rand.NextFloat(-10f, 10f);

                Vector2 dustVel = (Data(player).rotation + rotation.NonZeroSign() * Main.rand.NextFloat(0f, 0.4f)).ToRotationVector2() + player.velocity * 0.3f;
                Dust d = Dust.NewDustPerfect(potLid, DustID.Granite, dustVel, Scale: Main.rand.NextFloat(0.5f, 1.1f));
                d.shader = GameShaders.Armor.GetSecondaryShader(player.cMount, player);
            }

            return false;
        }

        public static void AddDrawDataWithMountShader(List<DrawData> playerDrawData, DrawData data, Player drawPlayer)
        {
            if (!drawPlayer.miscDyes[3].active || drawPlayer.miscDyes[3] == null)
            {
                playerDrawData.Add(data);
                return;
            }

            data.shader = GameShaders.Armor.GetShaderIdFromItemId(drawPlayer.miscDyes[3].type);
            playerDrawData.Add(data);
        }

        public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
        {
            int potFrame = (int)((1 - drawPlayer.GetModPlayer<PeculiarPotPlayer>().PotHealth / (float)PeculiarPot.MAX_POT_HEALTH) * 2.99999f);

            Rectangle mountFrame = texture.Frame(1, 3, 0, potFrame, 0, -2);
            Vector2 origin = mountFrame.Size() / 2f;
            Vector2 drawPos = drawPosition ;

            if (Math.Abs(drawPlayer.gfxOffY) > 15f)
                drawPos.Y -= (Math.Abs(drawPlayer.gfxOffY) - 15) * Math.Sign(drawPlayer.gfxOffY);

            AddDrawDataWithMountShader(playerDrawData, new DrawData(
                texture,
                drawPos,
                mountFrame,
                drawColor,
                ((PotMountData)drawPlayer.mount._mountSpecificData).rotation,
                origin,
                drawScale,
                0, 0),
                drawPlayer);

            // by returning true, the regular drawing will still happen.
            return false;
        }
    }

    public class PeculiarPotCameraCorrection : ICameraModifier
    {
        private int _framesToLast;
        private int _framesLasted;
        public float heightDiff;

        public string UniqueIdentity => "PeculiarPotCameraTug";
        public bool Finished {
            get;
            private set;
        }

        public PeculiarPotCameraCorrection(int framesToLast, float heightDiff)
        {
            _framesToLast = framesToLast;
            _framesLasted = 0;
            this.heightDiff = heightDiff;
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            float completion = 1 - _framesLasted / (float)_framesToLast;
            cameraInfo.CameraPosition += Vector2.UnitY * -heightDiff * (float)Math.Pow(completion, 2.5f);


            _framesLasted++;
            if (_framesLasted >= _framesToLast)
                Finished = true;
        }
    }
}
