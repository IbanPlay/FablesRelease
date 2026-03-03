using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    /// <summary>
    /// Interface that can be used by chestplates to appear extra bulky, by drawing partly over the players helmet with an extra layer
    /// </summary>
    public interface IBulkyArmor
    {
        /// <summary>
        /// The texture of the part that goes above the helmet
        /// This should be the same size as a regular equipped sprite (20 x 560 in 1x1)
        /// </summary>
        string BulkTexture { get; }

        /// <summary>
        /// The name of the equip slot for the item. If left empty, the equip slot that is looked for will use the name of the item.
        /// Useful if you have multiple body textures you need to add bulk to.
        /// </summary>
        string EquipSlotName(Player drawPlayer) => "";
    }

    //Extends the player's body sprite to draw over the head, useful for bulky chestplates such as the victide breastplate.
    public class BodyBulkLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.shadow == 0f || !drawInfo.drawPlayer.dead;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            //While the extra bulk draws over the players head position, we don't want to have it drawn when the head alone is being drawn.
            if (drawInfo.headOnlyRender)
                return;

            Player drawPlayer = drawInfo.drawPlayer;
            Item bodyItem = drawPlayer.armor[1];
            if (drawPlayer.armor[11].type > ItemID.None)
                bodyItem = drawPlayer.armor[11];

            if (ModContent.GetModItem(bodyItem.type) is IBulkyArmor chestplateBulkDrawer)
            {
                string equipSlotName = chestplateBulkDrawer.EquipSlotName(drawPlayer) != "" ? chestplateBulkDrawer.EquipSlotName(drawPlayer) : bodyItem.ModItem.Name;
                int equipSlot = EquipLoader.GetEquipSlot(Mod, equipSlotName, EquipType.Body);
                if (drawPlayer.body != equipSlot)
                    return;

                int dyeShader = drawPlayer.dye?[1].dye ?? 0;
                Vector2 drawPosition = drawInfo.Position - Main.screenPosition;

                // Using drawPlayer to get width & height and such is perfectly fine, on the other hand. Just center everything
                drawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);

                //Convert to int to remove the jitter.
                drawPosition = new Vector2((int)drawPosition.X, (int)drawPosition.Y);

                //Some dispalcements
                drawPosition += drawPlayer.bodyPosition + drawInfo.bodyVect;

                //Grab the extension texture
                Texture2D extraPieceTexture = ModContent.Request<Texture2D>(chestplateBulkDrawer.BulkTexture).Value;

                //Get the frame of the extension based on the players body frame
                Rectangle frame = extraPieceTexture.Frame(1, 20, 0, drawPlayer.bodyFrame.Y / drawPlayer.bodyFrame.Height);

                DrawData pieceDrawData = new DrawData(extraPieceTexture, drawPosition, frame, drawInfo.colorArmorBody, drawPlayer.fullRotation, drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0)
                {
                    shader = dyeShader
                };
                drawInfo.DrawDataCache.Add(pieceDrawData);
            }
        }
    }
}
