using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.Graves
{
    public abstract class BaseGrave : ModTile
    {
        public abstract Color MapColor { get; }
        public abstract int BreakDust { get; }
        public virtual bool Glowing => false;
        public virtual bool Gilded => false;
        public virtual bool WorldgenOnly => false;
        public virtual int RandomStyleRange => 1;

        public virtual List<AutoloadedGravestoneItem> Items { get; }
        public virtual List<AutoloadedGravestoneProjectile> Projectiles { get; }
        public virtual List<int> ProjectilePool { get; }
        public virtual string DirectoryPath => AssetDirectory.Graves;
        public abstract string[] GravestoneNames { get; }

        public override void Load()
        {
            if (Name != "BaseGrave")
                GravestoneLoader.LoadGravestones(Mod, ProjectilePool, Projectiles, Items, DirectoryPath, GravestoneNames);
        }

        public override void Unload()
        {
            Items.Clear();
            Projectiles.Clear();
            ProjectilePool.Clear();
        }

        public override void SetStaticDefaults()
        {
            if (!WorldgenOnly)
                ConfirmGravestones();
            Main.tileLighted[Type] = Glowing;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.TileInteractRead[Type] = true;
            Main.tileSign[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 2;
            if (WorldgenOnly)
                TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            if (RandomStyleRange > 1)
                TileObjectData.newTile.RandomStyleRange = RandomStyleRange;

            TileObjectData.newTile.StyleHorizontal = true;

            TileObjectData.addTile(Type);

            DustType = BreakDust;
            AddMapEntry(MapColor, Language.GetText("ItemName.Tombstone"));
            base.SetStaticDefaults();
        }

        public void ConfirmGravestones() => GravestoneLoader.ConfirmGravestones(Type, Items, Projectiles, Gilded);

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
        public override void KillMultiTile(int i, int j, int frameX, int frameY) => Sign.KillSign(i, j);
    }
}
