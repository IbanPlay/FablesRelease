using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Tiles.VanityTrees;
using CalamityFables.Cooldowns;
using CalamityFables.Particles;
using ReLogic.Graphics;
using System.Text.RegularExpressions;

namespace CalamityFables.Content.Debug
{
    public class DebugItem : ModItem
    {
        public override string Texture => AssetDirectory.Debug + Name;

        public Heap<MeshNode> testHeap;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("DEBUG CONTENT - USE AT YOUR OWN WORLD'S RISK");
        }


        public override void SetDefaults()
        {
            Item.useTime = Item.useAnimation = 8;
            Item.width = 30;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item1;
            Item.value = Item.buyPrice(0, 0, 10, 0);
            Item.rare = ItemRarityID.Blue;
        }

        public override void HoldItem(Player player)
        {
            //if (Main.GameUpdateCount % 10 == 0)
            //    ParticleHandler.SpawnParticle(new ElectricArcPrim(Main.MouseWorld, player.Center, Vector2.UnitX, 2f));
            return;
        }

        public override bool? UseItem(Player player)
        {
            //BossIntroScreens.debug_drawnCard = ModContent.GetInstance<Crabulon>().GetIntroCard;
            //BossIntroScreens.debug_drawnCard.flipped = player.Center.X < Main.MouseWorld.X;

            //BossIntroScreens.currentCard = ModContent.GetInstance<Crabulon>().GetIntroCard;



            string[] splitSnippets = Regex.Split("<s:1.5>中文测试，。：；‘“……”’【（）】</s>", @"(?!^)(?=<[\w:\d/\.]+>)|(?<=<[\w:\d/\.]+>)(?!$)");

            int test = splitSnippets.Length;

            int tileType = Main.tile[Player.tileTargetX, Player.tileTargetY].TileType;

            if (Main.mouseRight)
            {
                if (FrostTree.ValidTileToGrowOn(Main.tile[Player.tileTargetX, Player.tileTargetY]))
                    FrostTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);
                else if (SpiderTree.ValidTileToGrowOn(tileType))
                    SpiderTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);
                else
                    MallowTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);
                return true;
            }

            if (WisteriaTree.ValidTileToGrowOn(Main.tile[Player.tileTargetX, Player.tileTargetY]))
                WisteriaTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);
            else if (InkyTree.ValidTileToGrowOn(tileType))
                InkyTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);
            else
                MarrowTree.GrowTree(Player.tileTargetX, Player.tileTargetY, false, false);



            return true;

            DustDevil spin = new DustDevil(player);

            for (int i = 0; i < 100; i++)
            {
                spin.Update();
                spin.windCharge = 1f;
                spin.spinDirection = 1;
                spin.attachedPlayer = player;
            }

            spin.jumping = true;
            spin.position = Main.MouseWorld;
            spin.attachedPlayer = null;
            spin.JumpStartEffects();

            return true;

            //NPC.NewNPC(player.GetSource_FromAI(), Player.tileTargetX * 16, Player.tileTargetY * 16, ModContent.NPCType<ASTARCRAWLER>()) ;

            Point ground = Main.MouseWorld.ToTileCoordinates();
            int maxIterations = 50;
            ground = AStarPathfinding.OffsetUntilNavigable(ground, new Point(0, 1), Crabulon.CrabulonCrawlPathfind, ref maxIterations);
            if (maxIterations < 0)
                return false;


            maxIterations = 34;
            //Try to find navigable ground below the player
            Point pathfindingEnd = AStarPathfinding.OffsetUntilNavigable(Main.LocalPlayer.Center.ToTileCoordinates(), new Point(0, 1), Crabulon.CrabulonCrawlPathfind, ref maxIterations);

            //If theres no floor under the player then give up
            if (maxIterations < 0)
                return false;

           return  AStarPathfinding.IsThereAPath(ground, pathfindingEnd, Crabulon.CrabulonStride, Crabulon.CrabulonCrawlPathfind, 500f);

            //ParticleHandler.SpawnParticle(new FeedTheHungerEffect(Main.MouseWorld));

            /*
            player.AddCooldown(WarriorsAmphoraCooldown.ID, 122);
            player.AddCooldown(SandsmokeBombCooldown.ID, 122);
            player.AddCooldown(WulfrumRoverDriveRecharge.ID, 122);
            */

            //Chest.SetupTravelShop();

            //ParticleHandler.SpawnParticle(new ElectricArcPrim(Main.MouseWorld, player.Center, Vector2.UnitX, 2f));
            //return true;

            //player.KillMe(PlayerDeathReason.ByCustomReason("Eris died for showcase purposes"), 500, 0);

            /*
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                {
                    Tile t = Main.tile[(int)Main.MouseWorld.X / 16 + i + 100, (int)Main.MouseWorld.Y / 16 + j - 10];
                    t.HasTile = false;
                    t.TileFrameX = 0;
                    t.TileFrameY = 0;
                }
                    */

            //player.Center.RaytraceTo(Main.MouseWorld);

            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            return true;
            /*
            Texture2D techySquareNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "TechyNoise").Value;
            Effect laserScopeEffect = Scene["SecondaryTextureEffectTest"].GetShader().Shader;
            laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "CertifiedCrustyNoise").Value);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(techySquareNoise, Main.MouseScreen, null, Color.White, 0, techySquareNoise.Size() / 2f, 1f, 0, 0);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
            */
        }
    }

}