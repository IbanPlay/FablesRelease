namespace CalamityFables.Core
{
    public class DialoguePortrait
    {
        public Asset<Texture2D> basePortait;
        public Asset<Texture2D> nonSilouettedPortrait;

        public Asset<Texture2D> basePortaitFlip;
        public Asset<Texture2D> nonSilouettedPortraitFlip;

        public string portraitPath;

        public DialoguePortrait(string portraitPath, string nonsilouettePortraitPath = "", string flippedPortraitPath = "", string nonsilouetteFlippedPortraitPath = "")
        {
            basePortait = ModContent.Request<Texture2D>(portraitPath);


            if (nonsilouettePortraitPath != "")
                nonSilouettedPortrait = ModContent.Request<Texture2D>(nonsilouettePortraitPath);
            if (flippedPortraitPath != "")
                basePortaitFlip = ModContent.Request<Texture2D>(flippedPortraitPath);
            if (nonsilouetteFlippedPortraitPath != "")
                nonSilouettedPortraitFlip = ModContent.Request<Texture2D>(nonsilouetteFlippedPortraitPath);


            this.portraitPath = portraitPath;
        }

        public DialoguePortrait(string portraitPath, bool extra, bool extraFlip = false)
        {
            basePortait = ModContent.Request<Texture2D>(portraitPath);
            if (extra)
            {
                nonSilouettedPortrait = ModContent.Request<Texture2D>(portraitPath + "Extra");
                
                if (extraFlip)
                    nonSilouettedPortraitFlip = ModContent.Request<Texture2D>(portraitPath + "ExtraFlip");
            }

            this.portraitPath = portraitPath;
        }
    }
}
