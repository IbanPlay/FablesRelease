using CalamityFables.Particles;
using Terraria.WorldBuilding;
using CalamityFables.Content.Boss.DesertWormBoss;
using static CalamityFables.Content.NPCs.GeodeGrawlers.GeodeCrawler;
using System.IO;
using CalamityFables.Content.NPCs.GeodeGrawlers;

namespace CalamityFables.Content.Items.VanityMisc
{
    [ReplacingCalamity("ScuttlersJewel")]
    public class Geodecorn : ModItem
    {
        public override string Texture => AssetDirectory.MiscVanity + "SmeeboItem";

        public override void Load()
        {
            On_WorldGen.SetGemTreeDrops += DropFromGemTrees;
        }

        private void DropFromGemTrees(On_WorldGen.orig_SetGemTreeDrops orig, int gemType, int seedType, Tile tileCache, ref int dropItem, ref int secondaryItem)
        {
            orig(gemType, seedType, tileCache, ref dropItem, ref secondaryItem);

            if (secondaryItem == seedType && Main.rand.NextBool(15))
                secondaryItem = Type;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ShadowOrb);

            Item.shoot = ModContent.ProjectileType<GeodeCrawlerPet>();
            Item.buffType = ModContent.BuffType<GeodecornBuff>();

            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(0, 5, 0, 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.GemTreeAmethystSeed)
                .AddIngredient(ItemID.GemTreeTopazSeed)
                .AddIngredient(ItemID.GemTreeSapphireSeed)
                .AddIngredient(ItemID.GemTreeEmeraldSeed)
                .AddIngredient(ItemID.GemTreeRubySeed)
                .AddIngredient(ItemID.GemTreeDiamondSeed)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600);
            return true;
        }
    }

    public class GeodecornBuff : ModBuff
    {
        public override string Texture => AssetDirectory.MiscVanity + "Smeebo";
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.lightPet[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            bool _ = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<GeodeCrawlerPet>());
        }
    }

    public class GeodeCrawlerPet : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.MiscVanity + Name;

        public bool Flying {
            get => Projectile.ai[0] == 1;
            set {
                Projectile.ai[0] = value.ToInt();
                FlightTimer = 0;
                Projectile.netUpdate = true;
            }
        }

        public ref float FlightTimer => ref Projectile.ai[1];

        public CrystalType[] CrystalTypes
        {
            get => new CrystalType[3] { (CrystalType)Projectile.localAI[0], (CrystalType)Projectile.localAI[1], (CrystalType)Projectile.localAI[2] };
            set
            {
                Projectile.localAI[0] = (int)value[0];
                Projectile.localAI[1] = (int)value[1];
                Projectile.localAI[2] = (int)value[2];
            }
        }

        public Color AverageColor
        {
            get
            {
                Vector3 colorVector = Vector3.Zero;
                for (int i = 0; i < CrystalTypes.Length; i++)
                {
                    if (CrystalColorTable.TryGetValue(CrystalTypes[i], out Color c))
                        colorVector += c.ToVector3() * (i == 0 ? 6f : 1f);
                }
                colorVector = Vector3.Clamp(colorVector / (CrystalTypes.Length + 5f), Vector3.Zero, Vector3.One);
                return new(colorVector);
            }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 13;
            Main.projPet[Type] = true;
            ProjectileID.Sets.LightPet[Type] = true;

            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] =
                ProjectileID.Sets.SimpleLoop(0, 6, 5);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeOfCthulhuPet);
            Projectile.aiStyle = -1;
            Projectile.width = 28;
            Projectile.height = 36;
            Projectile.ignoreWater = true;
            GetRandomGemColors();
        }

        public void GetRandomGemColors()
        {
            for (int i = 0; i < 3; i++)
                Projectile.localAI[i] = (int)Main.rand.NextFromList(CrystalType.Amethyst, CrystalType.Topaz, CrystalType.Sapphire, CrystalType.Emerald, CrystalType.Ruby, CrystalType.Diamond);

            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            if (!Owner.active)
            {
                Projectile.active = false;
                return;
            }


            if (!Owner.dead && Owner.HasBuff<GeodecornBuff>())
                Projectile.timeLeft = 2;

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            if (distanceToPlayer > 2000)
                Projectile.Center = Owner.Center;

            float horizontalDistanceToPlayer = Math.Abs(Owner.Center.X - Projectile.Center.X);
            float verticalDistanceToPlayer = Math.Abs(Owner.Center.Y - Projectile.Center.Y);

            if (!Flying)
            {
                Projectile.tileCollide = true;
                Projectile.rotation = 0;

                if (FlightTimer > 0 || Projectile.velocity.Y == 0)
                FlightTimer++;

                bool shouldJump = false;

                //Catch up to the player whle flying if too far
                if (distanceToPlayer > 500 || verticalDistanceToPlayer > 300 || Owner.rocketDelay2 > 0)
                {
                    Flying = true;
                    if (Main.netMode != NetmodeID.Server)
                        ShedCrystals();
                }

                //Go towards the player
                else if (horizontalDistanceToPlayer > 100)
                {
                    int direction = (Owner.Center.X - Projectile.Center.X).NonZeroSign();
                    float acceleration = 0.04f;
                    if (Projectile.velocity.X.NonZeroSign() != direction)
                        acceleration *= 2f;

                    float maxSpeed = 6f;
                    if (Owner.velocity.Length() > maxSpeed)
                        maxSpeed = Owner.velocity.Length();

                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, maxSpeed * direction, acceleration);

                    //Cjeck ahead for obstacles
                    Point tilePos = new Point((int)Projectile.Center.X / 16, ((int)Projectile.Bottom.Y - 4) / 16);
                    tilePos.X += Projectile.velocity.X.NonZeroSign();
                    tilePos.X += (int)(Projectile.velocity.X);

                    if (WorldGen.SolidTile(tilePos))
                        shouldJump = true;
                }
                else
                {
                    //Slowdown
                    Projectile.velocity.X *= 0.9f;
                    if (Math.Abs(Projectile.velocity.X) < 0.2f)
                        Projectile.velocity.X = 0f;
                }

                Collision.StepUp(ref Projectile.position, ref Projectile.velocity, Projectile.width, Projectile.height, ref Projectile.stepSpeed, ref Projectile.gfxOffY);

                //Boing
                if (Projectile.velocity.Y == 0f && shouldJump)
                {
                    float bounceStrenght = 4;
                    //Jump harder when further away
                    bounceStrenght += Utils.GetLerpValue(140, 500, distanceToPlayer, true) * 4f;

                    Point tilePos = Projectile.Center.ToTileCoordinates();
                    tilePos.X += Projectile.velocity.X.NonZeroSign();
                    tilePos.X += (int)(Projectile.velocity.X);
                    tilePos.Y -= (int)(Utils.GetLerpValue(4, 7, bounceStrenght, true) * 2.9f);

                    for (int i = 0; i < 4; i++)
                    {
                        if (!WorldGen.InWorld(tilePos.X, tilePos.Y) || bounceStrenght > 11)
                            break;

                        Tile t = Main.tile[tilePos];
                        if (WorldGen.SolidTile(t))
                            bounceStrenght += 2.5f;

                        tilePos.Y--;
                    }

                    Projectile.velocity.Y = -bounceStrenght;
                }

                //Fall
                if (Projectile.velocity.Y < 10)
                    Projectile.velocity.Y += 0.52f;

                //Sitting still
                if (Math.Abs(Projectile.velocity.X) < 0.1f)
                    Projectile.frame = 0;
                //Animating
                else
                {
                    if (++Projectile.frameCounter > 5)
                    {
                        Projectile.frame++;
                        Projectile.frameCounter = 0;
                    }
                    //Loop
                    if (Projectile.frame > 5)
                        Projectile.frame = 1;
                }

                if (Projectile.velocity.X != 0)
                    Projectile.spriteDirection = Projectile.velocity.X.NonZeroSign();
            }

            //Flying
            else
            {
                Projectile.tileCollide = false;

                float aheadPlayerPoint = Owner.Center.X + Owner.direction * 70f;
                Projectile.spriteDirection = (aheadPlayerPoint - Projectile.Center.X).NonZeroSign();

                Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * 30f + Vector2.UnitX * Owner.direction * 30;

                //Have a lil offset
                Vector2 idealPositionOffset = Vector2.UnitY.RotatedBy(FlightTimer * 0.08f * Owner.direction) * 40f;
                idealPositionOffset.Y *= 0.6f;
                idealPosition += idealPositionOffset;

                idealPositionOffset = Vector2.UnitY.RotatedBy(FlightTimer * 0.2f * Owner.direction) * 60f;
                idealPositionOffset.Y *= 0.8f;
                idealPosition += idealPositionOffset;

                Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;

                FlightTimer += 0.2f + 0.8f * Utils.GetLerpValue(290f, 0f, distanceToPlayer, true);


                float approachAcceleration = 0.1f + MathF.Pow(Utils.GetLerpValue(70, 0, distanceToPlayer, true), 2f) * 0.3f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
                Projectile.velocity *= 0.98f;

                Projectile.rotation = Projectile.velocity.X * 0.07f;
                Projectile.rotation = Math.Clamp(Projectile.rotation, -0.2f, 0.2f);

                //Stop flying
                if (distanceToPlayer < (float)200 && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    //Regenerate a new coat
                    GetRandomGemColors();
                    Flying = false;
                }


                if (Projectile.frame < 6)
                {
                    Projectile.frame = 6;
                }

                //Animation
                if (++Projectile.frameCounter > 1)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.frame > 12)
                    Projectile.frame = 8;
            }

            // Randomly emit sparkles off the crystals.
            if (!Flying && Main.rand.NextBool(32))
            {
                Color dustColor = CrystalColorTable.GetValueOrDefault(CrystalTypes[Main.rand.Next(3)], Color.Gold) * 2f;
                Vector2 sparkleSpawnPosition = Projectile.Center + new Vector2(-Projectile.spriteDirection * 6f, 2f).RotatedBy(Projectile.rotation) + Main.rand.NextVector2Circular(7f, 7f);
                Dust sparkle = Dust.NewDustPerfect(sparkleSpawnPosition, 267, Vector2.Zero, 0, dustColor);
                sparkle.scale = 0.3f;
                sparkle.fadeIn = Main.rand.NextFloat(1.2f);
                sparkle.noLightEmittence = true;
                sparkle.noGravity = true;
            }


            Color glowColor = CrystalColorTable.GetValueOrDefault(CrystalTypes[0], Color.Gold) * 0.4f;
            Lighting.AddLight(Projectile.Center, glowColor.ToVector3());
        }

        public void ShedCrystals()
        {
            for (int i = 0; i < 3; i++)
            {
                // Create the crystal gores.
                Color crystalColor = Color.Transparent;
                CrystalType crystalType = i < CrystalTypes.Length ? CrystalTypes[i] : CrystalTypes.Last();
                if (CrystalColorTable.TryGetValue(crystalType, out Color c))
                    crystalColor = c;

                GeodeCrawlerPetCrystalGore gore = new(Projectile.Center, -Vector2.UnitY.RotatedByRandom(1.6f) * Main.rand.NextFloat(0.5f, 5f), crystalColor, Projectile.scale);
                ParticleHandler.SpawnParticle(gore);
            }

            // Create some sparkle particles.
            for (int i = 0; i < 6; i++)
            {
                Color dustColor = Color.Lerp(AverageColor, Color.Pink, Main.rand.NextFloat(0.16f)) * 1.6f;
                Vector2 sparkleSpawnPosition = Projectile.Center + new Vector2(-Projectile.spriteDirection * 12f, -5f).RotatedBy(Projectile.rotation) + Main.rand.NextVector2Circular(30f, 7f);
                Dust sparkle = Dust.NewDustPerfect(sparkleSpawnPosition, 267, -Vector2.UnitY.RotatedByRandom(0.91f) * Main.rand.NextFloat(4f), 0, dustColor);
                sparkle.scale = 0.9f;
                sparkle.fadeIn = Main.rand.NextFloat(1.2f);
                sparkle.noLightEmittence = true;
                sparkle.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTex = TextureAssets.Projectile[Type].Value;
            Rectangle frame = projectileTex.Frame(7, 13, 0, Projectile.frame, -2, -2);
            Vector2 origin = new Vector2(frame.Width / 2, frame.Height / 2);

            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;


            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation;

            Vector2 scale = Vector2.One * Projectile.scale;
            if (!Flying)
            {
                drawPosition = Projectile.Bottom - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
                origin.Y = frame.Height;
            }

            // Draw the smeebo.
            Main.EntitySpriteDraw(projectileTex, drawPosition, frame, lightColor, drawRotation, origin, scale, effects, 0);

            if (Projectile.frame > 5 || FlightTimer == 0)
                return false;

            //Draw the crystals
            for (int i = 0; i < CrystalTypes.Length; i++)
            {
                CrystalColorTable.TryGetValue(CrystalTypes[i], out Color c);
                Color crystalColor = c;


                if (lightColor.R < 100)
                    lightColor.R = 100;
                if (lightColor.G < 100)
                    lightColor.G = 100;
                if (lightColor.B < 100)
                    lightColor.B = 100;


                //Shell glows with spelunker
                if (Main.LocalPlayer.findTreasure)
                {
                    if (lightColor.R < 200)
                        lightColor.R = 200;
                    if (lightColor.G < 170)
                        lightColor.G = 170;
                }


                crystalColor = crystalColor.MultiplyRGB(lightColor);

                Rectangle crystalFrame = frame;
                crystalFrame.X += 48 * (i + 1);
                crystalFrame.Y += 72;

                Main.spriteBatch.Draw(projectileTex, drawPosition, crystalFrame, crystalColor, drawRotation, origin, scale, effects, 0f);

                float crystalReformProgress = MathF.Pow(Utils.GetLerpValue(30f + i * 4f, 0f, FlightTimer, true), 1.5f);
                if (crystalReformProgress > 0f)
                    Main.spriteBatch.Draw(projectileTex, drawPosition, crystalFrame, (Color.White with { A = 0 }) * crystalReformProgress, drawRotation, origin, scale, effects, 0f);


                // Draw crystal highlights.
                if (CrystalHighlightColorTable.TryGetValue(CrystalTypes[i], out c))
                {
                    crystalFrame.X += 144;
                    crystalColor = Color.Lerp(c, c.MultiplyRGB(lightColor), 0.9f);
                    if (CrystalTypes[i] == CrystalType.Sapphire)
                        crystalColor *= 0.8f;
                    crystalColor.A /= 2;
                    Main.spriteBatch.Draw(projectileTex, drawPosition, crystalFrame, crystalColor, drawRotation, origin, scale, effects, 0f);
                }
            }

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(Projectile.localAI[2]);

        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            Projectile.localAI[2] = reader.ReadSingle();
        }
    }

    public class GeodeCrawlerPetCrystalGore : GeodeCrawlerCrystalGore
    {
        public override string Texture => $"{AssetDirectory.MiscVanity}GemcornGores/GeodeCrawlerPet_CrystalGore{Variant + 1}";

        public override bool UseCustomDraw => true;

        public GeodeCrawlerPetCrystalGore(Vector2 position, Vector2 speed, Color color, float scale = 1f) : base(position, speed, color, scale)
        {
            Variant = Main.rand.Next(3);
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            float opacity = Utils.GetLerpValue(0f, -180f, Time - Lifetime, true);
            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());
            Texture2D texture = ParticleTexture;
            Texture2D highlight = ModContent.Request<Texture2D>($"{AssetDirectory.MiscVanity}GemcornGores/GeodeCrawlerPet_CrystalGore{Variant + 1}").Value;
            spriteBatch.Draw(texture, Position - basePosition, null, Color.MultiplyRGBA(lightColor) * opacity, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
            spriteBatch.Draw(texture, Position - basePosition, null, Color.MultiplyRGBA(lightColor) with { A = 25 } * opacity, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
            spriteBatch.Draw(highlight, Position - basePosition, null, lightColor * opacity * 0.6f, Rotation, texture.Size() * 0.5f, Scale, 0, 0);
        }
    }
}
