namespace CalamityFables.Content.Items.Wulfrum
{
    [AutoloadEquip(EquipType.Head)]
    [ReplacingCalamity("AbandonedWulfrumHelmet")]
    public class AbandonedWulfrumHelmet : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void Load()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, Texture + "_HeadSet", EquipType.Head, name: "WulfrumOldSetHead");
                EquipLoader.AddEquipTexture(Mod, Texture + "_Body", EquipType.Body, this);
                EquipLoader.AddEquipTexture(Mod, Texture + "_Legs", EquipType.Legs, this);
            }
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Abandoned Wulfrum Helmet");
            Tooltip.SetDefault("A worn and rusty helmet ressembling older models of wulfrum armor\n" +
                "Transforms the holder into a wulfrum robot\n" +
                "Can also be worn in the helmet slot as a regular helm\n" +
                "[c/83B87E:This rather flimsy armor was commonly worn by scavengers and looters]\n" +
                "[c/83B87E:Its versatility and common nature led it to be used as currency in trades]");

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlotHead = EquipLoader.GetEquipSlot(Mod, "WulfrumOldSetHead", EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlotHead] = false;

            int equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            ArmorIDs.Body.Sets.HidesTopSkin[equipSlotBody] = true;
            ArmorIDs.Body.Sets.HidesArms[equipSlotBody] = true;

            int equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlotLegs] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
            Item.hasVanityEffects = true;
        }

        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<WulfrumTransformationPlayer>().transformationActive = true;
            player.GetModPlayer<WulfrumTransformationPlayer>().vanityEquipped = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
            {
                player.GetModPlayer<WulfrumTransformationPlayer>().transformationActive = true;
                player.GetModPlayer<WulfrumTransformationPlayer>().vanityEquipped = true;
            }
        }
    }

    public class WulfrumTransformationPlayer : ModPlayer
    {
        public bool vanityEquipped = false;
        public bool transformationActive = false;
        public bool forceHelmetOn = false;

        public override void ResetEffects()
        {
            vanityEquipped = false;
            transformationActive = false;
            forceHelmetOn = false;
        }

        public override void PostUpdateEquips()
        {
            if (transformationActive)
                Player.SetCustomHurtSound(SoundID.NPCHit4, 10);
        }

        public override void FrameEffects()
        {
            if (forceHelmetOn || transformationActive)
            {
                Player.head = EquipLoader.GetEquipSlot(Mod, "WulfrumOldSetHead", EquipType.Head);
                Player.face = -1;
            }

            if (transformationActive)
            {
                Player.legs = EquipLoader.GetEquipSlot(Mod, "AbandonedWulfrumHelmet", EquipType.Legs);
                Player.body = EquipLoader.GetEquipSlot(Mod, "AbandonedWulfrumHelmet", EquipType.Body);

                Player.HideAccessories();
            }
        }
    }
}
