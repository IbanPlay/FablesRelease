using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using System.Runtime.CompilerServices;

namespace CalamityFables.Content.Debug
{
    public class DebugItem2 : ModItem
    {
        public override string Texture => AssetDirectory.Debug + Name;

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

        public static AwesomeSentence coolAssSetence;


        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "stopDrops")]
        extern static ref bool GetSetStopDrops(WorldGen w);
        private static WorldGen worldgenRef;

        private int lastSeed = 0;

        public override bool? UseItem(Player player)
        {
            int x = (int)Main.MouseWorld.X / 16;
            int y = (int)Main.MouseWorld.Y / 16;

            foreach (RustyWulfrumChain chain in RustyWulfrumChainsItem.chainManager.PlaceablesByPosition.Values)
            {
                if (chain.Anchor.ToWorldCoordinates().Distance(new Vector2(x, y) * 16f) < 2000f)
                    RustyWulfrumChainsItem.chainManager.RemovePlaceable(chain);
            }

            for (int i = 0; i < Main.maxGore; i++)
                Main.gore[i].active = false;
            for (int i = 0; i < Main.maxItems; i++)
                Main.item[i].active = false;

            if (WulfrumScrapyard.CheckXCoordinate(x, out int surfaceheight, out int bunkerHeight, 1f))
            {
                WulfrumScrapyard.PlaceScrapyard(new Point(x, surfaceheight), bunkerHeight);
                WulfrumScrapyard.PlaceBunker(new Point(x, bunkerHeight), surfaceheight);
            }

            WorldGen.RangeFrame(x - 50, y - 50, x + 50, y + 50);

            return true;
            SealedChamber.NautilusChamberRect = new Rectangle(x, y, (int)SealedChamber.ChamberSize.X, (int)SealedChamber.ChamberSize.Y);
            SealedChamber.InnerChamberRect = new Rectangle(x + (int)((SealedChamber.ChamberSize.X - SealedChamber.ChamberInnerSize.X) / 2f), y + (int)((SealedChamber.ChamberSize.Y - SealedChamber.ChamberInnerSize.Y) / 2f), (int)SealedChamber.ChamberInnerSize.X, (int)SealedChamber.ChamberInnerSize.Y);

            worldgenRef ??= new WorldGen();
            WorldGen.noTileActions = true;
            WorldGen.generatingWorld = true;
            GetSetStopDrops(worldgenRef) = true;

            SealedChamber.PlaceChamber();

            WorldGen.generatingWorld = false;
            WorldGen.noTileActions = false;
            GetSetStopDrops(worldgenRef) = false;

            PointOfInterestMarkerSystem.NautilusChamberPos = new Vector2(SealedChamber.NautilusChamberRect.Center.X, SealedChamber.NautilusChamberRect.Bottom - 8);

            for (int i = 0; i < Main.maxItems; i++)
                Main.item[i].active = false;
            /*
                for (int i = Player.tileTargetX - 15; i < Player.tileTargetX + 16; i++)
                    for (int j = Player.tileTargetY - 15; j < Player.tileTargetY + 16; j++)
                    {
                        Tile t = Main.tile[i, j];
                        if (t.HasTile && t.TileType == TileID.SandstoneBrick)
                            t.TileType = (ushort)ModContent.TileType<SandstoneBrickBootleg>();
                    }

            WorldGen.RangeFrame(Player.tileTargetX - 15, Player.tileTargetY - 15, Player.tileTargetX + 15, Player.tileTargetY + 15);

            */

            //BossIntroScreens.currentCard = ModContent.GetInstance<Crabulon>().GetIntroCard;

            //Projectile proj = Projectile.NewProjectileDirect(player.GetSource_FromAI(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<DesertScourgeElectroblast>(), 2, 10, Main.myPlayer);

            //WorldGen.Pyramid((int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16);

            //NPC npc = NPC.NewNPCDirect(player.GetSource_ItemUse(Item), (int)Main.MouseWorld.X, (int)Main.MouseWorld.Y, ModContent.NPCType<Crabulon>());
            ////npc.ai[0] = 660;

            // npc.scale = 0.4f;
            //npc.dontTakeDamage = false;
            //return true;

            //Projectile proj = Projectile.NewProjectileDirect(player.GetSource_FromAI(), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<KinematicsCreature>(), 2, 10, Main.myPlayer);

            return true;

            //int blastDiameter = 350 + 80;
            //proj.Resize(blastDiameter, blastDiameter);

            if (!player.RemoveKeepsake("NautilusPendant"))
                player.GiveKeepsake("NautilusPendant");

            player.GiveKeepsake("OgsculePendant");
            player.GiveKeepsake("NautilusPendant1");

            player.GiveKeepsake("NautilusPendant2");




            /*
            coolAssSetence = new(600f, null,
                new TextSnippet("Mashallah les amis ", Color.White),
                new TextSnippet(" ", Color.White, 0.4f),
                new TextSnippet("Comment Va La Familia?", Color.White, 0.1f, 1.5f, apparition:delegate(int i, float completion) 
                
                {
                    return new Vector3(0, -(float)Math.Pow(1 - completion, 2.6f) * 14f * (i % 2 == 1 ? 1 : -1), (float)Math.Pow(completion, 2f));


                }
                
                
                , displacement:TextSnippet.RandomDisplacement),
                new TextSnippet(" ", Color.White, 1.4f)
                );
            */

            coolAssSetence = new(600f, null,
                new TextSnippet(" ", Color.White, 1f),
                new TextSnippet("Message ignored ", Color.White),
                new TextSnippet("...  ", Color.White, 0.4f),
                new TextSnippet(" Kill Yourself ", Color.Red, 0.13f, 3.5f, apparition: TextSnippet.AppearFadingFromTop, displacement: TextSnippet.RandomDisplacement),
                new TextSnippet(" ", Color.White, 1f)
                //new TextSnippet("Procreation", Color.GreenYellow, 0.1f, 2.5f, apparition: TextSnippet.AppearFadingFromTop, displacement: TextSnippet.RandomDisplacement),
                //new TextSnippet(", even. Enfin bref, pas que du joli quoi", Color.White, 0.1f, 1f),
                //new TextSnippet(" ", Color.White, 1f)

                ); ;
            ;

            coolAssSetence = new(600f, null,
                new TextSnippet(" ", Color.White, 1f),
                new TextSnippet("What the fuck did you just fucking say about me, you little bitch?", delegate (int i, float c) { return Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, 0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i * 0.4f)); }, 0.03f),
                new TextSnippet(" I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al - Quaeda, and I have over 300 confirmed kills. I am trained in gorilla warfare and I'm the top sniper in the entire US armed forces. You are nothing to me but just another target. I will wipe you the fuck out with precision the likes of which has never been seen before on this Earth, mark my fucking words. You think you can get away with saying that shit to me over the Internet? Think again, fucker. As we speak I am contacting my secret network of spies across the USA and your IP is being traced right now so you better prepare for the storm, maggot. The storm that wipes out the pathetic little thing you call your life. You're fucking dead, kid.I can be anywhere, anytime, and I can kill you in over seven hundred ways, and that's just with my bare hands. Not only am I extensively trained in unarmed combat, but I have access to the entire arsenal of the United States Marine Corps and I will use it to its full extent to wipe your miserable ass off the face of the continent, you little shit. If only you could have known what unholy retribution your little \"clever\" comment was about to bring down upon you, maybe you would have held your fucking tongue. But you couldn't, you didn't, and now you're paying the price, you goddamn idiot. I will shit fury all over you and you will drown in it.You're fucking dead, kiddo.", Color.White, 0.01f, apparition: CharacterDisplacements.AppearFadingFromTopZipper),
                new TextSnippet(" ", Color.White, 1f)
                //new TextSnippet("Procreation", Color.GreenYellow, 0.1f, 2.5f, apparition: TextSnippet.AppearFadingFromTop, displacement: TextSnippet.RandomDisplacement),
                //new TextSnippet(", even. Enfin bref, pas que du joli quoi", Color.White, 0.1f, 1f),
                //new TextSnippet(" ", Color.White, 1f)

                ); ;
            ;


            return true;
        }


        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.LocalPlayer.HeldItem.type != Type)
                return true;

            return true;

            Point startPoint = AStarPathfinding.OffsetUntilNavigable(Main.MouseWorld.ToTileCoordinates(), new Point(0, 1), Crabulon.CrabulonCrawlPathfind);
            Point endPoint = AStarPathfinding.OffsetUntilNavigable(Main.LocalPlayer.Center.ToTileCoordinates(), new Point(0, 1), Crabulon.CrabulonCrawlPathfind);

            AStarPathfinding.IsThereAPath(startPoint, endPoint, Crabulon.CrabulonStride, Crabulon.CrabulonCrawlPathfind, 400f);



            //if (AStarPathfinding.IsThereAPath(startPoint, endPoint, AStarNeighbour.DoubleStride, AStarPathfinding.EdgeRunner, 120f));
            //Dust.QuickDustLine(Main.LocalPlayer.Top + Main.rand.NextVector2CircularEdge(20f, 20f), Main.LocalPlayer.Top + Main.rand.NextVector2CircularEdge(20f, 20f), 10f, Color.Red);

            /*
            for (int i = 1; i < path.Count; i++)
            {
                Color color = Color.Lerp(Color.Red, Color.Yellow, i / (float)path.Count);
                Vector2 start = path[i - 1].ToWorldCoordinates();
                Vector2 end = path[i].ToWorldCoordinates();
                Vector2 scalez = new Vector2(1, start.Distance(end) / TextureAssets.MagicPixel.Value.Height);

                if (Main.GameUpdateCount % 20 == 0)
                    Dust.QuickDustLine(start, end, 2, color);

                //spriteBatch.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, null, color * 0.2f, start.AngleTo(end) + MathHelper.PiOver2, Vector2.One, scalez, 0, 0);
            }
            */


            return true;

            if (coolAssSetence != null)
            {
                Utils.DrawInvBG(spriteBatch, new Rectangle((int)Main.MouseScreen.X + 20, (int)Main.MouseScreen.Y - 10, (int)600 + 20, (int)600 + 16), new Color(50, 20, 35) * 0.925f);

                coolAssSetence.Draw(Main.GlobalTimeWrappedHourly % coolAssSetence.maxProgress, Main.MouseScreen + Vector2.UnitX * 30f, 0f);
            }


            return true;
        }
    }
}