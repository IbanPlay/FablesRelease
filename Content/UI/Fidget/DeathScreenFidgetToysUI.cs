using Terraria.ModLoader.IO;
using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class DeathScreenFidgetToysUI : SmartUIState
    {
        public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        public override InterfaceScaleType Scale {
            get {
                if (SelectedFidgetToy is not FidgetToyUI toy)
                    return InterfaceScaleType.UI;
                return toy.Scale;
            }
        }

        public override bool UpdatesWhileInvisible => false;

        public static FidgetToyUI SelectedFidgetToy;

        public override bool Visible {
            get {
                return Main.LocalPlayer.dead || Main.LocalPlayer.sleeping.isSleeping;
            }
        }

        public override void OnInitialize()
        {
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SelectedFidgetToy?.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            SelectedFidgetToy?.Draw(spriteBatch);
        }

        /*
        return;

        if (!Main.LocalPlayer.controlHook)
            Main.LocalPlayer.respawnTimer = 10 * 60;

        else Main.LocalPlayer.respawnTimer = 0;

        if (DeathScreenFidgetToysUI.FidgetToy is not WulfrumFidgetGrappler)
            DeathScreenFidgetToysUI.FidgetToy = new WulfrumFidgetGrappler();
        */

        public static void SavePlayerData(FablesPlayer player, TagCompound tag)
        {
            TagCompound deathFidgetInfoTag = new TagCompound();

            deathFidgetInfoTag["wulfrumButtonClickCount"] = player.deathFidgetData.wulfrumButtonClickCount;
            deathFidgetInfoTag["unlockedWulfrumButton"] = player.deathFidgetData.unlockedWulfrumButton;
            deathFidgetInfoTag["unlockedWulfrumGear"] = player.deathFidgetData.unlockedWulfrumGear;
            deathFidgetInfoTag["unlockedWulfrumGrappler"] = player.deathFidgetData.unlockedWulfrumGrappler;
            deathFidgetInfoTag["unlockedWulfrumMortar"] = player.deathFidgetData.unlockedWulfrumMortar;
            deathFidgetInfoTag["unlockedWulfrumSwitch"] = player.deathFidgetData.unlockedWulfrumSwitch;
            tag["deathFidgetInfoTag"] = deathFidgetInfoTag;
        }

        public static void LoadPlayerData(FablesPlayer player, TagCompound tag)
        {
            player.deathFidgetData = new();

            if (tag.TryGet("deathFidgetInfoTag", out TagCompound deathFidgetInfoTag))
            {
                player.deathFidgetData.wulfrumButtonClickCount = deathFidgetInfoTag.GetOrDefault<int>("wulfrumButtonClickCount");
                player.deathFidgetData.unlockedWulfrumButton = deathFidgetInfoTag.GetOrDefault<bool>("unlockedWulfrumButton");
                player.deathFidgetData.unlockedWulfrumGear = deathFidgetInfoTag.GetOrDefault<bool>("unlockedWulfrumGear");
                player.deathFidgetData.unlockedWulfrumGrappler = deathFidgetInfoTag.GetOrDefault<bool>("unlockedWulfrumGrappler");
                player.deathFidgetData.unlockedWulfrumMortar = deathFidgetInfoTag.GetOrDefault<bool>("unlockedWulfrumMortar");
                player.deathFidgetData.unlockedWulfrumSwitch = deathFidgetInfoTag.GetOrDefault<bool>("unlockedWulfrumSwitch");
            }
        }
    }

    public abstract class FidgetToyUI : UIElement
    {
        public virtual InterfaceScaleType Scale { get; } = InterfaceScaleType.UI;
    }
}

namespace CalamityFables.Core
{ 
    public partial class FablesPlayer : ModPlayer
    {
        public DeathFidgetData deathFidgetData = new();

        /// <summary>
        /// Holder class for all the data related to nautilus's dialogue
        /// </summary>
        public class DeathFidgetData
        {
            public int wulfrumButtonClickCount;
            public bool unlockedWulfrumButton;
            public bool unlockedWulfrumGear;
            public bool unlockedWulfrumGrappler;
            public bool unlockedWulfrumMortar;
            public bool unlockedWulfrumSwitch;
        }
    }
}
