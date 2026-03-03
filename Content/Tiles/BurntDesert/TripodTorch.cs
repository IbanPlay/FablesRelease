using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Items.Wulfrum;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class TripodTorch : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tripod Torch");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<TripodTorchTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Torch).
                AddIngredient(ItemID.PalmWood, 3).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }

    public class TripodTorchSpectral : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spectral Tripod Torch");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<TripodTorchTile>());
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 0, 1, 0);
            Item.rare = ItemRarityID.White;
            Item.placeStyle = 1;
        }
    }

    public class TripodTorchTile : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public static Asset<Texture2D> FlameTex;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(0, 2);
            TileObjectData.newTile.CoordinateHeights = new int[] { 24, 24, 24 };
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop , TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.WaterDeath = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = -6;
            TileObjectData.newTile.StyleLineSkip = 6;
            TileObjectData.newTile.StyleWrapLimit = 2;

            TileObjectData.addTile(Type);

            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.BorealWood;

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Torch");
            AddMapEntry(new Color(117, 92, 69), name);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameY < 78)
            {
                if (Main.tile[i, j].TileFrameX == 0)
                {
                    r = 1f;
                    g = 0.95f;
                    b = 0.65f;

                    if (SirNautilus.SignathionVisualInfluence < 1)
                    {
                        Color color = CommonColors.DesertMirageBlue;
                        r = MathHelper.Lerp(r, color.R / 255f, SirNautilus.TorchLightColorShift);
                        g = MathHelper.Lerp(g, color.G / 255f, SirNautilus.TorchLightColorShift);
                        b = MathHelper.Lerp(b, color.B / 255f, SirNautilus.TorchLightColorShift);
                    }
                }
                else
                {
                    Color color = CommonColors.DesertMirageBlue;
                    r = color.R / 255f;
                    g = color.G / 255f;
                    b = color.B / 255f;
                }
            }
        }

        public override void HitWire(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            int topY = j - tile.TileFrameY / 26 % 3;

            short frameAdjustment = (short)(tile.TileFrameY >= 78 ? -78 : 78);

            Main.tile[i, topY].TileFrameY += frameAdjustment;
            Main.tile[i, topY + 1].TileFrameY += frameAdjustment;
            Main.tile[i, topY + 2].TileFrameY += frameAdjustment;

            Wiring.SkipWire(i, topY);
            Wiring.SkipWire(i, topY + 1);
            Wiring.SkipWire(i, topY + 2);

            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, i, topY + 1, 3, TileChangeType.None);
        }


        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.gamePaused || !Main.instance.IsActive || Lighting.UpdateEveryFrame && !Main.rand.NextBool(4))
                return;
            Tile tile = Main.tile[i, j];

            if (!TileDrawing.IsVisible(tile))
                return;

            //Only thet op of lamps makes light
            if (tile.TileFrameY != 0 || !Main.rand.NextBool(40))
                return;

            bool ghostly = tile.TileFrameX != 0 || SirNautilus.SignathionVisualInfluence == 0;

            if (!ghostly)
            {
                var dust = Dust.NewDustDirect(new Vector2(i * 16 + 4, j * 16 + 2), 4, 4, DustID.Torch, 0f, 0f, 100, default, 1f);
                dust.noGravity = !Main.rand.NextBool(3);
                dust.velocity *= 0.3f;
                dust.velocity.Y = dust.velocity.Y - 1.5f;
            }
            else
            {
                Dust.NewDustPerfect(Main.rand.NextVector2FromRectangle(new Rectangle(i * 16, j * 16, 16, 16)), 
                    DustType<SpectralWaterDustEmbers>(), -Vector2.UnitY, 15, Color.White, Main.rand.NextFloat(0.6f, 1.3f));

            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = -6;

            if (tileFrameX == 0 && SirNautilus.SignathionVisualInfluence == 0)
                tileFrameX = 18;
        }
        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter > 6)
            {
                frameCounter = 0;
                frame = ++frame % 5;
            }
        }
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            var tile = Main.tile[i, j];

            //On fire
            if (tile.TileFrameY < 78)
            {
                frameYOffset = Main.tileFrame[type] * 78;
            }
            //Turned off
            else
                frameYOffset = 78 * 4;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            if (!TileDrawing.IsVisible(tile))
                return;

            int width = 16;
            int offsetY = 0;
            int height = 16;
            short frameX = tile.TileFrameX;
            short frameY = tile.TileFrameY;

            int frameXOffset = 0;
            int frameYOffset = 0;

            TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height, ref frameX, ref frameY);
            TileLoader.SetAnimationFrame(Type, i, j, ref frameXOffset, ref frameYOffset);
            frameY += (short)frameYOffset;

            ulong randSeed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (long)(uint)i); // Don't remove any casts.

            FlameTex ??= Request<Texture2D>(Texture + "Flame");
            Vector2 drawOffset = FablesUtils.TileDrawOffset();

            // We can support different flames for different styles here: int style = Main.tile[j, i].frameY / 54;
            for (int c = 0; c < 1; c++)
            {
                float shakeX = Utils.RandomInt(ref randSeed, -10, 11) * 0.15f;
                float shakeY = Utils.RandomInt(ref randSeed, -10, 1) * 0.15f;

                spriteBatch.Draw(FlameTex.Value, new Vector2(i * 16 + shakeX, j * 16 + offsetY + shakeY) - drawOffset
                    , new Rectangle(frameX, frameY, width, height), new Color(100, 100, 100, 0), 0f, default, 1f, 0, 0f);
            }
        }

        public override void MouseOver(int i, int j)
        {
            if (Main.tile[i, j].TileFrameX == 0)
                SpectralWater.MouseOverIcon(i, j);
        }
    }
}
