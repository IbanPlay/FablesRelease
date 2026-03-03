using CalamityFables.Content.Tiles.VanityTrees;
using System.Reflection;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        private static readonly FieldInfo worldgen_numTreeShakes = typeof(WorldGen).GetField("numTreeShakes", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo worldgen_maxTreeShakes = typeof(WorldGen).GetField("maxTreeShakes", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo worldgen_TreeShakeX = typeof(WorldGen).GetField("treeShakeX", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo worldgen_TreeShakeY = typeof(WorldGen).GetField("treeShakeY", BindingFlags.NonPublic | BindingFlags.Static);

        public delegate bool IsLeafyTreeTopDelegate(int i, int j);

        public static bool ReachedTreeShakeCap()
        {
            int numTreeShakes = (int)worldgen_numTreeShakes.GetValue(null);
            int maxTreeShakes = (int)worldgen_maxTreeShakes.GetValue(null);
            return numTreeShakes >= maxTreeShakes;
        }

        /// <summary>
        /// Checks if the tree at the provided coordinates has already been shaken, and if not , register it as such
        /// </summary>
        /// <param name="i"></param>
        /// <param name="treeBottom"></param>
        /// <returns></returns>
        public static bool CheckIfTreeAlreadyShakenAndRegisterOtherwise(int i, int treeBottom)
        {
            int numTreeShakes = (int)worldgen_numTreeShakes.GetValue(null);
            int[] treeShakeX = (int[])worldgen_TreeShakeX.GetValue(null);
            int[] treeShakeY = (int[])worldgen_TreeShakeY.GetValue(null);

            for (int k = 0; k < numTreeShakes; k++)
            {
                if (treeShakeX[k] == i && treeShakeY[k] == treeBottom)
                    return false;
            }

            //Register tree shakes
            treeShakeX[numTreeShakes] = i;
            treeShakeY[numTreeShakes] = treeBottom;
            worldgen_numTreeShakes.SetValue(null, numTreeShakes + 1);
            return true;
        }

        /// <summary>
        /// Checks for the treetop and registers it as a proper shake by editing the worldgen private fields <br/>
        /// Additionally moves j to be at the top of the tree
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="type"></param>
        /// <param name="treetopCheck"></param>
        /// <returns></returns>
        public static bool ShakeTree(ref int i, ref int j, int type, IsLeafyTreeTopDelegate treetopCheck)
        {
            if (ReachedTreeShakeCap())
                return false;

            int treeBottom = j;
            while (Main.tile[i, treeBottom].HasTile && Main.tile[i, treeBottom].TileType == type)
                treeBottom++;

            if (!CheckIfTreeAlreadyShakenAndRegisterOtherwise(i, treeBottom))
                return false;

            int treeTop = j;
            while (treeTop > 10 && Main.tile[i, treeTop].HasTile && Main.tile[i, treeTop].TileType == type)
                treeTop--;
            treeTop++;

            if (!treetopCheck(i, treeTop) || Collision.SolidTiles(i - 2, i + 2, treeTop - 2, treeTop + 2))
                return false;

            j = treeTop;
            return true;

        }

        public static void DropCoinsFromTreeShake(int i, int j)
        {
            int coinType = ItemID.CopperCoin;
            int coinCount = WorldGen.genRand.Next(50, 100);

            if (WorldGen.genRand.NextBool(30))
            {
                coinType = ItemID.GoldCoin;
                coinCount = 1;
                if (WorldGen.genRand.NextBool(5))
                    coinCount++;
                if (WorldGen.genRand.NextBool(10))
                    coinCount++;
            }
            else if (WorldGen.genRand.NextBool(10))
            {
                coinType = ItemID.SilverCoin;
                coinCount = WorldGen.genRand.Next(1, 21);
                if (WorldGen.genRand.NextBool(3))
                    coinCount += WorldGen.genRand.Next(1, 21);

                if (WorldGen.genRand.NextBool(4))
                    coinCount += WorldGen.genRand.Next(1, 21);
            }

            Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), i * 16, j * 16, 16, 16, coinType, coinCount);
        }

        [Serializable]
        public abstract class TreeGrowFXPacket : NetEasy.Module
        {
            byte whoAmI;
            ushort i;
            ushort baseY;
            byte height;
            byte effectCount;

            public delegate void GrowEffectDelegate(int i, int y, bool tileBreak);
            public abstract GrowEffectDelegate GrowEffect { get;  }

            public TreeGrowFXPacket(int i, int baseY, int height, byte effectCount = 1)
            {
                whoAmI = (byte)Main.myPlayer;
                this.i = (ushort)i;
                this.baseY = (ushort)baseY;
                this.height = (byte)height;
                this.effectCount = effectCount;
            }

            protected override void Receive()
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    Send(-1, whoAmI, false);
                    return;
                }

                for (int y = baseY; y > baseY - height; y--)
                {
                    for (int d = 0; d < effectCount; d++)
                        GrowEffect(i, y, false);
                }
            }
        }
    }
}
