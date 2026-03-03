using Terraria.DataStructures;

namespace CalamityFables.Core
{
    public class FablesWall : GlobalWall
    {

        public delegate void KillWallDelegate(int i, int j, int type, ref bool fail);
        public static event KillWallDelegate KillWallEvent;
        public override void KillWall(int i, int j, int type, ref bool fail)
        {
            KillWallEvent?.Invoke(i, j, type, ref fail);
        }

        public delegate bool WallBoolDelegate(int i, int j, int type);
        public static event WallBoolDelegate CanExplodeEvent;
        public override bool CanExplode(int i, int j, int type)
        {
            if (CanExplodeEvent!= null)
            {
                foreach (WallBoolDelegate hook in CanExplodeEvent.GetInvocationList())
                    if (!hook(i, j, type))
                        return false;
            }

            return true;
        }


        public static event WallBoolDelegate CanPlaceEvent;
        public override bool CanPlace(int i, int j, int type)
        {
            if (CanExplodeEvent != null)
            {
                foreach (WallBoolDelegate hook in CanPlaceEvent.GetInvocationList())
                    if (!hook(i, j, type))
                        return false;
            }

            return true;
        }
    }
}
