using ReLogic.Utilities;
using static CalamityFables.Core.CuteSpeechSystem;


namespace CalamityFables.Core
{
    //Hesitated to call it CelesteSpeechSystem

    //https://twitter.com/regameyk/status/1416483583053602816 Short explanation of how Celeste's dialogue sfx is done
    //https://www.twitch.tv/videos/248998904?sr=a&t=1250s And a lovely in-depth explanation of how its all done
    //Despite getting the general idea right by simply guessing, these references were incredibly valuable, and this system is an attempt to recreate it.
    //Thank you kevin regamey!

    public class CuteSpeechSystem : ModSystem
    {
        public delegate float ToneShift(float progress);
        public static List<CuteSpeech> Speeches;

        public override void Load()
        {
            Speeches = new List<CuteSpeech>();
        }

        public override void Unload()
        {
            Speeches = null;
        }


        public static CuteSpeech Speak(Vector2 position, int setenceLenght, VoiceByte syllables, float pitchOffset = 0)
        {
            return Speak(position, setenceLenght, syllables, (float p) => pitchOffset);
        }

        public static CuteSpeech Speak(Vector2 position, int setenceLenght, VoiceByte syllables, ToneShift speechTone)
        {
            CuteSpeech setence = new CuteSpeech(position, setenceLenght, syllables, speechTone);
            Speeches.Add(setence);
            return setence;
        }

        public override void PostUpdateEverything()
        {
            foreach (CuteSpeech speech in Speeches)
            {
                //Don't play if a blabla is already playing
                if (SoundEngine.TryGetActiveSound(speech.speechSlot, out var activeSound) && activeSound.IsPlaying)
                    continue;


                float progress = speech.SyllableCount / (float)speech.MaxSyllables;


                SoundStyle usedStyle = speech.Syllables.regularSyllables;

                if ((Main.rand.NextBool(6) || speech.SyllableCount + 1 >= speech.MaxSyllables) && !speech.LastPlayedEnd)
                {
                    usedStyle = speech.Syllables.endSyllables;
                    speech.LastPlayedEnd = true;
                }
                else
                    speech.LastPlayedEnd = false;


                int variants = usedStyle.Variants.Length;
                int randomVariant = 1 + Main.rand.Next(variants);


                //if (variants > 1 && speech.LastPlayedVariant == randomVariant)
                //while (randomVariant == speech.LastPlayedVariant)
                //    randomVariant = 1 + Main.rand.Next(variants);

                speech.LastPlayedVariant = randomVariant;

                speech.speechSlot = SoundEngine.PlaySound(usedStyle with { Pitch = speech.SpeechTone(progress)/*, Variants = new ReadOnlySpan<int>(new int[] { speech.LastPlayedVariant })*/ }, speech.Position);

                speech.SyllableCount++;
            }

            Speeches.RemoveAll(s => s.SyllableCount >= s.MaxSyllables && s.LastPlayedEnd);

            base.PostUpdateEverything();
        }
    }

    public class VoiceByte
    {
        public SoundStyle regularSyllables;
        public SoundStyle endSyllables;

        public VoiceByte(SoundStyle baseSound, SoundStyle endSound)
        {
            regularSyllables = baseSound;
            endSyllables = endSound;
        }
    }

    public class CuteSpeech
    {
        //Avoids repetitions
        public int LastPlayedVariant = 0;
        public bool LastPlayedEnd = false;

        public VoiceByte Syllables;
        public ToneShift SpeechTone;
        public int SyllableCount;
        public int MaxSyllables;
        internal SlotId speechSlot;
        public Vector2 Position;

        public CuteSpeech(Vector2 position, int setenceLenght, VoiceByte voiceByte, ToneShift speechTone)
        {
            MaxSyllables = setenceLenght;
            SyllableCount = 0;
            Position = position;
            Syllables = voiceByte;
            SpeechTone = speechTone;
        }

    }
}
