using Terraria.Localization;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    [AutoloadEquip(EquipType.Head)]
    public class SeaRiderHelmet : ModItem
    {
        public override void Load()
        {
            On_Player.QuickMount += SummonSteed;
            FablesProjectile.ModifyHitNPCEvent += IncreaseSpearDamageWhenMounted;

            setBonusTitle = Mod.GetLocalization("Extras.ArmorSetBonus.Sailfish.Title");
            setBonusTooltip1 = Mod.GetLocalization("Extras.ArmorSetBonus.Sailfish.Tooltip1");
            setBonusTooltip2 = Mod.GetLocalization("Extras.ArmorSetBonus.Sailfish.Tooltip2").WithFormatArgs(MOUNTED_MELEE_CRIT_BOOST);
            setBonusTooltip3 = Mod.GetLocalization("Extras.ArmorSetBonus.Sailfish.Tooltip3").WithFormatArgs((MOUNTED_DAMAGE_BOOST_FOR_SPEARS - 1).ToString("P0"));
        }

        public override void Unload()
        {
            On_Player.QuickMount -= SummonSteed;
            FablesProjectile.ModifyHitNPCEvent -= IncreaseSpearDamageWhenMounted;
        }

        public override string Texture => AssetDirectory.SirNautilusDrops + Name;
        public static float MOUNTED_DAMAGE_BOOST_FOR_SPEARS = 1.1f;
        public static float MOUNTED_MELEE_CRIT_BOOST = 5;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Sailfish Helmet");
            Tooltip.SetDefault("8% increased melee critical strike chance\n" +
                "'A slightly dusty helmet, commonly issued for the Sea Kingdom guards'");

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.value = Item.sellPrice(silver: 20);
            Item.rare = ItemRarityID.Blue;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) => head.type == Type && body.type == ModContent.ItemType<SeaRiderTunic>() && legs.type == ModContent.ItemType<SeaRiderGreaves>();
        public static bool HasArmorSet(Player player) => player.armor[0].type == ModContent.ItemType<SeaRiderHelmet>() && player.armor[1].type == ModContent.ItemType<SeaRiderTunic>() && player.armor[2].type == ModContent.ItemType<SeaRiderGreaves>();
        public bool IsPartOfSet(Item item) => item.type == ModContent.ItemType<SeaRiderHelmet>() ||
                item.type == ModContent.ItemType<SeaRiderTunic>() ||
                item.type == ModContent.ItemType<SeaRiderGreaves>();

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Armored Signathion";
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += 8f;
        }

        private void IncreaseSpearDamageWhenMounted(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.LocalPlayer;
            if (player.mount.Active && player.mount.Type == ModContent.MountType<ArmoredSignathionMount>())
            {
                if (player.HeldItem == null || player.HeldItem.IsAir)
                    return;

                if (player.heldProj != projectile.whoAmI)
                    return;

                //Spear detection
                if (ItemID.Sets.Spears[player.HeldItem.type] ||
                    ProjectileID.Sets.NoMeleeSpeedVelocityScaling[projectile.type] ||
                    projectile.aiStyle == ProjAIStyleID.Spear || projectile.aiStyle == ProjAIStyleID.NorthPoleSpear)
                {
                    modifiers.SourceDamage *= MOUNTED_DAMAGE_BOOST_FOR_SPEARS;

                    float kineticEnergy = Utils.GetLerpValue(3f, 8f, Math.Abs(player.velocity.X), true);
                    if (kineticEnergy > 0)
                        modifiers.Knockback += kineticEnergy * 4f;
                }
            }
        }

        private void SummonSteed(On_Player.orig_QuickMount orig, Player self)
        {
            //Spoof the mount thing
            if (!self.mount.Active && HasArmorSet(self) && self.miscEquips[Player.miscSlotMount].IsAir)
            {
                if (self.frozen || self.tongued || self.webbed || self.stoned || self.gravDir == -1f || self.dead || self.noItems)
                    return;

                if (self.QuickMinecartSnapPublic())
                    return;

                int cnidrionMountType = ModContent.MountType<ArmoredSignathionMount>();

                if (self.mount.CanMount(cnidrionMountType, self))
                    self.mount.SetMount(cnidrionMountType, self);
            }

            else
                orig(self);
        }

        public static LocalizedText setBonusTitle;
        public static LocalizedText setBonusTooltip1;
        public static LocalizedText setBonusTooltip2;
        public static LocalizedText setBonusTooltip3;
        public static void ModifySetTooltips(ModItem item, List<TooltipLine> tooltips)
        {
            if (HasArmorSet(Main.LocalPlayer))
            {
                int setBonusIndex = tooltips.FindIndex(x => x.Name == "SetBonus" && x.Mod == "Terraria");

                if (setBonusIndex != -1)
                {
                    TooltipLine setBonus1 = new TooltipLine(item.Mod, "CalamityFables:SetBonus1", setBonusTitle.Value);
                    setBonus1.OverrideColor = Color.Lerp(new Color(255, 243, 161), new Color(137, 162, 255), 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f));
                    tooltips[setBonusIndex] = setBonus1;

                    TooltipLine setBonus2 = new TooltipLine(item.Mod, "CalamityFables:SetBonus2", setBonusTooltip1.Value);
                    setBonus2.OverrideColor = new Color(69, 211, 246);
                    tooltips.Insert(setBonusIndex + 1, setBonus2);

                    TooltipLine setBonus3 = new TooltipLine(item.Mod, "CalamityFables:SetBonus3", setBonusTooltip2.Value);
                    setBonus3.OverrideColor = new Color(69, 211, 246);
                    tooltips.Insert(setBonusIndex + 2, setBonus3);

                    TooltipLine setBonus4 = new TooltipLine(item.Mod, "CalamityFables:SetBonus4", setBonusTooltip3.Value);
                    setBonus4.OverrideColor = new Color(69, 211, 246);
                    tooltips.Insert(setBonusIndex + 3, setBonus4);
                }
            }
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) => ModifySetTooltips(this, tooltips);

    }

    [AutoloadEquip(EquipType.Body)]
    public class SeaRiderTunic : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Sailfish Tunic");
            Tooltip.SetDefault("6% increased melee damage\n" +
                "'Tunic adorned with the coat of arms of a long-disbanded regiment of seahorse riders'");

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
            Item.defense = 6;
            Item.value = Item.sellPrice(silver: 20);
            Item.rare = ItemRarityID.Blue;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => SeaRiderHelmet.ModifySetTooltips(this, tooltips);
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.06f;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class SeaRiderGreaves : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Sailfish Greaves");
            Tooltip.SetDefault("5% increased movement speed\n" +
                "Grants the ability to swim\n" +
                "'The boots have webbed feet...'");

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.value = Item.sellPrice(silver: 20);
            Item.rare = ItemRarityID.Blue;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips) => SeaRiderHelmet.ModifySetTooltips(this, tooltips);
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.05f;
            player.accFlipper = true;
        }
    }

    public class ArmoredSignathionBuff : ModBuff
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            DisplayName.SetDefault("Royal Steed");
            Description.SetDefault("This phantom still wears the distinct regalia of the Sea Kingdom\n" +
                "Increased damage from spears when charging");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<ArmoredSignathionMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }

    public class ArmoredSignathionMount : GhostlySeasaddleMount
    {
        internal static readonly SoundStyle _stepSound = new SoundStyle(SoundDirectory.Nautilus + "SignarmoredStep", 3) with { Pitch = 0.1f, MaxInstances = 0 };

        internal static readonly SoundStyle _landSound = new SoundStyle(SoundDirectory.Nautilus + "SignarmoredLand") with { Pitch = 0.1f };

        public override SoundStyle StepSound => _stepSound with { MaxInstances = 0 };
        public override SoundStyle LandSound => _landSound;
        //public override SoundStyle JumpSound => SirNautilus.SignathionWaterRifle with { Pitch = 0.2f };


        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            MountData.buff = ModContent.BuffType<ArmoredSignathionBuff>();
            MountData.acceleration = 0.13f; 
            MountData.runSpeed = 5.5f;
        }

        public override void UpdateEffects(Player player)
        {
            if (!SeaRiderHelmet.HasArmorSet(player))
            {
                player.mount.Dismount(player);
                return;
            }

            player.statDefense += 3;
            player.moveSpeed += 0.05f;
            player.GetCritChance(DamageClass.Melee) += SeaRiderHelmet.MOUNTED_MELEE_CRIT_BOOST;
            base.UpdateEffects(player);
        }
    }
}
