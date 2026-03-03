using CalamityFables.Content.UI;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace CalamityFables.Core
{
    public class KeepsakesPlayer : ModPlayer
    {
        public List<string> collectedKeepsakes;
        public bool hasSeenKeepsakeTutorial;
        public bool keepsakeTutorialBeingShown;

        public override void Initialize()
        {
            collectedKeepsakes = new List<string>();
        }

        #region Loading & saving
        public override void SaveData(TagCompound tag)
        {
            TagCompound keepsakeTag = new TagCompound();

            foreach (string keepsake in collectedKeepsakes)
            {
                keepsakeTag.Add(keepsake, true);
            }

            tag["keepsakes"] = keepsakeTag;
            tag["keepsakeTutorialDone"] = hasSeenKeepsakeTutorial;
        }

        public override void LoadData(TagCompound tag)
        {
            if (CalamityFables.NautilusDemo)
                return;

            hasSeenKeepsakeTutorial = tag.GetBool("keepsakeTutorialDone");

            if (!tag.ContainsKey("keepsakes"))
                return;

            TagCompound keepsakesTag = tag.GetCompound("keepsakes");
            var tagIterator = keepsakesTag.GetEnumerator();

            while (tagIterator.MoveNext())
            {
                KeyValuePair<string, object> kv = tagIterator.Current;
                collectedKeepsakes.Add(kv.Key);
            }
        }
        #endregion
    }

    public class KeepsakesSystem : ModSystem
    {
        public override void OnWorldLoad()
        {
            KeepsakeRackUI.NewKeepsakeCheckItOut = false;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            KeepsakesPlayer mp = Main.LocalPlayer.GetModPlayer<KeepsakesPlayer>();

            if (mp.keepsakeTutorialBeingShown && !Main.playerInventory)
                mp.hasSeenKeepsakeTutorial = true;

            bool shouldDoAnything = Main.playerInventory && (KeepsakeRackUI.NewKeepsakeCheckItOut);
            if (!shouldDoAnything)
                return;

            if (Main.EquipPageSelected == 2)
            {
                mp.hasSeenKeepsakeTutorial = true;
                return;
            }

            int maxHeightOfThing = 950;
            int accessoryCount = 8 + Main.LocalPlayer.GetAmountOfExtraAccessorySlotsToShow();
            float yPos = 152 + AutoUISystem.MapHeight;
            if (Main.screenHeight < maxHeightOfThing && accessoryCount >= 10)
            {
                yPos -= (int)(56f * Main.inventoryScale * (float)(accessoryCount - 9));
            }
            Vector2 equipMenuPosition = new Vector2(Main.screenWidth - 67, yPos);

            if (!mp.hasSeenKeepsakeTutorial && KeepsakeRackUI.NewKeepsakeCheckItOut)
            {
                mp.keepsakeTutorialBeingShown = true;

                if (ClueInVignetteUI.opacityMult == 0)
                    ClueInVignetteUI.opacityMult = 0.02f;
                ClueInVignetteUI.inUse = true;
                ClueInVignetteUI.holeSize = Vector2.One * (140f + 700f * (1 - ClueInVignetteUI.opacityMult));
                ClueInVignetteUI.center = equipMenuPosition;
            }

            if (KeepsakeRackUI.NewKeepsakeCheckItOut)
                NewKeepsakeAlertUI.equipmentTabPosition = equipMenuPosition;
        }
    }
}

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        public static List<string> CollectedKeepsakes(this Player p)
        {
            return p.GetModPlayer<KeepsakesPlayer>().collectedKeepsakes;
        }

        public static bool GiveKeepsake(this Player p, string keepsake, Color? popupColor = null, SoundStyle? popupSound = null, bool silent = false)
        {
            if (p.HasKeepsake(keepsake))
                return false;

            KeepsakeRackUI.NewKeepsakeCheckItOut = true;

            AdvancedPopupRequest popup = new AdvancedPopupRequest();
            popup.Text = Language.GetText("Mods.CalamityFables.Keepsakes.ObtentionMessage").Value + Language.GetText("Mods.CalamityFables.Keepsakes.Names." + keepsake).Value + "!";
            popup.Color = popupColor.HasValue ? popupColor.Value : Color.Gold;
            popup.DurationInFrames = 3 * 60;
            popup.Velocity = -Vector2.UnitY * 6f;
            PopupText.NewText(popup, p.Top);

            if (!silent)
                SoundEngine.PlaySound(popupSound.HasValue ? popupSound.Value : KeepsakeRackUI.ObtainNewKeepsakeSound, p.Center);

            p.GetModPlayer<KeepsakesPlayer>().collectedKeepsakes.Add(keepsake);
            return true;
        }

        public static bool RemoveKeepsake(this Player p, string keepsake)
        {
            bool removalSuccessful = p.GetModPlayer<KeepsakesPlayer>().collectedKeepsakes.Remove(keepsake);

            if (removalSuccessful && Main.playerInventory && Main.EquipPageSelected == 2)
                Main.EquipPageSelected = 0;

            return removalSuccessful;
        }

        public static bool HasKeepsake(this Player p, string keepsake)
        {
            return p.GetModPlayer<KeepsakesPlayer>().collectedKeepsakes.Contains(keepsake);
        }
    }
}
