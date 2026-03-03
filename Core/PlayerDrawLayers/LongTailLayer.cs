using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public interface ILongBackAccessory
    {
        /// <summary>
        /// The texture of the extension
        /// </summary>
        string EquipTexture { get; }
    }


    public class LongTailLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead && !drawInfo.drawPlayer.mount.Active;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            FablesPlayer modPlayer = drawPlayer.GetModPlayer<FablesPlayer>();
            ILongBackAccessory longTail = FablesSets.GetLongBackAccessory(EquipType.Back, drawPlayer.tail);

            //Referenced from DrawPlayer_08_1_Tails()
            if (longTail != null)
            {
                Texture2D texture = ModContent.Request<Texture2D>(longTail.EquipTexture).Value;
                Vector2 basePosition = Vector2.Zero;
                if (drawInfo.isSitting)
                    basePosition.Y += -2f;
                Vector2 value = new Vector2(0f, 8f);

                Vector2 drawPosition = basePosition + drawInfo.Position - Main.screenPosition + drawPlayer.bodyPosition;
                drawPosition += new Vector2(drawPlayer.width / 2, drawPlayer.height - drawPlayer.bodyFrame.Height / 2);
                drawPosition += value - Vector2.UnitY * 4f; //Offsets

                drawPosition = drawPosition.Floor();

                Rectangle adjustedFrame = drawPlayer.bodyFrame;
                adjustedFrame.Width = texture.Width;
                Vector2 origin = drawInfo.bodyVect;
                origin.X += (adjustedFrame.Width - drawPlayer.bodyFrame.Width) / 2;

                DrawData item = new DrawData(texture, drawPosition, adjustedFrame, drawInfo.colorArmorBody, drawPlayer.bodyRotation, origin, 1f, drawInfo.playerEffect, 0);
                item.shader = modPlayer.backEquipDye;
                drawInfo.DrawDataCache.Add(item);
            }
        }
    }
}
