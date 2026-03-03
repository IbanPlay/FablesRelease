using System.Reflection;
using Terraria.GameContent.Drawing;

namespace CalamityFables.Core
{
    public class WindHelper : ILoadable
    {
        public void Load(Mod mod)
        {
            //update once
            Update();
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += Update;
        }

        public static double treeWindCounter;
        public static double grassWindCounter;
        public static double sunflowerWindCounter;
        public static double vineWindCounter;
        public static int leafFrequency;

        public void Update()
        {
            //Copied from the private method in TileDrawing.Update, to get the proper cycles
            if (!Main.dedServ)
            {
                double baseWind = Math.Abs(Main.WindForVisuals);
                baseWind = Utils.GetLerpValue(0.08f, 1.2f, (float)baseWind, clamped: true);
                treeWindCounter += 1.0 / 240.0 + 1.0 / 240.0 * baseWind * 2.0;
                grassWindCounter += 1.0 / 180.0 + 1.0 / 180.0 * baseWind * 4.0;
                sunflowerWindCounter += 1.0 / 420.0 + 1.0 / 420.0 * baseWind * 5.0;
                vineWindCounter += 1.0 / 120.0 + 1.0 / 120.0 * baseWind * 0.4000000059604645;
                UpdateLeafFrequency((float)baseWind);
            }
        }

        private void UpdateLeafFrequency(float baseWind)
        {
            if (baseWind <= 0.1f)
                leafFrequency = 2000;
            else if (baseWind <= 0.2f)
                leafFrequency = 1000;
            else if (baseWind <= 0.3f)
                leafFrequency = 450;
            else if (baseWind <= 0.4f)
                leafFrequency = 300;
            else if (baseWind <= 0.5f)
                leafFrequency = 200;
            else if (baseWind <= 0.6f)
                leafFrequency = 130;
            else if (baseWind <= 0.7f)
                leafFrequency = 75;
            else if (baseWind <= 0.8f)
                leafFrequency = 50;
            else if (baseWind <= 0.9f)
                leafFrequency = 40;
            else if (baseWind <= 1f)
                leafFrequency = 30;
            else if (baseWind <= 1.1f)
                leafFrequency = 20;
            else
                leafFrequency = 10;
            leafFrequency *= 7;
        }

        /// <summary>
        /// Ripped from the private method in <see cref="TileDrawing"/> <br/>
        /// Unfunnily enough, the <see cref="TileDrawing.GetWindGridPushComplex(int, int, int, float, int, bool)"/> is public but not the one that gets the highest one, despite being a simple loop
        /// </summary>
        /// <returns></returns>
        public static float GetHighestWindGridPushComplex(int topLeftX, int topLeftY, int sizeX, int sizeY, int totalPushTime, float pushForcePerFrame, int loops, bool swapLoopDir) //Adapted from vanilla
        {
            float highestWindPush = 0f;
            int currentLowestWindTime = int.MaxValue;

            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    Main.instance.TilesRenderer.Wind.GetWindTime(topLeftX + i, topLeftY + j, totalPushTime, out int windTimeLeft, out _, out _);
                    float windGridPushComplex = Main.instance.TilesRenderer.GetWindGridPushComplex(topLeftX + i, topLeftY + j, totalPushTime, pushForcePerFrame, loops, swapLoopDir);

                    if (windTimeLeft < currentLowestWindTime && windTimeLeft != 0)
                    {
                        highestWindPush = windGridPushComplex;
                        currentLowestWindTime = windTimeLeft;
                    }
                }
            }

            return highestWindPush;
        }

        public void Unload() { }
    }
}
