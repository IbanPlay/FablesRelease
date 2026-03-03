using CalamityFables.Cooldowns;
using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using ReLogic.Utilities;
using Terraria.DataStructures;
using static Terraria.ModLoader.ModContent;
using CalamityFables.Content.Items.VanityMisc;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Terraria.Localization;

namespace CalamityFables.Content.Items.BurntDesert
{
    [AutoloadEquip(EquipType.Head)]
    [ReplacingCalamity("DesertProwlerHat")]
    public class DesertProwlerHat : ModItem
    {
        public static readonly SoundStyle HighNoonSound = new("CalamityFables/Sounds/DesertProwlerHighNoon");
        public static readonly SoundStyle JumpSound = new("CalamityFables/Sounds/DesertProwlerJump");
        public static readonly SoundStyle ReadySound = new("CalamityFables/Sounds/DesertProwlerReady");
        public static readonly SoundStyle WindLoopSound = new("CalamityFables/Sounds/DesertProwlerWindLoop") { MaxInstances = 0, IsLooped = true, Volume = 0.4f };

        public static float JumpDamageBoost = 0.5f;
        public static float MinimumRunSpeed = 2f;
        public static float FullChargeDistance = 1050f; //Seconds needed to fully charge the twister

        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void Load()
        {
            setBonusTitle = Mod.GetLocalization("Extras.ArmorSetBonus.DesertProwler.Title");
            setBonusTooltip1 = Mod.GetLocalization("Extras.ArmorSetBonus.DesertProwler.Tooltip1");
            setBonusTooltip2 = Mod.GetLocalization("Extras.ArmorSetBonus.DesertProwler.Tooltip2").WithFormatArgs((int)Math.Floor(JumpDamageBoost * 100));
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Desert Prowler Hat");
            Tooltip.SetDefault("4% increased ranged damage and 20% chance to not consume ammo");

            //Shimmers into the bloodborne set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<OldHunterHat>();

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHatHair[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 1; //6
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return (body.ModItem is DesertProwlerShirt && legs.ModItem is DesertProwlerPants);
        }

        public static bool HasArmorSet(Player player)
        {
            if (player.armor[0].IsAir || player.armor[1].IsAir || player.armor[2].IsAir)
                return false;

            //Use is instead of type checks to allow the old hunter vanity to work
            return player.armor[0].ModItem is DesertProwlerHat && 
                player.armor[1].ModItem is DesertProwlerShirt && 
                player.armor[2].ModItem is DesertProwlerPants;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Set bonus tooltip"; //More gets edited in elsewhere

            DesertProwlerPlayer armorPlayer = player.GetModPlayer<DesertProwlerPlayer>();
            armorPlayer.desertProwlerSet = true;
        }

        public static LocalizedText setBonusTitle;
        public static LocalizedText setBonusTooltip1;
        public static LocalizedText setBonusTooltip2;
        public static void ModifySetTooltips(ModItem item, List<TooltipLine> tooltips)
        {
            if (HasArmorSet(Main.LocalPlayer))
            {
                int setBonusIndex = tooltips.FindIndex(x => x.Name == "SetBonus" && x.Mod == "Terraria");

                if (setBonusIndex != -1)
                {
                    TooltipLine setBonus1 = new TooltipLine(item.Mod, "CalamityFables:SetBonus1", setBonusTitle.Value);
                    setBonus1.OverrideColor = Color.Lerp(new Color(255, 229, 156), new Color(233, 225, 198), 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f));
                    tooltips[setBonusIndex] = setBonus1;

                    TooltipLine setBonus2 = new TooltipLine(item.Mod, "CalamityFables:SetBonus2", setBonusTooltip1.Value);
                    setBonus2.OverrideColor = new Color(204, 181, 72);
                    tooltips.Insert(setBonusIndex + 1, setBonus2);

                    TooltipLine setBonus3 = new TooltipLine(item.Mod, "CalamityFables:SetBonus3", setBonusTooltip2.Value);
                    setBonus3.OverrideColor = new Color(204, 181, 72);
                    tooltips.Insert(setBonusIndex + 2, setBonus3);
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => ModifySetTooltips(this, tooltips);

        public override void UpdateEquip(Player player)
        {
            player.GetDamage<RangedDamageClass>() += 0.04f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.AntlionMandible, 2).
                AddIngredient(ItemID.Silk, 8).
                AddTile(TileID.Loom).
                Register();

            Recipe.Create(ItemType<DesertProwlerShirt>()).
                AddIngredient(ItemID.AntlionMandible, 3).
                AddIngredient(ItemID.Silk, 10).
                AddTile(TileID.Loom).
                Register();
            Recipe.Create(ItemType<DesertProwlerPants>()).
                AddIngredient(ItemID.AntlionMandible, 2).
                AddIngredient(ItemID.Silk, 5).
                AddTile(TileID.Loom).
                Register();
        }
    }

    [AutoloadEquip(EquipType.Body)]
    [ReplacingCalamity("DesertProwlerShirt")]
    public class DesertProwlerShirt : ModItem, IBulkyArmor
    {
        public override string Texture => AssetDirectory.DesertItems + Name;
        public string BulkTexture => Texture + "_Bulk";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Desert Prowler Shirt");
            Tooltip.SetDefault("4% increased ranged damage");

            //Shimmers into the bloodborne set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<OldHunterShirt>();

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);

            ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 3;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => DesertProwlerHat.ModifySetTooltips(this, tooltips);


        public override void UpdateEquip(Player player)
        {
            player.GetDamage<RangedDamageClass>() += 0.04f;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    [ReplacingCalamity("DesertProwlerPants")]
    public class DesertProwlerPants : ModItem
    {
        public override string Texture => AssetDirectory.DesertItems + Name;
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Desert Prowler Pants");
            Tooltip.SetDefault("12% increased movement speed and immunity to the Mighty Wind debuff");

            //Shimmers into the bloodborne set
            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<OldHunterPants>();
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 2;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) => DesertProwlerHat.ModifySetTooltips(this, tooltips);
        public override void UpdateEquip(Player player)
        {
            player.buffImmune[BuffID.WindPushed] = true;
            player.moveSpeed += 0.12f;
        }
    }

    public class DesertProwlerPlayer : ModPlayer
    {
        internal SlotId SmokeBombSoundSlot;
        public bool desertProwlerSet = false;
        public float twisterCharge = 0f; //0-1 charge for performing the twister jump
        public float chargeBufferTime;   //A small buffer of time that ticks down before the player loses their twister charge, in the case they're not running
        public bool twisterJump;         //Is the player performing the twister jump right now
        public float twisterJumpBufferTime; //Small extra period of time during which the buffs granted by the twister jump still apply
        public float jumpBuffVisualOpacity;

        public DustDevil attachedTwister;

        public override void Load()
        {
            FablesPlayer.PostJumpMovement += CustomTwisterJump;
            FablesPlayer.OnJumpEnd += ResetJump;

            if (!Main.dedServ)
            {
                FablesGeneralSystemHooks.PreUpdateProjectilesEvent += UpdateTwisters;
                FablesGeneralSystemHooks.ClearWorldEvent += ClearGhostTwisters;
                FablesDrawLayers.DrawThingsAbovePlayersEvent += DrawTwistersFront;
                FablesDrawLayers.PreDrawPlayersEvent += DrawTwistersBack;
                DustDevil.cloudSprite = Request<Texture2D>(AssetDirectory.DesertItems + "DustStormParticle");
            }
        }

        private void ClearGhostTwisters()
        {
            DustDevil.activeDustDevils.Clear();
        }

        private void UpdateTwisters()
        {
            for (int i = DustDevil.activeDustDevils.Count - 1; i >= 0; i--)
            {
                DustDevil twister = DustDevil.activeDustDevils[i];
                twister.Update();
            }
        }

        private void DrawTwistersBack (bool afterProjectiles)
        {
            if (afterProjectiles)
            {
                if (DustDevil.activeDustDevils.Count > 0)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (DustDevil visual in DustDevil.activeDustDevils)
                        visual.DrawBack(Main.spriteBatch);
                    Main.spriteBatch.End();
                }
            }
        }

        private void DrawTwistersFront(bool afterProjectiles)
        {
            if (afterProjectiles)
            {
                if (DustDevil.activeDustDevils.Count > 0)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (DustDevil visual in DustDevil.activeDustDevils)
                        visual.DrawFront(Main.spriteBatch);
                    Main.spriteBatch.End();
                }
            }
        }

