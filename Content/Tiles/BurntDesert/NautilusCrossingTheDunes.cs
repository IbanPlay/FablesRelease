using CalamityFables.Content.Boss.SeaKnightMiniboss;
using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class NautilusCrossingTheDunesItem : ModItem
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void Load()
        {
            IL_Chest.SetupTravelShop_GetPainting += AddToPaintingPool;
        }

        private void AddToPaintingPool(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(3),
                i => i.MatchLdcI4(1),
                i => i.MatchBgt(out _)
                ))
            {
                FablesUtils.LogILEpicFail("Add nautilus painting to travelling merchant shop", "Could not locate the minimumRarity <= 1 check");
                return;
            }

            ILLabel rarity1Check = cursor.DefineLabel();
            rarity1Check.Target = cursor.Next;

            cursor.Emit(Ldarg_0);
            cursor.Emit(Ldarg_1);
            cursor.Emit(Ldc_I4_1);
            cursor.Emit(Ldelem_I4);
            cursor.Emit(Callvirt, typeof(Player).GetMethod("RollLuck"));
            cursor.Emit(Brtrue_S, rarity1Check);

            cursor.Emit(Ldsfld, typeof(SirNautilusDialogue).GetField("DefeatedNautilus"));
            cursor.Emit(Brfalse_S, rarity1Check);

            cursor.Emit(Ldarg_2);
            cursor.EmitDelegate(() => ModContent.ItemType<NautilusCrossingTheDunesItem>());
            cursor.Emit(Stind_I4);
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Nautilus Crossing the Dunes");
            Tooltip.SetDefault("'H. Vihn'");
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
            Item.createTile = ModContent.TileType<NautilusCrossingTheDunes>();
            Item.value = Item.buyPrice(silver: 50);
        }
    }

    public class NautilusCrossingTheDunes : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.Height = 6;
            TileObjectData.newTile.Origin = new Point16(2, 3);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16, 16, 16 };

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Painting");
            AddMapEntry(new Color(99, 50, 30), name);
            TileID.Sets.FramesOnKillWall[Type] = true;
            DustType = 8;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }

}