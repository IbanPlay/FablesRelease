using Terraria.DataStructures;
using CalamityFables.Content.Items.CrabulonDrops;

namespace CalamityFables.Core.DrawLayers
{
    public class CrabulonMaskFrontLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead;

        public static Asset<Texture2D> CrabbyMaskTexture;
        public static Asset<Texture2D> CrabbyGlowmaskTexture;
        public static Asset<Texture2D> CrabbyWhiskerTexture1;
        public static Asset<Texture2D> CrabbyWhiskerTexture2;

        public static Asset<Texture2D> ShroomyMaskTexture;
        public static Asset<Texture2D> ShroomyGlowmaskTexture;
        public static Asset<Texture2D> ShroomyWhiskerTexture1;
        public static Asset<Texture2D> ShroomyWhiskerTexture2;
        public static Asset<Texture2D> ShroomyManeTexture;

        public static Asset<Texture2D> HairyMaskTexture;
        public static Asset<Texture2D> HairyGlowmaskTexture;

        public static void LoadTextures()
        {
            CrabbyMaskTexture = CrabbyMaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskHead");
            CrabbyGlowmaskTexture = CrabbyGlowmaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskHeadGlow");
            CrabbyWhiskerTexture1 = CrabbyWhiskerTexture1 ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskWhisker1");
            CrabbyWhiskerTexture2 = CrabbyWhiskerTexture2 ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskWhisker2");

            ShroomyMaskTexture = ShroomyMaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt1Head");
            ShroomyGlowmaskTexture = ShroomyGlowmaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt1HeadGlow");
            ShroomyWhiskerTexture1 = ShroomyWhiskerTexture1 ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt1Whisker1");
            ShroomyWhiskerTexture2 = ShroomyWhiskerTexture2 ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt1Whisker2");
            ShroomyManeTexture = ShroomyManeTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt1Mane");

            HairyMaskTexture = HairyMaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt2Head");
            HairyGlowmaskTexture = HairyGlowmaskTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskAlt2HeadGlow");
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.head != CrabulonBossMask.equipSlot && drawPlayer.head != CrabulonBossMaskAlt1.equipSlot && drawPlayer.head != CrabulonBossMaskAlt2.equipSlot)
                return;

            bool crabby = drawPlayer.head == CrabulonBossMask.equipSlot;
            bool hairy = drawPlayer.head == CrabulonBossMaskAlt2.equipSlot;
            bool shroomy = drawPlayer.head == CrabulonBossMaskAlt1.equipSlot;

            int dyeShader = drawPlayer.cHead;
            Vector2 drawPosition = drawInfo.HeadPosition(true);
            Vector2 drawPositionOffset = new Vector2(2, -2);

            LoadTextures();

            Texture2D maskTexture = CrabbyMaskTexture.Value;
            Vector2 origin = new Vector2(24, 26);
            Texture2D maskGlowTexture = CrabbyGlowmaskTexture.Value;

            Texture2D leftWhiskerTex = CrabbyWhiskerTexture1.Value;
            Vector2 leftWhiskerOrigin = new Vector2(16, 0);
            Vector2 leftWhiskerOffset = new Vector2(-4, -2);

            Texture2D rightWhiskerTex = CrabbyWhiskerTexture2.Value;
            Vector2 rightWhiskerOrigin = new Vector2(0, 0);
            Vector2 rightWhiskerOffset = new Vector2(10, -2);

            if (hairy)
            {
                maskTexture = HairyMaskTexture.Value;
                origin = new Vector2(18, 20);
                maskGlowTexture = HairyGlowmaskTexture.Value;
            }
            else if (shroomy)
            {
                maskTexture = ShroomyMaskTexture.Value;
                origin = new Vector2(16, 16);
                maskGlowTexture = ShroomyGlowmaskTexture.Value;

                leftWhiskerTex = ShroomyWhiskerTexture1.Value;
                leftWhiskerOrigin = new Vector2(10, 0);
                leftWhiskerOffset = new Vector2(0, -2);

                rightWhiskerTex = ShroomyWhiskerTexture2.Value;
                rightWhiskerOrigin = new Vector2(0, 0);
                rightWhiskerOffset = new Vector2(10, -2);
            }

            drawInfo.AdjustOffsetOrigin(maskTexture, ref drawPositionOffset, ref origin);

            FablesPlayer mp = drawPlayer.GetModPlayer<FablesPlayer>();
            float whiskerRotationExtra = mp.springyVelocityTracker.value.Y * 0.02f * drawPlayer.direction;
            float opacity = drawInfo.colorArmorHead.A / 255f;

            //Whiskers wiggle on hit, and the eyes spaz out
            float hitTimer = mp.JustHurtTimer;
            if (hitTimer > 0.5f)
            {
                whiskerRotationExtra += Main.rand.NextFloat(-0.3f, 0.3f) * MathF.Pow(hitTimer, 1.5f);

                string textureSuffix = "";
                if (hairy)
                    textureSuffix = "Alt2";
                else if (shroomy)
                    textureSuffix = "Alt1";

                maskGlowTexture = ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMask" + textureSuffix + "HeadGlow_Hurt" + Main.rand.Next(1, 6).ToString()).Value;
                if (Main.rand.NextBool())
                    maskGlowTexture = ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMask" + textureSuffix + "HeadGlow_Hurt3").Value;
            }