        public override void ResetEffects()
        {
            desertProwlerSet = false;
        }

        public override void UpdateDead()
        {
            desertProwlerSet = false;
            twisterJump = false;
            jumpBuffVisualOpacity = 0;
            twisterCharge = 0f;
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            //Why are you using desert prowler post-Yharon? don't ask me
            if (desertProwlerSet && item.CountsAsClass<RangedDamageClass>() && item.ammo == AmmoID.None)
                damage.Flat += 1f;
        }


        public override void PostUpdateEquips()
        {
            if (twisterJump || twisterJumpBufferTime > 0f)
            {
                //twister charge is consumed
                twisterCharge = 0f;
                Player.GetDamage<RangedDamageClass>() += DesertProwlerHat.JumpDamageBoost;
                Player.runAcceleration *= 2f;
                Player.maxRunSpeed *= 1.5f;

                if (twisterJump)
                    Player.jumpSpeedBoost = 12;

                if (twisterJumpBufferTime == 0)
                    twisterJumpBufferTime += 0.0001f;
            }

            if (twisterJumpBufferTime > 0)
            {
                //Wrap around if the jump ended and were falling
                if (!twisterJump && Player.velocity.Y >= 0 && twisterJumpBufferTime < 0.5f)
                    twisterJumpBufferTime = 1f - twisterJumpBufferTime;

                twisterJumpBufferTime += 1 / (60f * 1.2f);
            }

            if (twisterJumpBufferTime > 1)
                twisterJumpBufferTime = 0;


            jumpBuffVisualOpacity = (float)Math.Sin(twisterJumpBufferTime * MathHelper.Pi);
            if (Main.myPlayer == Player.whoAmI)
                VignetteFadeEffects.vignetteOpacityOverride = jumpBuffVisualOpacity * 0.8f;


            bool canDechargeTwister = Player.mount.Active || Math.Abs(Player.velocity.X) < DesertProwlerHat.MinimumRunSpeed;
            bool canChargeTwister = desertProwlerSet && Player.velocity.Y == 0 && twisterJumpBufferTime <= 0 && !canDechargeTwister;

            if (canChargeTwister)
            {
                chargeBufferTime = 1f;

                float lastCharge = twisterCharge;
                twisterCharge += Math.Abs(Player.velocity.X) / DesertProwlerHat.FullChargeDistance;
                if (attachedTwister == null)
                    attachedTwister = new DustDevil(Player);

                if (twisterCharge >= 1f)
                {
                    if (lastCharge < 1f)
                        SoundEngine.PlaySound(DesertProwlerHat.ReadySound, Player.Center);

                    twisterCharge = 1f;
                    chargeBufferTime = 2f;
                }
            }

            //Quickly lose charge
            else if (canDechargeTwister && twisterCharge > 0f)
            {
                if (Player.velocity.X == 0 && attachedTwister != null)
                {
                    if (chargeBufferTime > 0f)
                        chargeBufferTime -= 1 / 60f * 3f;
                }

                if (chargeBufferTime > 0f)
                    chargeBufferTime -= 1 / 60f;
                else 
                    twisterCharge -= 1 / (60f * 0.3f);
            }
        }

