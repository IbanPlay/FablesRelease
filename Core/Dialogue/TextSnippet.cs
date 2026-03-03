using ReLogic.Graphics;
using Terraria.UI.Chat;

namespace CalamityFables.Core
{
    /// <summary>
    /// An individual segment of text that can have a variety of different effects applied to it, but the effects are all consistent throughout the text displayed <br/>
    /// Multiple of them can be chained after one another into a coherent <see cref="AwesomeSentence"/> to allow for sentences with empathis and more
    /// </summary>
    public struct TextSnippet
    {
        //Displacement delegates
        public static Vector2 SmallRandomDisplacement(int character) => Main.rand.NextVector2Circular(1.2f, 1.2f);
        public static Vector2 RandomDisplacement(int character) => Main.rand.NextVector2Circular(2f, 2f);
        public static Vector2 SmallWaveDisplacement(int character) => new Vector2(0, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f + character * 0.8f) * 2.5f);

        //Apparition delegates
        public static Vector2 AppearFadingFromTop(int character, float progress) => new Vector2(0, -(float)Math.Pow(1 - progress, 1.6f) * 16f);

        public string content;
        public LetterColorDelegate color;
        public float characterApparitionDelay;
        public readonly DynamicSpriteFont font;
        public float fontSize;
        public CharacterDisplacementDelegate textDisplacement;
        public CharacterApparitionDelegate characterApparition;
        public float opacityMultiplier;

        public Vector2 dimensions;
        public float Duration => content.Length * characterApparitionDelay;
        public bool UsesPerCharacterEmphasis => textDisplacement != null && textDisplacement != CharacterDisplacements.NoDisplacement;

        public TextSnippet(string text, Color? color = null, float characterDelay = 0.025f, float size = 1f, DynamicSpriteFont font = null, CharacterApparitionDelegate apparition = null, CharacterDisplacementDelegate displacement = null)
        {
            if (Main.dedServ)
            {
                content = ""; this.color = default; opacityMultiplier = default; characterApparition = default; characterApparitionDelay = default; fontSize = default; this.font = default; textDisplacement = default; dimensions = default;
                return;
            }

            content = text;
            this.color = delegate (int character, float globalProgress) { return color ?? Color.White; };
            opacityMultiplier = 1f;

            characterApparitionDelay = characterDelay;
            fontSize = size;
            this.font = font ?? FontAssets.MouseText.Value;
            characterApparition = apparition ?? CharacterDisplacements.SuddenlyAppear;
            textDisplacement = displacement ?? CharacterDisplacements.NoDisplacement;

            dimensions = ChatManager.GetStringSize(this.font, content, Vector2.One) * fontSize;
        }
        public TextSnippet(string text, LetterColorDelegate color, float characterDelay = 0.025f, float size = 1f, DynamicSpriteFont font = null, CharacterApparitionDelegate apparition = null, CharacterDisplacementDelegate displacement = null)
        {
            if (Main.dedServ)
            {
                content = default; this.color = default; opacityMultiplier = default; characterApparition = default; characterApparitionDelay = default; fontSize = default; this.font = default; textDisplacement = default; dimensions = default;
                return;
            }

            content = text;
            this.color = color;
            opacityMultiplier = 1f;

            characterApparitionDelay = characterDelay;
            fontSize = size;
            this.font = font ?? FontAssets.MouseText.Value;
            characterApparition = apparition ?? CharacterDisplacements.SuddenlyAppear;
            textDisplacement = displacement ?? CharacterDisplacements.NoDisplacement;


            dimensions = ChatManager.GetStringSize(this.font, content, Vector2.One) * fontSize;
        }


        public TextSnippet(string text, TextSnippet copyDataFrom)
        {
            if (Main.dedServ)
            {
                content = default; this.color = default; opacityMultiplier = default; characterApparition = default; characterApparitionDelay = default; fontSize = default; this.font = default; textDisplacement = default; dimensions = default;
                return;
            }

            content = text;
            color = copyDataFrom.color;
            opacityMultiplier = copyDataFrom.opacityMultiplier;

            characterApparitionDelay = copyDataFrom.characterApparitionDelay;
            fontSize = copyDataFrom.fontSize;
            font = copyDataFrom.font;
            characterApparition = copyDataFrom.characterApparition;
            textDisplacement = copyDataFrom.textDisplacement;

            dimensions = ChatManager.GetStringSize(font, content, Vector2.One) * fontSize;
        }

        public void DrawSnippet(SpriteBatch sb, Vector2 position, float completion, int character, float rotation = 0f, float scaleMultiplier = 1f)
        {
            //if (!UsesPerCharacterEmphasis && completion >= Duration )
            //    FablesUtils.DrawBorderStringEightWay(sb, font, content, position, color(), Color.Black, fontSize, rotation);
            // else
            DrawLetterByLetterSnippet(sb, position, completion, character, rotation, scaleMultiplier);
        }


        public void DrawLetterByLetterSnippet(SpriteBatch sb, Vector2 position, float completion, int character, float rotation = 0f, float scaleMultiplier = 1f)
        {
            for (int i = 0; i < content.Length; i++)
            {
                if (completion < i * characterApparitionDelay)
                    return;

                Vector2 displacement = textDisplacement(character + i);
                if (completion >= i * characterApparitionDelay && completion < (i + 1) * characterApparitionDelay && characterApparition != CharacterDisplacements.SuddenlyAppear)
                    displacement += characterApparition(character + i, (completion - i * characterApparitionDelay) / characterApparitionDelay);

                displacement = displacement.RotatedBy(rotation) * scaleMultiplier;

                FablesUtils.DrawBorderStringEightWay(sb, font, content[i].ToString(), position + displacement, color(character + i, completion) * opacityMultiplier, Color.Black * opacityMultiplier, rotation, fontSize * scaleMultiplier);
                position += Vector2.UnitX.RotatedBy(rotation) * ChatManager.GetStringSize(font, content[i].ToString(), Vector2.One).X * fontSize * scaleMultiplier;
            }
        }

        public void RecalculateDimensions()
        {
            dimensions = ChatManager.GetStringSize(font, content, Vector2.One) * fontSize;
        }
    }
}
