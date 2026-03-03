using NetEasy;
using Terraria.GameInput;

namespace CalamityFables.Core
{
    public static class MouseSyncPlayerExtensions
    {
        public static void SyncRightClick(this Player player) => player.GetModPlayer<MouseSyncingPlayer>().rightClickListener = true;
        public static void SyncMousePosition(this Player player) => player.GetModPlayer<MouseSyncingPlayer>().mouseWorldListener = true;
        public static void SyncMouseRotation(this Player player) => player.GetModPlayer<MouseSyncingPlayer>().mouseRotationListener = true;

        public static bool RightClicking(this Player player) => player.GetModPlayer<MouseSyncingPlayer>().mouseRight;
        public static Vector2 MouseWorld(this Player player) => player.GetModPlayer<MouseSyncingPlayer>().mouseWorld;
    }

    public class MouseSyncingPlayer : ModPlayer
    {
        public bool mouseRight = false;
        private bool oldMouseRight = false;
        public Vector2 mouseWorld;
        private Vector2 oldMouseWorld;

        /// <summary>
        /// Set this to true if you need to recieve updates on right clicks from players and sync them in mp.
        /// Automatically resets itself after sending an update
        /// <\summary>
        public bool rightClickListener = false;
        /// <summary>
        /// Set this to true if you need to recieve updates on the position of the player's mouse and sync them in mp.
        /// Automatically resets itself after sending an update
        /// <\summary>
        public bool mouseWorldListener = false;
        /// <summary>
        /// Set this to true if you need to recieve updates on the rotation of the mouse to the player. This sends updates less frequently than the more tight tolerance mouseWorldListener
        /// Automatically resets itself after sending an update
        /// <\summary>
        public bool mouseRotationListener = false;

        /// <summary>
        /// Set this to true when something wants to send controls
        /// sets itself to false after one send
        /// </summary>
        public bool sendControls = false;

        public override void PreUpdate()
        {
            if (Main.myPlayer == Player.whoAmI)
            {
                mouseRight = PlayerInput.Triggers.Current.MouseRight;
                mouseWorld = Main.MouseWorld;

                if (rightClickListener && mouseRight != oldMouseRight)
                {
                    oldMouseRight = mouseRight;
                    sendControls = true;
                    rightClickListener = false;
                }

                if (mouseWorldListener && Vector2.Distance(mouseWorld, oldMouseWorld) > 10f)
                {
                    oldMouseWorld = mouseWorld;
                    sendControls = true;
                    mouseWorldListener = false;
                }

                if (mouseRotationListener && Math.Abs((mouseWorld - Player.MountedCenter).ToRotation() - (oldMouseWorld - Player.MountedCenter).ToRotation()) > 0.15f)
                {
                    oldMouseWorld = mouseWorld;
                    sendControls = true;
                    mouseRotationListener = false;
                }

                if (sendControls)
                {
                    sendControls = false;
                    ControlsPacket packet = new ControlsPacket(this);
                    packet.Send(-1, Player.whoAmI, false);
                }

            }
        }
    }


    [Serializable]
    public class ControlsPacket : Module
    {
        public readonly byte whoAmI;
        public readonly byte controls;
        public readonly short xDist;
        public readonly short yDist;

        public ControlsPacket(MouseSyncingPlayer cPlayer)
        {
            whoAmI = (byte)cPlayer.Player.whoAmI;

            if (cPlayer.mouseRight) controls |= 0b10000000;

            xDist = (short)(cPlayer.mouseWorld.X - cPlayer.Player.position.X);
            yDist = (short)(cPlayer.mouseWorld.Y - cPlayer.Player.position.Y);

        }

        protected override void Receive()
        {
            MouseSyncingPlayer Player = Main.player[whoAmI].GetModPlayer<MouseSyncingPlayer>();
            if ((controls & 0b10000000) == 0b10000000)
                Player.mouseRight = true;
            else
                Player.mouseRight = false;

            Player.mouseWorld = new Vector2(xDist + Player.Player.position.X, yDist + Player.Player.position.Y);

            if (Main.netMode == Terraria.ID.NetmodeID.Server)
            {
                Send(-1, Player.Player.whoAmI, false);
                return;
            }
        }
    }
}

