namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class DeadStormlionLarvaItem : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourge + Name;

        public virtual int SpawnedNPC => ModContent.NPCType<DeadStormlionLarva>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dead Stormlion Larva");
            Tooltip.SetDefault("'A treat for any desert predator'");
            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 9999;
            Item.width = 28;
            Item.height = 28;
            Item.rare = ItemRarityID.White;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.UseSound = SoundID.Item95;
            Item.bait = 10;
            Item.noUseGraphic = true;
            Item.useTurn = true;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC grubby = NPC.NewNPCDirect(player.GetSource_ItemUse(Item), (int)player.Center.X, (int)player.Center.Y, SpawnedNPC);
                grubby.velocity = player.velocity * 0.5f + Vector2.UnitX * player.direction * 4f - Vector2.UnitY * 2f;
            }

            return true;
        }
    }

    public class StormlionLarvaItem : DeadStormlionLarvaItem
    {
        public override string Texture => AssetDirectory.DesertScourge + Name;
        public override int SpawnedNPC => ModContent.NPCType<StormlionLarva>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Larva");
            Tooltip.SetDefault("'It flails about above the dunes... Perhaps they sense a threat below?'");
            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.bait += 10;
        }
    }

    [ReplacingCalamity("DesertMedallion")]
    public class BucketOfLarvae : DeadStormlionLarvaItem
    {
        public override int SpawnedNPC => (Main.rand.NextBool(50) || Main.getGoodWorld ) ? ModContent.NPCType<Fibsh>() : ModContent.NPCType<DeadStormlionLarva>();

        public override string Texture => AssetDirectory.DesertScourge + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Grub Bucket");
            Tooltip.SetDefault("Contains a seemingly infinite amount of wormfeed");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ItemRarityID.Blue;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.bait = 0;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.EmptyBucket).
                AddIngredient<DeadStormlionLarvaItem>(5).
                Register();
        }
    }
}