            drawPosition += drawPositionOffset.RotatedBy(drawPlayer.headRotation);

            //Shroomy mask draws the mane ontop of the body
            if (shroomy)
            {
                float drawRotation = 0f;
                Texture2D maneTexture = ShroomyManeTexture.Value;
                drawRotation += mp.hairLikeVelocityTracker.value.X * 0.12f;
                drawRotation += mp.hairLikeVelocityTracker.value.Y * drawPlayer.direction * 0.03f;

                Vector2 maneOffset = new Vector2(-9, -4);
                Vector2 maneOrigin = new Vector2(11, 4);

                int flip = drawInfo.playerEffect == SpriteEffects.FlipHorizontally ? -1 : 1;
                drawRotation = Math.Clamp(drawRotation * flip, -0.2f, MathHelper.PiOver2) * flip;
                drawRotation += drawPlayer.headRotation + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.1f;

                drawInfo.AdjustOffsetOrigin(maneTexture, ref maneOffset, ref maneOrigin);

                DrawData maneDrawData = new DrawData(maneTexture, drawPosition + maneOffset.RotatedBy(drawPlayer.headRotation), null, drawInfo.colorArmorHead, drawRotation, maneOrigin, 1f, drawInfo.playerEffect, 0) { shader = dyeShader };
                drawInfo.DrawDataCache.Add(maneDrawData);
            }


            DrawData maskDrawData = new DrawData(maskTexture, drawPosition, null, drawInfo.colorArmorHead, drawPlayer.headRotation, origin, 1f, drawInfo.playerEffect, 0) { shader = dyeShader };
            drawInfo.DrawDataCache.Add(maskDrawData);

            DrawData glowmaskDrawData = new DrawData(maskGlowTexture, drawPosition, null, Color.White * opacity, drawPlayer.headRotation, origin, 1f, drawInfo.playerEffect, 0); //{ shader = dyeShader };
            drawInfo.DrawDataCache.Add(glowmaskDrawData);

            //Hairy doesn't have whiskers
            if (hairy)
                return;

            drawInfo.AdjustOffsetOrigin(leftWhiskerTex, ref leftWhiskerOffset, ref leftWhiskerOrigin);
            drawInfo.AdjustOffsetOrigin(rightWhiskerTex, ref rightWhiskerOffset, ref rightWhiskerOrigin);

            //Whiskers are slightly fullbright
            Color whiskerColor = Color.Lerp(drawInfo.colorArmorHead, Color.White * opacity, 0.6f);

            DrawData rightWhiskerDrawData = new DrawData(rightWhiskerTex, drawPosition + rightWhiskerOffset.RotatedBy(drawPlayer.headRotation), null, whiskerColor, drawPlayer.headRotation - whiskerRotationExtra, rightWhiskerOrigin, 1f, drawInfo.playerEffect, 0) { shader = dyeShader };
            drawInfo.DrawDataCache.Add(rightWhiskerDrawData);;
            DrawData leftWhiskerDrawData = new DrawData(leftWhiskerTex, drawPosition + leftWhiskerOffset.RotatedBy(drawPlayer.headRotation), null, whiskerColor, drawPlayer.headRotation + whiskerRotationExtra, leftWhiskerOrigin, 1f, drawInfo.playerEffect, 0) { shader = dyeShader };
            drawInfo.DrawDataCache.Add(leftWhiskerDrawData);
        }
    }

    public class CrabulonMaskBackLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead;

        public static Asset<Texture2D> ManeTexture;

        public override bool IsHeadLayer => true;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer.head != CrabulonBossMask.equipSlot)
                return;

            int dyeShader = drawPlayer.cHead;
            Vector2 drawPosition = drawInfo.HeadPosition(true);
            ManeTexture = ManeTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "CrabulonBossMaskMane");
            Texture2D maneTexture = ManeTexture.Value;
            Vector2 origin = new Vector2(14, 10);
            Vector2 drawPositionOffset = new Vector2(-10, 4);

            int flip = 1;

            if ((drawInfo.playerEffect & SpriteEffects.FlipVertically) != 0)
            {
                origin.Y = maneTexture.Height - origin.Y;
                drawPositionOffset.Y *= -1;
            }
            if ((drawInfo.playerEffect & SpriteEffects.FlipHorizontally) != 0)
            {
                origin.X = maneTexture.Width - origin.X;
                drawPositionOffset.X *= -1;
                flip = -1;
            }

            FablesPlayer mp = drawPlayer.GetModPlayer<FablesPlayer>();
            drawPosition += drawPositionOffset.RotatedBy(drawPlayer.headRotation);
            float drawRotation = 0f;

            drawRotation += mp.hairLikeVelocityTracker.value.X * 0.12f;
            drawRotation += mp.hairLikeVelocityTracker.value.Y * drawPlayer.direction * 0.03f;

            drawRotation = Math.Clamp(drawRotation * flip, -0.2f, MathHelper.PiOver2) * flip;
            drawRotation += drawPlayer.headRotation + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.1f;

            DrawData maneDrawData = new DrawData(maneTexture, drawPosition, null, drawInfo.colorArmorHead, drawRotation, origin, 1f, drawInfo.playerEffect, 0)
            {
                shader = dyeShader
            };
            drawInfo.DrawDataCache.Add(maneDrawData);
        }
    }
}
