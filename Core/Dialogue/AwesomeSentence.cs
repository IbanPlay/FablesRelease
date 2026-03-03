using Newtonsoft.Json.Linq;
using ReLogic.Graphics;
using System.Globalization;
using System.Text.RegularExpressions;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace CalamityFables.Core
{
    /// <summary>
    /// Text that can be scrolled one letter at a time, made up of multiple different <see cref="TextSnippet"/>s, allowing the text to have pauses, different font sizes, moving text, etc...
    /// </summary>
    public class AwesomeSentence
    {
        public readonly VoiceByte voice;
        internal LocalizedText plainText;
        public List<TextSnippet> snippets;
        internal float maxProgress;
        internal float totalWidth;
        internal float maxWidthBeforeWrap;

        public static readonly List<AwesomeSentence> localizedSetences = new();

        public AwesomeSentence(float textboxWidth, VoiceByte voiceUsed, params TextSnippet[] textSnippets)
        {
            if (Main.dedServ)
                return;

            maxWidthBeforeWrap = textboxWidth;
            voice = voiceUsed;
            snippets = new List<TextSnippet>(textSnippets);

            ResizeProperties();

            if (maxWidthBeforeWrap >= totalWidth)
                return;
            else
                ReWrap();
        }

        public AwesomeSentence(float textboxWidth, VoiceByte voiceUsed, string localizationKey)
        {
            if (Main.dedServ)
                return;


            maxWidthBeforeWrap = textboxWidth;
            voice = voiceUsed;
            plainText = CalamityFables.Instance.GetLocalization(localizationKey);
            try
            {
                ParseSnippetsFromLocalizedText();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse the dialogue with the key " + localizationKey, e);
            }

            ResizeProperties();
            if (maxWidthBeforeWrap < totalWidth)
                ReWrap();

            localizedSetences.Add(this);
        }

        ~AwesomeSentence()
        {
            if (plainText != null)
                localizedSetences.Remove(this);
        }

        #region Localization parsing
        public void ParseSnippetsFromLocalizedText()
        {
            string plainText = this.plainText.Value;
            snippets = new();

            //Initialize stacks to keep track of all the values
            Stack<float> fontSizeStack = new();
            Stack<float> characterDelayStack = new();
            Stack<Color?> colorStack = new();
            Stack<DynamicSpriteFont> fontStack = new(); 
            Stack<CharacterApparitionDelegate> appearDisplaceStack = new();
            Stack<CharacterDisplacementDelegate> animDisplaceStack = new();

            //Push default values at the base of the stacks
            fontSizeStack.Push(1f);
            characterDelayStack.Push(0.025f);
            colorStack.Push(null);
            fontStack.Push(null); //Right now font stack is never used but maybe eventually
            appearDisplaceStack.Push(null);
            animDisplaceStack.Push(null);

            //Regex so ugly :broken_heart: this splits the text into the individual tags alongside the text inbetween
            string[] splitSnippets = Regex.Split(plainText, @"(?!^)(?=<[\w:\d/\.]+>)|(?<=<[\w:\d/\.]+>)(?!$)");

            if (splitSnippets.Length == 1)
            {
                snippets.Add(new TextSnippet(splitSnippets[0], colorStack.Peek(), characterDelayStack.Peek(), fontSizeStack.Peek(), fontStack.Peek(), appearDisplaceStack.Peek(), animDisplaceStack.Peek()));
                return;
            }

            for (int i = 0; i < splitSnippets.Length; i++)
            {
                string snippet = splitSnippets[i];

                //Closing format tag, it's as simple as removing the value from the top of the respective stack
                if (snippet.StartsWith("</"))
                {
                    switch(snippet[2])
                    {
                        //Color
                        case 'c':
                            colorStack.Pop();
                            break;
                        case 's':
                            fontSizeStack.Pop();
                            break;
                        case 'd':
                            characterDelayStack.Pop();
                            break;
                        case 'a':
                            appearDisplaceStack.Pop();
                            break;
                        case 'm':
                            animDisplaceStack.Pop();
                            break;
                    }
                }
                //Opening format tag
                else if (snippet.StartsWith("<"))
                {
                    //Get the parameter as the part of it after the : and then trim the final >
                    string parameter = snippet.Split(':')[1];
                    parameter = parameter[..^1];

                    switch (snippet[1])
                    {
                        //Color
                        case 'c':
                            colorStack.Push(FablesUtils.ColorFromHex(int.Parse(parameter, NumberStyles.HexNumber)));
                            break;
                        //Size
                        case 's':
                            fontSizeStack.Push(float.Parse(parameter, CultureInfo.InvariantCulture));
                            break;
                        //Delay
                        case 'd':
                            characterDelayStack.Push(float.Parse(parameter, CultureInfo.InvariantCulture));
                            break;
                        //Apparition animation
                        case 'a':
                            appearDisplaceStack.Push(CharacterDisplacements.apparitionDelegates[parameter]);
                            break;
                        //Letter motion
                        case 'm':
                            animDisplaceStack.Push(CharacterDisplacements.displacementDelegates[parameter]);
                            break;
                    }
                }
                //Text inbetween tags gets added as a text snippet using the formatting at the top of the stack
                else
                    snippets.Add(new TextSnippet(snippet, colorStack.Peek(), characterDelayStack.Peek(), fontSizeStack.Peek(), fontStack.Peek(), appearDisplaceStack.Peek(), animDisplaceStack.Peek()));
            }
        }

        public void UpdateLocalization()
        {
            try
            {
                ParseSnippetsFromLocalizedText();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse the dialogue with the key " + plainText.Key, e);
            }

            ResizeProperties();
            if (maxWidthBeforeWrap >= totalWidth)
                return;
            else
                ReWrap();
        }
        #endregion


        public void ReWrap()
        {
            //Remove all linebreaks
            snippets.RemoveAll(t => t.content == "\n");

            float lineWidth = 0f;
            List<TextSnippet> wrappedSnippets = new List<TextSnippet>();

            while (snippets.Count > 0)
            {
                TextSnippet currentSnippet = snippets[0];
                float upcomingSnippetWidth = currentSnippet.dimensions.X;

                //If we can add the whole snippet without the need for any line breaks, add it (easy peasy)
                if (lineWidth + upcomingSnippetWidth <= maxWidthBeforeWrap)
                {
                    lineWidth += upcomingSnippetWidth;
                    wrappedSnippets.Add(currentSnippet);
                    snippets.RemoveAt(0);

                    //If we perfectly matched the edge of the textbox, add a line break and go to the next line
                    if (lineWidth == maxWidthBeforeWrap)
                    {
                        wrappedSnippets.Add(new TextSnippet("\n"));
                        lineWidth = 0;
                    }
                }

                //If adding the snippet would go over the current width, we will have to split it (or not, depending on how long the first word is)
                else
                {
                    //Right half is everything that we'll have to put on the next line, while left half is everything that can be fit into the current line
                    string splitSnippetRightHalf = currentSnippet.content;
                    string splitSnippetLeftHalf = "";

                    //Regex splits word by word
                    var firstWordRegex = Regex.Matches(splitSnippetRightHalf, "\\s*\\S+");
                    var wordByWordList = firstWordRegex.ToList();
                    
                    // FIX: It is possible that the first word ends up being
                    // whitespace (e.g. ` `), and this may cause the regex to
                    // fail to match any words.  This results in an indefinite
                    // loop because `firstWordRegex` is empty and nothing gets
                    // processed.  The simplest fix is to just ignore the
                    // whitespace since it shouldn't need to render at the start
                    // or end of lines.  We'll process the right half of the
                    // snippet as a word as a fallback if we somehow reach this
                    // condition without whitespace.
                    if (string.IsNullOrWhiteSpace(splitSnippetRightHalf))
                        snippets.RemoveAt(0);
                    else if (firstWordRegex.Count == 0)
                        wordByWordList = Regex.Matches(splitSnippetRightHalf, "(?s).*").ToList();

                    //Go over every word and check if it fits within the line
                    while (wordByWordList.Count > 0)
                    {
                        //Get the width of the current word we're at 
                        string nextWord = wordByWordList[0].Value;
                        float nextWordWidth = ChatManager.GetStringSize(currentSnippet.font, nextWord, Vector2.One).X * currentSnippet.fontSize;

                        //If we can fit the next snippet fragment into the line, keep going
                        if (lineWidth + nextWordWidth <= maxWidthBeforeWrap)
                        {
                            lineWidth += nextWordWidth;

                            //Shift everything over
                            splitSnippetLeftHalf += nextWord; //We can fit in the current fragment in the line, so add it to the left half
                            splitSnippetRightHalf = splitSnippetRightHalf.Substring(nextWord.Length); //Crop it out from the right half

                            //Somehow we can't just shave off the first element from the regex results, so welp, another check it is
                            wordByWordList.RemoveAt(0);

                            //If we ran out of words but theres still some more characters left in the right half (aka spaces at the end of the setence, add them)
                            //Tldr: issue was caused by a linebreak being determined to occur because of a space at the end of the snippet
                            if (wordByWordList.Count == 0 && splitSnippetRightHalf.Length > 0)
                            {
                                lineWidth += ChatManager.GetStringSize(currentSnippet.font, splitSnippetRightHalf, Vector2.One).X * currentSnippet.fontSize;

                                //Gtfo out of the loop
                                wrappedSnippets.Add(currentSnippet);
                                snippets.RemoveAt(0);
                            }
                        }

                        //If adding this word into the line causes it to exceed the lenght, do a linebreak, and split the snippets in 2
                        else
                        {
                            // Failsafe for extra long words / huge font sizes / languages without spaces between words like chinese
                            // These cases may end up with a whole "word" so long that it is wider than the textbox by itself, leading to an infinite loop of adding new lines
                            // In this scenario, we will have to split it letter by letter
                            if (nextWordWidth >= maxWidthBeforeWrap || Language.ActiveCulture.Name == "zh-Hans")
                                SplitWord(nextWord, ref splitSnippetLeftHalf, ref splitSnippetRightHalf, ref lineWidth, currentSnippet);

                            //Add the split left half of the snippet if it wasnt empty
                            //Left half can be empty if the first world was already too big to fit
                            if (splitSnippetLeftHalf != "")
                                wrappedSnippets.Add(new TextSnippet(splitSnippetLeftHalf, currentSnippet));

                            //Break line
                            wrappedSnippets.Add(new TextSnippet("\n"));
                            lineWidth = 0;

                            //Remove space at the start of the next line
                            if (splitSnippetRightHalf[0] == ' ')
                                splitSnippetRightHalf = splitSnippetRightHalf.Substring(1);

                            //Replace the next current snippet by the right half so we can continue as normal. Almost recursive in a way.
                            snippets[0] = new TextSnippet(splitSnippetRightHalf, currentSnippet);
                            break;
                        }
                    }
                }
            }

            snippets = wrappedSnippets;
            ResizeProperties();
        }

        /// <summary>
        /// Splits a word in 2 to make it wrap around a line character per character, and modifies the left & right halves of the setence it was part of accordingly <br/>
        /// Used for chinese wrapping, and for very large words that would take up an entire line
        /// </summary>
        /// <param name="wordToSplit"></param>
        /// <param name="currentSnippet"></param>
        public void SplitWord(string wordToSplit, ref string snippetLeftHalf, ref string snippetRightHalf, ref float lineWidth, TextSnippet currentSnippet)
        {
            // This regex splits words character by character, but keeps punctuation marks and such attached so they don't get broken off
            //It also includes any spaces at the start of words, since the word by word split includes the space in front of the character
            //[—《“"'‘（(]*\S[。.？?！!，,、：:；;》”’"')）…]*
             var characterSplitRegex = Regex.Matches(wordToSplit, "[—《“\"'‘（(\\s]*\\S[。.？?！!，,、：:；;》”’\"')）…]*");
            var characterByCharacterList = characterSplitRegex.ToList();

            string nextWordLeftHalf = "";
            string nextWordRightHalf = wordToSplit;

            while (characterByCharacterList.Count > 0)
            {
                string nextCharacter = characterByCharacterList[0].Value;
                float nextCharacterWidth = ChatManager.GetStringSize(currentSnippet.font, nextCharacter, Vector2.One).X * currentSnippet.fontSize;

                //Go character by character, checking if we can make them fit
                if (lineWidth + nextCharacterWidth <= maxWidthBeforeWrap)
                {
                    lineWidth += nextCharacterWidth;
                    nextWordLeftHalf += nextCharacter;
                    nextWordRightHalf = nextWordRightHalf.Substring(nextCharacter.Length);
                    characterByCharacterList.RemoveAt(0);
                }
                //Okay we reached the end of the line
                else
                    break;
            }

            snippetLeftHalf += nextWordLeftHalf;
            snippetRightHalf = snippetRightHalf.Substring(nextWordLeftHalf.Length);
        }

        public void ResizeProperties()
        {
            List<TextSnippet> linebreakLessSnippets = snippets.FindAll(t => t.content != "\n");

            maxProgress = 0f;
            totalWidth = 0f;
            foreach (TextSnippet snippet in linebreakLessSnippets)
            {
                maxProgress += snippet.Duration;
                totalWidth += snippet.dimensions.X;
            }
        }

        public float GetLineHeight(int line)
        {
            float maxLineHeight = 0f;

            //Go through each snippet
            for (int i = 0; i < snippets.Count; i++)
            {
                //If we encounter a line break
                if (snippets[i].content == "\n")
                {
                    //Either it was the line we wanted, so we stop
                    if (line == 0)
                        break;

                    //Otherwise, we shift the line index dowards, indicating we reached anohter line
                    else
                        line--;
                }

                //If we reached the line we desire, add the height of this line to the line thing
                if (line == 0)
                    maxLineHeight = Math.Max(maxLineHeight, snippets[i].dimensions.Y);
            }

            return maxLineHeight;
        }

        public float GetTotalHeight()
        {
            float height = GetLineHeight(0);
            int currentLine = 0;

            foreach (TextSnippet snippet in snippets)
            {
                if (snippet.content == "\n")
                {
                    currentLine++;
                    height += GetLineHeight(currentLine) * 0.6f;
                    continue;
                }
            }

            return height;
        }

        public float GetDelayAtProgress(float progression)
        {
            float currentProgress = 0f;

            foreach (TextSnippet snippet in snippets)
            {
                if (progression >= currentProgress && currentProgress + snippet.Duration >= progression)
                {
                    return snippet.characterApparitionDelay;
                }

                currentProgress += snippet.Duration;
            }

            return 0.05f;
        }

        public string GetLetterAtProgress(float progression)
        {
            float currentProgress = 0f;

            foreach (TextSnippet snippet in snippets)
            {
                if (progression >= currentProgress && currentProgress + snippet.Duration >= progression)
                {
                    foreach (char character in snippet.content)
                    {
                        if (progression >= currentProgress && currentProgress + snippet.characterApparitionDelay >= progression)
                        {
                            return character.ToString();
                        }

                        currentProgress += snippet.characterApparitionDelay;
                    }
                }

                currentProgress += snippet.Duration;
            }

            return " ";
        }

        public void Draw(float progression, Vector2 position, float rotation = 0f, float scaleMultiplier = 1f)
        {
            Vector2 currentPosition = position;
            Vector2 currentTextBorder = position;

            float setenceProgress = 0f;
            int currentCharacter = 0;

            float currentLineHeight = GetLineHeight(0);
            int currentLine = 0;

            foreach (TextSnippet snippet in snippets)
            {
                if (setenceProgress > progression)
                    return;

                if (snippet.content == "\n")
                {
                    currentTextBorder += Vector2.UnitY.RotatedBy(rotation) * currentLineHeight * 0.6f * scaleMultiplier;
                    currentPosition = currentTextBorder;
                    currentLine++;
                    currentLineHeight = GetLineHeight(currentLine);

                    continue;
                }

                Vector2 snippetHeightDown = Vector2.UnitY * (currentLineHeight - snippet.dimensions.Y) / 2f * scaleMultiplier;

                snippet.DrawSnippet(Main.spriteBatch, currentPosition + snippetHeightDown, progression - setenceProgress, currentCharacter, rotation, scaleMultiplier);
                currentPosition += snippet.dimensions.X * Vector2.UnitX.RotatedBy(rotation) * scaleMultiplier;

                setenceProgress += snippet.Duration;
                currentCharacter += snippet.content.Length;
            }
        }
    }
}
