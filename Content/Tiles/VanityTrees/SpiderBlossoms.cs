using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using Steamworks;
using System.Linq;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Metadata;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.VanityTrees
{
    public class SpiderBlossoms : ModTile
    {
        public override string Texture => AssetDirectory.VanityTrees + Name;

        public override void SetStaticDefaults()
        {
            SpiderTree.SpiderBlossomType = Type;

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoFail[Type] = true;
            TileID.Sets.ReplaceTileBreakUp[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 1;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 32 };
            TileObjectData.newTile.CoordinateWidth = 22;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = -14;

            TileObjectData.newTile.AnchorValidTiles = new[] { (int)TileID.CrimsonGrass, (int)TileID.CrimsonJungleGrass, (int)TileID.Crimsand };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.DrawFlipHorizontal = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.RandomStyleRange = 6;
            TileObjectData.newTile.StyleMultiplier = 6;
            TileObjectData.addTile(Type);

            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
            AddMapEntry(new Color(229, 54, 8));

            DustType = DustID.CrimsonPlants;
            HitSound = SoundID.Grass;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = 1;

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects effects)
        {
            if (i % 2 == 0)
                effects = SpriteEffects.FlipHorizontally;
        }
    }
}