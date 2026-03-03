using Terraria.DataStructures;
using CalamityFables.Content.Items.VanityMisc;

namespace CalamityFables.Core.DrawLayers
{

    public class PochitaChainsawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead && !drawInfo.headOnlyRender;

        public static Asset<Texture2D> Texture;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.head != HeroOfHellMask.equipSlot && drawPlayer.head != HeroOfHellMask.equipSlotHurt)
                return;

            int dyeShader = drawPlayer.cHead;
            Vector2 drawPosition = drawInfo.HeadPosition(true);
            Texture = Texture ?? ModContent.Request<Texture2D>(AssetDirectory.MiscVanity + "HeroOfHellMask_Saw");

            Texture2D sawTexture = Texture.Value;
            Rectangle sawFrame = sawTexture.Frame(1, 3, 0, (drawPlayer.miscCounter / 5) % 3, 0, -2) ;

            Vector2 origin = new Vector2(0, sawFrame.Height / 2);
            Vector2 sawOffset = new Vector2(10, -8f);

            //Applies flip effects
            drawInfo.AdjustOffsetOrigin(sawFrame, ref sawOffset, ref origin);

            DrawData chainsawData = new DrawData(
                sawTexture, 
                drawPosition + sawOffset.RotatedBy(drawPlayer.headRotation), 
                sawFrame, 
                drawInfo.colorArmorHead, 
                drawPlayer.headRotation, 
                origin, 
                1f, 
                drawInfo.playerEffect, 
                0) 
            { shader = dyeShader };
            drawInfo.DrawDataCache.Add(chainsawData);;
        }
    }

}
