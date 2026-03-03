using CalamityFables.Content.Items.Wulfrum;
using Terraria.Localization;
using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class InvisibleBunkerWall : ModWall
    {
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DustType = -1;
            //Main.wallLight[Type] = true;
            AddMapEntry(new Color(83, 82, 60));
            WallID.Sets.Transparent[Type] = true;
            WallID.Sets.CannotBeReplacedByWallSpread[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r += 0.2f;
            g += 0.25f;
            b += 0.2f;
        }

        public override bool CanExplode(int i, int j) => false;
        public override bool CanPlace(int i, int j) => false;
        public override void KillWall(int i, int j, ref bool fail) => fail = true;
    }
}