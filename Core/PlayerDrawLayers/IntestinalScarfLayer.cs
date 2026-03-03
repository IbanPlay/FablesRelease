using CalamityFables.Content.Items.DesertScourgeDrops;
using Terraria.DataStructures;

namespace CalamityFables.Core.DrawLayers
{
    public class IntestinalScarfLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayerLoader.Layers.FirstOrDefault(l => l is CrabulonMaskBackLayer));
        public static Asset<Texture2D> ScarfTexture;
        public static Asset<Texture2D> ScarfEndTexture;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return drawInfo.drawPlayer.neck == IntestinalScarf.NeckEquipSlot;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            ScarfTexture = ScarfTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "IntestinalScarf_TrailSegment");
            ScarfEndTexture = ScarfEndTexture ?? ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "IntestinalScarf_TrailEnd");

            VerletNet scarf = drawInfo.drawPlayer.GetModPlayer<FablesPlayer>().scarfSimulation;
            if (scarf == null)
                return;

            Texture2D tex = ScarfTexture.Value;
            Texture2D endTex = ScarfEndTexture.Value;

            Vector2 stringPoint = scarf.points[0].position;
            Vector2 offset = drawInfo.HeadPosition() - (drawInfo.drawPlayer.Center - Main.screenPosition) + Vector2.UnitY * (-drawInfo.drawPlayer.gfxOffY + 5f) * drawInfo.drawPlayer.gravDir;
            float rotationOffset = MathHelper.Pi;

            Vector2 origin = new Vector2(tex.Width - 2, tex.Height / 2f);
            Vector2 scarfEndOrigin = new Vector2(endTex.Width - 2, endTex.Height / 2f);

            if ((int)(drawInfo.playerEffect & SpriteEffects.FlipHorizontally) == 1)
            {
                origin.X = 2;
                scarfEndOrigin.X = 2;
                rotationOffset = 0;
            }

            Texture2D usedTex = tex;
            Vector2 usedOrigin = origin;
            for (int i = 0; i < scarf.points.Count - 2; i++)
            {
                Vector2 nextPoint = scarf.points[i + 1].position - scarf.points[i].position;
                Color color = Lighting.GetColor(scarf.points[i].position.ToTileCoordinates()) * (drawInfo.colorArmorBody.A / 255f);
                Vector2 scale = new Vector2(nextPoint.Length() / (usedTex.Width - 2), 1f);

                scale.Y += Math.Max(0, Utils.GetLerpValue(0.6f, 1f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f - i * 0.4f), true)) * 0.4f;

                drawInfo.DrawDataCache.Add(new(usedTex, stringPoint + offset - Main.screenPosition, null, color, nextPoint.ToRotation() + rotationOffset, usedOrigin, scale, drawInfo.playerEffect, 0) { shader = drawInfo.drawPlayer.cNeck});
                stringPoint += nextPoint;

                if (i == scarf.points.Count - 4)
                {
                    usedTex = endTex;
                    usedOrigin = scarfEndOrigin;
                }
            }
        }
    }
}
