using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public partial class FablesPlayer : ModPlayer
    {
        public NPC riddenPlatform;

        public void DetachFromPlatform()
        {
            if (riddenPlatform != null)
            {
                IMovingSurface plat = (riddenPlatform.ModNPC as IMovingSurface);
                plat.RidingPlayers.Remove(Player);
                riddenPlatform = null;
            }
        }

        public void AttachToPlatform(NPC platform)
        {
            riddenPlatform = platform;
            IMovingSurface plat = (riddenPlatform.ModNPC as IMovingSurface);
            plat.RidingPlayers.Add(Player);

            if (riddenPlatform.velocity.Y != 0)
            {
                highestPlatformYvelocity = riddenPlatform.velocity.Y;
                //Keep a buffer of the highest Y velocity
                platformMomentumCarryBuffer = 6;
            }

        }

        public float platformJumpMomentumShare = 0f;
        public float highestPlatformYvelocity = 0f;
        public int platformMomentumCarryBuffer = 0;
        
        public void DetachFromPlatformOnJump(Player player)
        {
            FablesPlayer modPlayer = player.Fables();
            if (player.justJumped && modPlayer.riddenPlatform != null)
            {
                if (modPlayer.riddenPlatform.velocity.Y < 0 || modPlayer.highestPlatformYvelocity < 0)
                {
                    float transferredVelocity = Math.Min(modPlayer.highestPlatformYvelocity, modPlayer.riddenPlatform.velocity.Y);
                    transferredVelocity = Math.Max(-12f, transferredVelocity);

                    player.velocity.Y += transferredVelocity * 1.2f;
                    modPlayer.platformJumpMomentumShare = transferredVelocity * 1.2f;
                    modPlayer.platformMomentumCarryBuffer = 0;
                    player.jump = 3;
                }
                modPlayer.DetachFromPlatform();
            }
        }
    }
}