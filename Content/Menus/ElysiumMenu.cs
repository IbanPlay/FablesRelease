using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityFables.Content.Menus
{
	public class ElysiumMenu : ModMenu
	{
        public static Asset<Texture2D> logoTex;

        public static Asset<Texture2D> bgTex;
        public static Asset<Texture2D> bgBloomTex;
        public static Asset<Texture2D> displaceNoise;
        public static Asset<Texture2D> dustMaskNoise;
        public static Asset<Texture2D> grimeTex;
        public static Asset<Texture2D> dustTex;
        public static Asset<Texture2D> squigglePalette;

        public static Asset<Texture2D> emberDisplace;
        public static Asset<Texture2D> emberWarble;

        public static float pixelizationLevel = 0f;

        public static List<MenuEmbers> floatingEmbers = new List<MenuEmbers>();

        public static Vector2 lastMousePosition;

        public static Color menuTint;
        public static float menuTintOpacity;

        public static LocalizedText menuName;

        public override void Load() {

            if (Main.dedServ)
                return;

            menuName = Mod.GetLocalization("Extras.ModMenus.ElysiumMenu");

            logoTex = ModContent.Request<Texture2D>(AssetDirectory.UI + "FablesLogo");

            bgTex = ModContent.Request<Texture2D>(AssetDirectory.UI + "ElysiumBackground3");
            bgBloomTex = ModContent.Request<Texture2D>(AssetDirectory.UI + "ElysiumBackground");
            displaceNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "CracksDisplace2");
            dustMaskNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PatchyTallNoise");
            grimeTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "RainbowGrimeNoise");
            dustTex = ModContent.Request<Texture2D>(AssetDirectory.Noise + "RGBNoise");

            emberWarble = ModContent.Request<Texture2D>(AssetDirectory.Noise + "GradientNoise");
            emberDisplace = ModContent.Request<Texture2D>(AssetDirectory.Noise + "DisplaceNoise2");
            squigglePalette = ModContent.Request<Texture2D>(AssetDirectory.UI + "ElysiumSquigglePalette");

            //On_Main.DrawMenu += ChangeFavoriteColor;
        }

        private void ChangeFavoriteColor(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            Color favoriteColorCache = Main.OurFavoriteColor;
            if (MenuLoader.CurrentMenu != null && MenuLoader.CurrentMenu.Name == "ElysiumMenu")
                Main.OurFavoriteColor = new Color(237, 232, 151);
            orig(self, gameTime);
            Main.OurFavoriteColor = favoriteColorCache;
        }

        public override Asset<Texture2D> Logo => logoTex;

		public override Asset<Texture2D> SunTexture => null;

		public override Asset<Texture2D> MoonTexture => null;

		public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/TitleTheme");

		public override string DisplayName => menuName.Value;

		public override void OnSelected() {
            pixelizationLevel = 0f;
		}

        public override void Update(bool isOnTitleScreen)
        {
            if (isOnTitleScreen && pixelizationLevel < 1)
            {
                pixelizationLevel += 1 / (60f * 0.2f);
                if (pixelizationLevel > 1f)
                    pixelizationLevel = 1f;
            }


        }

        public void SpawnEmbers()
        {
            if (Main.rand.NextBool(18))
            {
                Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 3f);
                floatingEmbers.Add(new MenuEmbers(new Vector2(Main.rand.NextFloat(0f, Main.screenWidth), Main.screenHeight), velocity));
            }
        }

        public void UpdateEmbers()
        {
            Vector2 mouseVelocity = Main.MouseScreen - lastMousePosition;

            for (int i = floatingEmbers.Count - 1; i >= 0; i--)
            {
                MenuEmbers ember = floatingEmbers[i];

                float mouseInfluence = Utils.GetLerpValue(120f, 30f, Main.MouseScreen.Distance(ember.position), true);
                ember.velocity += mouseInfluence * mouseVelocity * 0.04f;

                ember.position += ember.velocity;
                ember.velocity.X += (float)Math.Sin(Main.GlobalTimeWrappedHourly + ember.position.Y * 0.3f) * 0.1f;

                ember.velocity = ember.velocity.RotatedBy(ember.swerviness * 0.015f * (float)Math.Sin(ember.uniqueOffset + Main.GlobalTimeWrappedHourly));

                if (ember.position.Y < 0 || ember.position.X < 0 || ember.position.X > Main.screenWidth || ember.position.Y > Main.screenHeight + 30)
                    floatingEmbers.RemoveAt(i);
            }
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor) {

            Main.time = 27000;
            Main.dayTime = true;

            Vector2 resolution = new  Vector2(Main.screenWidth / 3f, Main.screenHeight / 3f);
            resolution = Vector2.Lerp(new Vector2(Main.screenWidth / 20f, Main.screenHeight / 20f), resolution, pixelizationLevel);

            Texture2D background = bgTex.Value;

            // Calculate the draw position offset and scale in the event that someone is using a non-16:9 monitor
            Vector2 drawOffset = Vector2.Zero;

            //if the resolution is odd, it looks weird cuz theres a pixel on the right thats not covered
            int screenWidth = Main.screenWidth;
            if (screenWidth % 2 == 1)
                screenWidth++;

            float xScale = (float)screenWidth / (float)background.Width;
            float yScale = (float)Main.screenHeight / (float)background.Height;
            float scale = xScale;

            // if someone's monitor isn't in wacky dimensions, no calculations need to be performed at all
            if (xScale != yScale)
            {
                // If someone's monitor is tall, it needs to be shifted to the left so that it's still centered on screen
                // Additionally the Y scale is used so that it still covers the entire screen
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (background.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                    // The opposite is true if someone's monitor is widescreen
                    drawOffset.Y -= (background.Height * scale - Main.screenHeight) * 0.5f;
            }

            Effect bgShader = Scene["MenuBackgroundShader"].GetShader().Shader;
            bgShader.Parameters["bloomTex"].SetValue(bgBloomTex.Value);
            bgShader.Parameters["grimeTex"].SetValue(grimeTex.Value);
            bgShader.Parameters["displaceTex"].SetValue(displaceNoise.Value);
            bgShader.Parameters["dustTex"].SetValue(dustTex.Value);
            bgShader.Parameters["dustMaskTex"].SetValue(dustMaskNoise.Value);
            bgShader.Parameters["squigglePaletteTexture"].SetValue(squigglePalette.Value);

            bgShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.06f + pixelizationLevel * 0.4f);

            float underglowStrenght = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.2f + 1.2f;

            bgShader.Parameters["pixelRes"].SetValue(resolution);
            bgShader.Parameters["underglowColor"].SetValue(new Vector3(0.1f, 0.3f, 0.2f) * underglowStrenght);
            bgShader.Parameters["dustColor"].SetValue(new Vector3(0.9f, 0.9f, 0.3f));
            bgShader.Parameters["grimeThreshold"].SetValue(0.08f);

            bgShader.Parameters["screenTint"].SetValue(menuTint with { A = (byte)(menuTintOpacity * 255)});

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, bgShader, Main.UIScaleMatrix);

            spriteBatch.Draw(background, drawOffset, null, (Color.White * 0.7f) with { A = 255 }, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f) ;

            SpawnEmbers();
            UpdateEmbers();

            Effect embersShader = Scene["MenuEmbers"].GetShader().Shader;
            embersShader.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 1.06f);
            embersShader.Parameters["pixelRes"].SetValue(10);
            embersShader.Parameters["emberColor"].SetValue(new Vector3(1.1f, 1.1f, 0.6f));
            embersShader.Parameters["emberColor2"].SetValue(new Vector3(0.9f, 0.8f, 0.4f));
            embersShader.Parameters["sizeWarbleNoise"].SetValue(emberWarble.Value);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, embersShader, Main.UIScaleMatrix);

            foreach (MenuEmbers ember in floatingEmbers)
            {
                spriteBatch.Draw(emberDisplace.Value, ember.position, null, ember.colorValue, 0f, emberDisplace.Size() / 2f, ember.size * 0.034f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);


            drawColor = Color.White;
            logoScale *= 0.24f;
            logoDrawCenter.Y += 20f;

            lastMousePosition = Main.MouseScreen;
            return true;
		}
	}

    public class MenuEmbers
    {
        public Color colorValue;
        public Vector2 position;
        public Vector2 velocity;
        public float size;
        public float swerviness;
        public float uniqueOffset;

        public MenuEmbers(Vector2 position, Vector2 velocity)
        {
            this.position = position;
            this.velocity = velocity;

            size = Main.rand.NextFloat(0.6f, 1f);
            if (!Main.rand.NextBool(10))
                size *= 0.3f;
            colorValue = new Color(Main.rand.NextFloat(), Main.rand.NextFloat(), Main.rand.NextFloat());

            swerviness = (float)Math.Pow(Main.rand.NextFloat(), 5f) * (Main.rand.NextBool() ? -1 : 1);
            uniqueOffset = Main.rand.NextFloat();
        }
    }
}
