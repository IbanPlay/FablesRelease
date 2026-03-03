using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class HeldItemCustomDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.JustDroppedAnItem)
                return false;

            // Check if the held layer should be drawn
            Player player = drawInfo.drawPlayer;
            Item heldItem = drawInfo.heldItem;
            if (!FablesSets.HasCustomHeldDrawing[heldItem.type] || drawInfo.shadow != 0f || drawInfo.drawPlayer.frozen || drawInfo.drawPlayer.dead)
                return false;

            // Get held draw instance. If one cannot be retrieved, stop here.
            if (!GetCustomHeldDraw(heldItem, out ICustomHeldDraw drawInstance))
                return false;

            // Check if the item should be rendered by checking if a use or hold animation is active.
            bool usingItem = player.ItemAnimationActive && heldItem.useStyle != ItemUseStyleID.None;
            bool holdingItem = (drawInstance.HoldOut || heldItem.holdStyle != ItemUseStyleID.None) && player.CanVisuallyHoldItem(heldItem) && !player.pulley;

            return (usingItem || holdingItem) && !(player.wet && heldItem.noWet);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Item heldItem = drawInfo.heldItem;
            int itemType = heldItem.type;

            if (!GetCustomHeldDraw(heldItem, out ICustomHeldDraw customDraw))
                return;

            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Rectangle frame = customDraw.GetDrawFrame(texture, drawInfo);
            Vector2 position = drawInfo.ItemLocation - Main.screenPosition;
            position = position.Floor();
            float itemScale = drawInfo.drawPlayer.GetAdjustedItemScale(heldItem);

            //Bottom corner
            Vector2 origin = new Vector2(frame.Width / 2f * (1 - 1 * drawInfo.drawPlayer.direction), frame.Height);
            if (drawInfo.drawPlayer.gravDir == -1f)
                origin.Y = frame.Height - origin.Y;

            float itemRotation = drawInfo.drawPlayer.itemRotation;
            if (heldItem.useStyle == ItemUseStyleID.Shoot)
            {
                //The -10s here are found from the default return value of DrawPlayerItemPos
                //I have no idea if these negative origin values even matter
                //Besides that, the origin is at the vertical center of it, to teh edge
                origin = new Vector2(-10, frame.Height / 2);
                if (drawInfo.drawPlayer.direction == -1)
                    origin.X = frame.Width + 10; //Frame center

                position.Y += frame.Size().Y / 2f;
            }

            #region Color
            drawInfo.itemColor = Lighting.GetColor((drawInfo.Position + drawInfo.drawPlayer.Size / 2f).ToTileCoordinates());

            // Weird vanilla stealth code
            if (heldItem.CountsAsClass<RangedDamageClass>() && (drawInfo.drawPlayer.shroomiteStealth || drawInfo.drawPlayer.setVortex))
            {
                float stealth = drawInfo.drawPlayer.stealth;
                if (stealth < 0.03f)
                    stealth = 0.03f;

                if (drawInfo.drawPlayer.shroomiteStealth)
                {
                    float stealthBlue = (1f + stealth * 10f) / 11f;
                    drawInfo.itemColor = new Color((byte)((float)(int)drawInfo.itemColor.R * stealth), (byte)((float)(int)drawInfo.itemColor.G * stealth), (byte)((float)(int)drawInfo.itemColor.B * stealthBlue), (byte)((float)(int)drawInfo.itemColor.A * stealth));

                }
                if (drawInfo.drawPlayer.setVortex)
                    drawInfo.itemColor = drawInfo.itemColor.MultiplyRGBA(new Color(Vector4.Lerp(Vector4.One, new Vector4(0f, 0.12f, 0.16f, 0f), 1f - stealth)));
            }
            Color lightColor = heldItem.GetAlpha(drawInfo.itemColor);
            #endregion

            customDraw.DrawHeld(ref drawInfo, texture, position, frame, itemRotation, lightColor, itemScale, origin);
        }

        private static bool GetCustomHeldDraw(Item item, out ICustomHeldDraw value)
        {
            // Check mod item for ICustomHeldDraw
            if (item.ModItem is ICustomHeldDraw customDrawItem)
            {
                value = customDrawItem;
                return true;
            }

            value = null;
            return false;
        }
    }

    public interface ICustomHeldDraw
    {
        /// <summary>
        /// Determines if the item should be drawn while held, regardless of the item's hold style.
        /// </summary>
        public bool HoldOut => false;

        public void DrawHeld(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 position, Rectangle frame, float rotation, Color color, float scale, Vector2 origin);

        public Rectangle GetDrawFrame(Texture2D texture, PlayerDrawSet drawInfo) => texture.Frame();
    }
}
