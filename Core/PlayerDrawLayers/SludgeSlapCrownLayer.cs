using CalamityFables.Content.Items.EarlyGameMisc;
using Terraria.DataStructures;
using static CalamityFables.Content.Items.EarlyGameMisc.SludgeSlapSlimelingBuff;

namespace CalamityFables.Core.DrawLayers
{
    public class SludgeSlapCrownLayer : PlayerDrawLayer
    {
        public static Asset<Texture2D> CrownTexture;
        public static Asset<Texture2D> CrownGlow;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetPlayerFlag("SludgeSlapSlimelingBuff");

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            CrownTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "GelKingCrown");
            CrownGlow ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "GelKingCrown_Glow");
            Texture2D bloomFlare = AssetDirectory.CommonTextures.BloomStreak.Value;

            Player player = drawInfo.drawPlayer;
            int flip = (drawInfo.playerEffect & SpriteEffects.FlipHorizontally) != 0 ? 1 : -1;

            FablesPlayer modPlayer = player.GetModPlayer<FablesPlayer>();
            if (!player.GetPlayerData<SludgeSlapCrownTimeData>(out var data))
                data = new SludgeSlapCrownTimeData();
            int buffAge = data.SlimelingBuffAge;
            float appearEffect = Utils.GetLerpValue(20, 0, buffAge, true);

            Vector2 position = drawInfo.BodyPosition();
            Vector2 origin = new(11, 16);
            if (flip == -1)
                origin.X = CrownTexture.Value.Width - origin.X;

            Vector2 offset = new Vector2(2 * player.direction, -16);
            offset.Y += 2 * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f) + 1;
            offset -= Vector2.Clamp(modPlayer.hairLikeVelocityTracker.value * 0.3f, Vector2.One * -5f, Vector2.One * 5f);

            //Crown rotates more or less with time to make it wobble a bit even when the player moves at a constant speed
            float rotationVaraince = 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f + Math.PI) + 1;
            float rotationOffset = modPlayer.springyVelocityTracker.value.X * -0.05f;
            rotationOffset = Math.Clamp(rotationOffset * flip, -0.05f, 0.1f) * flip * rotationVaraince;

            //Draw flash of light
            if (appearEffect > 0)
                offset.Y -= player.gravDir * 20f * MathF.Pow(Utils.GetLerpValue(20, -5, buffAge, true), 3f);

            DrawData crown = new(CrownTexture.Value, position + offset, CrownTexture.Frame(), drawInfo.colorArmorHead, player.headRotation + rotationOffset, origin, 1, drawInfo.playerEffect);
            drawInfo.DrawDataCache.Add(crown);

            if (appearEffect > 0) {

                //White crown
                float glowFactor = (float)Math.Pow(appearEffect, 0.7f);
                Color glowColor = new Color(255, 255, 255) * glowFactor;
                crown = new(CrownGlow.Value, position + offset, CrownGlow.Frame(), glowColor, player.headRotation + rotationOffset, origin, 1, drawInfo.playerEffect);
                drawInfo.DrawDataCache.Add(crown);


                Vector2 bloomOrigin = bloomFlare.Size() / 2;
                Vector2 bloomOffset = new Vector2(0, -8) + offset;
                glowColor = Color.Goldenrod with { A = 0 } * 0.75f;

                glowFactor = Utils.GetLerpValue(0.4f, 1f, appearEffect, true);
                Vector2 bloomScale = new Vector2(MathF.Pow(glowFactor, 1.8f) * 0.75f, MathF.Pow(1 - glowFactor, 0.4f) * 1.2f);

                DrawData bloom = new(bloomFlare, position + bloomOffset, null, glowColor * 0.2f, MathHelper.PiOver2, bloomOrigin, bloomScale * 2f, SpriteEffects.None);
                drawInfo.DrawDataCache.Add(bloom);
                //bloom = new(bloomFlare, position + bloomOffset, null, Color.White, MathHelper.PiOver2, bloomOrigin, bloomScale * 1.5f, SpriteEffects.None);
                //drawInfo.DrawDataCache.Add(bloom);
            }

            //Dust
            if (drawInfo.headOnlyRender)
                return;

            // Create sparkles right after buff is applied
            if (buffAge <= 4)
            {
                int dustCount = buffAge == 1 ? 9 : 1;

                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustPosition = drawInfo.drawPlayer.Center - Vector2.UnitY * 15f * drawInfo.drawPlayer.gravDir + offset + new Vector2(Main.rand.NextFloat(-12f, 12f), 0f);
                    Dust dust = Dust.NewDustPerfect(dustPosition , DustID.TintableDustLighted,
                        -2 * Vector2.UnitY * drawInfo.drawPlayer.gravDir * Main.rand.NextFloat(0.6f, 2f),
                        255, Color.Gold, Main.rand.NextFloat(0.6f, 1f));
                    dust.noGravity = true;
                    dust.noLightEmittence = true;
                    if (buffAge > 1f)
                        dust.scale *= 0.6f;

                    drawInfo.DustCache.Add(dust.dustIndex);
                }
            }
            // Create gold dust at crown position
            else if (Main.rand.NextBool(40))
            {
                Vector2 dustPosition = drawInfo.drawPlayer.Center + new Vector2(0, -28 * drawInfo.drawPlayer.gravDir) + Main.rand.NextVector2Circular(15, 15);
                drawInfo.DustCache.Add(Dust.NewDustPerfect(dustPosition, DustID.GoldCoin, Vector2.Zero).dustIndex);
            }

        }
    }
}
