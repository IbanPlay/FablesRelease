using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class DustyBricksLichen : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public int DustyBricksType;

        public override void SetStaticDefaults()
        {
            DustyBricksType = ModContent.TileType<DustyBricks>();
            DustType = DustID.Clay;
            HitSound = SoundID.Grass;
            TileID.Sets.NeedsGrassFraming[Type] = true;
            TileID.Sets.NeedsGrassFramingDirt[Type] = DustyBricksType;
            Main.tileBrick[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            TileID.Sets.DoesntPlaceWithTileReplacement[Type] = true;
            FablesSets.PlacesLikeGrassSeeds[Type] = true;

            AddMapEntry(new Color(128, 87, 77));
            FablesPlayer.TryTransformWhenHitEvent.Add(Type, TryTransformWhenHit);
            FablesPlayer.ScrapeTileEvent.Add(Type, ScrapeLichenOff);
            FablesPlayer.CanPlaceOverTileEvent.Add(Type, PutLichenOnBricks);
        }

        public bool TryTransformWhenHit(Player player, HitTile hitCounter, int damage, int x, int y, int pickPower, int bufferIndex, Tile tileTarget)
        {
            return hitCounter.AddDamage(bufferIndex, damage, updateAmount: false) >= 100;
        }

        public void ScrapeLichenOff(Player player, int x, int y)
        {
            WorldGen.KillTile(x, y, true);
            if (Main.tile[x, y].TileType != DustyBricksType)
                return;

            player.ApplyItemTime(player.inventory[player.selectedItem]);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);

            int number = Item.NewItem(new EntitySource_ItemUse(player, player.HeldItem), x * 16, y * 16, 16, 16, ModContent.ItemType<DustyBricksLichenItem>());
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, number, 1f);
        }

        public bool PutLichenOnBricks(int x, int y)
        {
            return Main.tile[x, y].HasTile && Main.tile[x, y].TileType == DustyBricksType;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            //Turns into non lichen bricks
            if (fail)
                Main.tile[i, j].TileType = (ushort)ModContent.TileType<DustyBricks>();
        }

        public override void PostTileFrame(int i, int j, int up, int down, int left, int right, int upLeft, int upRight, int downLeft, int downRight)
        {
            if (j % 2 == 0)
            {
                Tile t = Main.tile[i, j];
                t.TileFrameX += 288;
            }
        }

        public override bool CanPlace(int i, int j)
        {
            return Main.tile[i, j].HasTile && Main.tile[i, j].TileType == DustyBricksType;
        }
    }

    public class DustyBricksLichenItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void Load()
        {
            FablesNPC.ModifyShopEvent += SoldFromDyrad;
        }

        private void SoldFromDyrad(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Dryad)
            {
                shop.Add(Type, Condition.InGraveyard);
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Drab Lichen");
            Tooltip.SetDefault("Can be placed on dusty bricks");
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<DustyBricksLichen>());
            Item.buyPrice(copper: 30);
        }
    }
}