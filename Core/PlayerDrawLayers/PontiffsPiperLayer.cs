using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class PontiffsPiperLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.FinchNest);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;

            return drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<PontiffsPiperSquid>()] > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            int whoAmI = drawInfo.drawPlayer.whoAmI;
            Projectile piperProj = Main.projectile.Where(p => p.owner == whoAmI && p.type == ModContent.ProjectileType<PontiffsPiperSquid>()).FirstOrDefault();
            if (piperProj == null)
                return;

            Texture2D piper = ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "PontiffsPiperSquid").Value;

            //Copied from vanilla finch nest code, dont wanna deal w alignment
            Vector2 drawPosition = drawInfo.HeadPosition(true);

            Vector2 drawOffset = new Vector2(3f * drawInfo.drawPlayer.direction, -2);

            Rectangle frame = piper.Frame(1, 12, 0, piperProj.frame, 0, -2);
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height);
            if ((drawInfo.playerEffect & SpriteEffects.FlipVertically) != 0)
            {
                origin.Y = 0;
                drawOffset.Y *= -1;
            }


            DrawData item = new DrawData(piper, drawPosition + drawOffset.RotatedBy(drawInfo.drawPlayer.headRotation), frame, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, origin, 1f, drawInfo.playerEffect, 0);
            item.shader = drawInfo.drawPlayer.cHead;
            drawInfo.DrawDataCache.Add(item);
        }
    }
}
