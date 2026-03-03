using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalamityFables.Core
{
    public static class AltFunctionUseSyncPlayerExtensions
    {
        /// <summary>
        /// Call to request that the player's altFunctionUse field be synced to other clients this frame
        /// </summary>
        /// <param name="player"></param>
        public static void SyncAltFunctionUse(this Player player) => player.GetModPlayer<AltFunctionUseSyncPlayer>().shouldSyncAltFunctionUse = true;
    }

    public class AltFunctionUseSyncPlayer : ModPlayer
    {
        public bool shouldSyncAltFunctionUse;

        public override void PreUpdate()
        {
            if (Main.myPlayer == Player.whoAmI)
            {
                if (shouldSyncAltFunctionUse)
                {
                    shouldSyncAltFunctionUse = false;
                    new AltFunctionUsePacket(this).Send(-1, Player.whoAmI, false);
                }
            }
        }
    }

    [Serializable]
    public class AltFunctionUsePacket : Module
    {
        public readonly byte whoAmI;
        public readonly byte altUse;

        public AltFunctionUsePacket(AltFunctionUseSyncPlayer cPlayer)
        {
            whoAmI = (byte)cPlayer.Player.whoAmI;
            altUse = (byte)cPlayer.Player.altFunctionUse;
        }

        protected override void Receive()
        {
            var player = Main.player[whoAmI];
            player.altFunctionUse = altUse;


            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, player.whoAmI, false);
                return;
            }
        }
    }
}
