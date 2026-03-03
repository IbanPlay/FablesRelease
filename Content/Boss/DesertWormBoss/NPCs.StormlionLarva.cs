using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class StormlionLarva : ModNPC
    {
        #region setup
        public static readonly SoundStyle DeathSound = new(SoundDirectory.Sounds + "StormlionLarvaDeath");

        public virtual float DesertScourgeSpawnMultiplier => 1;
        public virtual bool DesertScourgeSpawnsPassive => true;
        public virtual bool MinionImmunity => false;

        public override string Texture => AssetDirectory.DesertScourge + Name;

        public bool FrozenInFear { get => NPC.ai[2] == 1; set => NPC.ai[2] = value ? 1f : 0f; }
        public ref float AttractionTimer => ref NPC.ai[3];
        public bool Falling => NPC.velocity.Y != 0;


        public static int BannerType;
        public static AutoloadedBanner bannerTile;
        public override void Load()
        {
            BannerType = BannerLoader.LoadBanner(Name, "Stormlion Larva", AssetDirectory.Banners, out bannerTile);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Larva");
            Main.npcFrameCount[Type] = 5;
            Main.npcCatchable[NPC.type] = true;
            NPCID.Sets.CountsAsCritter[Type] = true;
            NPCID.Sets.ShimmerTransformToNPC[Type] = NPCID.Shimmerfly;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                SpriteDirection = 1
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            bannerTile.NPCType = Type;
        }

        public override void SetDefaults()
        {
            NPC.catchItem = ModContent.ItemType<StormlionLarvaItem>();
            NPC.damage = 0;
            NPC.lifeMax = 5;
            NPC.friendly = false;
            NPC.chaseable = false;
            NPC.width = 26;
            NPC.height = 14;
            NPC.behindTiles = true;
            NPC.dontCountMe = true;

            NPC.dontTakeDamageFromHostiles = true;

            NPC.aiStyle = NPCAIStyleID.CritterWorm;
            NPC.HitSound = SoundID.NPCHit25;
            NPC.DeathSound = DeathSound;
            NPC.direction = Main.rand.NextBool() ? 1 : -1;

            Banner = Type;
            BannerItem = BannerType;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
				new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.StormlionLarva")
            });
        }

        public override void FindFrame(int frameHeight)
        {
            if (Falling)
            {
                NPC.frame.Y = 4;
                NPC.rotation += (0.1f + Math.Abs(NPC.velocity.Y) * 0.016f) * -NPC.spriteDirection;
            }

            else
            {
                NPC.rotation = 0;
                float animSpeed = Math.Abs(NPC.velocity.X) * 0.4f;

                if (NPC.IsABestiaryIconDummy)
                    animSpeed = 0.1f;
                else if (NPC.velocity.X != 0)
                    NPC.spriteDirection = -NPC.velocity.X.NonZeroSign();

                NPC.frameCounter += animSpeed;
                NPC.frame.Y = (int)(NPC.frameCounter % 4);
            }

            NPC.frame = new Rectangle(0, NPC.frame.Y * frameHeight, 40, frameHeight - 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.PlayerSafe || !spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneBeach)
                return 0f;

            if (NPC.AnyNPCs(ModContent.NPCType<DesertScourge>()))
                return 0f;

            float spawnRate = SpawnCondition.OverworldDayDesert.Chance * 0.83f;

            //Spawns less frequently in the underground desert
            if (spawnInfo.Player.ZoneUndergroundDesert)
                spawnRate = SpawnCondition.DesertCave.Chance * 0.1f;

            //If the player isnt in the ug desert
            else
            {
                //Can only spawn on clear surface if the player isnt in the ug desert (aka the larva cant spawn inside a high up cave)
                if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].WallType != 0)
                    return 0f;

                //Only one max (on the surface)
                if (NPC.AnyNPCs(ModContent.NPCType<StormlionLarva>()))
                    return 0f;
            }


            return spawnRate;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ModContent.ItemType<DeadStormlionLarvaItem>());
        }
        public override bool? CanFallThroughPlatforms() => true;

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (MinionImmunity && Main.player[projectile.owner].MinionAttackTargetNPC != NPC.whoAmI && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
                return false;

            return base.CanBeHitByProjectile(projectile);
        }
        #endregion

        public override bool PreAI()
        {
            //Just in case a invincible "struck in fear" larvae stays invincible
            if (NPC.dontTakeDamage && !NPC.AnyNPCs(ModContent.NPCType<DesertScourge>()))
                NPC.dontTakeDamage = false;

            if (NPC.oldVelocity.Y > 1.2f && !Falling)
                SoundEngine.PlaySound(SoundID.NPCHit19, NPC.Center);

            if (NPC.type == ModContent.NPCType<StormlionLarva>())
            {
                if (Main.rand.NextBool(4))
                {
                    Vector2 dustPosition = Main.rand.NextVector2Circular(NPC.width * 1.2f, NPC.height);
                    if (dustPosition.Y > 0)
                        dustPosition.Y *= -1;

                    Dust dus = Dust.NewDustPerfect(NPC.Bottom + dustPosition, 202, -Vector2.UnitY * 1f * Main.rand.NextFloat(0.5f, 1.2f), 122);
                    dus.noGravity = true;
                    dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4);
                    dus.noLight = true;
                }

                Lighting.AddLight(NPC.Center, Color.RoyalBlue.ToVector3() * (0.45f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly)));
            }

            if (FrozenInFear)
            {
                //Keep setting donttakedamage just in case for mp purposes
                NPC.dontTakeDamage = true;
                NPC.dontTakeDamageFromHostiles = true;
                ScaryVisuals(AttractionTimer);
                return false;
            }

            //If DS is already here just reset
            bool DSPresent = NPC.AnyNPCs(ModContent.NPCType<DesertScourge>());
            if (DSPresent)
            {
                AttractionTimer = MathHelper.Lerp(AttractionTimer, 0, 0.07f);
                if (AttractionTimer < 0.06f)
                    AttractionTimer = 0;
                return base.PreAI();
            }

            //Automatically increase attraction if we're almost done
            bool increaseAttraction = AttractionTimer > 0.95f;


            //Stormlion larvae (live) wont attract DS after its death
            if (NPC.type == ModContent.NPCType<StormlionLarva>() && (WorldProgressionSystem.DefeatedDesertScourge && !CalamityFables.DesertScourgeDemo))
                increaseAttraction = false;

            else if (!increaseAttraction)
            {
                Player nearestPlayer = Main.player.Where
                     (p => p.active && !p.dead && p.ZoneDesert && !p.ZoneUndergroundDesert &&
                     p.Distance(NPC.Center) < 1000 &&
                     (Collision.CanHitLine(p.Top, 1, 1, NPC.Center, 1, 1) || Collision.CanHitLine(p.Top, 1, 1, NPC.Center - Vector2.UnitY * 60f, 1, 1)))
                     .OrderBy(p => p.Distance(NPC.Center))
                     .FirstOrDefault();

                if (nearestPlayer != default)
                    increaseAttraction = true;
            }

            if (increaseAttraction)
            {
                //Takes 20 seconds to summon DS
                AttractionTimer += 1 / (60f * 20f) * DesertScourgeSpawnMultiplier;

                if (AttractionTimer > 0.75f)
                {
                    ScaryVisuals((AttractionTimer - 0.75f) / 0.25f);
                }

                if (AttractionTimer > 1f)
                    SpawnDesertScourge();
            }

            return base.PreAI();
        }

        public void ScaryVisuals(float effectIntensity)
        {
            RumblingSandEffects(4f * effectIntensity);

            effectIntensity *= Utils.GetLerpValue(1300f, 900f, Main.LocalPlayer.Distance(NPC.Center), true);
            if (effectIntensity > 0)
            {
                if (CameraManager.Shake < 4f * effectIntensity)
                    CameraManager.Shake = 4f * effectIntensity;

                if (NPC.type == ModContent.NPCType<DeadStormlionLarva>())
                    return;

                ScreenDesaturation.desaturationOverride = 0.4f * effectIntensity;
                VignetteFadeEffects.vignetteOpacityOverride = (float)Math.Pow(effectIntensity, 2f) * 0.5f;
            }
        }

        public void SpawnDesertScourge()
        {
            AttractionTimer = 1f;
            FrozenInFear = true;
            NPC.dontTakeDamage = true;
            NPC.dontTakeDamageFromHostiles = true;
            NPC.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int playerTarget = Player.FindClosest(NPC.Center, NPC.width, NPC.height);

                int desertScourgeCount = 1;
                if (Main.getGoodWorld)
                    desertScourgeCount = 7;

                for (int i = 0; i < desertScourgeCount; i++)
                {
                    float spawnPassive = DesertScourgeSpawnsPassive ? 1f : 0f;
                    Vector2 spawnPosition = NPC.Center + Vector2.UnitY * 1400;
                    if (i > 0)
                        spawnPosition.X += i * 200f;

                    int scourge = NPC.NewNPC(NPC.GetSource_FromThis(), (int)spawnPosition.X, (int)spawnPosition.Y + 1400, ModContent.NPCType<DesertScourge>(), 0, 0, 0, spawnPassive, NPC.whoAmI, Target: playerTarget);
                    if (scourge < 200 && Main.dedServ)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, scourge);
                }
            }
        }

        public void RumblingSandEffects(float speed)
        {
            if (Falling)
                return;

            bool onTheGround = false;
            for (int i = (int)(NPC.BottomLeft.X / 16); i < NPC.BottomRight.X / 16 + 1; i++)
            {
                Tile bottomTile = Main.tile[i, (int)(NPC.Bottom.Y / 16)];
                if (bottomTile.IsTileSolid())
                {
                    onTheGround = true;
                    break;
                }
            }

            if (!onTheGround)
                return;

            for (int d = 0; d < 2; d++)
            {
                Vector2 dustPosition = NPC.Bottom + Vector2.UnitX * Main.rand.NextFloat(-1f, 1f) * NPC.width * 1.4f;

                Dust dus = Dust.NewDustPerfect(dustPosition, DustID.Sand, -Vector2.UnitY * speed * Main.rand.NextFloat(0.5f, 1.2f), 0);
                dus.noGravity = false;
                dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 238, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 20; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 238, hit.HitDirection, -1f, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            if (NPC.IsABestiaryIconDummy)
                lightColor = Color.White;

            DrawMenacingEffects(spriteBatch, screenPos, lightColor);

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Texture2D outline = ModContent.Request<Texture2D>(Texture + "Glow").Value;

            Rectangle frame = NPC.frame;
            Rectangle outlineFrame = outline.Frame(1, 5, 0, (int)(NPC.frame.Y / 28), 0, -1);

            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
            Vector2 outlineOrigin = new Vector2(outlineFrame.Width / 2, outlineFrame.Height);

            Vector2 position = NPC.Bottom + Vector2.UnitY * 2f;
            Vector2 outlinePosition = NPC.Bottom + Vector2.UnitY * 5f;

            SpriteEffects flip = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (Falling)
            {
                position = outlinePosition = NPC.Center;
                origin = frame.Size() / 2;
                outlineOrigin = outlineFrame.Size() / 2;
                outlineOrigin.Y -= 1;
            }

            Color outlineColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly, Color.RoyalBlue, Color.Goldenrod);
            lightColor = NPC.GetNPCColorTintedByBuffs(lightColor);

            spriteBatch.Draw(outline, outlinePosition - screenPos, outlineFrame, outlineColor, NPC.rotation, outlineOrigin, NPC.scale, flip, 0);
            spriteBatch.Draw(texture, position - screenPos, frame, lightColor, NPC.rotation, origin, NPC.scale, flip, 0);
            return false;
        }

        public void DrawMenacingEffects(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            if (Type == ModContent.NPCType<DeadStormlionLarva>() || NPC.IsABestiaryIconDummy)
                return;

            Vector2 bloomCenter = NPC.Center;
            if (!Falling)
                bloomCenter = NPC.Bottom - Vector2.UnitY * 5f;

            Texture2D ligjht = AssetDirectory.CommonTextures.BigBloomCircle.Value;
            float inverseBloomSizeMult = 0.7f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly);

            float bloomOpacity = AttractionTimer;
            inverseBloomSizeMult *= 1f + AttractionTimer * 0.5f;

            Effect effect = Scene["RadialWarp"].GetShader().Shader;

            effect.Parameters["noiseTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "PerlinNoise").Value);
            effect.Parameters["minRadius"].SetValue(0.05f);
            effect.Parameters["lerpStrenght"].SetValue(1f);
            effect.Parameters["radiusDisplacement"].SetValue(0.1f);
            effect.Parameters["noiseScale"].SetValue(0.6f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.1f + NPC.whoAmI * 0.04f);
            effect.Parameters["color"].SetValue(Color.Black.ToVector4() * 0.5f * bloomOpacity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(ligjht, bloomCenter - screenPos, null, Color.White * 0.4f, 0, ligjht.Size() / 2f, 0.5f * inverseBloomSizeMult, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ligjht, bloomCenter - screenPos, null, Color.White * 0.2f, 0, ligjht.Size() / 2f, 0.7f * inverseBloomSizeMult, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    public class DeadStormlionLarva : StormlionLarva
    {
        #region setup
        public override float DesertScourgeSpawnMultiplier => 10f;
        public override bool DesertScourgeSpawnsPassive => false;
        public override bool MinionImmunity => true;

        public override string Texture => AssetDirectory.DesertScourge + Name;
        public override void Load() { }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dead Stormlion Larva");
            Main.npcFrameCount[Type] = 2;
            NPCID.Sets.CountsAsCritter[Type] = true;
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.lifeMax = 5;
            NPC.friendly = false;
            NPC.chaseable = false;
            NPC.width = 26;
            NPC.height = 14;

            NPC.aiStyle = -1;

            NPC.HitSound = SoundID.NPCHit25;
            NPC.DeathSound = SoundID.NPCDeath47;

            NPC.direction = Main.rand.NextBool() ? 1 : -1;
            NPC.dontTakeDamageFromHostiles = false;
        }

        public override void FindFrame(int frameHeight)
        {
            if (Falling)
            {
                NPC.frame.Y = 1;
                NPC.rotation += (0.1f + Math.Abs(NPC.velocity.Y) * 0.016f) * -NPC.spriteDirection;
            }

            else
            {
                NPC.rotation = 0;
                if (NPC.velocity.X != 0)
                    NPC.spriteDirection = -NPC.velocity.X.NonZeroSign();
                NPC.frame.Y = 0;
            }

            NPC.frame = new Rectangle(0, NPC.frame.Y * frameHeight, 40, frameHeight - 2);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;
        #endregion

        public override bool PreAI()
        {
            NPC.velocity.X *= 0.98f;
            if (!Falling)
                NPC.velocity.X *= 0.3f;

            return base.PreAI();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            DrawMenacingEffects(spriteBatch, screenPos, lightColor);
            Texture2D texture = TextureAssets.Npc[Type].Value;

            Rectangle frame = NPC.frame;
            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
            Vector2 position = NPC.Bottom + Vector2.UnitY * 2f;
            SpriteEffects flip = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (Falling)
            {
                position = NPC.Center;
                origin = frame.Size() / 2;
            }

            lightColor = NPC.GetNPCColorTintedByBuffs(lightColor);
            spriteBatch.Draw(texture, position - screenPos, frame, lightColor, NPC.rotation, origin, NPC.scale, flip, 0);
            return false;
        }

        //Don't make it drop itself
        public override void ModifyNPCLoot(NPCLoot npcLoot) { }
    }

    public class Fibsh : DeadStormlionLarva
    {
        public override float DesertScourgeSpawnMultiplier => 9f;
        public override string Texture => AssetDirectory.DesertScourge + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Walbert");
            Main.npcFrameCount[Type] = 2;
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.catchItem = 0; //Can't catch walbert
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = -NPC.velocity.X.NonZeroSign();

            NPC.frameCounter += 0.2f;
            NPC.frame.Y = (int)(NPC.frameCounter % 2);

            NPC.frame = new Rectangle(0, NPC.frame.Y * frameHeight, 40, frameHeight - 2);
        }

        public override bool PreAI()
        {
            if (!base.PreAI())
                return false;


            if (NPC.wet)
            {
                SwimmingAI();
                return true;
            }

            NPC.noGravity = false;
            bool slap = false;

            if (NPC.collideY)
            {
                NPC.velocity.Y *= -1.3f;

                if (Main.rand.NextBool())
                    NPC.velocity.X *= -1;

                if (Math.Abs(NPC.velocity.Y) < 1f)
                {
                    NPC.velocity.Y = -4f;

                    if (Math.Abs(NPC.velocity.X) < 1f)
                    {
                        NPC.velocity.X = Main.rand.NextFloat(-3f, 3f);

                    }
                }

                slap = true;
            }

            if (NPC.collideX)
            {
                NPC.velocity.Y *= -1.1f;
                slap = true;
            }

            if (slap)
                SoundEngine.PlaySound(DesertScourge.SlapLarvaIntoTheAir, NPC.Center);

            NPC.rotation = Vector2.Dot(NPC.velocity, -Vector2.UnitX) * MathHelper.PiOver4 * 0.7f;

            return true;
        }

        public void SwimmingAI()
        {
            //Adapted from vanilla swim behavior
            if (NPC.rotation != 0f)
                NPC.rotation *= .9f;

            NPC.noGravity = true;

            Point tilePos = NPC.Center.ToTileCoordinates();
            Tile centerTile = Main.tile[tilePos];
            Tile tileBelow = Main.tile[tilePos + new Point(0, 1)];
            Tile tileAbove = Main.tile[tilePos + new Point(0, -1)];


            //Handle slopes
            if (centerTile.TopSlope)
            {
                if (centerTile.LeftSlope)
                {
                    NPC.direction = -1;
                    NPC.velocity.X = -Math.Abs(NPC.velocity.X);
                }
                else
                {
                    NPC.direction = 1;
                    NPC.velocity.X = Math.Abs(NPC.velocity.X);
                }
            }
            else if (tileBelow.TopSlope)
            {
                if (tileBelow.LeftSlope)
                {
                    NPC.direction = -1;
                    NPC.velocity.X = Math.Abs(NPC.velocity.X) * -1f;
                }
                else
                {
                    NPC.direction = 1;
                    NPC.velocity.X = Math.Abs(NPC.velocity.X);
                }
            }

            //Flip around
            if (NPC.collideX)
            {
                NPC.velocity.X *= -1f;
                NPC.direction *= -1;
                NPC.netUpdate = true;
            }
            //Bonk on tiles vertically..?
            if (NPC.collideY)
            {
                NPC.netUpdate = true;
                if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = Math.Abs(NPC.velocity.Y) * -1f;
                    NPC.directionY = -1;
                }
                else if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y = Math.Abs(NPC.velocity.Y);
                    NPC.directionY = 1;
                }
            }

            NPC.velocity.X += (float)NPC.direction * 0.1f;
            float maxSwimSpeed = 1f;
            if (Math.Abs(NPC.velocity.X) < maxSwimSpeed)
                NPC.velocity.X *= 0.95f;

            NPC.velocity.Y += NPC.directionY * 0.01f;
            if (NPC.velocity.Y * NPC.directionY > 0.3f)
                NPC.directionY *= -1;
            
            if (tileAbove.LiquidAmount > 128)
            {
                if (tileBelow.HasTile)
                    NPC.directionY = -1;
                else if (Main.tile[tilePos + new Point(0, 2)].HasTile)
                    NPC.directionY = -1;
            }

            if (Math.Abs(NPC.velocity.Y) > 0.4f)
                NPC.velocity.Y *= 0.95f;
        }

    
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            DrawMenacingEffects(spriteBatch, screenPos, lightColor);
            Texture2D texture = TextureAssets.Npc[Type].Value;

            Rectangle frame = NPC.frame;
            Vector2 origin = frame.Size() / 2; ;
            Vector2 position = NPC.Center;
            SpriteEffects flip = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            lightColor = NPC.GetNPCColorTintedByBuffs(lightColor);

            spriteBatch.Draw(texture, position - screenPos, frame, lightColor, NPC.rotation, origin, NPC.scale, flip, 0);
            return false;
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, hit.HitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 20; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, hit.HitDirection, -1f, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                }
            }
        }

    }
}
