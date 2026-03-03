using Terraria.DataStructures;
using CalamityFables.Content.Items.VanityMisc;
using Terraria.Graphics.Shaders;
using CalamityFables.Content.Items.CrabulonDrops;

namespace CalamityFables.Core.DrawLayers
{

    public class SporethrowerTankLayer : PlayerDrawLayer
    {
        public static int SporethrowerType;
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Backpacks);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.heldItem.type != SporethrowerType || drawInfo.drawPlayer.HeldItem.type != SporethrowerType)
                return false;

            return !drawInfo.drawPlayer.turtleArmor && drawInfo.drawPlayer.body != 106 && drawInfo.drawPlayer.body != 170 && (drawInfo.drawPlayer.backpack <= 0 || drawInfo.drawPlayer.mount.Active);
        }

        public static Asset<Texture2D> TankTexture;
        public static Asset<Texture2D> GlassTexture;
        public static Asset<Texture2D> ShroomTexture;
        public static Asset<Texture2D> ShroomGlowTexture;
        public static Asset<Texture2D> UnderlightTexture;
        public static Asset<Texture2D> LEDTexture;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Vector2 drawPosition = drawInfo.BodyPosition();
            Player drawPlayer = drawInfo.drawPlayer;

            TankTexture = TankTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTank");
            GlassTexture = GlassTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTankGlass");
            ShroomTexture = ShroomTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTankMushroom");
            ShroomGlowTexture = ShroomGlowTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTankMushroomGlow");
            UnderlightTexture = UnderlightTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTankUnderlight");
            LEDTexture = LEDTexture ?? ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerTankLight");

            //Get the rotation based on the velocity tracker
            FablesPlayer mp = drawPlayer.GetModPlayer<FablesPlayer>();
            float extraRotation = mp.springyVelocityTracker.value.X * -0.05f;
            int flip = (drawInfo.playerEffect & SpriteEffects.FlipHorizontally) != 0 ? 1 : -1;
            extraRotation = Math.Clamp(extraRotation * flip, -0.05f, 0.1f) * flip;

            Vector2 origin = new Vector2(11, 18);
            Vector2 drawOffset = new Vector2(-15, 2f);
            drawInfo.AdjustOffsetOrigin(TankTexture, ref drawOffset, ref origin);

            drawOffset.Y -= MathHelper.Clamp(mp.hairLikeVelocityTracker.value.Y * 0.3f, -5f, 5f);
            Vector2 shroomOffset = -Vector2.UnitY * MathHelper.Clamp(mp.springyVelocityTracker.value.Y * 0.3f, -2f, 2f);



            float animProgress = 1 - drawInfo.drawPlayer.itemAnimation / (float)drawInfo.drawPlayer.itemAnimationMax;
            bool cooldown = drawInfo.drawPlayer.reuseDelay == 0;
            if (cooldown)
                animProgress = drawInfo.drawPlayer.itemTime / (float)drawInfo.heldItem.reuseDelay;
            Color glowColor = Color.Lerp(new Color(10, 10, 255), new Color(0, 80, 255), animProgress) * MathF.Pow(animProgress, 0.7f) * 0.6f;

            int shroomShader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.GelDye);

            //Shrink and grow based on size
            float chargeProgress = (drawInfo.drawPlayer.HeldItem.ModItem as Sporethrower).charge / (float)Sporethrower.MAX_CHARGE;
            float shroomSize = 0.4f + 0.6f * chargeProgress;
            glowColor *= MathF.Pow(chargeProgress, 0.4f);


            Color shroomColor = Color.Lerp(drawInfo.colorArmorBody, Color.Black, 0.1f);
            DrawData shroom = new DrawData(ShroomTexture.Value, drawPosition + (drawOffset + shroomOffset).RotatedBy(drawPlayer.bodyRotation), null, shroomColor, drawPlayer.bodyRotation + extraRotation, origin, shroomSize, drawInfo.playerEffect);
            DrawData shroomGlow = new DrawData(ShroomGlowTexture.Value, drawPosition + (drawOffset + shroomOffset).RotatedBy(drawPlayer.bodyRotation), null, glowColor with { A = 0 }, drawPlayer.bodyRotation + extraRotation, origin, shroomSize, drawInfo.playerEffect);


            //Glass is just the tank but half transparent
            DrawData underlight = new DrawData(UnderlightTexture.Value, drawPosition + drawOffset.RotatedBy(drawPlayer.bodyRotation), null, new Color(140, 180, 255) * 0.5f * (1 - drawInfo.shadow), drawPlayer.bodyRotation + extraRotation, origin, 1f, drawInfo.playerEffect);
            DrawData glass = new DrawData(GlassTexture.Value, drawPosition + drawOffset.RotatedBy(drawPlayer.bodyRotation), null, drawInfo.colorArmorBody * 0.3f, drawPlayer.bodyRotation + extraRotation, origin, 1f, drawInfo.playerEffect);
            DrawData mainTank = new DrawData(TankTexture.Value, drawPosition + drawOffset.RotatedBy(drawPlayer.bodyRotation), null, drawInfo.colorArmorBody, drawPlayer.bodyRotation + extraRotation, origin, 1f, drawInfo.playerEffect);
            DrawData light = new DrawData(LEDTexture.Value, drawPosition + drawOffset.RotatedBy(drawPlayer.bodyRotation), null, Color.White with { A = 0 } * MathF.Pow(animProgress, 2f) * (1 - drawInfo.shadow), drawPlayer.bodyRotation + extraRotation, origin, 1f, drawInfo.playerEffect);


            drawInfo.DrawDataCache.Add(underlight);
            drawInfo.DrawDataCache.Add(shroom with { shader = shroomShader });
            drawInfo.DrawDataCache.Add(shroomGlow);
            drawInfo.DrawDataCache.Add(glass);
            drawInfo.DrawDataCache.Add(mainTank);
            drawInfo.DrawDataCache.Add(light);
        }
    }

    public class SporethrowerMaskLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FaceAcc);
        public override bool IsHeadLayer => true;
        public static Asset<Texture2D> Texture;
        public static Asset<Texture2D> GlowTexture;

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.heldItem.type == SporethrowerTankLayer.SporethrowerType || drawInfo.drawPlayer.GetPlayerFlag("TechwearSporeMask");

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Texture ??= ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerMask");
            GlowTexture ??= ModContent.Request<Texture2D>(AssetDirectory.CrabulonDrops + "SporethrowerMaskGlow");

            Vector2 headPosition = drawInfo.HeadPosition(false, true);
            Rectangle frame = drawInfo.drawPlayer.bodyFrame;

            DrawData item = new DrawData(Texture.Value, headPosition, frame, drawInfo.colorArmorHead, drawInfo.drawPlayer.headRotation, drawInfo.headVect, 1f, drawInfo.playerEffect);
            drawInfo.DrawDataCache.Add(item);

            item = new DrawData(GlowTexture.Value, headPosition, frame, Color.White * (1 - drawInfo.shadow), drawInfo.drawPlayer.headRotation, drawInfo.headVect, 1f, drawInfo.playerEffect);
            item.shader = drawInfo.drawPlayer.GetModPlayer<FablesPlayer>().gogglesDye;
            
            drawInfo.DrawDataCache.Add(item);
        }
    }
}
