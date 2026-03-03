using CalamityFables.Cooldowns;
using CalamityFables.Helpers;
using CalamityFables.Particles;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("RoverDrive")]
    public class RoverDrive : ModItem
    {
        public static readonly SoundStyle ShieldHurtSound = new(SoundDirectory.Wulfrum + "RoverDriveHit") { PitchVariance = 0.6f, Volume = 0.6f, MaxInstances = 0 };
        public static readonly SoundStyle ActivationSound = new(SoundDirectory.Wulfrum + "RoverDriveActivate") { Volume = 0.85f };
        public static readonly SoundStyle BreakSound = new(SoundDirectory.Wulfrum + "RoverDriveBreak") { Volume = 0.75f };

        public static int ProtectionMatrixDurabilityMax = 50;
        public static int ProtectionMatrixRechargeTime = 60 * 10;
        public static int ProtectionMatrixDefenseBoost = 10;

        public static Asset<Texture2D> NoiseTex;

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void Load()
        {
            Terraria.On_Main.DrawInfernoRings += DrawRoverDriveShields;
        }
        public override void Unload()
        {
            Terraria.On_Main.DrawInfernoRings -= DrawRoverDriveShields;
        }


        private void DrawRoverDriveShields(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            bool playerFound = false;

            for (int i = 0; i < 255; i++)
            {
                if (!Main.player[i].active || Main.player[i].outOfRange || Main.player[i].dead)
                    continue;

                RoverDrivePlayer modPlayer = Main.player[i].GetModPlayer<RoverDrivePlayer>();
                bool vanityEquipped = modPlayer.VisibleShield && !modPlayer.RoverDriveOn;

                if (!vanityEquipped && (!modPlayer.VisibleShield || modPlayer.ProtectionMatrixDurability <= 0))
                    continue;

                float scale = 0.15f + 0.03f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f + i * 0.2f));
                Effect shieldEffect = Scene["RoverDriveShield"].GetShader().Shader;

                if (playerFound == false)
                {
                    float noiseScale = MathHelper.Lerp(0.4f, 0.8f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);
                    shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
                    shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
                    shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
                    shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

                    shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

                    GetShieldBorderColor(Main.player[i], out Color edgeColor, out Color innerColor);
                    shieldEffect.Parameters["shieldColor"].SetValue(innerColor.ToVector3());
                    shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

                }

                float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
                float shieldOpacityMultiplier = vanityEquipped ? 1f : (float)Math.Pow(modPlayer.ProtectionMatrixDurability / (float)ProtectionMatrixDurabilityMax, 0.5f);
                shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity * (0.5f + 0.5f * shieldOpacityMultiplier));


                playerFound = true;
                Player myPlayer = Main.player[i];
                if (NoiseTex == null)
                    NoiseTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "TechyNoise");

                Texture2D tex = NoiseTex.Value;
                Vector2 pos = myPlayer.MountedCenter + myPlayer.gfxOffY * Vector2.UnitY - Main.screenPosition;

                Main.spriteBatch.Draw(tex, pos, null, Color.White, 0, tex.Size() / 2f, scale, 0, 0);
            }

            if (playerFound)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            }

            orig(self);
        }

        public void GetShieldBorderColor(Player player, out Color edgeColor, out Color innerColor)
        {
            if (Main.netMode != NetmodeID.SinglePlayer && player.team != 0)
            {
                switch (player.team)
                {
                    case 1: //Red team
                        innerColor = new Color(178, 24, 31);
                        edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, Color.Tomato, Color.Crimson, innerColor);
                        break;
                    case 2: //Green team
                        innerColor = new Color(194, 255, 67) * 0.7f;
                        edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, Color.Chartreuse, Color.YellowGreen, new Color(194, 255, 67));
                        break;
                    case 3: //Blue team
                        innerColor = new Color(64, 207, 200);
                        edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, Color.MediumSpringGreen, Color.DeepSkyBlue, new Color(64, 207, 200));
                        break;
                    case 4: //Yellow team
                        innerColor = new Color(176, 156, 45);
                        edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, Color.Gold, Color.Coral, Color.LightGoldenrodYellow);
                        break;
                    case 5: //Purple team
                    default:
                        innerColor = new Color(173, 111, 221);
                        edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, Color.DeepPink, Color.MediumOrchid, Color.MediumPurple);
                        break;
                }

            }

            else
            {
                Color blueTint = new Color(51, 102, 255);
                Color cyanTint = new Color(71, 202, 255);
                Color wulfGreen = new Color(194, 255, 67) * 0.8f;
                edgeColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, blueTint, cyanTint, wulfGreen);
                innerColor = blueTint;
            }
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Rover Drive");
            Tooltip.SetDefault($"Activates a protective matrix that can absorb {ProtectionMatrixDurabilityMax} damage and grants {ProtectionMatrixDefenseBoost} defense\n" +
            $"However, the systems are fickle and the shield will need {ProtectionMatrixRechargeTime / 60} seconds to charge up fully\n" +
            "Getting hit during the shield recharge period will reset it back to zero\n" +
                "Can also be scrapped at an extractinator");

            ItemID.Sets.ExtractinatorMode[Item.type] = Item.type;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;

            //Needed for extractination
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 10;
            Item.useTime = 2;
            Item.consumable = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            RoverDrivePlayer modPlayer = player.GetModPlayer<RoverDrivePlayer>();
            //modPlayer.roverDrive = true;

            modPlayer.RoverDriveOn = true;
            modPlayer.VisibleShield = !hideVisual;

            if (modPlayer.ProtectionMatrixDurability > 0)
            {
                player.statDefense += ProtectionMatrixDefenseBoost;
                player.SetCustomHurtSound(ShieldHurtSound, 20, 5f);
            }
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<RoverDrivePlayer>().VisibleShield = true;
        }

        //Scrappable for 3-6 wulfrum scrap or a 20% chance to get an energy core
        public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
        {
            resultType = ModContent.ItemType<WulfrumMetalScrap>();
            resultStack = Main.rand.Next(3, 6);

            if (Main.rand.NextFloat() > 0.8f)
            {
                resultStack = 1;
                resultType = ModContent.ItemType<EnergyCore>();
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int consumableIndex = tooltips.FindIndex(x => x.Name == "Consumable" && x.Mod == "Terraria");
            if (consumableIndex != -1)
                tooltips.RemoveAt(consumableIndex);
        }
    }

    public class RoverDrivePlayer : ModPlayer
    {
        public bool RoverDriveOn;
        public bool VisibleShield;
        public int ProtectionMatrixDurability = 0;
        public int ProtectionMatrixCharge = 0;
        public int ProtectionMatrixRepairTimer = 0;
        public int ProtectionMatrixRepairIncrementTimer = 0;

        public override void ResetEffects()
        {
            //Turn this into armor health when we can
            if (RoverDriveOn)
                Player.statLifeMax2 += ProtectionMatrixDurability;
            else
                ProtectionMatrixDurability = 0;

            RoverDriveOn = false;
            VisibleShield = false;
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (RoverDriveOn)
            {
                if (ProtectionMatrixDurability > 0)
                {
                    ProtectionMatrixDurability -= (int)info.Damage;
                    if (ProtectionMatrixDurability <= 0)
                    {
                        ProtectionMatrixDurability = 0;
                        SoundEngine.PlaySound(RoverDrive.BreakSound, Player.Center);
                        CameraManager.Shake += 2;
                    }

                    int numParticles = Main.rand.Next(2, 6);
                    for (int i = 0; i < numParticles; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3, 14);
                        velocity.X += 5f * info.HitDirection;
                        ParticleHandler.SpawnParticle(new TechyHoloysquareParticle(Player.Center, velocity, Main.rand.NextFloat(2.5f, 3f), Main.rand.NextBool() ? new Color(99, 255, 229) : new Color(25, 132, 247), 25));
                    }
                }

                if (Player.FindCooldown(WulfrumRoverDriveDurability.ID, out var cdDurability))
                {
                    cdDurability.timeLeft = ProtectionMatrixDurability;
                }

                ProtectionMatrixRepairTimer = 0;

                //Reset recharge time.
                if (Player.FindCooldown(WulfrumRoverDriveRecharge.ID, out var cd))
                {
                    cd.timeLeft = RoverDrive.ProtectionMatrixRechargeTime;
                }
            }
        }

        public override void UpdateDead()
        {
            ProtectionMatrixDurability = 0;
        }


        public override void PostUpdateMiscEffects()
        {
            if (!RoverDriveOn)
            {
                if (Player.FindCooldown(WulfrumRoverDriveDurability.ID, out var cdDurability) && !RoverDriveOn)
                    cdDurability.timeLeft = 0;

                if (Player.FindCooldown(WulfrumRoverDriveRecharge.ID, out var cdRecharge) && !RoverDriveOn)
                    cdRecharge.timeLeft = 0;

                ProtectionMatrixRepairTimer = 0;
            }

            else
            {
                //Slowly repair
                if (ProtectionMatrixDurability > 0 && ProtectionMatrixDurability < RoverDrive.ProtectionMatrixDurabilityMax)
                {
                    ProtectionMatrixRepairTimer++;
                    if (ProtectionMatrixRepairTimer > RoverDrive.ProtectionMatrixRechargeTime)
                    {
                        ProtectionMatrixRepairIncrementTimer++;
                        if (ProtectionMatrixRepairIncrementTimer > 3)
                        {
                            ProtectionMatrixRepairIncrementTimer = 0;
                            ProtectionMatrixDurability++;
                            if (Player.FindCooldown(WulfrumRoverDriveDurability.ID, out var cdDurability))
                                cdDurability.timeLeft = ProtectionMatrixDurability;
                        }
                    }
                }
                else
                    ProtectionMatrixRepairTimer = 0;


                if (Player.whoAmI == Main.myPlayer && ProtectionMatrixDurability == 0 && !Player.HasCooldown(WulfrumRoverDriveRecharge.ID))
                {
                    Player.AddCooldown(WulfrumRoverDriveRecharge.ID, RoverDrive.ProtectionMatrixRechargeTime);
                }

                //Handling case in MP where someone logs in with someone already having the rover shield, we set the value ourselves
                if (Player.whoAmI != Main.myPlayer && ProtectionMatrixDurability == 0 && Player.FindCooldown(WulfrumRoverDriveDurability.ID, out var cdHanlder))
                    ProtectionMatrixDurability = cdHanlder.timeLeft;

                if (ProtectionMatrixDurability > 0 && !Player.HasCooldown(WulfrumRoverDriveDurability.ID))
                {
                    CooldownInstance durabilityCooldown = Player.AddCooldown(WulfrumRoverDriveDurability.ID, RoverDrive.ProtectionMatrixDurabilityMax);
                    durabilityCooldown.timeLeft = ProtectionMatrixDurability;
                    SoundEngine.PlaySound(RoverDrive.ActivationSound, Player.Center);
                }

                if (ProtectionMatrixDurability > 0)
                {
                    Lighting.AddLight(Player.Center, Color.DeepSkyBlue.ToVector3() * 0.2f);
                }
            }
        }
    }
}
