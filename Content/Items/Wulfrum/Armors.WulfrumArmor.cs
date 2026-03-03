using CalamityFables.Cooldowns;
using CalamityFables.Core.DrawLayers;
using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using static Microsoft.Xna.Framework.Input.Keys;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Items.Wulfrum
{
    #region Armor pieces
    //Datsuzei / Moonstone armor from starlight river was a nice jumping off point for this kind of set.

    [AutoloadEquip(EquipType.Head)]
    [ReplacingCalamity("WulfrumHat")]
    public class WulfrumHat : ModItem, IExtendedHat
    {
        #region big hat
        public string ExtensionTexture => AssetDirectory.WulfrumItems + Name + "_HeadExtension";
        public Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo) => -Vector2.UnitY * 2f;
        public string EquipSlotName(Player drawPlayer) => drawPlayer.Male ? Name : "WulfrumHatFemale";
        #endregion

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public static readonly SoundStyle SetActivationSound = new(SoundDirectory.Wulfrum + "WulfrumBastionActivate");
        public static readonly SoundStyle SetBreakSound = new(SoundDirectory.Wulfrum + "WulfrumBastionBreak");
        public static readonly SoundStyle SetBreakSoundSafe = new(SoundDirectory.Wulfrum + "WulfrumBastionBreakSafely");

        public static int BastionBuildTime = (int)(0.55f * 60);
        public static int BastionTime = 15 * 60;
        public static int TimeLostPerHit = 1 * 60;
        public static int BastionCooldown = 20 * 60;


        internal static Item DummyCannon = new Item(); //Used for the attack swap. Basically we force the player to hold a fake item.

        public static bool PowerModeEngaged(Player player, out CooldownInstance cd)
        {
            cd = null;
            bool hasWulfrumBastionCD = player.FindCooldown(WulfrumBastionCooldown.ID, out cd);
            return (hasWulfrumBastionCD && cd.timeLeft > BastionCooldown);
        }

        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, AssetDirectory.WulfrumItems + Name + "_FemaleHead", EquipType.Head, name: "WulfrumHatFemale");
            }

            FablesPlayer.ArmorSetBonusActivatedEvent += ActivateSetBonus;
            On_Main.DrawPendingMouseText += SpoofMouseItem;

            setBonusTitle = Mod.GetLocalization("Extras.ArmorSetBonus.Wulfrum.Title");
            setBonusTooltip1 = Mod.GetLocalization("Extras.ArmorSetBonus.Wulfrum.Tooltip1");
            setBonusTooltip2 = Mod.GetLocalization("Extras.ArmorSetBonus.Wulfrum.Tooltip2");
            setBonusFusionCannon = Mod.GetLocalization("Extras.ArmorSetBonus.Wulfrum.ExpandCannonInfo");
        }

        public override void Unload()
        {
            DummyCannon.TurnToAir();
            DummyCannon = null;
        }

        private void ActivateSetBonus(Player player)
        {
            if (HasArmorSet(player) && !player.mount.Active)
            {
                //Only activate if no cooldown & available scrap.
                if (player.FindCooldown(WulfrumBastionCooldown.ID, out CooldownInstance cd))
                {
                    if (cd.timeLeft > BastionCooldown && cd.timeLeft < BastionCooldown + BastionTime - 60 * 3)
                    {
                        cd.timeLeft = BastionCooldown + 1;
                        player.GetModPlayer<CooldownsPlayer>().SyncCooldownDictionary(false);
                    }
                }

                else if (player.HasItem(ItemType<WulfrumMetalScrap>()))
                {
                    player.ConsumeItem(ItemType<WulfrumMetalScrap>());
                    //I Thiiiinnnk there's no need to add mp syncing packets sicne cooldowns get auto synced right.
                    player.AddCooldown(WulfrumBastionCooldown.ID, BastionCooldown + BastionTime);
                    //Though do i need to sync that or is the player inventory auto synced?
                    DummyCannon.SetDefaults(ItemType<WulfrumFusionCannon>());
                }
            }
        }

        //Replaces the tooltip of the armor set with the fusion cannon if the player holds shift
        private void SpoofMouseItem(Terraria.On_Main.orig_DrawPendingMouseText orig)
        {
            var player = Main.LocalPlayer;

            if (DummyCannon.IsAir && !Main.gameMenu)
                DummyCannon.SetDefaults(ItemType<WulfrumFusionCannon>());

            if (IsPartOfSet(Main.HoverItem) && HasArmorSet(player) && Main.keyState.IsKeyDown(LeftShift))
            {
                Main.HoverItem = DummyCannon.Clone();
                Main.hoverItemName = DummyCannon.Name;
            }

            orig();
        }


        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Hat & Goggles");
            Tooltip.SetDefault("10% increased whip range\n" +
                "Comes equipped with hair extensions");

            if (Main.dedServ)
                return;
            for (int i = 1; i < 9; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumPowerSuit" + i.ToString()).Type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(0, 0, 75, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) => body.type == ItemType<WulfrumJacket>() && legs.type == ItemType<WulfrumOveralls>();
        public static bool HasArmorSet(Player player) => player.armor[0].type == ItemType<WulfrumHat>() && player.armor[1].type == ItemType<WulfrumJacket>() && player.armor[2].type == ItemType<WulfrumOveralls>();
        public bool IsPartOfSet(Item item) => item.type == ItemType<WulfrumHat>() ||
                item.type == ItemType<WulfrumJacket>() ||
                item.type == ItemType<WulfrumOveralls>();

        public override void UpdateArmorSet(Player player)
        {
            WulfrumArmorPlayer armorPlayer = player.GetModPlayer<WulfrumArmorPlayer>();
            WulfrumTransformationPlayer transformationPlayer = player.GetModPlayer<WulfrumTransformationPlayer>();

            armorPlayer.wulfrumSet = true;

            player.setBonus = Language.GetTextValue("CommonItemTooltip.IncreasesMaxMinionsBy", 1); //The cooler part of the set bonus happens in modifytooltips because i can't recolor it otherwise. Madge
            player.maxMinions++;
            if (PowerModeEngaged(player, out var cd))
            {
                if (cd.timeLeft == BastionCooldown + BastionTime)
                {
                    ActivationEffects(player);
                }

                //Stats
                player.statDefense += 13;
                player.endurance += 0.05f; //10% Dr in total with the chestplate
                player.GetModPlayer<FablesPlayer>().MoveSpeedModifier *= 0.8f;

                //Can't account for previous fullbody transformations but at this point, whatever.
                Item headItem = player.armor[10].type != 0 ? player.armor[10] : player.armor[0];
                bool hatVisible = !transformationPlayer.transformationActive && headItem.type == ItemType<WulfrumHat>();


                //Spawn the hat
                if (cd.timeLeft == BastionCooldown + BastionTime - (int)(BastionBuildTime * 0.9f) && hatVisible)
                {
                    Particle leftoverHat = new WulfrumHatParticle(player, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 7f), 25);
                    ParticleHandler.SpawnParticle(leftoverHat);
                }


                //Visuals
                if (cd.timeLeft < BastionCooldown + BastionTime - BastionBuildTime)
                    player.GetModPlayer<WulfrumTransformationPlayer>().transformationActive = true;
                else if (cd.timeLeft <= BastionCooldown + BastionTime - (int)(BastionBuildTime * 0.9f))
                    player.GetModPlayer<WulfrumTransformationPlayer>().forceHelmetOn = true;


                //Swapping the arm.
                if (DummyCannon.IsAir)
                    DummyCannon.SetDefaults(ItemType<WulfrumFusionCannon>());

                if (Main.myPlayer == player.whoAmI)
                {
                    //Drop the player's held item if they were holding something before
                    if (!(Main.mouseItem.type == DummyCannon.type) && !Main.mouseItem.IsAir)
                        Main.LocalPlayer.QuickSpawnItem(null, Main.mouseItem, Main.mouseItem.stack);

                    Main.mouseItem = DummyCannon;
                }

                //Slot 58 is the "fake" slot thats used for the item the player is holding in their mouse.
                player.inventory[58] = DummyCannon;
                player.selectedItem = 58;
            }

            else if (Main.myPlayer == player.whoAmI)
            {
                //Clear the player's hand
                if (Main.mouseItem.type == ItemType<WulfrumFusionCannon>())
                    Main.mouseItem = new Item();

                DummyCannon.TurnToAir();
            }
        }

        public void ActivationEffects(Player player)
        {
            SoundEngine.PlaySound(SetActivationSound, player.Center);

            //Do'nt do the effect ifthe player is already using the wulfrum vanity lol.
            bool transformedAlready = player.GetModPlayer<WulfrumTransformationPlayer>().transformationActive;

            if (!transformedAlready)
            {
                for (int i = 0; i < 5; i++)
                {
                    Particle part = new WulfrumBastionPartsParticle(player, i, BastionBuildTime + 2);
                    ParticleHandler.SpawnParticle(part);
                }
            }

            //Do spawn the cannon always though
            Particle gun = new WulfrumBastionPartsParticle(player, 5, BastionBuildTime + 2);

            if (transformedAlready)
            {
                (gun as WulfrumBastionPartsParticle).TimeOffset = 0;
                (gun as WulfrumBastionPartsParticle).AnimationTime = BastionBuildTime + 2;
            }
            ParticleHandler.SpawnParticle(gun);
        }

        public static LocalizedText setBonusTitle;
        public static LocalizedText setBonusTooltip1;
        public static LocalizedText setBonusTooltip2;
        public static LocalizedText setBonusFusionCannon;
        public static void ModifySetTooltips(ModItem item, List<TooltipLine> tooltips)
        {
            if (HasArmorSet(Main.LocalPlayer))
            {
                int setBonusIndex = tooltips.FindIndex(x => x.Name == "SetBonus" && x.Mod == "Terraria");

                if (setBonusIndex != -1)
                {
                    string doubleTapDir = Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.UP" : "Key.DOWN");
                    TooltipLine setBonus1 = new TooltipLine(item.Mod, "CalamityFables:SetBonus1", setBonusTitle.Format(doubleTapDir));
                    setBonus1.OverrideColor = Color.Lerp(new Color(194, 255, 67), new Color(112, 244, 244), 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f));
                    tooltips.Insert(setBonusIndex + 1, setBonus1);

                    TooltipLine setBonus2 = new TooltipLine(item.Mod, "CalamityFables:SetBonus2", setBonusTooltip1.Value);
                    setBonus2.OverrideColor = new Color(110, 192, 93);
                    tooltips.Insert(setBonusIndex + 2, setBonus2);

                    TooltipLine setBonus3 = new TooltipLine(item.Mod, "CalamityFables:SetBonus3", setBonusTooltip2.Value);
                    setBonus3.OverrideColor = new Color(110, 192, 93);
                    tooltips.Insert(setBonusIndex + 3, setBonus3);

                    if (!Main.keyState.IsKeyDown(LeftShift))
                    {
                        TooltipLine itemDisplay = new TooltipLine(item.Mod, "CalamityFables:ArmorItemDisplay", setBonusFusionCannon.Value);
                        itemDisplay.OverrideColor = new Color(190, 190, 190);
                        tooltips.Add(itemDisplay);
                    }
                }

            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) => ModifySetTooltips(this, tooltips);

        public override void UpdateEquip(Player player)
        {
            player.whipRangeMultiplier += 0.1f;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(5).
                AddIngredient<EnergyCore>().
                AddTile(TileID.Anvils).
                Register();

            Recipe.Create(ItemType<WulfrumJacket>()).
                AddIngredient<WulfrumMetalScrap>(12).
                AddIngredient<EnergyCore>().
                AddTile(TileID.Anvils).
                Register();

            Recipe.Create(ItemType<WulfrumOveralls>()).
                AddIngredient<WulfrumMetalScrap>(8).
                AddIngredient<EnergyCore>().
                AddTile(TileID.Anvils).
                Register();
        }
    }

    [AutoloadEquip(EquipType.Body)]
    [ReplacingCalamity("WulfrumJacket")]
    public class WulfrumJacket : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Jacket");
            Tooltip.SetDefault("5% increased damage reduction"); //Increases to 10 with the wulfrum bastion active

            if (Main.netMode != NetmodeID.Server)
            {
                var equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
                ArmorIDs.Body.Sets.HidesArms[equipSlot] = true;
                ArmorIDs.Body.Sets.HidesTopSkin[equipSlot] = true;
            }
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(0, 0, 50, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 2;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => WulfrumHat.ModifySetTooltips(this, tooltips);

        public override void UpdateEquip(Player player) => player.endurance += 0.05f;
    }

    [AutoloadEquip(EquipType.Legs)]
    [ReplacingCalamity("WulfrumOveralls")]
    public class WulfrumOveralls : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Overalls");
            Tooltip.SetDefault("5% increased movement speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(0, 0, 25, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 1;

            if (Main.netMode != NetmodeID.Server)
            {
                var equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
                ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) => WulfrumHat.ModifySetTooltips(this, tooltips);
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.05f;
        }
    }
    #endregion

    public class WulfrumArmorPlayer : ModPlayer
    {
        public static int BastionShootDamage = 10;
        public static float BastionShootSpeed = 18f;
        public static int BastionShootTime = 10;

        public bool wulfrumSet = false;

        public override void ResetEffects()
        {
            wulfrumSet = false;
        }

        public override void UpdateDead()
        {
            wulfrumSet = false;
        }

        public override void UpdateEquips()
        {
            if (wulfrumSet && (Player.name.ToLower() == "wagstaff" || Player.name.ToLower() == "john wulfrum"))
                Player.SetCustomHurtSound(SoundID.DSTMaleHurt, 10);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (WulfrumHat.PowerModeEngaged(Player, out var cd) && Main.netMode != NetmodeID.Server)
            {
                SetBonusEndEffect(true);
                if (!Player.GetModPlayer<WulfrumTransformationPlayer>().vanityEquipped)
                    Player.GetModPlayer<WulfrumTransformationPlayer>().transformationActive = false;
            }
        }

        public override void PostUpdate()
        {
            if (wulfrumSet && Player.FindCooldown(WulfrumBastionCooldown.ID, out var cd) && cd.timeLeft == WulfrumHat.BastionCooldown)
            {
                SetBonusEndEffect(false);
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (WulfrumHat.PowerModeEngaged(Player, out var cd))
            {
                cd.timeLeft -= WulfrumHat.TimeLostPerHit;
                if (cd.timeLeft < WulfrumHat.BastionCooldown)
                {
                    cd.timeLeft = WulfrumHat.BastionCooldown - 1;
                    if (Main.netMode != NetmodeID.Server)
                        SetBonusEndEffect(true);
                }
            }
        }

        public void SetBonusEndEffect(bool violent)
        {
            if (Main.dedServ)
                return;

            SoundStyle breakSound = WulfrumHat.SetBreakSoundSafe;
            float goreSpeed = 3f;
            int goreCount = 4;
            int goreIncrement = 2;

            if (violent)
            {
                breakSound = WulfrumHat.SetBreakSound;
                goreSpeed = 9f;
                goreCount = 9;
                goreIncrement = 1;
            }

            SoundEngine.PlaySound(breakSound, Player.Center);
            //Only spawn the cannon gore if the player already has the vanity on.
            if (Player.GetModPlayer<WulfrumTransformationPlayer>().vanityEquipped)
            {
                Vector2 shrapnelVelocity = Main.rand.NextVector2Circular(goreSpeed, goreSpeed);
                Gore.NewGore(Player.GetSource_Death(), Player.Center, shrapnelVelocity, Mod.Find<ModGore>("WulfrumPowerSuit1").Type);
            }

            else
            {
                int j = 1;

                for (int i = 1; i < goreCount; i++)
                {
                    Vector2 shrapnelVelocity = Main.rand.NextVector2Circular(goreSpeed, goreSpeed);
                    string goreType = "WulfrumPowerSuit" + j.ToString();
                    Gore.NewGore(Player.GetSource_Death(), Player.Center, shrapnelVelocity, Mod.Find<ModGore>(goreType).Type);

                    j += Main.rand.Next(1, goreIncrement);
                }
            }
        }


        public override void PostUpdateMiscEffects()
        {
            //This is important. Prevents ppl cheat sheeting the item in from softlocking their mouse button lol
            if (Main.mouseItem.ModItem is WulfrumFusionCannon && !WulfrumHat.PowerModeEngaged(Player, out _))
            {
                Main.mouseItem.TurnToAir();
            }

            //This shouldn't ever be possible since the power mode prevents you from using or moving items around
            if (!wulfrumSet && WulfrumHat.PowerModeEngaged(Player, out var cd))
            {
                cd.timeLeft = WulfrumHat.BastionCooldown;
            }
        }

        public override void FrameEffects()
        {
            //Give the braids variant to w*men
            if (!Player.Male && Player.head == EquipLoader.GetEquipSlot(Mod, "WulfrumHat", EquipType.Head))
                Player.head = EquipLoader.GetEquipSlot(Mod, "WulfrumHatFemale", EquipType.Head);
        }
    }

    #region Particles
    public class WulfrumBastionPartsParticle : Particle
    {
        public override bool SetLifetime => true;
        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumBastionParts";
        public override bool UseCustomDraw => true;

        internal Rectangle Frame;
        internal Vector2 DestinationOffset;
        public Vector2 Offset;
        public float RotationOffset;
        public Player Owner;
        public float TimeOffset;
        public int AnimationTime;
        public float LifetimeCompletionAdjusted => Math.Clamp((Time - Lifetime * TimeOffset) / (float)AnimationTime, 0, 1f);

        public WulfrumBastionPartsParticle(Player owner, int variant, int lifetime)
        {
            Offset = (-Vector2.UnitY * (40f + Main.rand.NextFloat(34f))).RotatedByRandom(MathHelper.PiOver4 * 0.04f);
            Owner = owner;
            Position = Owner.Center;
            Scale = 1f;
            Color = Color.White;
            Velocity = Vector2.Zero;
            Rotation = 0f;
            RotationOffset = Main.rand.NextFloat(MathHelper.PiOver4 * 0f) * (Main.rand.NextBool() ? -1f : 1f);
            Lifetime = lifetime;
            AnimationTime = (int)(lifetime * 0.44f);

            switch (variant)
            {
                case 0: //Back leg
                    Frame = new Rectangle(46, 4, 10, 14);
                    DestinationOffset = new Vector2(7f, 16f); //1f?
                    TimeOffset = 0;
                    break;
                case 1: //Front leg
                    Frame = new Rectangle(30, 4, 12, 14);
                    DestinationOffset = new Vector2(-4f, 16f);
                    TimeOffset = 0;
                    break;
                case 2: //Bottom torso
                    Frame = new Rectangle(4, 30, 22, 12);
                    DestinationOffset = new Vector2(1f, 5f);
                    TimeOffset = 0.15f;
                    break;
                case 3: //Torso
                    Frame = new Rectangle(30, 24, 30, 16);
                    DestinationOffset = new Vector2(1f, -3f);
                    TimeOffset = 0.35f;
                    break;
                case 4: //Helmet
                    Frame = new Rectangle(2, 2, 22, 24);
                    DestinationOffset = new Vector2(-1f, -15f);
                    TimeOffset = 0.43f;
                    break;
                case 5: //Fusion Cannon
                    Frame = new Rectangle(14, 46, 38, 18);
                    DestinationOffset = new Vector2(-2f, -2f);
                    TimeOffset = 0.6f;
                    break;
            }

            Origin = Frame.Size() / 2;

            //Fusion Cannon rotates around the shoulder
            if (variant == 5)
                Origin.X -= 12f;

        }

        public override void Update()
        {
            Rotation = MathHelper.Lerp(RotationOffset, 0f, (float)Math.Pow(LifetimeCompletionAdjusted, 0.8f));

            //The gun
            if (TimeOffset == 0.6f)
                Rotation = (Owner.MouseWorld() - Owner.Center).ToRotation() + (Owner.direction < 0 ? MathHelper.Pi : 0f);


            if (Owner.dead || !Owner.active)
                Kill();
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            if (Owner.dead || !Owner.active)
                return;

            Texture2D baseTex = ParticleTexture;
            Vector2 center = new Vector2((int)Owner.MountedCenter.X, (int)(Owner.MountedCenter.Y + Owner.gfxOffY));
            Vector2 currentOffset = Vector2.Lerp(Offset, Vector2.Zero, (float)Math.Pow(LifetimeCompletionAdjusted, 0.8f));
            //Vector2 currentOffset = Vector2.Lerp(Offset, Vector2.Zero, 1f);
            Color lightColor = Lighting.GetColor(Owner.Center.ToTileCoordinates());

            Vector2 realDestinationOffset = new Vector2(DestinationOffset.X * Owner.direction, DestinationOffset.Y * Owner.gravDir);
            Vector2 realCurrentOffset = new Vector2(currentOffset.X * Owner.direction, currentOffset.Y * Owner.gravDir);


            SpriteEffects spriteEffect = SpriteEffects.None;
            Vector2 origin = Origin;

            if (Owner.direction < 0)
            {
                spriteEffect = SpriteEffects.FlipHorizontally;
                origin.X = Frame.Width - Origin.X;
            }


            float opacity = Math.Clamp(LifetimeCompletionAdjusted * 4f, 0f, 1f);

            spriteBatch.Draw(baseTex, center + realDestinationOffset + realCurrentOffset - basePosition, Frame, lightColor * opacity, Rotation, origin, Scale, spriteEffect, 0);
        }
    }

    public class WulfrumHatParticle : Particle
    {
        public override bool SetLifetime => true;
        public override string Texture => AssetDirectory.WulfrumItems + "WulfrumHatParticle";
        public override bool UseCustomDraw => true;

        public int Direction;

        public WulfrumHatParticle(Player owner, Vector2 velocity, int lifetime)
        {
            Position = owner.Center - Vector2.UnitY * 20f;
            Direction = owner.direction;
            Scale = 1f;
            Color = Color.White;
            Velocity = velocity;
            Rotation = 0f;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Rotation += 0.05f * Math.Sign(Velocity.X);

            Velocity *= 0.95f;
            Velocity.Y += 0.22f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D baseTex = ParticleTexture;
            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());

            SpriteEffects spriteEffect = SpriteEffects.None;
            if (Direction < 0)
            {
                spriteEffect = SpriteEffects.FlipHorizontally;
            }

            float opacity = Math.Clamp(1 - (float)Math.Pow(LifetimeCompletion, 3f), 0f, 1f);
            spriteBatch.Draw(baseTex, Position - basePosition, null, lightColor * opacity, Rotation, baseTex.Size() / 2f, Scale, spriteEffect, 0);
        }
    }
    #endregion
}
