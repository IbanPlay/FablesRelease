namespace CalamityFables.Content.Tiles
{
    public static class TileDirections
    {
        public enum TileDirection : byte
        {
            Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight, Center
        }

        public enum TileAlignment : byte
        {
            Vertical, Horizontal, Single
        }

        //Broad angles collections to make some of the condition checks less fat
        public static readonly TileDirection?[] BroadTopLeft = new TileDirection?[3] { TileDirection.Left, TileDirection.UpLeft, TileDirection.Up };
        public static readonly TileDirection?[] BroadTopRight = new TileDirection?[3] { TileDirection.Right, TileDirection.UpRight, TileDirection.Up };
        public static readonly TileDirection?[] BroadBottomLeft = new TileDirection?[3] { TileDirection.Left, TileDirection.DownLeft, TileDirection.Down };
        public static readonly TileDirection?[] BroadBottomRight = new TileDirection?[3] { TileDirection.Right, TileDirection.DownRight, TileDirection.Down };

        public static readonly TileDirection?[] BroadLeft = new TileDirection?[3] { TileDirection.Left, TileDirection.UpLeft, TileDirection.DownLeft };
        public static readonly TileDirection?[] BroadRight = new TileDirection?[3] { TileDirection.Right, TileDirection.UpRight, TileDirection.DownRight };
        public static readonly TileDirection?[] BroadTop = new TileDirection?[3] { TileDirection.UpLeft, TileDirection.Up, TileDirection.UpRight };
        public static readonly TileDirection?[] BroadBottom = new TileDirection?[3] { TileDirection.DownRight, TileDirection.Down, TileDirection.DownLeft };

        // There is a chungus among us
        // This is based largely on characteristics exhibited by vanilla's tile sheets.
        public static readonly TileDirection?[,] WhereDoIPoint = new TileDirection?[,]
        {
            { TileDirection.Left, TileDirection.Up, TileDirection.Up, TileDirection.Up, TileDirection.Right, null, null, null, null, null, TileDirection.Left, TileDirection.Right, null },
            { TileDirection.Left, TileDirection.Center, TileDirection.Center, TileDirection.Center, TileDirection.Right, null, TileDirection.Up, TileDirection.Up, TileDirection.Up, null, TileDirection.Left, TileDirection.Right, null },
            { TileDirection.Left, TileDirection.Down, TileDirection.Down, TileDirection.Down, TileDirection.Right, null, TileDirection.Down, TileDirection.Down, TileDirection.Down, null, TileDirection.Left, TileDirection.Right, null },
            { TileDirection.UpLeft, TileDirection.UpRight, TileDirection.UpLeft, TileDirection.UpRight, TileDirection.UpLeft, TileDirection.UpRight, null, null, null, null, null, null, null },
            { TileDirection.DownLeft, TileDirection.DownRight, TileDirection.DownLeft, TileDirection.DownRight, TileDirection.DownLeft, TileDirection.DownRight, null, null, null, null, null, null, null }
        };

        public static readonly int?[,] WhatVariantAmI = new int?[,]
        {
            { 1, 1, 2, 3, 1, 1, 1, 2, 3, 1, 1, 1, 1 },
            { 2, 1, 2, 3, 2, 2, 1, 2, 3, 2, 2, 2, 2 },
            { 3, 1, 2, 3, 3, 3, 1, 2, 3, 3, 3, 3, 3 },
            { 1, 1, 2, 2, 3, 3, 1, 2, 3, 1, 2, 3, null },
            { 1, 1, 2, 2, 3, 3, 1, 2, 3, null, null, null, null }
        };

        public static readonly TileAlignment?[,] HowAmIAligned = new TileAlignment?[,]
        {
            { null, null, null, null, null, TileAlignment.Vertical, TileAlignment.Vertical, TileAlignment.Vertical, TileAlignment.Vertical, TileAlignment.Horizontal, null, null, TileAlignment.Horizontal },
            { null, null, null, null, null,  TileAlignment.Vertical, null, null, null, TileAlignment.Horizontal, null, null, TileAlignment.Horizontal },
            { null, null, null, null, null,  TileAlignment.Vertical, null, null, null, TileAlignment.Horizontal, null, null, TileAlignment.Horizontal },
            { null, null, null, null, null, null, TileAlignment.Vertical, TileAlignment.Vertical, TileAlignment.Vertical, TileAlignment.Single,  TileAlignment.Single,  TileAlignment.Single, null },
            { null, null, null, null, null, null, TileAlignment.Horizontal, TileAlignment.Horizontal, TileAlignment.Horizontal, null, null, null, null }
        };

        public static readonly TileDirection?[,] ExtremityDirections = new TileDirection?[,]
        {
            { null, null, null, null, null, null, TileDirection.Up, TileDirection.Up, TileDirection.Up, TileDirection.Left, null, null, TileDirection.Right },
            { null, null, null, null, null, null, null, null, null, TileDirection.Left, null, null, TileDirection.Right  },
            { null, null, null, null, null,  null, null, null, null, TileDirection.Left, null, null, TileDirection.Right  },
            { null, null, null, null, null, null, TileDirection.Down, TileDirection.Down, TileDirection.Down, null, null, null, null },
            { null, null, null, null, null, null, null, null, null, null, null, null, null }
        };

        public static TileDirection? GiveDirection(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            int slotY = tile.TileFrameX / 18;
            int slotX = tile.TileFrameY / 18;
            //Just to be safe
            if (slotX >= WhereDoIPoint.GetLength(0) || slotY >= WhereDoIPoint.GetLength(1))
                return null;

            return WhereDoIPoint[slotX, slotY];
        }

        public static int GiveVariant(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            int slotY = tile.TileFrameX / 18;
            int slotX = tile.TileFrameY / 18;

            //Just to be safe
            if (slotX >= WhereDoIPoint.GetLength(0) || slotY >= WhereDoIPoint.GetLength(1))
                return 1;

            return (int)WhatVariantAmI[slotX, slotY];
        }
        public static TileAlignment? GiveAlignment(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            int slotY = tile.TileFrameX / 18;
            int slotX = tile.TileFrameY / 18;
            //Just to be safe
            if (slotX >= HowAmIAligned.GetLength(0) || slotY >= HowAmIAligned.GetLength(1))
                return null;

            return HowAmIAligned[slotX, slotY];
        }

        public static TileDirection? GiveExtremityAlignment(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            int slotY = tile.TileFrameX / 18;
            int slotX = tile.TileFrameY / 18;
            //Just to be safe
            if (slotX >= HowAmIAligned.GetLength(0) || slotY >= HowAmIAligned.GetLength(1))
                return null;

            return ExtremityDirections[slotX, slotY];
        }

    }
}