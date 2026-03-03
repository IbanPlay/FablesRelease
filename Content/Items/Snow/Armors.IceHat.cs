using CalamityFables.Content.Items.EarlyGameMisc;
using CalamityFables.Content.NPCs.Snow;
using CalamityFables.Cooldowns;
using CalamityFables.Core.DrawLayers;
using CalamityFables.Core.Visuals;
using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using static CalamityFables.Content.Items.Snow.IceHat;
using static CalamityFables.Content.Items.Snow.IceHatAbilityHandler;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CalamityFables.Content.Items.Snow
{
    [AutoloadEquip(EquipType.Head)]
    public class IceHat : ModItem, IExtendedHat
    {
        public override string Texture => AssetDirectory.SnowItems + Name;
        internal static LocalizedText SetBonus;

        #region Sounds
        public static readonly SoundStyle SummonSound = new SoundStyle(SoundDirectory.Sounds + "IceHatSummon") { MaxInstances = 5 };
        public static readonly SoundStyle SlideLoop = new SoundStyle(SoundDirectory.Sounds + "IceHatSlideLoop") { IsLooped = true, Volume = 0.8f, MaxInstances = 0 };
        public static readonly SoundStyle BreakSound = new SoundStyle(SoundDirectory.Sounds + "IceHatBreak") { MaxInstances = 5 };
        public static readonly SoundStyle ChipSound = new SoundStyle(SoundDirectory.Sounds + "IceHatCrack", 2);
        public static readonly SoundStyle LandingSound = new SoundStyle(SoundDirectory.Sounds + "IceHatCubeLand", 2) { Volume = 0.5f };
        public static readonly SoundStyle MaxManaSound = new SoundStyle(SoundDirectory.Sounds + "IceHatMaxManaOverlay");
        public static readonly SoundStyle CooldownRefreshSound = new SoundStyle(SoundDirectory.Sounds + "IceHatCooldownRefresh");

        #endregion

        internal static int IceCubeMountID;

        #region big hat
        public string ExtensionTexture => AssetDirectory.SnowItems + Name + "_HeadExtension";
        public Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo) => new Vector2(-6);
        public string EquipSlotName(Player drawPlayer) => Name;
        #endregion

        #region Reflection Fields
        public static int MAXIMUM_MANA_BONUS = 40;
        public static int CRIT_CHANCE_BONUS = 4;
        /// <summary> The maximum cube mount cooldown after being broken. </summary>
        public static int COOLDOWN_MAX = 600;
        /// <summary> The amount of time it takes for the cube to break after being fatally struck. </summary>
        public static int CUBE_DEATH_LENGTH = 15;
        /// <summary> The maximum amount of unscaled contact damage the cube can deal. </summary>
        public static int DAMAGE_MAX = 40;
        /// <summary> The cube's multiplier to damage taken. </summary>
        public static float DAMAGE_REDUCTION = 0.5f;
        /// <summary> The maximum number of hits the cube can sustain. </summary>
        public static int CUBE_LIFE_MAX = 3;

        #endregion

        public override void Load()
        {
            FablesPlayer.ModifyManaRegenMultiplierEvent += ModifyManaRegenMultiplier;
            FablesPlayer.OnAchieveFullMana += OnAchieveFullMana;
            FablesItem.ModifyTooltipsEvent += ModifyTooltips;

            IceHatAbilityHandler.Load();
        }

        public override void Unload()
        {
            FablesPlayer.ModifyManaRegenMultiplierEvent -= ModifyManaRegenMultiplier;
            FablesPlayer.OnAchieveFullMana -= OnAchieveFullMana;
            FablesItem.ModifyTooltipsEvent -= ModifyTooltips;

            IceHatAbilityHandler.Unload();
        }

        public override void SetStaticDefaults() => SetBonus = Mod.GetLocalization("Extras.ArmorSetBonus.IceHat");

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Green;
            Item.defense = 4;
        }

        public override void UpdateEquip(Player player)
        {
            player.SetPlayerFlag(Name);
            player.GetCritChance<MagicDamageClass>() += CRIT_CHANCE_BONUS;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) => head.type == Type && FablesSets.WizardSetRobe[body.type];

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = Name;

            if (player.GetPlayerFlag(Name) && IsEncased(player))
            {
                player.SetPlayerFlag(Name + "ManaRegen");
                FablesResourceOverlay.SetOverlay(ShouldDrawManaStarOverlay, DrawManaStarOverlay, 2.0f);

                player.manaRegenDelay = 0;  // Instantly start regeneration
                player.manaRegenBonus = player.statManaMax2 / 3;
            }
        }

        private void ModifyManaRegenMultiplier(Player player, ref float manaRegenMultiplier)
        {
            if (player.GetPlayerFlag(Name + "ManaRegen"))
            {
                // Less harsh scaling dependant on max mana
                manaRegenMultiplier = Math.Min(manaRegenMultiplier, Utils.GetLerpValue(0, player.statManaMax2, player.statMana) * 0.5f + 0.5f);
                // Increase all mana regen by 30%
                manaRegenMultiplier *= 1.3f;
            }
        }

        #region Mana overlay
        public float ManaOverlayOpacity = 0;

        private void OnAchieveFullMana(Player player)
        {
            if (IsEncased(player))
                SoundEngine.PlaySound(MaxManaSound);
        }

        private bool ShouldDrawManaStarOverlay(PlayerStatsSnapshot snapshot, bool drawingLife)
        {
            if (drawingLife)
                return false;

            Player player = Main.LocalPlayer;

            // Opacity scales with mana 
            if (IsEncased(player))
                ManaOverlayOpacity = MathF.Pow(Utils.GetLerpValue(0, snapshot.ManaMax, snapshot.Mana), 2f);
            else
                ManaOverlayOpacity = 0f;

            return true;
        }

        private void DrawManaStarOverlay(ResourceOverlayDrawContext context)
        {
            FablesResourceOverlay.ResourceType assetType = FablesResourceOverlay.GetAssetType(context.texture);

            //Don't draw if not the asset we care about, or in the case of fancy & classic, if we're past the 6 stars at te end of the bar
            if (assetType == FablesResourceOverlay.ResourceType.Other)
                return;

            Color overlayColor = new Color(74, 166, 245) with { A = 0 };

            // Colored overlay over bar
            if (assetType == FablesResourceOverlay.ResourceType.BarFill)
            {
                context.texture = PlasmaRod.MaxxedManaStarOverlay;
                context.source = new Rectangle(5, 35, 12, 10);
                context.color = overlayColor;
                context.color *= 0.7f + 0.3f * MathF.Sin(context.resourceNumber * 0.8f + Main.GlobalTimeWrappedHourly * 3f);
                context.color *= ManaOverlayOpacity;
                context.Draw();
            }
            // Draw colored overlay over base stars
            else
            {
                if (assetType == FablesResourceOverlay.ResourceType.BarPanel)
                {
                    context.position.X += 20;
                    context.position.Y += 4;
                }

                int frame = 2;
                if (assetType == FablesResourceOverlay.ResourceType.Fancy || assetType == FablesResourceOverlay.ResourceType.BarPanel)
                    frame = 1;

                // Overlay opacity, fades with base opacity
                Vector3 baseColor = context.color.ToVector3();
                float opacity = ManaOverlayOpacity * baseColor.X * baseColor.Y * baseColor.Z;

                context.texture = PlasmaRod.MaxxedManaStarOverlay;
                context.source = context.texture.Frame(1, 3, 0, frame, 0, -2);
                context.color = overlayColor;
                context.color *= 0.7f + 0.3f * MathF.Sin(context.resourceNumber * 0.8f + Main.GlobalTimeWrappedHourly * 3f);
                context.color *= opacity;
                context.Draw();
            }
        }

        #endregion

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindIndex(x => x.Name == "Tooltip0" && x.Mod == "Terraria");

            if (index == -1)
                return;

            tooltips[index] = new TooltipLine(Mod, "tooltip", base.Tooltip.Format(CRIT_CHANCE_BONUS, Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.UP" : "Key.DOWN")));
            tooltips.RemoveAt(index + 1);
        }

        private void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            // Check if player should have set bonus
            Player player = Main.LocalPlayer;
            if (player.armor[0].type != Type || !FablesSets.WizardSetRobe[player.armor[1].type])
                return;

            // Find set bonus line
            int setBonusIndex = tooltips.FindIndex(x => x.Name == "SetBonus" && x.Mod == "Terraria");

            if (setBonusIndex == -1)
                return;

            // Modify line
            TooltipLine setBonus = new TooltipLine(Mod, "CalamityFables:SetBonus", SetBonus.Value)
            {
                OverrideColor = Color.Lerp(new Color(139, 219, 255), new Color(176, 240, 255), 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f))
            };
            tooltips[setBonusIndex] = setBonus;
        }
    }

    public class IceHatAbilityHandler
    {
        #region Events
        public static void Load()
        {
            FablesPlayer.ArmorSetBonusActivatedEvent += ArmorSetBonusActivated;
            On_Player.QuickMount += QuickMount;
            FablesPlayer.CanUseItemEvent += CanUseItem;
            FablesPlayer.CanAutoReuseItemEvent += CanAutoReuseItem;
            FablesPlayer.DisableItemHoldEvent += DisableItemHold;
            FablesPlayer.PostUpdateMiscEffectsEvent += PostUpdateMiscEffects;
            FablesPlayer.SetControlsEvent += SetControls;
            FablesPlayer.ModifyHurtEvent += ModifyHurt;
            FablesPlayer.HideDrawLayersEvent += HideDrawLayers;
            FablesPlayer.ResetEffectsEvent += ResetEffects;
            FablesPlayer.UpdateDeadEvent += UpdateDead;
        }

        public static void Unload()
        {
            FablesPlayer.ArmorSetBonusActivatedEvent -= ArmorSetBonusActivated;
            On_Player.QuickMount -= QuickMount;
            FablesPlayer.CanUseItemEvent -= CanUseItem;
            FablesPlayer.CanAutoReuseItemEvent -= CanAutoReuseItem;
            FablesPlayer.DisableItemHoldEvent -= DisableItemHold;
            FablesPlayer.PostUpdateMiscEffectsEvent -= PostUpdateMiscEffects;
            FablesPlayer.SetControlsEvent -= SetControls;
            FablesPlayer.ModifyHurtEvent -= ModifyHurt;
            FablesPlayer.HideDrawLayersEvent -= HideDrawLayers;
            FablesPlayer.ResetEffectsEvent -= ResetEffects;
            FablesPlayer.UpdateDeadEvent -= UpdateDead;
        }

        #endregion

        public class IceHatData : CustomGlobalData
        {
            public int Cooldown = 0;
            public int CubeLife = CUBE_LIFE_MAX;

            public bool CubeBroken => CubeLife <= 0;
            public bool CubeCracked => CubeLife < CUBE_LIFE_MAX;
        }

        public static bool IsEncased(Player player) => player.mount.Active && player.mount.Type == IceCubeMountID;

        public static int GetMountDamage(Player player) => (int)player.GetDamage<MagicDamageClass>().ApplyTo(DAMAGE_MAX);

        private static void ArmorSetBonusActivated(Player player)
        {
            if (!player.GetPlayerFlag("IceHat") || !player.GetPlayerData(out IceHatData data))
                return;

            TryEncase(player, data);
        }

        private static void QuickMount(On_Player.orig_QuickMount orig, Player player)
        {
            // Use quick mount to activate set bonus if mount slot is empty
            if (player.mount.Active || !player.GetPlayerFlag("IceHat") || !player.GetPlayerData(out IceHatData data) || !player.miscEquips[Player.miscSlotMount].IsAir)
                orig(player);
            else
            {
                if (player.frozen || player.tongued || player.webbed || player.stoned || player.gravDir == -1f || player.dead || player.noItems || player.QuickMinecartSnapPublic())
                    return;

                TryEncase(player, data);
            }
        }

        private static void TryEncase(Player player, IceHatData data)
        {
            if (!data.CubeCracked || data.Cooldown <= CUBE_DEATH_LENGTH)
            {
                if (player.mount.Active)
                    player.mount.Dismount(player);
                player.mount.SetMount(IceCubeMountID, player);
            }
        }

        private static bool CanUseItem(Player player, Item item)
        {
            // Dismout upon using an item
            if (IsEncased(player) && !item.IsPotionLike())
                player.mount.Dismount(player);

            return true;
        }

        private static bool? CanAutoReuseItem(Player player, Item item) => IsEncased(player) ? false : null;    // Prevent instantly dismounting if holding the attack button

        private static bool DisableItemHold(Player player, Item item, Rectangle heldItemFrame) => IsEncased(player);

        private static void PostUpdateMiscEffects(Player player)
        {
            if (!player.GetPlayerFlag("IceHat"))
                return;

            // Ensure players with the set bonus have the data
            if (!player.GetPlayerData(out IceHatData data))
            {
                data = new IceHatData();
                player.SetPlayerData(data);
            }

            bool shouldDisplayCooldown = IsEncased(player) || data.CubeCracked;

            // Add/Remove cooldown when it should be displayed
            if (!shouldDisplayCooldown)
                player.RemoveCooldown(EncasedCooldown.ID);
            else if (!player.HasCooldown(EncasedCooldown.ID))
            {
                CooldownInstance cooldown = player.AddCooldown(EncasedCooldown.ID, COOLDOWN_MAX);
                cooldown.timeLeft = COOLDOWN_MAX;
            }
            // Update cooldown
            else if (player.FindCooldown(EncasedCooldown.ID, out var cooldown))
            {
                // Based on cooldown when cracked or broken
                if ((data.CubeCracked && !IsEncased(player)) || data.CubeBroken)
                    cooldown.timeLeft = data.Cooldown;
                // Otherwise based on cube life
                else
                    cooldown.timeLeft = (int)(COOLDOWN_MAX * Utils.GetLerpValue(0, CUBE_LIFE_MAX, data.CubeLife));
            }
        }

        private static void SetControls(Player player)
        {
            // Disable controls
            if (IsEncased(player))
            {
                player.controlLeft = false;
                player.controlRight = false;
            }
        }

        private static void ModifyHurt(Player player, ref Player.HurtModifiers modifiers)
        {
            // Reduce damage but reduce cube HP
            if (IsEncased(player))
            {
                modifiers.FinalDamage *= DAMAGE_REDUCTION;
                if (player.GetPlayerData(out IceHatData data))
                    data.CubeLife--;
            }
        }

        private static void HideDrawLayers(Player player, PlayerDrawSet drawInfo)
        {
            // Hide non-mount draw layers
            if (IsEncased(player) && !drawInfo.headOnlyRender)
            {
                foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
                {
                    if (layer.Name != nameof(PlayerDrawLayers.MountFront))
                        layer.Hide();
                }
            }
        }

        private static void ResetEffects(Player player)
        {
            if (player.GetPlayerData(out IceHatData data) && ((data.CubeCracked && !IsEncased(player)) || data.CubeBroken) && data.Cooldown++ > COOLDOWN_MAX)
            {
                RechargeEffects(player);
                data.CubeLife = CUBE_LIFE_MAX;
                data.Cooldown = 0;
            }
        }

        private static void RechargeEffects(Player player)
        {
            SoundEngine.PlaySound(CooldownRefreshSound, player.MountedCenter);
            ParticleHandler.SpawnParticle(new CircularPulseShine(player.MountedCenter, Color.SkyBlue));

            for (int i = 0; i < Main.rand.Next(4, 6); i++)
            {
                Vector2 velocity = (Vector2.UnitY * Main.rand.NextFloat(1.5f, 3f)).RotateRandom(MathHelper.PiOver2);
                Particle sharticle = new IceShardParticle(Main.rand.NextVector2FromRectangle(player.Hitbox), velocity, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 70));

                sharticle.Velocity.Y -= 4f;
                ParticleHandler.SpawnParticle(sharticle);
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                Dust dust = Dust.NewDustPerfect(player.MountedCenter, DustID.ApprenticeStorm, velocity, 200, Color.White, Main.rand.NextFloat(1.5f, 2.5f));
                dust.noGravity = true;
            }
        }

        private static void UpdateDead(Player player)
        {
            if (player.GetPlayerData(out IceHatData data))
            {
                data.CubeLife = CUBE_LIFE_MAX;
                data.Cooldown = 0;
            }
        }
    }

    public class Encased : ModBuff
    {
        public override string Texture => AssetDirectory.SnowItems + Name;
        internal static LocalizedText SetBonusDescription;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(IceCubeMountID, player);
            player.buffTime[buffIndex] = 10;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            if (Main.LocalPlayer.GetPlayerFlag("IceHatManaRegen"))
            {
                SetBonusDescription ??= Mod.GetLocalization("Buffs.Encased.SetBonusDescription");
                tip = SetBonusDescription.Value;
            }
        }
    }

    public class IceHatMount : ModMount
    {
        public class IceHatObject
        {
            public static readonly Asset<Texture2D> Texture = ModContent.Request<Texture2D>(AssetDirectory.SnowItems + "IceHatMount_Hat");

            public readonly Player owner;

            public Vector2 position;
            public Vector2 velocity;
            public float rotation;

            public Vector2 RestingPosition => owner.Top + new Vector2(1, -(owner.mount?.YOffset ?? 0));

            public IceHatObject(Player player)
            {
                owner = player;
                position = RestingPosition;
            }

            public void Update()
            {
                float speed = owner.velocity.X;
                float distance = RestingPosition.Y - position.Y;

                position = new Vector2(RestingPosition.X - speed, Math.Min(position.Y + 5 + (distance / 30f), RestingPosition.Y));

                if (position.Y != RestingPosition.Y)
                    rotation += 0.2f * owner.direction;
                else
                    rotation = -speed / 30;
            }

            public void Draw(List<DrawData> playerDrawData)
            {
                Texture2D texture = Texture.Value;
                Rectangle source = texture.Frame(1, 8, 0, owner.mount._frame, 0, -2);
                SpriteEffects effect = (owner.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                AddDrawDataWithMountShader(playerDrawData, new DrawData(
                    texture,
                    position.ToPoint().ToVector2() + new Vector2(0, owner.gfxOffY) - Main.screenPosition,
                    source,
                    Lighting.GetColor(position.ToTileCoordinates()),
                    rotation,
                    source.Size() / 2,
                    1, effect, 0),
                    owner);
            }
        }

        protected class HatMountSpecificData
        {
            public IceHatObject hatObject;
            public Vector2 oldVelocity;
            public int mountDuration;

            public float rotation;

            public SlotId SlideSoundSlot;
            public float SlideVolume;
        }

        public static float Pulse => (float)Math.Sin(Main.timeForVisualEffects / 50f);
        protected static HatMountSpecificData Data(Player player) => (HatMountSpecificData)player.mount._mountSpecificData;
        public override string Texture => AssetDirectory.SnowItems + Name;

        public override void SetStaticDefaults()
        {
            IceCubeMountID = Type;
            MountData.buff = ModContent.BuffType<Encased>(); // The ID number of the buff assigned to the mount.

            // Movement
            MountData.blockExtraJumps = false; // Determines whether or not you can use a double jump (like cloud in a bottle) while in the mount.
            MountData.constantJump = false; // Allows you to hold the jump button down.
            MountData.fallDamage = 0.8f; // Fall damage multiplier.

            MountData.acceleration = 0.99f; // The rate at which the mount speeds up.
            MountData.runSpeed = 7.5f;

            MountData.dashSpeed = 8f; // The speed the mount moves when in the state of dashing.
            MountData.fatigueMax = 0;
            MountData.jumpHeight = 10; // How high the mount can jump.
            MountData.jumpSpeed = 7.5f;

            // Frame data and player offsets
            MountData.totalFrames = 8;
            MountData.playerYOffsets = Enumerable.Repeat(0, MountData.totalFrames).ToArray();

            MountData.xOffset = 0;
            MountData.yOffset = 2;
            MountData.playerHeadOffset = 0;
            MountData.bodyFrame = 0;
        }

        public override void UpdateEffects(Player player)
        {
            if (!player.GetPlayerData(out IceHatData data))
            {
                data = new IceHatData();
                player.SetPlayerData(data);
            }

            // Dismount when broken or the player jumps
            if ((data.CubeBroken && data.Cooldown > CUBE_DEATH_LENGTH) || (player.controlJump && player.releaseJump))
            {
                player.mount.Dismount(player);
                return;
            }

            //player.GetDamage(DamageClass.Generic) *= 0.01f;
            FablesUtils.SetCustomHurtSound(player, ChipSound, 10, 2f);

            player.channel = false;
            player.noKnockback = true;
            player.slippy2 = true;
            
            // Float in water
            if (player.wet)
            {
                player.velocity.Y = Math.Max(player.velocity.Y - 0.5f, -10);
            }
            // Accelerated fall speed
            else
            {
                player.maxFallSpeed += 16;
                player.gravity = Player.defaultGravity * 3f;
            }

            // Subtract from cube life when hitting walls
            if (player.velocity.X == 0 && Data(player).oldVelocity.X != 0)
            {
                player.velocity.X = -(Data(player).oldVelocity.X * 1.5f);
                player.direction = Math.Sign(player.velocity.X);
                SoundEngine.PlaySound(ChipSound, player.MountedCenter);

                if (data.CubeLife-- <= 0)
                    data.Cooldown = CUBE_DEATH_LENGTH; //Skip the death delay
            }

            ref var soundSlot = ref Data(player).SlideSoundSlot;
            ref var slideVolume = ref Data(player).SlideVolume;

            // Handle slide loop
            if (!SoundEngine.TryGetActiveSound(soundSlot, out var sound))
                soundSlot = SoundEngine.PlaySound(SlideLoop, player.MountedCenter);
            if (SoundEngine.TryGetActiveSound(soundSlot, out sound))
            {
                if (player.velocity.Y == 0)
                    slideVolume = Utils.GetLerpValue(0, 10f, Math.Abs(player.velocity.X), true);
                else if (slideVolume > 0)
                    slideVolume = Math.Max(slideVolume - 0.06f, 0f);

                // Increases volume with speed
                sound.Position = player.MountedCenter;
                sound.Volume = slideVolume;
                sound.Pitch = MathHelper.Lerp(-0.25f, 0f, slideVolume);

                sound.Update();
            }

            SoundHandler.TrackSound(soundSlot);

            Data(player).oldVelocity = player.velocity;
        }

        public override bool UpdateFrame(Player mountedPlayer, int state, Vector2 velocity)
        {
            float rotation = Math.Clamp(mountedPlayer.velocity.Y * 0.05f, -1, 1) * mountedPlayer.direction;
            Data(mountedPlayer).rotation = Utils.AngleLerp(Data(mountedPlayer).rotation, rotation, 0.7f);

            float rate = Math.Min(Math.Abs(mountedPlayer.velocity.X / 20), 0.25f);
            mountedPlayer.mount._frame = (int)(Math.Abs(mountedPlayer.mount._frameCounter += rate) % MountData.totalFrames);

            Data(mountedPlayer).mountDuration++;
            Data(mountedPlayer).hatObject?.Update();

            return false;
        }

        private static IEntitySource GetMountSource(Player player) => player.mount.GetProjectileSpawnSource(player);

        public override void SetMount(Player player, ref bool skipDust)
        {
            skipDust = true;

            player.mount._mountSpecificData = new HatMountSpecificData();
            Data(player).hatObject = new(player);

            if (!Main.dedServ)
            {
                if (Main.myPlayer == player.whoAmI)
                {
                    Projectile.NewProjectile(GetMountSource(player), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<IceHatTrail>(), GetMountDamage(player), 8, Main.myPlayer, 1);
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f);
                    Dust dust = Dust.NewDustPerfect(player.MountedCenter, DustID.ApprenticeStorm, velocity, 200, Color.White, Main.rand.NextFloat(1.5f, 2.5f));
                    dust.noGravity = true;
                }

                SoundEngine.PlaySound(SummonSound, player.MountedCenter);
            }
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            if (!player.GetPlayerData(out IceHatData data))
                return;

            skipDust = true;

            if (!Main.dedServ)
            {
                // Apply shortened cooldown if cube is cracked
                if (!data.CubeBroken && data.CubeCracked)
                    data.Cooldown = (int)(COOLDOWN_MAX * Utils.GetLerpValue(0, CUBE_LIFE_MAX, data.CubeLife));

                SoundEngine.PlaySound(BreakSound, player.Center);

                if (Main.myPlayer == player.whoAmI)
                {
                    Projectile.NewProjectileDirect(GetMountSource(player), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<IceHatTrail>(), 0, 0, Main.myPlayer);
                    CameraManager.Shake += 15;

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 velocity = (Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 6f)) - (Vector2.UnitY * 3);
                        Projectile.NewProjectileDirect(GetMountSource(player), player.MountedCenter, velocity, ModContent.ProjectileType<IceShard>(), GetMountDamage(player) / 3, 4, Main.myPlayer);
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = (player.velocity * Main.rand.NextFloat(0.5f, 1f)).RotateRandom(1);
                    Particle sharticle = new IceShardParticle(Main.rand.NextVector2FromRectangle(player.Hitbox), velocity, Main.rand.NextFloat(1f, 1.6f), Main.rand.Next(30, 70));

                    sharticle.Velocity.Y -= 4f;
                    ParticleHandler.SpawnParticle(sharticle);
                }
            }
        }

        public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
        {
            if (!drawPlayer.GetPlayerData(out IceHatData data))
                return false;

            int horizontalFrame = Math.Clamp(3 - data.CubeLife, 0, 2);
            Rectangle mountFrame = texture.Frame(3, MountData.totalFrames, horizontalFrame, drawPlayer.mount._frame, -2, -2);
            Vector2 origin = mountFrame.Size() / 2f;

            float opacity = Math.Max(1f - (Data(drawPlayer).mountDuration / 30f), Utils.GetLerpValue(0, CUBE_DEATH_LENGTH, data.Cooldown));
            opacity += 0.1f + Pulse * 0.15f;

            AddDrawDataWithMountShader(playerDrawData, new DrawData(
                texture,
                drawPosition,
                mountFrame,
                drawColor,
                Data(drawPlayer).rotation,
                origin,
                1,
                spriteEffects, 0),
                drawPlayer);

            Data(drawPlayer).hatObject?.Draw(playerDrawData);

            if (shadow == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 position = drawPosition + i switch
                    {
                        1 => new Vector2(0, 2),
                        2 => new Vector2(-2, 0),
                        3 => new Vector2(0, -2),
                        _ => new Vector2(2, 0)
                    };

                    AddDrawDataWithMountShader(playerDrawData, new DrawData(
                        texture,
                        position,
                        mountFrame,
                        (drawColor * (0.5f * opacity)) with { A = 0 },
                        Data(drawPlayer).rotation,
                        origin,
                        1,
                        spriteEffects, 0),
                        drawPlayer);
                }
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
    }

    #region projectiles
    public class IceHatTrail : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public bool DoesLinger => Projectile.ai[0] == 1;
        public bool OnMount => Projectile.TryGetOwner(out Player owner) && owner.mount.Type == IceCubeMountID;

        public BlizzardCloudRenderTarget cloudTarget;
        public Vector2 canvasPosition;
        internal PrimitiveTrail TrailDrawer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Player.defaultWidth;
            Projectile.height = Player.defaultHeight;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.FinalDamage *= Utils.GetLerpValue(0, 10f, Projectile.velocity.Length(), true);

        public override void AI()
        {
            if (Projectile.TryGetOwner(out Player owner))
            {
                Projectile.Center = owner.Center;
                Projectile.velocity = owner.velocity;

                if (DoesLinger && owner.mount.Type == IceCubeMountID && Projectile.timeLeft < 2)
                    Projectile.timeLeft++;
            }

            if (!Main.dedServ)
            {
                canvasPosition = Projectile.Center;
                canvasPosition.X = (int)(canvasPosition.X / 2) * 2;
                canvasPosition.Y = (int)(canvasPosition.Y / 2) * 2;

                SpawnAndUpdateClouds();
                ManageTrail();

                if (cloudTarget != null)
                    cloudTarget.Position = canvasPosition;
            }
        }

        public void ManageTrail()
        {
            float multiplier = (Projectile.timeLeft - 30f) / 30f;
            List<Vector2> positions = [];

            for (int i = (int)(ProjectileID.Sets.TrailCacheLength[Type] * multiplier) - 1; i >= 0; i--)
                positions.Add(Projectile.oldPos[i]);

            if (positions.Count != 0 && OnMount)
            {
                TrailDrawer = TrailDrawer ?? new PrimitiveTrail(ProjectileID.Sets.TrailCacheLength[Type], WidthFunction, ColorFunction);
                TrailDrawer.SetPositionsSmart(positions, Projectile.position, FablesUtils.SmoothBezierPointRetreivalFunction);
                TrailDrawer.NextPosition = Projectile.position + Projectile.velocity;
            }
            else
            {
                TrailDrawer = null;
            }
        }

        public static float WidthFunction(float completion) => 22f;

        public static Color ColorFunction(float completion)
        {
            Color color = new Color(140, 230, 255);
            return Color.Lerp(Color.Transparent, Color.Lerp(color, Color.Transparent, (completion - 0.5f) / 0.5f), completion) * 0.4f;
        }

        public void SpawnAndUpdateClouds()
        {
            const int time_left_max = 60;
            const int smoke_duration = 10;

            if (Main.dedServ)
                return;

            if (cloudTarget == null)
                cloudTarget = new BlizzardCloudRenderTarget(400);

            float chance = Math.Max((Projectile.timeLeft - (time_left_max - smoke_duration)) / (float)smoke_duration, 0);

            if (DoesLinger)
            {
                if (Projectile.velocity.Y == 0 && Math.Abs(Projectile.velocity.X) > 2 && IceHatMount.Pulse > 0.5f && Main.rand.NextBool(5))
                {
                    Vector2 position = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
                    ParticleHandler.SpawnParticle(new SquishyLightParticle(position, Projectile.velocity * 0.5f, Main.rand.NextFloat(0.1f, 0.25f), Color.Cyan * 0.25f, Main.rand.Next(10, 30), 1, 1, 3, 1));
                }
            }
            else
            {
                if (Main.rand.NextFloat() < chance)
                {
                    Vector2 velocity = (Projectile.velocity * 0.4f).RotatedByRandom(0.5f);
                    float scale = Main.rand.NextFloat(1f, 2.5f);

                    BlizzardCloudParticle newCloud = new BlizzardCloudParticle(Projectile.Center, velocity, scale);
                    cloudTarget.SpawnParticle(newCloud);
                }
            }

            if (chance != 0 && Main.rand.NextBool())
            {
                Vector2 position = Main.rand.NextVector2FromRectangle(Projectile.Hitbox);
                Dust d = Dust.NewDustPerfect(position, DustID.SnowflakeIce, Main.rand.NextVector2Circular(3f, 3f), 0, Color.White with { A = 0 }, Main.rand.NextFloat(1f, 2.5f));
                d.velocity.X += (d.position.X - Projectile.Center.X) * 0.5f;
                d.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!Main.dedServ && oldVelocity.Y > 4 && Projectile.soundDelay == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    Particle sharticle = new IceShardParticle(Projectile.Bottom, (Vector2.UnitY * -Main.rand.NextFloat(3f, 5f)).RotatedByRandom(1.5f), 0.5f, Main.rand.Next(30, 70));
                    ParticleHandler.SpawnParticle(sharticle);
                }

                if (OnMount)
                    SoundEngine.PlaySound(LandingSound, Projectile.Bottom);

                Projectile.soundDelay = 10;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = AssetDirectory.PrimShaders.StreakyTrail;
            effect.Parameters["time"].SetValue(Main.GameUpdateCount * 0.02f);
            effect.Parameters["verticalStretch"].SetValue(0.5f);
            effect.Parameters["repeats"].SetValue(1f);

            effect.Parameters["overlayScroll"].SetValue(Main.GameUpdateCount * -0.01f);
            effect.Parameters["overlayOpacity"].SetValue(0.5f);

            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
            effect.Parameters["streakNoiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "TroubledWateryNoise").Value);
            effect.Parameters["streakScale"].SetValue(1f);
            TrailDrawer?.Render(effect, Projectile.Size * 0.4f - Main.screenPosition);


            cloudTarget?.DrawRenderTargetWithOffset(Main.spriteBatch, Main.screenPosition, Projectile.whoAmI);

            if (Projectile.ai[0] == 1)
            {
                Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
                float intensity = Math.Max((Projectile.timeLeft - 50f) / 10f, 0);

                Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.DarkGray with { A = 0 } * 0.5f * intensity, 0f, bloom.Size() / 2f, 0.3f, 0, 0);
                DrawLensFlare(MathHelper.PiOver2, intensity, 1 * intensity, 1.5f + intensity);
                Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, Color.CornflowerBlue with { A = 0 } * 0.5f * intensity, 0f, bloom.Size() / 2f, 0.5f, 0, 0);
            }

            return false;
        }

        public void DrawLensFlare(float rotation, float opacity, float thickness, float length)
        {
            Texture2D flareTex = AssetDirectory.CommonTextures.BloomStreak.Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            float sheenScale = MathF.Pow(1, 0.2f) * Projectile.scale;
            Vector2 flareOrigin = flareTex.Size() / 2f;

            Main.spriteBatch.Draw(flareTex, center - new Vector2(0, 2), null, (Color.Blue with { A = 100 }) * opacity, rotation, flareOrigin, new Vector2(thickness, length) * sheenScale, 0, 0);
            Main.spriteBatch.Draw(flareTex, center + new Vector2(0, 2), null, (Color.Blue with { A = 100 }) * opacity, rotation, flareOrigin, new Vector2(thickness, length) * sheenScale, 0, 0);
            Main.spriteBatch.Draw(flareTex, center, null, (Color.White with { A = 0 }) * opacity, rotation, flareOrigin, new Vector2(thickness, length) * sheenScale, 0, 0);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
    }

    public class IceShard : ModProjectile
    {
        public override string Texture => AssetDirectory.SnowItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ice Shard");
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 300;
            Projectile.friendly = true;
            Projectile.scale = 0;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, new(0, 10), 0.03f);
            Projectile.scale = Math.Min(Projectile.scale + 0.1f, 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 5; i++)
                ParticleHandler.SpawnParticle(new IceShardParticle(Projectile.Bottom, (Projectile.velocity * -Main.rand.NextFloat(0.5f, 1f)).RotatedByRandom(1), Projectile.scale + 0.25f, Main.rand.Next(30, 70)));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                Color trailColor = Projectile.GetAlpha(Color.PaleVioletRed) * (1f - (i / (float)ProjectileID.Sets.TrailCacheLength[Type]));
                Main.EntitySpriteDraw(texture, Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2, null, trailColor with { A = 0 }, Projectile.oldRot[i], texture.Size() / 2, Projectile.scale, default);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() / 2, Projectile.scale, default);
            return false;
        }
    }

    #endregion
}