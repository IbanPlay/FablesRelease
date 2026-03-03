using CalamityFables.Content.Items.SirNautilusDrops;
using System.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace CalamityFables.Core
{
    public class PointOfInterestMarkerSystem : ModSystem
    {
        public static bool foundNautilusChamber = false;
        public static Vector2 NautilusChamberPos;

        private static Point _wulfrumBunkerPos;
        public static Point WulfrumBunkerPos
        {
            get => _wulfrumBunkerPos;
            set
            {
                _bunkerWallRect = null;
                _bunkerRect = null;
                _wulfrumBunkerPos = value;
            }
        }

        //Nautie chamber dimensions in tiles : 74 x 32 (inner dimensions)
        public static Rectangle NautilusChamberRectangle => new Rectangle((int)(NautilusChamberPos.X - SealedChamber.ChamberInnerSize.X * 0.5f), (int)(NautilusChamberPos.Y - 26), (int)SealedChamber.ChamberInnerSize.X, (int)SealedChamber.ChamberInnerSize.Y);
        public static Rectangle NautilusChamberWorldRectangle => new Rectangle((int)(NautilusChamberPos.X - SealedChamber.ChamberInnerSize.X * 0.5f) * 16, (int)(NautilusChamberPos.Y - 26) * 16, (int)SealedChamber.ChamberInnerSize.X * 16, (int)SealedChamber.ChamberInnerSize.Y * 16);
        //Eventually, could include the in-world elemental stones, the brimmy statue, the community markers & the community itself..
        //no way we're ading the community lmfao what was he cooking?

        private static Rectangle? _bunkerRect;
        public static Rectangle WulfrumBunkerRectangle
        {
            get
            {
                if (_bunkerRect.HasValue)
                    return _bunkerRect.Value;

                _bunkerRect = new Rectangle(WulfrumBunkerPos.X - WulfrumScrapyard.BunkerSize.X / 2, WulfrumBunkerPos.Y - 40, WulfrumScrapyard.BunkerSize.X + 1, WulfrumScrapyard.BunkerSize.Y + 3);
                return _bunkerRect.Value;
            }
        }

        private static Rectangle? _bunkerWallRect;
        public static Rectangle WulfrumBunkerWallProtectionRectangle
        {
            get
            {
                if (_bunkerWallRect.HasValue && false)
                    return _bunkerWallRect.Value;

                _bunkerWallRect = new Rectangle(WulfrumBunkerPos.X - 18, WulfrumBunkerPos.Y - 20, 18 * 2 + 1, 20);
                return _bunkerWallRect.Value;
            }
        }


        public override void PreUpdatePlayers()
        {
            if (!foundNautilusChamber && Main.LocalPlayer.Distance(NautilusChamberPos * 16f) < 830)
            {
                foundNautilusChamber = true;
                //Syncs it to the server and other players
                new SyncSealedChamberDiscoveryPacket().Send();
            }
        }

        internal static void ResetAllFlags()
        {
            foundNautilusChamber = false;
            NautilusChamberPos = Vector2.Zero;
            WulfrumBunkerPos = Point.Zero;
        }

        public override void ClearWorld() => ResetAllFlags();
        //public override void OnWorldLoad() => ResetAllFlags();
        //public override void OnWorldUnload() => ResetAllFlags();

        public override void SaveWorldData(TagCompound tag)
        {
            tag["nautilusHideoutFound"] = foundNautilusChamber;
            tag["nautilusChamberPos"] = NautilusChamberPos;
            tag["wulfrumBunkerPos"] = WulfrumBunkerPos;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            foundNautilusChamber = tag.GetBool("nautilusHideoutFound");
            NautilusChamberPos = tag.Get<Vector2>("nautilusChamberPos");
            WulfrumBunkerPos = tag.Get<Point>("wulfrumBunkerPos");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(foundNautilusChamber);
            writer.WriteVector2(NautilusChamberPos);
            writer.Write(WulfrumBunkerPos.X);
            writer.Write(WulfrumBunkerPos.Y);
        }

        public override void NetReceive(BinaryReader reader)
        {
            foundNautilusChamber = reader.ReadBoolean();
            NautilusChamberPos = reader.ReadVector2();
            WulfrumBunkerPos = new Point(reader.ReadInt32(), reader.ReadInt32());
        }
    }

    public class NautilusChamberMapIcon : ModMapLayer
    {
        public static Asset<Texture2D> MapIcon;
        public static bool MouseOver = false;

        public static LocalizedText sealedChamberLabel;
        public override void Load()
        {
            sealedChamberLabel = Mod.GetLocalization("Extras.MapIcons.SealedChamber");
        }


        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (!PointOfInterestMarkerSystem.foundNautilusChamber || PointOfInterestMarkerSystem.NautilusChamberPos == Vector2.Zero)
                return;

            if (MapIcon == null)
                MapIcon = ModContent.Request<Texture2D>(AssetDirectory.UI + "UndergroundDesertGrave");
            Texture2D icon = MapIcon.Value;

            bool hasRecallPot = false;
            bool hasUnityPot = false;

            if (Main.LocalPlayer.HasItem(ItemID.RecallPotion))
                hasRecallPot = true;
            else if (Main.LocalPlayer.HasItem(ItemID.WormholePotion))
                hasUnityPot = true;

            // Here we define the scale that we wish to draw the icon when hovered and not hovered.
            float scaleIfNotSelected = 1f;
            float scaleIfSelected = (hasRecallPot || hasUnityPot) ? 1.15f : 1f;

            if (context.Draw(icon, PointOfInterestMarkerSystem.NautilusChamberPos, Color.White, new(1, 1, 0, 0), scaleIfNotSelected, scaleIfSelected, Alignment.Center).IsMouseOver)
            {
                text = sealedChamberLabel.Value; //Holy fucking shi     Did you know that the witch Jenka once had a brother? 

                if (hasRecallPot || hasUnityPot)
                {
                    if (!MouseOver)
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        MouseOver = true;
                    }


                    text = Language.GetTextValue("Game.TeleportTo", sealedChamberLabel.Value);
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        Main.mouseLeftRelease = false;
                        Main.mapFullscreen = false;

                        int consumedPotionID = hasRecallPot ? ItemID.RecallPotion : ItemID.WormholePotion;
                        Main.LocalPlayer.CustomTeleport(PointOfInterestMarkerSystem.NautilusChamberPos * 16f, WarriorsAmphora.TELEPORT_STYLE);
                        Main.LocalPlayer.ConsumeItem(consumedPotionID);
                    }
                }
            }

            else
                MouseOver = false;
        }
    }


    [Serializable]
    public class SyncSealedChamberDiscoveryPacket : Module
    {
        protected override void Receive()
        {
            PointOfInterestMarkerSystem.foundNautilusChamber = true;
            if (Main.netMode == NetmodeID.Server)
                Send(-1, -1, false);
        }
    }
}
