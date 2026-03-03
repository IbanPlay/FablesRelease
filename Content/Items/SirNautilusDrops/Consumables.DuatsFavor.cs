namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class DuatsFavor : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Duat's Favor");
            Tooltip.SetDefault("Unlocks an ability toggle to the left of the inventory\n" +
                "The toggle lets you choose the appearance of gravestones dropped on death\n" +
                "You can also disable graves with the toggle\n" +
                "Biome graves don't count towards the formation of a graveyard");

            //Old tooltip to be restored when more grave type unlocks exist
            /*
            "When enabled, gravestones dropped on death will take on a desertic appearance\n" +
            "Sandstone graves don't count towards the formation of a graveyard");*/
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 22;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item4;
            Item.useAnimation = Item.useTime = 30;
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && player.ItemTimeIsZero)
            {
                //unlockedBiomeTorches = true;
                //UsingBiomeTorches = true;
                CustomGravesPlayer mp = player.GetModPlayer<CustomGravesPlayer>();


                mp.Unlock(CustomGravesPlayer.CustomGraveType.None);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Ice);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Jungle);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Hell);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Crimson);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Corrupt);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Hallow);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Sky);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Beach);
                mp.Unlock(CustomGravesPlayer.CustomGraveType.Mushroom);
                //TODO : Mushroom graves unlock item for crabulon

                return mp.Unlock(CustomGravesPlayer.CustomGraveType.Sandstone);
            }

            return false;
        }
    }
}
