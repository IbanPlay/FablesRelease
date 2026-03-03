using CalamityFables.Content.NPCs.Wulfrum;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Debug
{
    public abstract class WulfrumSpawner : ModItem
    {
        public abstract int TileToPlace { get; }
        public abstract string ThingName { get; }

        public override string Texture => AssetDirectory.Debug + "WulfrumTile";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(ThingName + " spawner");
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = TileToPlace;
        }
    }

    public abstract class WulfrumSpawnerTile : ModTile
    {
        public abstract string PreviewTexturePath { get; }
        public abstract int NPCToSpawn { get; }
        public override string Texture => AssetDirectory.Debug + "WulfrumTile";

        public static Asset<Texture2D> previewTex;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.CoordinateHeights = new[] { 16 };

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Spawner");
            AddMapEntry(CommonColors.WulfrumBlue, name);
            TileID.Sets.FramesOnKillWall[Type] = true;
            DustType = 8;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void HitWire(int i, int j)
        {
            NPC.NewNPC(new EntitySource_Wiring(i, j), i * 16 + 8, j * 16 + 8, NPCToSpawn, Target: Main.myPlayer);
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
        }
        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            previewTex = ModContent.Request<Texture2D>(PreviewTexturePath);

            Texture2D preview = previewTex.Value;

            // This is lighting-mode specific, always include this if you draw tiles manually
            Vector2 offScreen = new Vector2(Main.offScreenRange);
            if (Main.drawToScreen)
            {
                offScreen = Vector2.Zero;
            }

            // Take the tile, check if it actually exists
            Point p = new Point(i, j);
            Tile tile = Main.tile[p.X, p.Y];
            if (tile == null || !tile.HasTile)
            {
                return;
            }

            Vector2 origin = preview.Size() / 2f;
            Vector2 worldPos = p.ToWorldCoordinates(8f, 8f);

            Color color = Lighting.GetColor(p.X, p.Y).MultiplyRGB(CommonColors.WulfrumBlue) * (0.6f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.4f));
            color.A = 0;

            // Some math magic to make it smoothly move up and down over time
            const float TwoPi = (float)Math.PI * 2f;
            float offset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * TwoPi / 5f);
            Vector2 drawPos = worldPos + offScreen - Main.screenPosition + new Vector2(0f, -40f) + new Vector2(0f, offset * 4f);

            // Draw the main texture
            spriteBatch.Draw(preview, drawPos, null, color, 0f, origin, 1f, 0f, 0f);

        }
    }

    public class RoverSpawner : WulfrumSpawner
    {
        public override int TileToPlace => ModContent.TileType<RoverSpawnerTile>();
        public override string ThingName => "Rover";
    }

    public class RoverSpawnerTile : WulfrumSpawnerTile
    {
        public override string PreviewTexturePath => AssetDirectory.WulfrumNPC + "WulfrumRover_Preview";
        public override int NPCToSpawn => ModContent.NPCType<WulfrumRover>();
    }


    public class RollerSpawner : WulfrumSpawner
    {
        public override int TileToPlace => ModContent.TileType<RollerSpawnerTile>();
        public override string ThingName => "Roller";
    }

    public class RollerSpawnerTile : WulfrumSpawnerTile
    {
        public override string PreviewTexturePath => AssetDirectory.WulfrumNPC + "WulfrumRoller_Preview";
        public override int NPCToSpawn => ModContent.NPCType<WulfrumRoller>();
    }


    public class MagnetizerSpawner : WulfrumSpawner
    {
        public override int TileToPlace => ModContent.TileType<MagnetizerSpawnerTile>();
        public override string ThingName => "Magnetizer";
    }

    public class MagnetizerSpawnerTile : WulfrumSpawnerTile
    {
        public override string PreviewTexturePath => AssetDirectory.WulfrumNPC + "WulfrumMagnetizer_Preview";
        public override int NPCToSpawn => ModContent.NPCType<WulfrumMagnetizer>();
    }

    public class MortarSpawner : WulfrumSpawner
    {
        public override int TileToPlace => ModContent.TileType<MortarSpawnerTile>();
        public override string ThingName => "Mortar";
    }

    public class MortarSpawnerTile : WulfrumSpawnerTile
    {
        public override string PreviewTexturePath => AssetDirectory.WulfrumNPC + "WulfrumMortar_Preview";
        public override int NPCToSpawn => ModContent.NPCType<WulfrumMortar>();
    }

    public class GrapplerSpawner : WulfrumSpawner
    {
        public override int TileToPlace => ModContent.TileType<GrapplerSpawnerTile>();
        public override string ThingName => "Grappler";
    }

    public class GrapplerSpawnerTile : WulfrumSpawnerTile
    {
        public override string PreviewTexturePath => AssetDirectory.WulfrumNPC + "WulfrumGrappler_Preview";
        public override int NPCToSpawn => ModContent.NPCType<WulfrumGrappler>();
    }

}