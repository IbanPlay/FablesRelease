using ReLogic.Graphics;
using Terraria.UI.Chat;

namespace CalamityFables.Core
{
    public delegate Vector2 CharacterDisplacementDelegate(int character);
    public delegate Vector2 CharacterApparitionDelegate(int character, float progress);
    public delegate Color LetterColorDelegate(int character, float globalProgress);

    /// <summary>
    /// An individual segment of text that can have a variety of different effects applied to it, but the effects are all consistent throughout the text displayed <br/>
    /// Multiple of them can be chained after one another into a coherent <see cref="AwesomeSentence"/> to allow for sentences with empathis and more
    /// </summary>
    public static class CharacterDisplacements
    {
        //Displacement delegates
        public static Vector2 NoDisplacement(int character) => Vector2.Zero;
        public static Vector2 SmallRandomDisplacement(int character) => Main.rand.NextVector2Circular(1.2f, 1.2f);
        public static Vector2 RandomDisplacement(int character) => Main.rand.NextVector2Circular(2f, 2f);
        public static Vector2 SmallWaveDisplacement(int character) => new Vector2(0, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f + character * 0.8f) * 2.5f);
        public static Vector2 WaveDisplacement(int character) => new Vector2(0, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f + character * 0.8f) * 4f);
        public static Vector2 SmallEmpathisWave(int character) => new Vector2(0, -4f * MathHelper.Clamp(((float)Math.Sin(-Main.GlobalTimeWrappedHourly * 4f + character * 0.2f) - 0.7f) / 0.3f, 0f, 1f));

        public static Dictionary<string, CharacterDisplacementDelegate> displacementDelegates = new Dictionary<string, CharacterDisplacementDelegate>
        {
            { "None", NoDisplacement },
            { "SmallShake", SmallRandomDisplacement },
            { "Shake", RandomDisplacement },
            { "SmallWave", SmallWaveDisplacement },
            { "Wave", WaveDisplacement },
            { "Empathis", SmallEmpathisWave }
        };

        //Apparition delegates
        public static Vector2 SuddenlyAppear(int character, float progress) => Vector2.Zero;
        public static Vector2 AppearFadingFromTop(int character, float progress) => new Vector2(0, -(float)Math.Pow(1 - progress, 1.6f) * 16f);
        public static Vector2 AppearFadingFromTopZipper(int character, float progress) => new Vector2(0, -(float)Math.Pow(1 - progress, 1.6f) * 16f * (character % 2 == 1 ? 1 : -1));

        public static Dictionary<string, CharacterApparitionDelegate> apparitionDelegates = new Dictionary<string, CharacterApparitionDelegate>
        {
            { "None", SuddenlyAppear },
            { "FromTop", AppearFadingFromTop },
            { "Zipper", AppearFadingFromTopZipper }
        };
    }
}
