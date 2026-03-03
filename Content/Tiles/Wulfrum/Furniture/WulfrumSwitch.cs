using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.Wulfrum.Furniture
{
    public class WulfrumSwitchItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumFurnitureItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Switch");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumSwitch>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 0, 50);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumHullItem>(2).
                AddTile<WulfrumWorkshop>().
                Register();
        }
    }

    public class WulfrumSwitch : ModTile, ICustomPaintable, ICustomTileDrawOffset
    {
        public bool PaintColorHasCustomTexture(int paintColor) => CommonColors.WulfrumCustomPaintjob(paintColor);
        public string PaintedTexturePath(int paintColor) => CommonColors.WulfrumPaintName(paintColor, Name);

        public override string Texture => AssetDirectory.WulfrumFurniture + Name;
        public override void Load()
        {
            On_Wiring.HitSwitch += HitWulfrumSwitch;
        }

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Tungsten;
            HitSound = SoundID.Tink;

            RegisterItemDrop(ModContent.ItemType<WulfrumSwitchItem>(), -1);

            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            //Top facing
            TileObjectData.newTile.CopyFrom(TileObjectData.StyleSwitch);;

            //Side facing
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleSwitch);
            TileObjectData.newAlternate.AnchorLeft = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, 1, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[7] { 124, 561, 574, 575, 576, 577, 578 }; //Beams
            TileObjectData.newAlternate.DrawXOffset = -2;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(1);
            //Other side facing
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleSwitch);
            TileObjectData.newAlternate.AnchorRight = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.Tree | AnchorType.AlternateTile, 1, 0);
            TileObjectData.newAlternate.AnchorAlternateTiles = new int[7] { 124, 561, 574, 575, 576, 577, 578 };
            TileObjectData.newAlternate.DrawXOffset = 2;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(2);

            //Wall facing
            TileObjectData.newAlternate.CopyFrom(TileObjectData.StyleSwitch);
            TileObjectData.newAlternate.AnchorWall = true;
            TileObjectData.newAlternate.DrawYOffset = 0;
            TileObjectData.addAlternate(3);

            //Tôp facing
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidSide, 1, 0);
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);

            AdjTiles = new int[] { TileID.Switches };
            AddMapEntry(CommonColors.WulfrumLeatherRed);
            FablesSets.CustomPaintedSprites[Type] = true;
            FablesSets.CustomDrawOffset[Type] = true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<WulfrumSwitchItem>();
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);

        public override bool RightClick(int i, int j)
        {
            Wiring.HitSwitch(i, j);
            NetMessage.SendData(MessageID.HitSwitch, -1, -1, null, i, j);
            return true;
        }

        //Necessary to do it like that so it works even from a netmessage or other thinjgs
        private void HitWulfrumSwitch(On_Wiring.orig_HitSwitch orig, int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileType == Type)
            {
                tile.TileFrameY = (short)((tile.TileFrameY + 18) % 36);
                SoundEngine.PlaySound(SoundID.MenuTick, new Vector2(i, j) * 16f);
                Wiring.TripWire(i, j, 1, 1);
            }
            else
                orig(i, j);
        }

        public void DrawOffset(TileDrawInfo drawData, int i, int j, ref Vector2 drawPosition)
        {
            switch (drawData.tileFrameX / 18)
            {
                case 1:
                    drawPosition.X += -2f;
                    break;
                case 2:
                    drawPosition.X += 2f;
                    break;
            }
            if (drawData.tileFrameX != 0)
                drawPosition.Y -= 2f;
        }
    }
}