        private void CustomTwisterJump(Player player)
        {
            var desertPlayer = player.GetModPlayer<DesertProwlerPlayer>();
            if (player.justJumped && player.controlUp && player.velocity.Y < 0 && player.jump > 0 && desertPlayer.twisterCharge >= 1f)
            { 
                //Short jump timeframe
                player.jump = 4;
                player.velocity.Y *= 2f;
                desertPlayer.twisterJump = true;
                desertPlayer.twisterCharge = 0;
                SoundEngine.PlaySound(DesertProwlerHat.JumpSound, player.Center);
                SoundEngine.PlaySound(DesertProwlerHat.HighNoonSound, player.Center);
                //desertPlayer.attachedTwister.position.Y -= player.velocity.Y;
                desertPlayer.attachedTwister?.JumpStartEffects();
            }
        }

        public void ResetJump(Player player) => player.GetModPlayer<DesertProwlerPlayer>().twisterJump = false;

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            r *= (1 - jumpBuffVisualOpacity * 0.7f);
            g *= (1 - jumpBuffVisualOpacity * 0.7f);
            b *= (1 - jumpBuffVisualOpacity * 0.7f);
        }
    }


    public class DustDevil
    {
        public static Asset<Texture2D> cloudSprite;
        public static List<DustDevil> activeDustDevils = new List<DustDevil>();

        public List<DustDevilCloud> clouds = new List<DustDevilCloud>();
        public List<DustDevilSwirl> swirls = new List<DustDevilSwirl>();

        public MergeBlendTextureContent frontCloudRT;
        public MergeBlendTextureContent backCloudRT;
        public DrawActionTextureContent primSwirlRT;

        public int dustSpawnTimer;
        public int swirlSpawnTimer;

        public Vector2 position;
        public Vector2 velocity;
        public Player attachedPlayer;
        public float windCharge;
        public bool jumping;
        public float spinDirection;
        public float spinTimer;
        public SlotId twisterSoundSlot = SlotId.Invalid;

        public DustDevil(Player player)
        {
            attachedPlayer = player;
            velocity = player.velocity;
            activeDustDevils.Add(this);
            RegisterRenderTargets();
        }


        public void Update()
        {
            spinTimer += 1 / 60f;
            float lastWindCharge = windCharge;

            if (attachedPlayer == null || !attachedPlayer.active)
            {
                float dischargeTime = jumping ? 1.5f : 0.7f;

                //Just Keep going where you are
                position += velocity;
                velocity *= 0.99f;
                windCharge -= 1 / (60f * dischargeTime);
                spinDirection *= 0.98f;
                if (windCharge <= 0f)
                    Kill();
            }
            else
            {
                var modPlayer = attachedPlayer.GetModPlayer<DesertProwlerPlayer>();

                //Detach if the player doesnt have any twister charge
                if (modPlayer.twisterCharge <= 0)
                {
                    attachedPlayer = null;
                    modPlayer.attachedTwister = null;

                    if (modPlayer.twisterJump)
                    {
                        //Halt in tracks
                        jumping = true;
                        velocity.X *= 0.3f;
                        velocity.Y *= 0f;
                    }
                }
                else
                {
                    windCharge = modPlayer.twisterCharge; //Sync charge as well
                                                          //Sync stats with attached player
                    position = attachedPlayer.Bottom + Vector2.UnitY * attachedPlayer.gfxOffY;
                    velocity = Vector2.Lerp(velocity, attachedPlayer.velocity, 0.1f + Math.Abs(attachedPlayer.velocity.X) * 0.1f); ;
                    if (attachedPlayer.velocity.X != 0)
                        spinDirection = attachedPlayer.velocity.X.NonZeroSign();
                    velocity.Y *= 0.3f;

                }
            }

            UpdateSound();


            if (Main.rand.NextBool(7) && !jumping)
            {
                Vector2 dustOffset = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 dustAngle = dustOffset.RotatedBy(MathHelper.PiOver2 * spinDirection.NonZeroSign());

                dustAngle *= new Vector2(1f, 0.3f);
                dustOffset *= new Vector2(27f, 4f);
                Vector2 dustPosition = position + dustOffset;
                Vector2 dustSpeed = dustAngle * windCharge - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 0.06f;

                dustSpeed += velocity - Vector2.UnitY * 0.5f;
                dustPosition.Y -= Main.rand.NextFloat(0f, 60f * windCharge);

                Particle dust = new SandyDustParticle(dustPosition, dustSpeed, Color.White, Main.rand.NextFloat(0.7f, 1f), Main.rand.Next(20, 50), 0.03f, Vector2.UnitY * 0.03f);
                ParticleHandler.SpawnParticle(dust);
            }

            if (jumping)
                SpawnMidJumpEffects(lastWindCharge);
            UpdateAndSpawnClouds();
            UpdateAndSpawnSwirls();
        }

        public void JumpStartEffects()
        {
            velocity.Y = 0;

            //Burst of dust
            for (int i = 0; i < 10; i++)
            {
                Vector2 dustPosition = position + Main.rand.NextVector2Circular(5f, 5f);
                float dustAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                dustPosition.X += (float)Math.Sin(dustAngle * spinDirection) * Main.rand.NextFloat(30f, 50f);

                Vector2 dustSpeed = velocity * 0.1f - Vector2.UnitY * 1f + Vector2.UnitX * (float)Math.Cos(dustAngle * spinDirection);

                Particle dust = new SandyDustParticle(dustPosition, dustSpeed, Color.White, Main.rand.NextFloat(0.7f, 1.2f), Main.rand.Next(20, 50), 0.03f, -Vector2.UnitY * 0.08f);
                ParticleHandler.SpawnParticle(dust);
            }

            //3 smaller swirl up streaks
            for (int i = 0; i < 3; i++)
            {
                DustDevilSwirl newSwirl = new DustDevilSwirl(Color.Sienna, Color.Lerp(Color.Sienna, Color.Black, 0.4f), 1f);

                newSwirl.flatPos = new Vector2(i / 3f + 0.15f, -20f);
                newSwirl.flatTrajectory = new Vector2(0.4f, -Main.rand.NextFloat(35f, 55f));
                newSwirl.startRadius = Main.rand.NextFloat(25f, 31f);
                newSwirl.radiusChange = -12;
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                newSwirl.radiusProgressExponent = 0.6f;

                swirls.Add(newSwirl);
            }

            //3 Big swirl up streaks
            for (int i = 0; i < 3; i++)
            {
                Color colorFront = Main.rand.NextBool(3) ? Color.LightGoldenrodYellow : Main.rand.NextBool() ? Color.PeachPuff : Color.Wheat;

                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.DarkKhaki, 1.2f);

                newSwirl.flatPos = new Vector2(i / 3f, Main.rand.NextFloat(2f));
                newSwirl.flatTrajectory = new Vector2(0.4f, -Main.rand.NextFloat(30f, 46f));
                newSwirl.startRadius = Main.rand.NextFloat(30f, 35f);
                newSwirl.radiusChange = -10;
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                newSwirl.radiusProgressExponent = 0.6f;

                swirls.Add(newSwirl);
            }

            //Small 2 bottom swirls
            for (int i = 0; i < 2; i++)
            {
                Color colorFront = Main.rand.NextBool(3) ? Color.SandyBrown : Main.rand.NextBool() ? Color.Peru : Color.Chocolate;

                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.Sienna, Main.rand.NextFloat(0.8f, 1.2f));

                newSwirl.flatPos = new Vector2(i / 2f + 0.2f, Main.rand.NextFloat(1f));
                newSwirl.flatTrajectory = new Vector2(Main.rand.NextFloat(0.2f, 0.5f), -Main.rand.NextFloat(2f, 4f));
                newSwirl.startRadius = Main.rand.NextFloat(18f, 20f);
                newSwirl.radiusChange = 10;
                newSwirl.radiusSineBump = 2f;
                newSwirl.flatSineBump = new Vector2(0f, -Main.rand.NextFloat(3f, 7f) - 1f);
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                newSwirl.radiusProgressExponent = 2f;
                swirls.Add(newSwirl);
            }

            //Big 2 Bottom swirls
            for (int i = 0; i < 2; i++)
            {
                Color colorFront = Main.rand.NextBool(3) ? Color.LightGoldenrodYellow : Main.rand.NextBool() ? Color.PeachPuff : Color.Wheat;

                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.DarkGoldenrod, Main.rand.NextFloat(0.6f, 1f));

                newSwirl.flatPos = new Vector2(i / 2f, Main.rand.NextFloat(1f) - 3f);
                newSwirl.flatTrajectory = new Vector2(Main.rand.NextFloat(0.4f, 0.5f), -Main.rand.NextFloat(2f, 4f));
                newSwirl.startRadius = Main.rand.NextFloat(25f, 35f);
                newSwirl.radiusChange = 20;
                newSwirl.radiusSineBump = 2f;
                newSwirl.flatSineBump = new Vector2(0f, -Main.rand.NextFloat(3f, 7f) - 1f);
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                newSwirl.radiusProgressExponent = 1.6f;

                swirls.Add(newSwirl);
            }
        }

        public void Kill()
        {
            if (attachedPlayer != null)
                attachedPlayer.GetModPlayer<DesertProwlerPlayer>().attachedTwister = null;
            UnloadRenderTargets();
            activeDustDevils.Remove(this);
        }

        public void UpdateSound()
        {
            if (windCharge < 0.1f)
                return;

            if (!SoundEngine.TryGetActiveSound(twisterSoundSlot, out ActiveSound swoosh))
            {
                twisterSoundSlot = SoundEngine.PlaySound(DesertProwlerHat.WindLoopSound, position, SyncSoundWithCharge);
                SoundEngine.TryGetActiveSound(twisterSoundSlot, out swoosh);
            }
            if (swoosh != null)
                SoundHandler.TrackSoundWithFade(twisterSoundSlot, 4);
        }

        public bool SyncSoundWithCharge(ActiveSound soundInstance)
        {
            soundInstance.Position = position;
            soundInstance.Volume = Utils.GetLerpValue(0.1f, 1f, windCharge, true) * 0.6f;
            soundInstance.Pitch = (float)Math.Sin(spinTimer * 3f) * 0.1f;
            return true;
        }

        public void SpawnMidJumpEffects(float lastWindCharge)
        {
            if (windCharge > 0.6f)
            {
                float flatStreakLerp = Utils.GetLerpValue(1f, 0.6f, windCharge, true);
                float flatStreakExp = 1.4f;

                float progress = (float)Math.Pow(flatStreakLerp, flatStreakExp);
                float lastProgress = (float)Math.Pow(Utils.GetLerpValue(1f, 0.6f, lastWindCharge, true), flatStreakExp);

                float stepTrehold = (float)Math.Floor(progress * 3) / 3f;

                if (stepTrehold > lastProgress || lastProgress == 0)
                {
                    float height = -80f * (float)Math.Pow(flatStreakLerp, 1.6f);

                    //Small 2 flat 
                    for (int i = 0; i < 2; i++)
                    {
                        DustDevilSwirl newSwirl = new DustDevilSwirl(Color.Orange, Color.IndianRed, Main.rand.NextFloat(0.4f, 0.6f));

                        newSwirl.flatPos = new Vector2(i / 2f + 0.4f, height - 10);
                        newSwirl.flatTrajectory = new Vector2(0.8f, -38f);
                        newSwirl.startRadius = Main.rand.NextFloat(14f, 16f) + (0.66f - progress) * 10f;
                        newSwirl.radiusChange = -10;
                        newSwirl.horizontalTwirlMultiplier = -spinDirection;
                        newSwirl.radiusProgressExponent = 2f;
                        swirls.Insert(0, newSwirl); 
                    }
                }
            }

            //Lots of dust 
            if (windCharge - 0.3f > Main.rand.NextFloat() && Main.rand.NextBool())
            {
                Vector2 dustDisplace = Main.rand.NextVector2Circular(60f, 10f);
                Vector2 dustPosition = position + dustDisplace;
                dustPosition.Y -= Main.rand.NextFloat(0.5f, 50f);
                Vector2 dustSpeed = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * 4.16f;
                dustSpeed += velocity * 0.3f;

                Particle dust = new SandyDustParticle(dustPosition, dustSpeed, Color.White, Main.rand.NextFloat(0.7f, 1.2f), Main.rand.Next(20, 50), 0.03f, -Vector2.UnitY * 0.06f - Vector2.UnitX * spinDirection * 0.1f);
                ParticleHandler.SpawnParticle(dust);

            }

        }

        public void UpdateAndSpawnClouds()
        {
            //Dust spawns faster and faster
            dustSpawnTimer++;
            int spawnRate = (int)(8 - windCharge * 7f);
            int dustAmount = (windCharge >= 1 && Main.rand.NextBool(2)) ? 3 : 2;

            if (dustSpawnTimer > spawnRate && (attachedPlayer != null || windCharge > 0.6f))
            {
                dustSpawnTimer = 0;
                for (int i = 0; i < dustAmount; i++)
                {
                    DustDevilCloud newCloud = new DustDevilCloud(Color.White);
                    if (Main.rand.NextBool(3) && windCharge >= 1)
                        newCloud.frameSpeed++;
                    clouds.Add(newCloud);
                }

            }

            if (Main.rand.NextFloat() < windCharge)
            {
                DustDevilCloud newCloud = new DustDevilCloud(Color.White, true);
                newCloud.position.Y -= Main.rand.NextFloat(0f, 30f) * windCharge;
                clouds.Add(newCloud);
            }

            //Update clouds
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                DustDevilCloud cloud = clouds[i];
                cloud.frameCount++;
                if (cloud.frameCount > cloud.frameSpeed)
                {
                    cloud.frameCount = 0;
                    cloud.frame++;
                    if (cloud.frame >= 6)
                    {
                        clouds.RemoveAt(i);
                        continue;
                    }
                }

                cloud.position += cloud.velocity;

                //Accelerate
                if (cloud.frame < 4)
                {
                    cloud.velocity.X = MathHelper.Lerp(cloud.velocity.X, 0.06f * windCharge * -spinDirection, 0.03f);
                    cloud.velocity.Y = MathHelper.Lerp(cloud.velocity.Y, -0.8f, 0.05f);
                }
                //Slown down
                else if (!jumping)
                    cloud.velocity = Vector2.Lerp(cloud.velocity, Vector2.Zero, 0.04f);

                if (jumping)
                    cloud.velocity.Y *= 1.04f;


                cloud.position += cloud.velocity;

                //Warp around
                if (cloud.position.X > 2)
                    cloud.position.X -= 2;
                if (cloud.position.X < 0)
                    cloud.position.X += 2;

                cloud.rotation -= spinDirection * 0.012f;
            }
        }

        public void UpdateAndSpawnSwirls()
        {
            swirlSpawnTimer++;
            int spawnRate = (int)(14 - windCharge * 6f);

            if (swirlSpawnTimer > spawnRate && Main.rand.NextBool(10) && !jumping)
            {
                swirlSpawnTimer = 0;

                Color colorFront = Main.rand.NextBool(3)? Color.BurlyWood : Main.rand.NextBool() ? Color.SandyBrown : Color.Wheat;

                DustDevilSwirl newSwirl = new DustDevilSwirl(colorFront, Color.DarkGoldenrod, Main.rand.NextFloat(0.3f, 0.5f));

                newSwirl.flatPos = new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat(6f));
                newSwirl.flatTrajectory = new Vector2(0.6f, -15f + -10f * windCharge);
                newSwirl.startRadius = Main.rand.NextFloat(15f, 25f) * (0.8f + 0.3f * windCharge);
                newSwirl.radiusChange = -6f * windCharge - 7;
                newSwirl.horizontalTwirlMultiplier = -spinDirection;
                swirls.Add(newSwirl);
            }

            //Update swirls
            for (int i = swirls.Count - 1; i >= 0; i--)
            {
                DustDevilSwirl swirl = swirls[i];
                swirl.timer += 1 / (60f * swirl.lifetime);
                if (swirl.timer > 1)
                {
                    swirls.RemoveAt(i);
                    continue;
                }

                swirl.flatPos.X += swirl.horizontalTwirlMultiplier * 0.02f;
            }
        }


        #region Drawing
        public static float TwisterWidth = 90f;

        public void DrawCloudsFront(SpriteBatch spriteBatch, bool backgroundPass)
        {
            Texture2D tex = cloudSprite.Value;
            Vector2 textureOrigin = new Vector2(100f, 350f);
            Color color = backgroundPass ? Color.Black : Color.White;
            Color darkColor = new Color(178, 135, 100);

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                DustDevilCloud cloud = clouds[i];
                if (cloud.position.X >= 1f)
                    continue;

                Rectangle frame = tex.Frame(6, 6, cloud.frame, cloud.variant);
                Vector2 offset = cloud.position;
                offset.X -= 0.5f;
                offset.X *= TwisterWidth * (0.4f + 0.6f * Utils.GetLerpValue(0f, -35f, offset.Y, true));


                Color usedColor = color;
                if (!backgroundPass)
                    usedColor = Color.Lerp(color, darkColor, Utils.GetLerpValue(0.2f, 0f, cloud.position.X, true) + Utils.GetLerpValue(0.8f, 1f, cloud.position.X, true));

                spriteBatch.Draw(tex, textureOrigin + offset, frame, usedColor, cloud.rotation, frame.Size() / 2f, 1f, 0, 0);
            }
        }

        public void DrawCloudsBack(SpriteBatch spriteBatch, bool backgroundPass)
        {
            Texture2D tex = cloudSprite.Value;
            Vector2 textureOrigin = new Vector2(100f, 350f);
            Color color = backgroundPass ? Color.Black : new Color(178, 135, 100);

            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                DustDevilCloud cloud = clouds[i];
                if (cloud.position.X < 1f)
                    continue;

                Rectangle frame = tex.Frame(6, 6, cloud.frame, cloud.variant);
                Vector2 offset = cloud.position;
                offset.X -= 1.5f;
                offset.X *= -TwisterWidth * (0.4f + 0.6f * Utils.GetLerpValue(0f, -35f, offset.Y, true));


                spriteBatch.Draw(tex, textureOrigin + offset, frame, color, cloud.rotation, frame.Size() / 2f, 1f, SpriteEffects.FlipHorizontally, 0);
            }
        }

        public void DrawSwirls(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Begin();
            Main.spriteBatch.End();

            int swirlCount = swirls.Count;
            if (swirlCount == 0)
                return;

            //translation so its got the proper origin
            Matrix translation = Matrix.CreateTranslation(75, 190, 0);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, 300, 200, 0, -1, 1);
            Effect swirlEffect = Scene["DustDevilPrimitive"].GetShader().Shader;
            swirlEffect.Parameters["uWorldViewProjection"].SetValue(translation * projection);

            //Multiply the values by 2 because every swirl will draw twice, once for the front side and once for the back side
            int verticesPerSwirl = 2 * DustDevilSwirl.SWIRL_TRAIL_DEFINITION * 2;
            int indicesPerSwirl = 6 * (DustDevilSwirl.SWIRL_TRAIL_DEFINITION - 1) * 2;

            int vertexCount = verticesPerSwirl * swirlCount;
            int indexCount = indicesPerSwirl * swirlCount;

            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[vertexCount];
            short[] indices = new short[indexCount];

            for (int i = 0; i < swirlCount; i++)
            {
                swirls[i].ConstructPrimitives(i * verticesPerSwirl, i * indicesPerSwirl, ref vertices, ref indices, Vector2.UnitX * 150);
            }

            swirlEffect.CurrentTechnique.Passes[0].Apply();
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
        }

        public void DrawFront(SpriteBatch spriteBatch)
        {
            Color tileColor = Lighting.GetColor(position.ToTileCoordinates());

            float opacity = windCharge;
            if (jumping)
                opacity = (float)Math.Pow(windCharge, 0.3f);

            frontCloudRT.Request();
            if (frontCloudRT.IsReady)
                spriteBatch.Draw(frontCloudRT.GetTarget(), position - Main.screenPosition, null, tileColor * 0.7f * opacity, 0, new Vector2(100f, 350f), 1f, 0, 0);

            //primSwirlRT.Request();
            if (primSwirlRT.IsReady)
                spriteBatch.Draw(primSwirlRT.GetTarget(), position - Main.screenPosition, new Rectangle(0, 0, 150, 200), tileColor * 0.6f * opacity, 0, new Vector2(75f, 190f), 2f, 0, 0);
       }

        public void DrawBack(SpriteBatch spriteBatch)
        {
            Color tileColor = Lighting.GetColor(position.ToTileCoordinates());
            backCloudRT.Request();

            float opacity = windCharge;
            if (jumping)
                opacity = (float)Math.Pow(windCharge, 0.3f);

            if (backCloudRT.IsReady)
                spriteBatch.Draw(backCloudRT.GetTarget(), position - Main.screenPosition, null, tileColor * 0.3f * opacity, 0, new Vector2(100f, 350f), 1f, 0, 0);

            primSwirlRT.Request();
            if (primSwirlRT.IsReady)
                spriteBatch.Draw(primSwirlRT.GetTarget(), position - Main.screenPosition, new Rectangle(150, 0, 150, 200), tileColor * 0.6f * opacity, 0, new Vector2(75f, 190f), 2f, 0, 0);
        }

        public void RegisterRenderTargets()
        {
            frontCloudRT = new MergeBlendTextureContent(DrawCloudsFront, 200, 400);
            Main.ContentThatNeedsRenderTargets.Add(frontCloudRT);
            backCloudRT = new MergeBlendTextureContent(DrawCloudsBack, 200, 400);
            Main.ContentThatNeedsRenderTargets.Add(backCloudRT);
            primSwirlRT = new DrawActionTextureContent(DrawSwirls, 300, 200, startSpritebatch:false);
            Main.ContentThatNeedsRenderTargets.Add(primSwirlRT);
        }

        public void UnloadRenderTargets()
        {
            frontCloudRT.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(frontCloudRT);
            frontCloudRT.GetTarget()?.Dispose();
            backCloudRT.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(backCloudRT);
            backCloudRT.GetTarget()?.Dispose();
            primSwirlRT.Reset();
            Main.ContentThatNeedsRenderTargets.Remove(primSwirlRT);
        }
        #endregion


        public class DustDevilCloud
        {
            public int variant;
            public int frameCount;
            public int frameSpeed;
            public int frame;
            public float rotation;
            public Vector2 velocity;
            public Vector2 position;
            public Color color;

            public DustDevilCloud(Color tint, bool small = false)
            {
                variant = Main.rand.Next(3);
                if (small)
                    variant += 3;
                frameSpeed = Main.rand.Next(5, 9);

                frameCount = 0;
                frame = 0;
                velocity = -Vector2.UnitY * Main.rand.NextFloat(0f, 0.6f);
                position = new Vector2(Main.rand.NextFloat(0f, 2f), -Main.rand.NextFloat(0f, 10f));
                color = tint;
                rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }

        public class DustDevilSwirl
        {
            public const int SWIRL_TRAIL_DEFINITION = 10;

            public Color color;
            public Color backColor;

            public Vector2 spawnPosition; //Used for the sandstorm sprite swirls
            public Vector2 drawOffset;
            public bool extraThin = false;

            public Vector2 flatPos;         //Start position
            public Vector2 flatTrajectory;  //The distance it travels on a unwrapped 2d projection of the tornado
            public Vector2 flatSineBump;    //An offset applied to the positions that has a sine easing, aka it applies at the center but not the ends

            public float startRadius;    //Start radius
            public float radiusChange;   //The change in radius along the path
            public float radiusSineBump; //An offset applied to the radius that has a sine easing, aka it applies at the center but not the ends

            public float positionProgressExponent = 1f;
            public float radiusProgressExponent = 1f;

            public float timer;
            public float lifetime;

            public float horizontalTwirlMultiplier = 1f;
            public float twirlMultiplierAim;

            public DustDevilSwirl(Color tint, Color backTint, float lifeTime)
            {
                color = tint;
                backColor = backTint;
                lifetime = lifeTime;
            }

            public Vector2 FlatPositionAlongPath(float progress, out float radius)
            {
                //Get the progress point of the start and end and then figure out what our progress is along the line
                float startPoint = (float)Math.Pow(timer, 3f);
                float endPoint = (float)Math.Pow(timer, 0.3f);
                float currentProgressPoint = MathHelper.Lerp(startPoint, endPoint, (float)Math.Pow(progress, positionProgressExponent));
                float sineBump = (float)Math.Sin(currentProgressPoint * MathHelper.Pi);

                Vector2 usedTrajectory = flatTrajectory;
                usedTrajectory.X *= horizontalTwirlMultiplier;

                Vector2 start = flatPos + usedTrajectory * startPoint;
                Vector2 end = flatPos + usedTrajectory * endPoint;

                radius = MathHelper.Lerp(startRadius + radiusChange * startPoint, startRadius + radiusChange * endPoint, (float)Math.Pow(progress, radiusProgressExponent));
                radius += radiusSineBump * sineBump;


                //Lerp between the two positions and then add the sine displacement
                return Vector2.Lerp(start, end, progress) + sineBump * flatSineBump;
            }

            public Vector2 TorsePosition(Vector2 flatPosition, float radius, out float depth)
            {
                float x = (float)Math.Sin(flatPosition.X * MathHelper.TwoPi) * radius;
                depth = (float)Math.Cos(flatPosition.X * MathHelper.TwoPi) * radius;

                return new Vector2(x, flatPosition.Y);
            }

            public void ConstructPrimitives(int vIndex, int iIndex, ref VertexPositionColorTexture[] vertices, ref short[] indices, Vector2 backSideOffset)
            {
                //Vertex and index count for a single swirl
                int vertexCount = SWIRL_TRAIL_DEFINITION * 2;
                int indexCount = (SWIRL_TRAIL_DEFINITION - 1) * 6;

                float swirlHeightMultiplier = 1 - (float)Math.Pow(timer, 4f);
                if (extraThin)
                    swirlHeightMultiplier *= Utils.GetLerpValue(0f, 0.8f, timer, true);

                for (int i = 0; i < SWIRL_TRAIL_DEFINITION; i++)
                {
                    float progress = i / (float)(SWIRL_TRAIL_DEFINITION - 1);
                    float swirlHeight = 1 + 2f * (float)Math.Sin(progress * MathHelper.Pi);
                    swirlHeight *= swirlHeightMultiplier;


                    //Get the 2D position along the cylinder and then transform it into a pseudo 3D position with sines and such
                    Vector2 flatPosition = FlatPositionAlongPath(progress, out float radius);
                    Vector2 vertexPos = TorsePosition(flatPosition, radius, out float depth) + drawOffset;
                    Vector2 vertexPosTop = vertexPos + Vector2.UnitY * swirlHeight;

                    vertices[vIndex     + i * 2] = new VertexPositionColorTexture(vertexPos.Vec3(), color, new Vector2(1, depth));
                    vertices[vIndex + 1 + i * 2] = new VertexPositionColorTexture(vertexPosTop.Vec3(), color, new Vector2(1, depth));

                    //Draw the same thing again but this time for the back side
                    vertices[vIndex     + i * 2 + vertexCount] = new VertexPositionColorTexture((vertexPos + backSideOffset).Vec3(), backColor, new Vector2(-1, depth));
                    vertices[vIndex + 1 + i * 2 + vertexCount] = new VertexPositionColorTexture((vertexPosTop + backSideOffset).Vec3(), backColor, new Vector2(-1, depth));

                    if (i < SWIRL_TRAIL_DEFINITION - 1)
                    {
                        //Front side indices
                        indices[iIndex + i * 6] =     (short)(vIndex + (i + 1) * 2);
                        indices[iIndex + i * 6 + 1] = (short)(vIndex + (i + 1) * 2 + 1);
                        indices[iIndex + i * 6 + 2] = (short)(vIndex + i * 2 + 1);
                        indices[iIndex + i * 6 + 3] = (short)(vIndex + (i + 1) * 2);
                        indices[iIndex + i * 6 + 4] = (short)(vIndex + i * 2 + 1);
                        indices[iIndex + i * 6 + 5] = (short)(vIndex + i * 2);

                        //Back side indices
                        indices[iIndex + i * 6     + indexCount] = (short)(vIndex + (i + 1) * 2      + vertexCount);
                        indices[iIndex + i * 6 + 1 + indexCount] = (short)(vIndex + (i + 1) * 2 + 1  + vertexCount);
                        indices[iIndex + i * 6 + 2 + indexCount] = (short)(vIndex + i * 2 + 1        + vertexCount);
                        indices[iIndex + i * 6 + 3 + indexCount] = (short)(vIndex + (i + 1) * 2      + vertexCount);
                        indices[iIndex + i * 6 + 4 + indexCount] = (short)(vIndex + i * 2 + 1        + vertexCount);
                        indices[iIndex + i * 6 + 5 + indexCount] = (short)(vIndex + i * 2            + vertexCount);
                    }
                }
            }
        }
    }

    public class SandyDustParticle : Particle
    {
        public override string Texture => AssetDirectory.DesertItems + "SandyDust";
        public override bool UsesNonPremultipliedAlpha => false; //Doesn't actually use half transparency, but guarantees that it gets drawn above the bigger smoke clouds
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        private float Spin;
        private float opacity;
        private Vector2 Gravity;
        public Rectangle Frame;

        public SandyDustParticle(Vector2 position, Vector2 velocity, Color color, float scale, int lifeTime, float rotationSpeed = 1f, Vector2? gravity = null)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Lifetime = lifeTime;
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Spin = rotationSpeed;
            Gravity = (Vector2)(gravity == null ? Vector2.Zero : gravity);
            Variant = Main.rand.Next(12);
            Frame = new Rectangle(Variant % 6 * 12, 12 + Variant / 6 * 12, 10, 10);
        }

        public override void Update()
        {
            Velocity += Gravity;
            opacity = (float)Math.Sin(LifetimeCompletion * MathHelper.PiOver2 + MathHelper.PiOver2);

            Color = Lighting.GetColor(Position.ToTileCoordinates()) with { A = Color.A };

            Velocity *= 0.95f;
            Rotation += Spin * ((Velocity.X > 0) ? 1f : -1f);
            Scale *= 0.98f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D dustTexture = ParticleTexture;
            spriteBatch.Draw(dustTexture, Position - basePosition, Frame, Color * opacity, Rotation, Frame.Size() / 2f, Scale, SpriteEffects.None, 0);
        }
    }
}
