using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Cooldowns;
using CalamityFables.Particles;
using ReLogic.Graphics;

namespace CalamityFables.Content.Debug
{
    /*
    public class BlendmodeTest : ModItem
    {
        public override string Texture => AssetDirectory.Debug + "TextureMerge";

        public static List<Vector2> drawPositions = new List<Vector2>();


        public override void SetDefaults()
        {
            Item.useTime = Item.useAnimation = 8;
            Item.width = 30;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item1;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                drawPositions.Clear();
                return true;
            }

            drawPositions.Add(Main.MouseWorld);
            return true;
        }




        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D tex = ModContent.Request<Texture2D>(AssetDirectory.Debug + "Cracks1").Value;

            //for (int i = 0; i < drawPositions.Count; i++)
            //{
            //    drawPositions[i] = drawPositions[i] + Vector2.UnitY * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + i * 0.1f) * 1f;
            //}
            
            MultiplyBlend = new BlendState
            {
                AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
            };

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            int i = 0;
            foreach (Vector2 drawPos in drawPositions)
            {
                float size = 2f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f + i) * 0.14f;
                //Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, null, Color.White, 0, tex.Size() / 2f, size, 0, 0);
                i++;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, MultiplyBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            
            //tex = ModContent.Request<Texture2D>(AssetDirectory.Debug + "TextureMergeDark1").Value;

            i = 0;
            foreach (Vector2 drawPos in drawPositions)
            {
                float size = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.3f + i) * 0.14f * 0f; 
                Main.spriteBatch.Draw(tex, drawPos - Main.screenPosition, null, Color.White, 0, tex.Size() / 2f, size, 0, 0);
                i++;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);

        }

        public static BlendState LightenBlend = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Max,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState DarkenBlend = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Min,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState MultiplyBlend = new BlendState
        {
            AlphaBlendFunction = BlendFunction.Max,
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };
    }

    */
}