using static CalamityFables.Content.Tiles.TileDirections;

namespace CalamityFables.Content.Tiles.BaseTypes
{
    public abstract class DoubleDirectionalTile : ModTile
    {
        public virtual bool TransitionHardRequired => false;

        public TileDirection? GiveDirection(int typeToConnectTo, int i, int j)
        {
            Tile tile = Main.tile[i, j];

            // We enforce racism here.
            if (tile.TileType != typeToConnectTo)
                return null;

            return TileDirections.GiveDirection(i, j);
        }


        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            if (GiveDirection(type, i, j) != TileDirection.Center)
                return;

            TileDirection? TopLeft = GiveDirection(type, i - 1, j - 1);
            TileDirection? Top = GiveDirection(type, i, j - 1);
            TileDirection? TopRight = GiveDirection(type, i + 1, j - 1);
            TileDirection? Left = GiveDirection(type, i - 1, j);
            TileDirection? Right = GiveDirection(type, i + 1, j);
            TileDirection? BottomLeft = GiveDirection(type, i - 1, j + 1);
            TileDirection? Bottom = GiveDirection(type, i, j + 1);
            TileDirection? BottomRight = GiveDirection(type, i + 1, j + 1);

            //Shitty if chain incoming :| Wonder if thats doable with a switch statement, i just dont know how.
            if (Top == TileDirection.Up && Bottom == TileDirection.Center) //Up
            {
                frameXOffset = -18 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (Top == TileDirection.Center && Bottom == TileDirection.Down) //Down
            {
                frameXOffset = -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (Left == TileDirection.Left && Right == TileDirection.Center) //Left
            {
                frameXOffset = 18 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (Left == TileDirection.Center && Right == TileDirection.Right) //Right
            {
                frameXOffset = 36 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if ((Top == TileDirection.Up && Left == TileDirection.Left && Bottom == TileDirection.Center && Right == TileDirection.Center) ||
                ((Top == TileDirection.Left || Top == TileDirection.UpLeft) && (Left == TileDirection.Up || Left == TileDirection.UpLeft) && (BottomRight == TileDirection.Center || BroadBottomRight.Contains(BottomRight)))) //Top left
            {
                frameXOffset = 54 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if ((Top == TileDirection.Up && Right == TileDirection.Right && Bottom == TileDirection.Center && Left == TileDirection.Center) ||
               ((Top == TileDirection.Right || Top == TileDirection.UpRight) && (Right == TileDirection.Up || Right == TileDirection.UpRight) && (BottomLeft == TileDirection.Center || BroadBottomLeft.Contains(BottomLeft)))) //Top right
            {
                frameXOffset = 72 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if ((Bottom == TileDirection.Down && Left == TileDirection.Left && Top == TileDirection.Center && Right == TileDirection.Center) ||
               ((Bottom == TileDirection.Left || Bottom == TileDirection.DownLeft) && (Left == TileDirection.Down || Left == TileDirection.DownLeft) && (TopRight == TileDirection.Center || BroadTopRight.Contains(TopRight)))) //Bottom left
            {
                frameXOffset = 90 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if ((Bottom == TileDirection.Down && Right == TileDirection.Right && Top == TileDirection.Center && Left == TileDirection.Center) ||
               ((Bottom == TileDirection.Right || Bottom == TileDirection.DownRight) && (Right == TileDirection.Down || Right == TileDirection.DownRight) && (TopLeft == TileDirection.Center || BroadTopLeft.Contains(TopLeft)))) //Bottom right
            {
                frameXOffset = 108 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }

            //If we don't absolutely need the transition layer to exist, we cut out anything else extra
            if (!TransitionHardRequired)
                return;

            if (BroadLeft.Contains(Left) && BroadRight.Contains(Right) && Top == TileDirection.Center && Bottom == TileDirection.Center) //Vertical thin line
            {
                frameXOffset = -18;
                frameYOffset = 126;
            }
            if (BroadTop.Contains(Top) && BroadBottom.Contains(Bottom) && Left == TileDirection.Center && Right == TileDirection.Center) //Horizontal thin line
            {
                frameXOffset = -18;
                frameYOffset = 144;
            }
            if (BroadTop.Contains(Top) && BroadBottom.Contains(Bottom) && BroadLeft.Contains(Left) && Right == TileDirection.Center) //Thin left end
            {
                frameXOffset = 126 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (BroadTop.Contains(Top) && BroadBottom.Contains(Bottom) && BroadRight.Contains(Right) && Left == TileDirection.Center) //Thin right end
            {
                frameXOffset = 144 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (BroadLeft.Contains(Left) && BroadRight.Contains(Right) && BroadTop.Contains(Top) && Bottom == TileDirection.Center) //Thin top end
            {
                frameXOffset = 162 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (BroadLeft.Contains(Left) && BroadRight.Contains(Right) && BroadBottom.Contains(Bottom) && Top == TileDirection.Center) //Thin bottom end
            {
                frameXOffset = 180 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }
            if (Left != TileDirection.Center && Right != TileDirection.Center && Top != TileDirection.Center && Bottom != TileDirection.Center) //Center
            {
                frameXOffset = 198 + -18 * (GiveVariant(i, j) - 1);
                frameYOffset = -18 + 90 + 18 * (GiveVariant(i, j) - 1);
            }

        }
    }
}
