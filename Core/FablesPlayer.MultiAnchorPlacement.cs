using MonoMod.Cil;
using Terraria.DataStructures;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public partial class FablesPlayer : ModPlayer
    {
        /// <summary>
        /// The item that is currently being used for multi-anchor placement
        /// </summary>
        public Item multiAnchorItem;
        /// <summary>
        /// 
        /// </summary>
        public int multiAnchorPlaceCount;

        public List<Point> multiAnchorPlacePoints = new();

        public bool justPlacedMultiAnchorItem = false;

        private void DrawMultiAnchorPlacementPreview()
        {
            FablesPlayer mp = Main.LocalPlayer.Fables();
            if (mp.multiAnchorItem != null && mp.multiAnchorItem.ModItem != null && mp.multiAnchorItem.ModItem is IMultiAnchorPlaceable multiAnchor && mp.multiAnchorPlaceCount > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                try
                {
                    multiAnchor.DrawPreview(mp.multiAnchorPlaceCount, mp.multiAnchorPlacePoints);
                }
                catch (Exception e)
                {
                    if (!Main.ignoreErrors)
                        throw;
                    TimeLogger.DrawException(e);
                }
                Main.spriteBatch.End();
            }
        }
    }

    /// <summary>
    /// An item that places a tile/tileentity/other object, and which attaches to more than 1 tile <br/>
    /// Use <see cref="FablesUtils.MultiAnchorPlace(Player, Item, Point)"/> in <see cref="ModItem.UseItem(Player)"/>. If it returns true, it means enough anchor points have been selected <br/>
    /// 
    /// </summary>
    public interface IMultiAnchorPlaceable
    {
        //TODO : Make support for min/max anchors
        /// <summary>
        /// How many anchors does the item need before it can be properly placed
        /// </summary>
        public int AnchorCount => 2;

        /// <summary>
        /// Draws the placement preview
        /// </summary>
        public void DrawPreview(int anchorCount, List<Point> existingAnchors);

        /// <summary>
        /// Updates the placement preview when the hovered tile has changed or the player clicks up/down as with the rubblemaker
        /// </summary>
        /// <param name="anchorCount"></param>
        /// <param name="existingAnchors"></param>
        public void UpdatePreview(int anchorCount, List<Point> existingAnchors, bool movedCursor) { }
    }
}

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        public static Point TileTarget(this Player player) => new Point(Player.tileTargetX, Player.tileTargetY);

        /// <summary>
        /// Tries to place a tile using the multi anchor placement item.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        /// <param name="placePos"></param>
        /// <param name="currentAnchors"></param>
        /// <returns>Wether or not enough placement anchors have been used and therefore the tile should get placed</returns>
        public static bool MultiAnchorPlace(this Player player, Item item, Point placePos, out List<Point> currentAnchors)
        {
            var mp = player.GetModPlayer<FablesPlayer>();
            currentAnchors = mp.multiAnchorPlacePoints;

            if (item.ModItem == null || item.ModItem is not IMultiAnchorPlaceable multiAnchorPlace)
                return false;

            //first placement
            if (mp.multiAnchorPlaceCount == 0 || mp.multiAnchorItem == null)
            {
                mp.multiAnchorItem = item;
                mp.multiAnchorPlaceCount = 1;
                mp.multiAnchorPlacePoints.Clear();
            }
            //repeat placements
            else
            {
                //Can't place it where we already placed it
                if (mp.multiAnchorPlacePoints.Contains(placePos))
                    return false;
                mp.multiAnchorPlaceCount++;
            }

            mp.multiAnchorPlacePoints.Add(placePos);
            bool shouldExecutePlacement = mp.multiAnchorPlaceCount >= multiAnchorPlace.AnchorCount;

            //Clear the placement stuff we have, and send over the copied list
            if (shouldExecutePlacement)
            {
                currentAnchors = new List<Point>(currentAnchors);
                mp.multiAnchorPlacePoints.Clear();
                mp.multiAnchorPlaceCount = 0;
                mp.multiAnchorItem = null;
                mp.justPlacedMultiAnchorItem = true;
            }

            multiAnchorPlace.UpdatePreview(mp.multiAnchorPlaceCount, mp.multiAnchorPlacePoints, mp.multiAnchorPlaceCount == 1);
            return shouldExecutePlacement;
                 
        }
    }
}


