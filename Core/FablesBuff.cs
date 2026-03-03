using MonoMod.Cil;
using Terraria.DataStructures;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public class FablesBuff : GlobalBuff
    {
        public delegate void PostDrawBuffDelegate(SpriteBatch spriteBatch, int type, int buffIndex, BuffDrawParams drawParams);
        public static event PostDrawBuffDelegate PostDrawBuffEvent;

        public override void PostDraw(SpriteBatch spriteBatch, int type, int buffIndex, BuffDrawParams drawParams)
        {
            PostDrawBuffEvent?.Invoke(spriteBatch, type, buffIndex, drawParams);
        }

        public delegate bool PreDrawBuffDelegate(SpriteBatch spriteBatch, int type, int buffIndex, ref BuffDrawParams drawParams);
        public static event PreDrawBuffDelegate PreDrawBuffEvent;
        public override bool PreDraw(SpriteBatch spriteBatch, int type, int buffIndex, ref BuffDrawParams drawParams)
        {
            if (PreDrawBuffEvent == null)
                return true;

            foreach (PreDrawBuffDelegate method in PreDrawBuffEvent.GetInvocationList())
                if (!method(spriteBatch, type, buffIndex, ref drawParams))
                    return false;

            return true;
        }

    }
}


