using Terraria.UI;

namespace CalamityFables.Content.UI
{
    public class ClueInVignetteUI : SmartUIState
    {
        public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name.Equals("Vanilla: Interface Logic 4"));

        public override bool Visible {
            get {
                return (Main.playerInventory && Main.EquipPageSelected != 2) || opacityMult > 0;
            }
        }

        public override bool UpdatesWhileInvisible => true;

        public static float opacityMult = 0f;
        public static Vector2 holeSize;
        public static Vector2 center;
        public static bool inUse = false;
        public static bool highlightTarget;
        public static Color highlightColor;


        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!inUse)
            {
                opacityMult -= 0.01f;
                opacityMult *= 0.96f;

                if (opacityMult < 0)
                    opacityMult = 0;
            }

            else
            {
                opacityMult += 0.01f;
                opacityMult *= 1.03f;

                if (opacityMult > 1)
                    opacityMult = 1;
            }

            inUse = false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            float opacity = 0.65f;
            //Code taken from slr <3
            var tex = ModContent.Request<Texture2D>(AssetDirectory.UI + "Vignette").Value;
            var targetRect = new Rectangle((int)center.X - (int)holeSize.X / 2, (int)center.Y - (int)holeSize.Y / 2, (int)holeSize.X, (int)holeSize.Y);

            var targetLeft = new Rectangle(0, 0, targetRect.X, Main.screenHeight);
            var targetRight = new Rectangle(targetRect.X + targetRect.Width, 0, Main.screenWidth - (targetRect.X + targetRect.Width), Main.screenHeight);
            var targetTop = new Rectangle(targetRect.X, 0, targetRect.Width, targetRect.Y);
            var targetBottom = new Rectangle(targetRect.X, targetRect.Y + targetRect.Height, targetRect.Width, Main.screenHeight - (targetRect.Y + targetRect.Height));

            spriteBatch.Draw(tex, targetRect, null, Color.White * opacity * opacityMult, 0, Vector2.Zero, 0, 0);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, targetLeft, Color.Black * opacity * opacityMult);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, targetRight, Color.Black * opacity * opacityMult);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, targetTop, Color.Black * opacity * opacityMult);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, targetBottom, Color.Black * opacity * opacityMult);
        }
    }
}
