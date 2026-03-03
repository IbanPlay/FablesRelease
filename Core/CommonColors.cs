namespace CalamityFables.Core
{
    public static class CommonColors
    {
        #region Wulfrum
        public static readonly Color WulfrumGreen = new Color(194, 255, 67);
        public static readonly Color WulfrumBlue = new Color(112, 244, 244);
        public static readonly Color WulfrumMetalLight = new Color(154, 170, 116);
        public static readonly Color WulfrumMetalDark = new Color(75, 71, 60);
        public static readonly Color WulfrumLeatherRed = new Color(158, 68, 74);
        public static readonly Color WulfrumLeatherDarkMaroon = new Color(106, 43, 54);
        public static readonly Color WulfrumPipeworksBrown = new Color(105, 58, 40);

        public static float WulfrumLightMultiplierWithFlickerOffset(int offset)
        {
            float colorMult = 1f;
            colorMult *= 0.9f + 0.1f * (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f));

            offset *= 318942671;
            offset %= 10;
            offset = Math.Abs(offset);

            switch (offset)
            {
                case 0:
                case 3:
                    if (Main.GameUpdateCount % 190 <= 3 || Main.GameUpdateCount % 190 > 13 && Main.GameUpdateCount % 190 <= 16)
                        colorMult *= 0.86f;
                    break;
                case 5:
                    if (Main.GameUpdateCount % 230 <= 3 || Main.GameUpdateCount % 230 > 13 && Main.GameUpdateCount % 230 <= 16)
                        colorMult *= 0.86f;
                    break;
                case 1:
                case 8:
                case 6:
                    if (Main.GameUpdateCount % 160 <= 3 || Main.GameUpdateCount % 160 > 13 && Main.GameUpdateCount % 160 <= 16)
                        colorMult *= 0.86f;
                    break;
                case 2:
                case 4:
                    if (Main.GameUpdateCount % 180 <= 3 || Main.GameUpdateCount % 180 > 13 && Main.GameUpdateCount % 180 <= 16)
                        colorMult *= 0.86f;
                    break;
                case 9:
                case 7:
                default:
                    if (Main.GameUpdateCount % 200 <= 3 || Main.GameUpdateCount % 200 > 13 && Main.GameUpdateCount % 200 <= 16)
                        colorMult *= 0.86f;
                    break;

            }


            return colorMult;
        }

        /// <summary>
        /// Opacity with a subtle sine, with random flicker periods 
        /// </summary>
        public static float WulfrumLightMultiplier => WulfrumLightMultiplierWithFlickerOffset(0);

        public static bool WulfrumCustomPaintjob(int paint) => (paint > 0 && paint <= 12) || (paint > 24 && paint <= 28);
        public static string WulfrumPaintName(int paint, string name)
        {
            if (paint > 24)
                paint -= 12; //Go from 25 (black) to 13
            return AssetDirectory.WulfrumFurniturePaint + name + "_Paint" + paint.ToString();
        }
        #endregion

        public static Color DesertMirageBlue => Color.Lerp(FablesUtils.MulticolorLerp(Math.Abs((float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f)), Color.Cyan, Color.Turquoise, Color.DeepSkyBlue), Color.White, 0.3f);

        public static readonly Color MushroomDeepBlue = new Color(67, 84, 202);

        public static readonly Color FurnitureLightYellow = new Color(253, 221, 3);

        public static readonly Color DemoniteBlue = new Color(33, 18, 124);
    }
}