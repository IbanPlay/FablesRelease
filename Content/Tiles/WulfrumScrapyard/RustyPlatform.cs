using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Terraria.DataStructures;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class RustyHandrailPlatform : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + "RustyPlatform";

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Iron;
            HitSound = SoundID.Tink;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileTable[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.Platforms[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(19, 0));
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor); 
            AddMapEntry(new Color(124, 122, 118));
            TileID.Sets.DisableSmartCursor[Type] = true;
            AdjTiles = new int[] { TileID.Platforms };
        }

        public override void PostSetDefaults()
        {
            Main.tileNoSunLight[Type] = false;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.tile[i, j].IsTileInvisible)
                return;
            ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.Background, false);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y);
            Texture2D drawTexture = TextureAssets.Tile[Type].Value;
            if (tile.TileColor != 0)
            {
                Texture2D paintedTexture = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, tile.TileColor);
                drawTexture = paintedTexture ?? drawTexture;
            }
            Color drawColor = Lighting.GetColor(i, j);
            if (tile.IsTileFullbright)
                drawColor = Color.White;

            drawOffset.Y -= 16;
            spriteBatch.Draw(drawTexture, drawOffset, new Rectangle(tile.TileFrameX, 18, 16, 16), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            bool oneThickLeft = tile.TileFrameX <= 18 || tile.TileFrameX == 74 || tile.TileFrameX == 126 || (tile.TileFrameX >= 216 && tile.TileFrameX <= 270) || tile.TileFrameX >= 450;
            bool oneThickRight = tile.TileFrameX == 0 || (tile.TileFrameX >= 36 && tile.TileFrameX <= 54) || tile.TileFrameX == 108 || (tile.TileFrameX >= 216 && tile.TileFrameX <= 252) || tile.TileFrameX == 288 || tile.TileFrameX >= 450;

            if (oneThickLeft && i > 0)
            {
                Tile tileLeft = Main.tile[i - 1, j];
                bool validRailing = tileLeft.HasTile && tileLeft.TileType == Type;
                if (validRailing && ((tileLeft.TileFrameX >= 144 && tileLeft.TileFrameX < 216) || (tileLeft.TileFrameX >= 306 && tileLeft.TileFrameX < 450)))
                    validRailing = false;

                if (!validRailing)
                {
                    Vector2 drawPos = drawOffset;
                    drawPos.X -= 2;
                    spriteBatch.Draw(drawTexture, drawPos, new Rectangle(36, 18, 4, 16), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
            }

            if (oneThickRight && i < Main.maxTilesX - 1)
            {
                Tile tileRight = Main.tile[i + 1, j];
                bool validRailing = tileRight.HasTile && tileRight.TileType == Type;
                if (validRailing && ((tileRight.TileFrameX >= 144 && tileRight.TileFrameX < 216) || (tileRight.TileFrameX >= 306 && tileRight.TileFrameX < 450)))
                    validRailing = false;

                if (!validRailing)
                {
                    Vector2 drawPos = drawOffset;
                    drawPos.X += 12;
                    spriteBatch.Draw(drawTexture, drawPos, new Rectangle(30, 18, 4, 16), drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
            }
        }
    }

    public class RustyPlatform : ModTile
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            MinPick = 30;
            DustType = DustID.Iron;
            HitSound = SoundID.Tink;

            Main.tileLighted[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileSolidTop[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileTable[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.Platforms[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(19, 0));
            TileObjectData.addTile(Type);

            AddToArray(ref TileID.Sets.RoomNeeds.CountsAsDoor);
            AddMapEntry(new Color(124, 122, 118));
            TileID.Sets.DisableSmartCursor[Type] = true;
            AdjTiles = new int[] { TileID.Platforms };
        }

        public override void PostSetDefaults()
        {
            Main.tileNoSunLight[Type] = false;
        }
    }

    public class RustyPlatformItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rusty Platform");
            Item.ResearchUnlockCount = 200;

            ItemID.Sets.ShimmerTransformToItem[Type] = ItemType<RustyHandrailPlatformItem>();
            ItemID.Sets.ShimmerTransformToItem[ItemType<RustyHandrailPlatformItem>()] = Type;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<RustyPlatform>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(2).
                AddIngredient<RustySheetsItem>(1).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class RustyHandrailPlatformItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rusty Handrail Platform");
            Item.ResearchUnlockCount = 200;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<RustyHandrailPlatform>());
        }

        public override void AddRecipes()
        {
            CreateRecipe(2).
                AddIngredient<RustySheetsItem>(1).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}
