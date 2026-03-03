using CalamityFables.Content.UI;
using System.IO;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class SirNautilusDialogue : ModSystem, ICoolDialogueHandler
    {
        public static readonly Color UndeadForcesColor = new Color(247, 77, 77);
        public static readonly Color SeaKingdomForcesColor = new Color(44, 220, 255);

        public LocalizedText GetLocalizedDialogue(string key) => Mod.GetLocalization("NautilusDialogue." + key);

        public override void Load()
        { 
            FablesPlayer.OnHurtEvent += RegisterNohitFailure;
            FablesPlayer.LoadDataEvent += LoadPlayerData;
            FablesPlayer.SaveDataEvent += SavePlayerData;
        }

        public override void PostSetupContent()
        {
            float textboxWidth = 480f;

            #region Instantiating all the textboxes

            #region Intro textboxes
            Intro_FirstTextbox = new(
           new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.Asleep"), portraits["bored"]);
               Intro_SecondTextbox = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.IntroWakeup"), portraits["pog"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
               Intro_ThirdTextbox = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.IntroSecond"), portraits["curious"]);
                Intro_FourthTextbox = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.IntroThird"), portraits["surprised"]);
                Intro_FifthTextbox = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.IntroFourth"), portraits["suspicious"], CompleteIntroduction, false);

            //Dialogue he uses only once, when finishing his intro speech
            Main_PostIntroTextbox = new(
                new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.PostIntroDuelProposition"), portraits["neutral"]);

            //Used when fighting him for the first time
            FightStart_JustificationOnFirstInteraction = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.FirstFightTextbox"), portraits["laughing"], SummonNautilus, true, QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            #endregion

            #region Main textboxes
            //Dialogue he uses before the player defeats him for the first time
            Main_PreDefeatTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPreDefeatTextbox1"), portraits["suspicious"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPreDefeatTextbox2"), portraits["angry"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPreDefeatTextbox3"), portraits["suspicious"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPreDefeatTextbox4"), portraits["laughing"])
            ];

            //Dialogue he uses after hes been defeated once
            Main_PostDefeatTextboxes = 
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox1"), portraits["neutral"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox2"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox3"), portraits["suspicious"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox4"), portraits["starstruckhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox5"), portraits["curious"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox6"), portraits["surprisedhands"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox7"), portraits["curious"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.RandomPostDefeatTextbox8"), portraits["shocked"], QuadrilateralBoxWithPortraitUI.ShakyPortrait)
            ];

            //Dialogue he uses when its been a while since the player last interacted with them
            Main_FirstInteractionInAWhileTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LongTimeNoSee1"), portraits["surprisedhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LongTimeNoSee2"), portraits["neutral"]),
            ];

            //Dialogue he uses after winning a duel
            Main_PlayerBeatenTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelWinTextbox1"), portraits["neutral"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelWinTextbox2"), portraits["neutral"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelWinTextbox3"), portraits["laughing"])
            ];

            //Dialogue he uses when talked to right after defeating him
            Main_NautilusJustBeatenTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelLoseTextbox1"), portraits["angryhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelLoseTextbox2"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelLoseTextbox3"), portraits["surprisedhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DuelLoseTextbox4"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
            ];

            //Dialogue he uses when talked right after nohitting him
            Main_NautilusNohittedTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.NohitTextbox1"), portraits["starstruckhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.NohitTextbox2"
                    ), portraits["surprisedhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait)
            ];

            //Rare dialogue he uses when talked right after nohitting him
            Main_RareNautilusNohitTextbox = new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.NohitTextboxRare")
                , portraits["starstruckhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);


            //One-time dialogue that appears after defeating Desert Scourge
            Progression_DesertScourgeDefeated = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.ProgressionDesertScourge"), portraits["neutral"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);

            //One-time dialogue that appears after entering Hardmode
            Progression_HardmodeReached = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.ProgressionHardmode"), portraits["curious"]);

            ProgressionReportTextboxes = [ Progression_DesertScourgeDefeated, Progression_HardmodeReached ];

            //Dialogue he uses right after beating him in less than 20 seconds
            Main_NautilusOutmatchedTextboxes =
            [
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.OutmatchedTextbox1"), portraits["unamused"]),
                //------------------------------------------------------------------
                new(new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.OutmatchedTextbox2"), portraits["nerd"], QuadrilateralBoxWithPortraitUI.ShakyPortrait)
            ];

            //One-time dialogue that appears when wearing Nautilus boss mask, Sea Rider Tunic and Sea Rider Greaves
            EasterEgg_Doppelganger = new(
                new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.DoppelgangerEasterEgg"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            #endregion

            #region Lore textboxes

            #region Pre defeat
            Lore_FirstWhoAreYou = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhoAreYou1"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            Lore_SecondWhoAreYou = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhoAreYou2"), portraits["curious"], ExitWhoAreYou);

            //------------------------------------------------------------------
            Lore_WhatAreYouDoing = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatAreYouDoing"), portraits["unamused"], ExitWhatAreYouDoing);

            //------------------------------------------------------------------
            Lore_WhyHere = new(
           new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhyHere"), portraits["suspicious"], ExitWhyHere, false);

            //------------------------------------------------------------------
            Lore_FirstWhatNowPreDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePreDefeatWhatNow1"), portraits["shocked"]);

            Lore_SecondWhatNowPreDefeat = new(
           new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePreDefeatWhatNow2"), portraits["neutral"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);

            Lore_ThirdWhatNowPreDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePreDefeatWhatNow3"), portraits["suspicious"], ExitWhatNowPreDefeat, false);
            #endregion

            #region Post defeat
            Lore_FirstWhatReallyHappened = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatReallyHappened1"), portraits["hollow"]);
            Lore_SecondWhatReallyHappened = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatReallyHappened2"), portraits["enraged"]);
            Lore_ThirdWhatReallyHappened = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatReallyHappened3"), portraits["hollow"]);
            Lore_FourthWhatReallyHappened = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatReallyHappened4"), portraits["enraged"], ExitWhatReallyHappened);

            //------------------------------------------------------------------
            Lore_FirstWhatHappenedToYou = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatHappenedToYou1"), portraits["hollow"]);
            Lore_SecondWhatHappenedToYou = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatHappenedToYou2"), portraits["neutral"], ExitWhatHappenedToYou);

            //------------------------------------------------------------------
            Lore_FirstHowDidYouKeepYourSanity = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreSanity1"), portraits["solemn"]);
            Lore_SecondHowDidYouKeepYourSanity = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreSanity2"), portraits["solemn"], ExitHowDidYouKeepYourSanity, false);

            //------------------------------------------------------------------
            Lore_FirstWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow1"), portraits["neutral"]); 
            Lore_SecondWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow2"), portraits["suspicious"]);
            Lore_ThirdWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow3"), portraits["curious"]);
            Lore_FourthWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow4"), portraits["pog"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            Lore_FifthWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow5"), portraits["starstruckhands"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            Lore_SixthWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow6"), portraits["laughing"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);
            Lore_SeventhWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow7"), portraits["laughing"], GiveKeepsakeToPlayer, false);

            Lore_EighthWhatNowPostDefeat = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LorePostDefeatWhatNow8"), portraits["surprised"], ExitWhatNowPostDefeat, false);

            Lore_FirstWhatsUp = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatsUp1"), portraits["neutral"]);
            Lore_SecondWhatsUp = new(
            new(textboxWidth, SirNautilus.RegularSpeech, "NautilusDialogue.LoreWhatsUp2"), portraits["curious"]);
            #endregion
            #endregion

            #endregion

            if (Main.dedServ)
                return;

            #region Linking textboxes together
            Intro_FirstTextbox.closeOnClick = false;
            Intro_FirstTextbox.clickEvent = () =>
            {
                WokeNautilusUp = true;
                CoolDialogueUIManager.theUI.SetTextbox(Intro_SecondTextbox);
            };


            //Intro_FirstTextbox.ChainToTextbox(Intro_SecondTextbox);
            Intro_SecondTextbox.ChainToTextbox(Intro_ThirdTextbox);
            Intro_ThirdTextbox.ChainToTextbox(Intro_FourthTextbox);
            Intro_FourthTextbox.ChainToTextbox(Intro_FifthTextbox);

            Lore_FirstWhoAreYou.ChainToTextbox(Lore_SecondWhoAreYou);
            Lore_FirstWhatNowPreDefeat.ChainToTextbox(Lore_SecondWhatNowPreDefeat);
            Lore_SecondWhatNowPreDefeat.ChainToTextbox(Lore_ThirdWhatNowPreDefeat);

            Lore_FirstWhatHappenedToYou.ChainToTextbox(Lore_SecondWhatHappenedToYou);
            Lore_FirstWhatReallyHappened.ChainToTextbox(Lore_SecondWhatReallyHappened);
            Lore_SecondWhatReallyHappened.ChainToTextbox(Lore_ThirdWhatReallyHappened);
            Lore_ThirdWhatReallyHappened.ChainToTextbox(Lore_FourthWhatReallyHappened);
            Lore_FirstHowDidYouKeepYourSanity.ChainToTextbox(Lore_SecondHowDidYouKeepYourSanity);
            Lore_FirstWhatNowPostDefeat.ChainToTextbox(Lore_SecondWhatNowPostDefeat);
            Lore_SecondWhatNowPostDefeat.ChainToTextbox(Lore_ThirdWhatNowPostDefeat);
            Lore_ThirdWhatNowPostDefeat.ChainToTextbox(Lore_FourthWhatNowPostDefeat);
            Lore_FourthWhatNowPostDefeat.ChainToTextbox(Lore_FifthWhatNowPostDefeat);
            Lore_FifthWhatNowPostDefeat.ChainToTextbox(Lore_SixthWhatNowPostDefeat);
            Lore_SixthWhatNowPostDefeat.ChainToTextbox(Lore_SeventhWhatNowPostDefeat);
            Lore_FirstWhatsUp.ChainToTextbox(Lore_SecondWhatsUp);

            IEnumerable<TextboxInfo> allMainTextboxes = Main_FirstInteractionInAWhileTextboxes
                .Concat(Main_NautilusJustBeatenTextboxes)
                .Concat(Main_NautilusNohittedTextboxes)
                .Append(Main_RareNautilusNohitTextbox)
                .Concat(Main_NautilusOutmatchedTextboxes)
                .Concat(Main_PlayerBeatenTextboxes)
                .Concat(Main_PostDefeatTextboxes)
                .Concat(Main_PreDefeatTextboxes)
                .Append(Main_PostIntroTextbox)
                .Concat(ProgressionReportTextboxes)
                .Append(EasterEgg_Doppelganger);

            foreach (TextboxInfo textbox in allMainTextboxes)
            {
                textbox.AddButton(StartFightButton).AddButton(LoreButton);

                //It's been forever since i've had a fight THIS exhilirating before
                //Hell yeah!
                if (textbox.setence.plainText?.Key == "Mods.CalamityFables.NautilusDialogue.DuelLoseTextbox2")
                    textbox.buttons[0] = new("NautilusDialogue.Buttons.StartFightHypedUp1", InitiateFight, false, 1.1f);

                //Ready for a rematch? I'll be sure to kick YOUR bucket this time!
                //Let's go!
                if (textbox.setence.plainText?.Key == "Mods.CalamityFables.NautilusDialogue.DuelLoseTextbox1")
                    textbox.buttons[0] = new("NautilusDialogue.Buttons.StartFightHypedUp2", InitiateFight, false, 1.1f);
            }

            Lore_SecondWhoAreYou.AddButton(GoToWhyHereButton).AddButton(GoToWhatAreYouDoingButton);
            Lore_WhyHere.AddButton(GoToWhatNowFromWhyHereButton);
            Lore_WhatAreYouDoing.AddButton(GoToWhatNowFromWhatchaDoinButton);
            Lore_FourthWhatReallyHappened.AddButton(GoToWhatHappenedToYouButton).AddButton(GoToHowDidYouKeepYourSanityButton);
            Lore_SecondWhatHappenedToYou.AddButton(GoToWhatNowFromWhatHappenedToYouButton);
            Lore_SecondHowDidYouKeepYourSanity.AddButton(GoToWhatNowFromHowDidYouKeepYourSanityButton);
            #endregion
        }



        const string PortraitFolder = AssetDirectory.SirNautilusDialogue;
        public static readonly Dictionary<string, DialoguePortrait> portraits = new Dictionary<string, DialoguePortrait>
        {
            ["frontfacing"] =       new(PortraitFolder + "FrontFacing"),
            ["neutral"] =           new(PortraitFolder + "Neutral"),
            ["laughing"] =          new(PortraitFolder + "Laughing", true),
            ["angry"] =             new(PortraitFolder + "Angry", true),
            ["angryhands"] =        new(PortraitFolder + "AngryHands", true),
            ["enraged"] =           new(PortraitFolder + "Enraged", true),
            ["solemn"] =            new(PortraitFolder + "Solemn"),
            ["bored"] =             new(PortraitFolder + "Bored", true, true),
            ["curious"] =           new(PortraitFolder + "Curious", true, true),
            ["pog"] =               new(PortraitFolder + "Pogging", true),
            ["starstruck"] =        new(PortraitFolder + "StarStruck"),
            ["starstruckhands"] =   new(PortraitFolder + "StarStruckHands", true),
            ["surprised"] =         new(PortraitFolder + "Surprised", true),
            ["surprisedhands"] =    new(PortraitFolder + "SurprisedHands", true),
            ["suspicious"] =        new(PortraitFolder + "Suspicious"),
            ["unamused"] =          new(PortraitFolder + "Unamused"),
            ["shocked"] =           new(PortraitFolder + "Shocked", true),
            ["hollow"] =            new(PortraitFolder + "Hollow"),
            ["nerd"] =              new(PortraitFolder + "Nerd"),
            ["uncanny"] =           new(PortraitFolder + "Uncanny"),
            ["shellshocked"] =      new(PortraitFolder + "ShellShocked"),
            ["fury"] =              new(PortraitFolder + "Fury", true),
        };

        #region Styling
        public void TurnOnUITheme()
        {
            QuadrilateralBoxWithPortraitUI ui = CoolDialogueUIManager.theUI.mainBox;

            //Name
            ui.CharacterName = GetLocalizedDialogue("CharacterName");

            //Colors
            ui.BackgroundColor = new Color(78, 159, 216) * 0.4f;
            ui.OutlineColor = new Color(78, 159, 216) * 0.975f;
            ui.HoveredOutlineColor = new Color(190, 144, 27) * 0.975f;
            ui.HoveredDoubleOutlineColor = Color.White * 0.6f;
            ui.HoveredBackgroundColor = new Color(178, 159, 16) * 0.4f;

            //Sprites
            ui.Portrait = portraits["neutral"];
            ui.FrontFacingPortrait = portraits["frontfacing"];
            ui.DialogueOverIcon = Request<Texture2D>(AssetDirectory.SirNautilusDialogue + "DialogueOver", AssetRequestMode.ImmediateLoad);
        }

        public void StyleButton(QuadrilateralButtonUI button, int index)
        {
            if (index % 2 == 0)
            {
                button.MainColor = new Color(67, 179, 255) * 0.8f;
                button.HoveredMainColor = new Color(67, 179, 255);
                button.HoveredOutlineColor = new Color(223, 86, 27);
                button.HoveredDecalColor = new Color(255, 140, 60);
            }
            else
            {
                button.MainColor = new Color(248, 88, 2) * 0.8f;
                button.HoveredMainColor = new Color(248, 88, 2);
                button.HoveredOutlineColor = new Color(2, 232, 242);
                button.HoveredDecalColor = new Color(70, 176, 255);
            }
        }
        #endregion

        public TextboxInfo GetFirstTextbox()
        {
            //return UncannyTextbox;
            //Skips the first zzz textbox if nautilus has been woken up already

            //This is for multiplayer, mostly. So nautilus doesn't suddenly snap back to not knowing you despite playing banjo before speaking to him
            if (DefeatedNautilus)
            {
                //Case where a player never spoke to nautilus, but joined a server where he has been defeated
                if (!ReadThroughTheFirstRundown)
                    return Intro_SecondTextbox;
                return GetRandomMainTextbox();
            }

            return !WokeNautilusUp? Intro_FirstTextbox : !ReadThroughTheFirstRundown ? Intro_SecondTextbox : GetRandomMainTextbox();
        }

        #region textboxes

        #region Intro
        public static TextboxInfo Intro_FirstTextbox, Intro_SecondTextbox, Intro_ThirdTextbox, Intro_FourthTextbox, Intro_FifthTextbox, Main_PostIntroTextbox;
        #endregion

        #region main textboxes
        public static TextboxInfo[] Main_PreDefeatTextboxes;
        public static TextboxInfo[] Main_PostDefeatTextboxes;
        public static TextboxInfo[] Main_FirstInteractionInAWhileTextboxes;
        public static TextboxInfo[] Main_PlayerBeatenTextboxes;
        public static TextboxInfo[] Main_NautilusJustBeatenTextboxes;

        public static TextboxInfo[] Main_NautilusNohittedTextboxes;
        public static TextboxInfo Main_RareNautilusNohitTextbox;
        public static TextboxInfo[] Main_NautilusOutmatchedTextboxes;

        public static TextboxInfo Progression_DesertScourgeDefeated;
        public static TextboxInfo Progression_HardmodeReached;
        public static TextboxInfo[] ProgressionReportTextboxes;

        public static TextboxInfo EasterEgg_Doppelganger;
        #endregion

        #region lore textboxes

        #region Pre defeat
        public static TextboxInfo Lore_FirstWhoAreYou;
        public static TextboxInfo Lore_SecondWhoAreYou;
        public static TextboxInfo Lore_WhatAreYouDoing;
        public static TextboxInfo Lore_WhyHere;
        public static TextboxInfo Lore_FirstWhatNowPreDefeat;
        public static TextboxInfo Lore_SecondWhatNowPreDefeat;
        public static TextboxInfo Lore_ThirdWhatNowPreDefeat;
        #endregion

        #region Post-defeat
        public static TextboxInfo Lore_FirstWhatReallyHappened;
        public static TextboxInfo Lore_SecondWhatReallyHappened;
        public static TextboxInfo Lore_ThirdWhatReallyHappened;
        public static TextboxInfo Lore_FourthWhatReallyHappened;
        public static TextboxInfo Lore_FirstWhatHappenedToYou;
        public static TextboxInfo Lore_SecondWhatHappenedToYou;
        public static TextboxInfo Lore_FirstHowDidYouKeepYourSanity;

        
        public static TextboxInfo Lore_SecondHowDidYouKeepYourSanity;

        public static TextboxInfo Lore_FirstWhatNowPostDefeat;
        public static TextboxInfo Lore_SecondWhatNowPostDefeat;
        public static TextboxInfo Lore_ThirdWhatNowPostDefeat;
        public static TextboxInfo Lore_FourthWhatNowPostDefeat;
        public static TextboxInfo Lore_FifthWhatNowPostDefeat;
        public static TextboxInfo Lore_SixthWhatNowPostDefeat;
        public static TextboxInfo Lore_SeventhWhatNowPostDefeat;
        public static TextboxInfo Lore_EighthWhatNowPostDefeat;

        public static TextboxInfo Lore_FirstWhatsUp;
        public static TextboxInfo Lore_SecondWhatsUp;
        #endregion

        #endregion

        #region Other textboxes
        public static TextboxInfo FightStart_JustificationOnFirstInteraction;

        public static readonly TextboxInfo UncannyTextbox = new(
           new(480f, SirNautilus.RegularSpeech,
               new TextSnippet("...", Color.White, 0.2f, 1.8f, null, CharacterDisplacements.AppearFadingFromTop),
               new TextSnippet(" wait", Color.White, 0.03f, 0.9f, null, CharacterDisplacements.AppearFadingFromTop, CharacterDisplacements.SmallRandomDisplacement),
               new TextSnippet(" ", Color.White, 0.36f, 1f, null),
               new TextSnippet("it was ", Color.White, 0.03f, 1f, null, CharacterDisplacements.AppearFadingFromTop, CharacterDisplacements.SmallRandomDisplacement),
               new TextSnippet("HOW MANY DEVS???", Color.White, 0.08f, 2f, null, CharacterDisplacements.AppearFadingFromTop, CharacterDisplacements.SmallRandomDisplacement)
               ), portraits["uncanny"], QuadrilateralBoxWithPortraitUI.ShakyPortrait);

        #endregion

        #endregion
        
        #region Buttons
        public static readonly ButtonInfo StartFightButton = new("NautilusDialogue.Buttons.StartFightPreDefeat", InitiateFight, false, labelDisplacement: CharacterDisplacements.RandomDisplacement);
        public static readonly ButtonInfo LoreButton = new("NautilusDialogue.Buttons.LoreButtonPreDefeat", OpenLoreQuestions, false);

        public static readonly ButtonInfo GoToWhyHereButton = new("NautilusDialogue.Buttons.WhyHerePreDefeat", AskWhyHere, false);
        public static readonly ButtonInfo GoToWhatAreYouDoingButton = new("NautilusDialogue.Buttons.WhatAreYouDoingPreDefeat", AskWhatAreYouDoing, false);

        public static readonly ButtonInfo GoToWhatNowFromWhyHereButton = new("NautilusDialogue.Buttons.WhatNowPreDefeat", AskWhatNowFromWhyHere, false);
        public static readonly ButtonInfo GoToWhatNowFromWhatchaDoinButton = new("NautilusDialogue.Buttons.WhatNowPreDefeat", AskWhatNowFromWhatAreYouDoing, false);

        public static readonly ButtonInfo GoToWhatHappenedToYouButton = new("NautilusDialogue.Buttons.WhatHappenedPostDefeat", AskWhatHappenedToYou, false);
        public static readonly ButtonInfo GoToHowDidYouKeepYourSanityButton = new("NautilusDialogue.Buttons.HowDidYouStaySanePostDefeat", AskHowDidYouKeepYourSanity, false);

        public static readonly ButtonInfo GoToWhatNowFromWhatHappenedToYouButton = new("NautilusDialogue.Buttons.WhatNowPostDefeat", AskWhatNowFromWhatHappenedToYou, false);
        public static readonly ButtonInfo GoToWhatNowFromHowDidYouKeepYourSanityButton = new("NautilusDialogue.Buttons.WhatNowPostDefeat", AskWhatNowFromHowDidYouKeepYourSanity, false);

        public static readonly ButtonInfo GoToWhatsUpFromWhatHappenedToYouButton = new("NautilusDialogue.Buttons.WhatsUpPostDefeat", AskWhatsUpFromWhatHappenedToYou, false);
        public static readonly ButtonInfo GoToWhatsUpFromHowDidYouKeepYourSanityButton = new("NautilusDialogue.Buttons.WhatsUpPostDefeat", AskWhatsUpFromHowDidYouKeepYourSanity, false);
        #endregion

        #region On-click delegates 

        //Reached when the player finishes the introduction textboxes. Sets the flat so it doesnt get repeated
        public static void CompleteIntroduction()
        {
            ReadThroughTheFirstRundown = true;
            Main_PostIntroTextbox.buttons[1].localizedLabel = CalamityFables.Instance.GetLocalization("NautilusDialogue.Buttons." + GetLoreButtonLabel());
            //CoolDialogueUIManager.RecalculateButtons();
            CoolDialogueUIManager.theUI.SetTextbox(Main_PostIntroTextbox);
        }

        public static TextboxInfo GetRandomMainTextbox()
        {
            LoreButton.localizedLabel = CalamityFables.Instance.GetLocalization("NautilusDialogue.Buttons." + GetLoreButtonLabel());
            //CoolDialogueUIManager.RecalculateButtons();

            Player player = Main.LocalPlayer;

            bool neverDefeated = !DefeatedNautilus;

            bool defeatedPlayer = ChallengedNautilusOnce && !PlayerWonLastDuel;

            bool justBeaten = PlayerWonLastDuel && PlayerKilledNautilusTimer > 0;
            bool unfairFight = justBeaten && LastFightDuration <= UnfairFightDurationTreshold;
            bool successfulNohit = justBeaten && LastFightNohitted;
            bool helloWelcome = LastSawNautilusTimer <= 0;

            bool hasHeadEquip = player.head == EquipLoader.GetEquipSlot(CalamityFables.Instance, "SirNautilusBossMask", EquipType.Head);
            bool hasBodyEquip = player.body == EquipLoader.GetEquipSlot(CalamityFables.Instance, "SeaRiderTunic", EquipType.Body);
            bool hasLeggingsEquip = player.legs == EquipLoader.GetEquipSlot(CalamityFables.Instance, "SeaRiderGreaves", EquipType.Legs);
            bool doppelganger = hasHeadEquip && hasBodyEquip && hasLeggingsEquip && !ReadThroughDoppelgangerEasterEgg;

            //this does not seem to function properly
            bool desertScourgeDead = WorldProgressionSystem.DefeatedDesertScourge && !ReadThroughDesertScourgeTopic;
            bool hardmodeReached = Main.hardMode && !ReadThroughHardmodeTopic;

            if (neverDefeated)
            {
                if (defeatedPlayer)
                    return Main.rand.Next(Main_PlayerBeatenTextboxes.Concat(Main_PreDefeatTextboxes));

                return Main.rand.Next(Main_PreDefeatTextboxes);
            }

            if (doppelganger)
            {
                ReadThroughDoppelgangerEasterEgg = true;
                return EasterEgg_Doppelganger;
            }

            if (desertScourgeDead)
            {
                ReadThroughDesertScourgeTopic = true;

                return Progression_DesertScourgeDefeated;
            }

            if (hardmodeReached)
            {
                ReadThroughHardmodeTopic = true;

                return Progression_HardmodeReached;
            }

            if (defeatedPlayer)
                return Main.rand.Next(Main_PlayerBeatenTextboxes.Concat(Main_PostDefeatTextboxes));

            if (unfairFight)
            {
                LastFightDuration = float.MaxValue;
                PlayerKilledNautilusTimer = 0;
                return Main.rand.Next(Main_NautilusOutmatchedTextboxes);
            }

            if (successfulNohit)
            {
                LastFightNohitted = false;
                PlayerKilledNautilusTimer = 0;

                if (Main.rand.NextBool(30) && false)
                    return Main_RareNautilusNohitTextbox;

                return Main.rand.Next(Main_NautilusNohittedTextboxes);
            }

            if (justBeaten)
            {
                PlayerKilledNautilusTimer = 0;
                return Main.rand.Next(Main_NautilusJustBeatenTextboxes);
            }

            if (helloWelcome)
            {
                LastSawNautilusTimer = HelloAgainNautilusDialogueTimeframe;
                return Main.rand.Next(Main_FirstInteractionInAWhileTextboxes.Concat(Main_PostDefeatTextboxes));
            }

            return Main.rand.Next(Main_PostDefeatTextboxes);
        }

        public static void SwitchToMainTextbox()
        {
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        //Either directly starts the fight or adds a small little dialogue box if it was the first time
        public static void InitiateFight()
        {
            if (ChallengedNautilusOnce)
                SummonNautilus();
            else
                CoolDialogueUIManager.theUI.SetTextbox(FightStart_JustificationOnFirstInteraction);
        }

        public static void SummonNautilus()
        {
            if (CoolDialogueUIManager.theUI.anchorNPC != null && CoolDialogueUIManager.theUI.anchorNPC.type == NPCType<SirNautilusPassive>())
            {
                NPC anchor = CoolDialogueUIManager.theUI.anchorNPC;
                
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    new SirNautilusSummonPacket(CoolDialogueUIManager.theUI.anchorNPC).Send(runLocally:false);
                else
                    NPC.NewNPCDirect(anchor.GetSource_FromThis(), (int)anchor.Center.X, (int)anchor.Center.Y + 32, NPCType<SirNautilus>());

                ChallengedNautilusOnce = true;
            }
        }


        #region Lore
        public static string GetLoreButtonLabel()
        {
            //Check the bottom method to see descriptions
            if (!DefeatedNautilus)
            {
                if (!ReadThroughWhoAreYou)
                    return "LoreButtonPreDefeat";

                else if (!ReadThroughWhatAreYouDoing)
                    return "WhatAreYouDoingPreDefeat";

                else if (!ReadThroughWhyHere)
                    return "WhyHerePreDefeat";

                else
                    return "WhatNowPreDefeat";
            }

            else
            {
                if (!ReadThroughWhatReallyHappened)
                    return "LoreButtonWhatHappened";

                else if (!ReadThroughWhatHappenedToYou)
                    return "WhatHappenedPostDefeat";

                else if (!ReadThroughHowDidYouKeepYourSanity)
                    return "HowDidYouStaySanePostDefeat";

                else if (!ReadThroughWhatNowPostDefeat)
                    return "WhatNowPostDefeat";

                else
                    return "WhatsUpPostDefeat";
            }
        }

        public static void OpenLoreQuestions()
        {
            //Pre-defeat textboxes
            if (!DefeatedNautilus)
            {
                //If the player never checked out the initial lore 
                if (!ReadThroughWhoAreYou)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhoAreYou);

                //If 
                //If the player hasn't seen all pre-defeat lore textboxes yet, it goes on a somewhat branching path
                //After completing the "who are you" line of dialogue boxes, the player will have a choice to make between 2 paths: "Why here" and "what are you doing?"
                //The option the player didn't choose will be presented to them as a direct option from the main textbox.
                //
                //Alternatively, if the player exits the textbox AFTER completing the "who are you" textboxes but not selecting any of either options, the player will be able to access them from the main menu

                else if (!ReadThroughWhatAreYouDoing)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_WhatAreYouDoing);

                //If the player already read what are you doing but hasnt read "why here"
                else if (!ReadThroughWhyHere)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_WhyHere);

                else
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_ThirdWhatNowPreDefeat);
            }
            //Post-defeat textboxes
            else
            {
                //If the player never checked out the initial lore 
                if (!ReadThroughWhatReallyHappened)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatReallyHappened);

                //Branching dialogue, like with Pre-Defeat lore textboxes
                else if (!ReadThroughWhatHappenedToYou)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatHappenedToYou);

                //If the player already read what happened to you but hasnt read "How are you still sane"
                else if (!ReadThroughHowDidYouKeepYourSanity)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstHowDidYouKeepYourSanity);

                else if (!ReadThroughWhatNowPostDefeat)
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatNowPostDefeat);

                else
                    CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatsUp);
            }
        }

        //Transitioning from textboxes while also checking off that the player already read X and Y
        //Pre-defeat lore textboxes
        public static void AskWhatAreYouDoing()
        {
            ReadThroughWhoAreYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_WhatAreYouDoing);
        }
        public static void AskWhyHere()
        {
            ReadThroughWhoAreYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_WhyHere);
        }
        public static void AskWhatNowFromWhatAreYouDoing()
        {
            ReadThroughWhatAreYouDoing = true;
            CoolDialogueUIManager.theUI.SetTextbox(ReadThroughWhatNowPreDefeat ? Lore_ThirdWhatNowPreDefeat : Lore_FirstWhatNowPreDefeat);
        }
        public static void AskWhatNowFromWhyHere()
        {
            ReadThroughWhyHere = true;
            CoolDialogueUIManager.theUI.SetTextbox(ReadThroughWhatNowPreDefeat ? Lore_ThirdWhatNowPreDefeat : Lore_FirstWhatNowPreDefeat);
        }

        //Post-defeat lore textboxes
        public static void AskWhatHappenedToYou()
        {
            ReadThroughWhatReallyHappened = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatHappenedToYou);
        }
        public static void AskHowDidYouKeepYourSanity()
        {
            ReadThroughWhatReallyHappened = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstHowDidYouKeepYourSanity);
        }
        public static void AskWhatNowFromWhatHappenedToYou()
        {
            ReadThroughWhatHappenedToYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatNowPostDefeat);
        }
        public static void AskWhatNowFromHowDidYouKeepYourSanity()
        {
            ReadThroughHowDidYouKeepYourSanity = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatNowPostDefeat);
        }
        public static void AskWhatsUpFromWhatHappenedToYou()
        {
            ReadThroughWhatHappenedToYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatsUp);
        }
        public static void AskWhatsUpFromHowDidYouKeepYourSanity()
        {
            ReadThroughHowDidYouKeepYourSanity = true;
            CoolDialogueUIManager.theUI.SetTextbox(Lore_FirstWhatsUp);
        }

        public static void GiveKeepsakeToPlayer()
        {
            ReadThroughWhatNowPostDefeat = true;
            Main.LocalPlayer.GiveKeepsake("NautilusPendant");
            CoolDialogueUIManager.theUI.SetTextbox(Lore_EighthWhatNowPostDefeat);
        }

        //These happen if the player just clicks out of the textbox
        //Pre-defeat
        public static void ExitWhoAreYou()
        {
            ReadThroughWhoAreYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitWhatAreYouDoing()
        {
            ReadThroughWhatAreYouDoing = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitWhyHere()
        {
            ReadThroughWhyHere = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitWhatNowPreDefeat()
        {
            ReadThroughWhatNowPreDefeat = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        //Post-defeat
        public static void ExitWhatReallyHappened()
        {
            ReadThroughWhatReallyHappened = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitWhatHappenedToYou()
        {
            ReadThroughWhatHappenedToYou = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitHowDidYouKeepYourSanity()
        {
            ReadThroughHowDidYouKeepYourSanity = true;
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        public static void ExitWhatNowPostDefeat()
        {
            CoolDialogueUIManager.theUI.SetTextbox(GetRandomMainTextbox());
        }

        #endregion
        #endregion

        #region Updating stats
        public override void UpdateUI(GameTime gameTime)
        {
            if (DefeatedNautilus && StartFightButton.localizedLabel.Key == "Mods.CalamityFables.NautilusDialogue.Buttons.StartFightPreDefeat")
            {
                StartFightButton.localizedLabel = Mod.GetLocalization("NautilusDialogue.Buttons.StartFightPostDefeat");
                StartFightButton.letterDisplace = CharacterDisplacements.NoDisplacement; //He chilled out and is no longer yelling about it, so wholesome
            }
            //Necessary when changing worlds
            else if (!DefeatedNautilus && StartFightButton.localizedLabel.Key == "Mods.CalamityFables.NautilusDialogue.Buttons.StartFightPostDefeat")
            {
                StartFightButton.localizedLabel = Mod.GetLocalization("NautilusDialogue.Buttons.StartFightPreDefeat");
                StartFightButton.letterDisplace = CharacterDisplacements.RandomDisplacement; //Go back to yelling
            }

            if (DefeatedNautilus && ReadThroughWhatNowPostDefeat)
            {
                if (Lore_SecondWhatHappenedToYou.buttons[0] == GoToWhatNowFromWhatHappenedToYouButton)
                    Lore_SecondWhatHappenedToYou.buttons[0] = GoToWhatsUpFromWhatHappenedToYouButton;

                if (Lore_SecondHowDidYouKeepYourSanity.buttons[0] == GoToWhatNowFromHowDidYouKeepYourSanityButton)
                    Lore_SecondHowDidYouKeepYourSanity.buttons[0] = GoToWhatsUpFromHowDidYouKeepYourSanityButton;
            }
        }

        public override void PostUpdateEverything()
        {
            if (LastSawNautilusTimer > 0)
                LastSawNautilusTimer--;

            if (PlayerKilledNautilusTimer > 0)
                PlayerKilledNautilusTimer--;

            if (PlayerKilledByNautilusTimer > 0)
                PlayerKilledByNautilusTimer--;

            if (Main.npc.Any(n => n.active && n.type == NPCType<SirNautilus>()))
            {
                LastFightDuration++;
                PlayerKilledByNautilusTimer = NautilusBeatPlayerDialogueTimeframe;
            }
        }

        private void RegisterNohitFailure(Player player, Player.HurtInfo info)
        {
            if (player.whoAmI == Main.myPlayer)
                LastFightNohitted = false;
        }

        #endregion

        #region Saving and managing data
        public static bool DefeatedNautilus;


        public static ref bool ChallengedNautilusOnce => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ChallengedNautilusOnce;
        public static ref bool ReadThroughDesertScourgeTopic => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughDesertScourgeTopic;
        public static ref bool ReadThroughHardmodeTopic => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughHardmodeTopic;
        public static ref bool ReadThroughDoppelgangerEasterEgg => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughDoppelgangerEasterEgg;
        public static ref bool WokeNautilusUp => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.WokeNautilusUp;
        public static ref bool ReadThroughTheFirstRundown => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughTheFirstRundown;
        public static ref bool ReadThroughWhoAreYou => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhoAreYou;
        public static ref bool ReadThroughWhatAreYouDoing => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhatAreYouDoing;
        public static ref bool ReadThroughWhyHere => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhyHere;
        public static ref bool ReadThroughWhatNowPreDefeat => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhatNowPreDefeat;
        public static ref bool ReadThroughWhatReallyHappened => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhatReallyHappened;
        public static ref bool ReadThroughWhatHappenedToYou => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhatHappenedToYou;
        public static ref bool ReadThroughHowDidYouKeepYourSanity => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughHowDidYouKeepYourSanity;
        public static ref bool ReadThroughWhatNowPostDefeat => ref Main.LocalPlayer.Fables().nautilusDialogueFlags.ReadThroughWhatNowPostDefeat;


        public static bool LastFightNohitted;
        public static bool PlayerWonLastDuel;
        public static float LastFightDuration = float.MaxValue;
        public static float PlayerKilledNautilusTimer;
        public static float PlayerKilledByNautilusTimer;
        public static float LastSawNautilusTimer;

        public const float JustDefeatedNautilusDialogueTimeframe = 5f * 60f * 60f; //5 minutes pass before he forgets to mention he just got beaten
        public const float HelloAgainNautilusDialogueTimeframe = 20f * 60f * 60f; //20 minutes pass before he forgets to mention he already saw you
        public const float NautilusBeatPlayerDialogueTimeframe = 30f * 60f * 60f; //30 minutes pass before he forgets to mention he just beat your ass
        public const float UnfairFightDurationTreshold = 20f * 60f; //If he's beaten faster than 10 seconds he will complain


        public static void LogDuelStart()
        {
            PlayerWonLastDuel = false;
            LastFightNohitted = true;
            LastFightDuration = 0f;
        }

        public static void RegisterNautilusDefeat()
        {
            DefeatedNautilus = true;
            PlayerWonLastDuel = true;
            PlayerKilledNautilusTimer = JustDefeatedNautilusDialogueTimeframe;
        }

        internal static void ResetAllFlags()
        {
            DefeatedNautilus = false;
            PlayerWonLastDuel = false;
            LastFightNohitted = false;
            LastFightDuration = float.MaxValue;
            PlayerKilledByNautilusTimer = 0f;
            PlayerKilledNautilusTimer = 0f;
            LastSawNautilusTimer = 0f;
        }

        public override void ClearWorld() => ResetAllFlags();


        public override void SaveWorldData(TagCompound tag)
        {
            tag["defeatedNautilus"] = DefeatedNautilus;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (CalamityFables.NautilusDemo) //HEEHEEHO
                return;

            DefeatedNautilus = tag.GetOrDefault<bool>("defeatedNautilus");
        }

        private void SavePlayerData(FablesPlayer player, TagCompound tag)
        {
            TagCompound nautilusDialogueFlags = new TagCompound();

            nautilusDialogueFlags["challengedNautilus"] = player.nautilusDialogueFlags.ChallengedNautilusOnce;
            nautilusDialogueFlags["WokeNautilusUp"] = player.nautilusDialogueFlags.WokeNautilusUp;
            nautilusDialogueFlags["alreadyGotIntroducedToNautilus"] = player.nautilusDialogueFlags.ReadThroughTheFirstRundown;
            nautilusDialogueFlags["readWhoAreYou"] = player.nautilusDialogueFlags.ReadThroughWhoAreYou;
            nautilusDialogueFlags["readWhatAreYouDoing"] = player.nautilusDialogueFlags.ReadThroughWhatAreYouDoing;
            nautilusDialogueFlags["readWhyHere"] = player.nautilusDialogueFlags.ReadThroughWhyHere;
            nautilusDialogueFlags["readWhatNowPreDefeat"] = player.nautilusDialogueFlags.ReadThroughWhatNowPreDefeat;
            nautilusDialogueFlags["readWhatReallyHappened"] = player.nautilusDialogueFlags.ReadThroughWhatReallyHappened;
            nautilusDialogueFlags["readWhatHappenedToYou"] = player.nautilusDialogueFlags.ReadThroughWhatHappenedToYou;
            nautilusDialogueFlags["readHowDidYouKeepYourSanity"] = player.nautilusDialogueFlags.ReadThroughHowDidYouKeepYourSanity;
            nautilusDialogueFlags["readWhatNowPostDefeat"] = player.nautilusDialogueFlags.ReadThroughWhatNowPostDefeat;
            nautilusDialogueFlags["readThroughDSTopic"] = player.nautilusDialogueFlags.ReadThroughDesertScourgeTopic;
            nautilusDialogueFlags["readThroughHMTopic"] = player.nautilusDialogueFlags.ReadThroughHardmodeTopic;
            nautilusDialogueFlags["readThroughDoppelganger"] = player.nautilusDialogueFlags.ReadThroughDoppelgangerEasterEgg;

            tag["nautilusDialogueFlags"] = nautilusDialogueFlags;
        }

        private void LoadPlayerData(FablesPlayer player, TagCompound tag)
        {
            if (CalamityFables.NautilusDemo) //HEEHEEHO
                return;

            player.nautilusDialogueFlags = new();
            if (!tag.ContainsKey("nautilusDialogueFlags"))
                return ;
            tag = tag.GetCompound("nautilusDialogueFlags");

            player.nautilusDialogueFlags.ChallengedNautilusOnce = tag.GetOrDefault<bool>("challengedNautilus");
            player.nautilusDialogueFlags.WokeNautilusUp = tag.GetOrDefault<bool>("WokeNautilusUp");
            player.nautilusDialogueFlags.ReadThroughTheFirstRundown = tag.GetOrDefault<bool>("alreadyGotIntroducedToNautilus");
            player.nautilusDialogueFlags.ReadThroughWhoAreYou = tag.GetOrDefault<bool>("readWhoAreYou");
            player.nautilusDialogueFlags.ReadThroughWhatAreYouDoing = tag.GetOrDefault<bool>("readWhatAreYouDoing");
            player.nautilusDialogueFlags.ReadThroughWhyHere = tag.GetOrDefault<bool>("readWhyHere");
            player.nautilusDialogueFlags.ReadThroughWhatNowPreDefeat = tag.GetOrDefault<bool>("readWhatNowPreDefeat");
            player.nautilusDialogueFlags.ReadThroughWhatReallyHappened = tag.GetOrDefault<bool>("readWhatReallyHappened");
            player.nautilusDialogueFlags.ReadThroughWhatHappenedToYou = tag.GetOrDefault<bool>("readWhatHappenedToYou");
            player.nautilusDialogueFlags.ReadThroughHowDidYouKeepYourSanity = tag.GetOrDefault<bool>("readHowDidYouKeepYourSanity");
            player.nautilusDialogueFlags.ReadThroughWhatNowPostDefeat = tag.GetOrDefault<bool>("readWhatNowPostDefeat");

            player.nautilusDialogueFlags.ReadThroughDesertScourgeTopic = tag.GetOrDefault<bool>("readThroughDSTopic");
            player.nautilusDialogueFlags.ReadThroughHardmodeTopic = tag.GetOrDefault<bool>("readThroughHMTopic");
            player.nautilusDialogueFlags.ReadThroughDoppelgangerEasterEgg = tag.GetOrDefault<bool>("readThroughDoppelganger");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(DefeatedNautilus);
        }

        public override void NetReceive(BinaryReader reader)
        {
            DefeatedNautilus = reader.ReadBoolean();
        }
        #endregion
    }

    [Serializable]
    public class SirNautilusSummonPacket : Module
    {
        int spawnerNPC;

        public SirNautilusSummonPacket(NPC anchor)
        {
            spawnerNPC = anchor.whoAmI;
        }

        protected override void Receive()
        {
            if (NPC.AnyNPCs(NPCType<SirNautilus>()))
                return;

            NPC anchor = Main.npc[spawnerNPC];
            int nautie = NPC.NewNPC(anchor.GetSource_FromThis(), (int)anchor.Center.X, (int)anchor.Center.Y + 32, NPCType<SirNautilus>());
            if (nautie < 200)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nautie);
        }
    }
}


namespace CalamityFables.Core
{
    public partial class FablesPlayer : ModPlayer
    {
        public NautilusDialogueData nautilusDialogueFlags = new();

        /// <summary>
        /// Holder class for all the data related to nautilus's dialogue
        /// </summary>
        public class NautilusDialogueData
        {

            public bool ChallengedNautilusOnce;
            public bool ReadThroughDesertScourgeTopic;
            public bool ReadThroughHardmodeTopic;
            public bool ReadThroughDoppelgangerEasterEgg;
            public bool WokeNautilusUp;
            public bool ReadThroughTheFirstRundown;
            public bool ReadThroughWhoAreYou;
            public bool ReadThroughWhatAreYouDoing;
            public bool ReadThroughWhyHere;
            public bool ReadThroughWhatNowPreDefeat;
            public bool ReadThroughWhatReallyHappened;
            public bool ReadThroughWhatHappenedToYou;
            public bool ReadThroughHowDidYouKeepYourSanity;
            public bool ReadThroughWhatNowPostDefeat;
        }
    }
}
