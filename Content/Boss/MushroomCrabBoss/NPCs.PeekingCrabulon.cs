using CalamityFables.Particles;
using ReLogic.Utilities;
using System.Text;
using Terraria.DataStructures;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class PeekingCrabulon : ModNPC, IDrawBehindTiles
    {
        public enum ActionState
        {
            StayStill,
            MoveAround,
            ScurryingBackUp,
            ScurryDownToDespawn,
            ScurryDownToRelocate,
            ScurryDownToSpawnCrabulon
        }

        public ActionState AIState {
            get => (ActionState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float ActionTimer => ref NPC.ai[1];

        public int DigRectangleWidth => 60;
        public int DigRectangleHeight => 30;
        public Rectangle InsideRectangle => new Rectangle((int)(NPC.Center.X - DigRectangleWidth / 2), (int)NPC.Bottom.Y, DigRectangleWidth, DigRectangleHeight);

        public ref float GoAwayTimer => ref NPC.ai[3];
        public float GoAwayTime => 30f;

        public bool ScurryingDown => (int)AIState >= (int)ActionState.ScurryDownToDespawn;

        public override string Texture => AssetDirectory.Crabulon + "CrabulonPeeking";

        public override void Load()
        {
            FablesNPC.EditSpawnRateEvent += LowerSpawnRatesWhenAlive;
            FablesPlayer.CanHitNPCEvent += ImmuneToMinecarts;
        }

        private bool ImmuneToMinecarts(Player player, NPC target)
        {
            if (target.type == Type && player.mount.Active && player.mount.Cart)
            {
                //Swinging a damage item means it deals damage
                if (player.HeldItem != null && !player.HeldItem.noMelee && player.HeldItem.damage > 0 && player.ItemAnimationActive)
                    return true;

                //Can hit if further away
                if (player.DistanceSQ(target.Center) > 10000)
                    return true;

                return false;
            }
            return true;
        }

        private void LowerSpawnRatesWhenAlive(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (NPC.AnyNPCs(Type) && player.ZoneGlowshroom)
            {
                spawnRate *= 3;
                maxSpawns = (int)(maxSpawns * 0.5f);
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crabulon");
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.lifeMax = 1;

            NPC.width = 40;
            NPC.height = 70;
            NPC.aiStyle = -1;
            AIType = -1;

            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;

            NPC.behindTiles = true;
            NPC.hide = true;

            NPC.HitSound = Crabulon.HitSound;
        }

        public override void DrawBehind(int index)
        {
            FablesNPC.DrawBehindTilesAlways.Add(index);
        }

        public override void OnSpawn(IEntitySource source)
        {
            AIState = ActionState.ScurryDownToRelocate;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            //Don't spawn if crabulon already spawned
            if (WorldProgressionSystem.DefeatedCrabulon)
                return 0f;

            //Only spawn in mushroom biome
            if (spawnInfo.SpawnTileType != TileID.MushroomGrass || !spawnInfo.Player.ZoneGlowshroom || spawnInfo.PlayerSafe || (!spawnInfo.Player.ZoneDirtLayerHeight && !spawnInfo.Player.ZoneRockLayerHeight))
                return 0f;

            if (NPC.AnyNPCs(Type) || NPC.AnyNPCs(ModContent.NPCType<Crabulon>()))
                return 0f;

            //heightened chance if post evil boss, or if you havent seen crabulon yet
            if (NPC.downedBoss2 || !WorldProgressionSystem.encounteredCrabulon)
                return 0.15f;

            //lower chance beforehand
            return 0.065f;
        }

        public override void AI()
        {
            GoAwayTimer++;

            //Despawn if alive for too long or if crabulon has already been spawned
            if (AIState != ActionState.ScurryDownToDespawn && AIState != ActionState.ScurryDownToSpawnCrabulon && ((GoAwayTimer > GoAwayTime * 60f) || NPC.AnyNPCs(ModContent.NPCType<Crabulon>())))
            {
                AIState = ActionState.ScurryDownToDespawn;
                ActionTimer = 1f;
            }

            NPC.TargetClosest();

            if (!WorldProgressionSystem.encounteredCrabulon && Main.player[NPC.target].WithinRange(NPC.Center, 600))
                WorldProgressionSystem.encounteredCrabulon = true;

            if (!Main.dedServ)
            {
                if (Props == null)
                {
                    Props = new List<VisualProp>()
                    {
                        new VisualProp(this, new Vector2(-12, -11),  0.1f, 0.3f, MathHelper.PiOver2 * 1.4f, "CrabulonPeekingWhisker", new Vector2(18, 85)),
                        new VisualProp(this, new Vector2(12, -12),  0.1f, 0.3f, MathHelper.PiOver2 * 1.4f, "CrabulonPeekingWhisker", new Vector2(18, 85), true),

                        new VisualProp(this, new Vector2(-8, -10),  0.1f, 0.3f, MathHelper.PiOver2 * 1.4f, "CrabulonPeekingEyestalk", new Vector2(12, 20)),
                        new VisualProp(this, new Vector2(12, -10),  0.1f, 0.3f, MathHelper.PiOver2 * 1.4f, "CrabulonPeekingEyestalk", new Vector2(12, 20), true)
                    };
                }

                bool chitter = Main.rand.NextBool(80);
                bool spread = AIState == ActionState.ScurryingBackUp;
                if (chitter && !ScurryingDown)
                    SoundEngine.PlaySound(Crabulon.TwitchSound, NPC.Center);


                foreach (VisualProp prop in Props)
                {
                    prop.Update();
                    if (chitter)
                        prop.position.X += Main.rand.NextFloat(-10f, 10f);

                    if (spread)
                        prop.position = prop.Anchor + (prop.position - prop.Anchor).RotatedBy(MathHelper.PiOver2 * 0.6f * (float)Math.Pow(ActionTimer, 3f) * (prop.Flip == SpriteEffects.None ? -1 : 1));

                        
                }
            }

            if (ScurryingDown)
            {
                NPC.dontTakeDamage = true;
                ActionTimer -= 1 / (60f * 0.5f);
                NPC.position.Y += 1f - 0.6f * ActionTimer;

                if (Main.rand.NextBool(6))
                    DigEffects();

                if (ActionTimer <= 0)
                {
                    switch (AIState)
                    {
                        case ActionState.ScurryDownToDespawn:
                            NPC.active = false;
                            return;
                        case ActionState.ScurryDownToSpawnCrabulon:
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                    NPC.NewNPC(NPC.GetBossSpawnSource(NPC.target), (int)NPC.Center.X, (int)NPC.Center.Y + 140, ModContent.NPCType<Crabulon>());

                                NPC.active = false;
                                return;
                            }
                        case ActionState.ScurryDownToRelocate:
                            {
                                Crabulon crabulonInstance = ModContent.GetInstance<Crabulon>();
                                int crabulonWidth = crabulonInstance.CollisionBoxWidth;
                                int crabulonHeight = crabulonInstance.CollisionBoxHeight + 30;
                                Vector2 size = new Vector2(crabulonWidth + 60, crabulonHeight);
                                Vector2 originOffset = -size * 0.5f;

                                Vector2 center = NPC.Bottom + Vector2.UnitY * 120f;
                                Vector2 bestPosition = center;
                                float bestScore = 0f;

                                //Randomly look around for a best position if the one we have atm doesnt fit
                                float minimumTreshold = 2f;
                                while (bestScore < Math.Min(0.8f, minimumTreshold))
                                {
                                    float leniency = Utils.GetLerpValue(2f, 0.5f, minimumTreshold, true);

                                    Vector2 randomOffset = Main.rand.NextVector2Circular(600f + leniency * 300f, 100f + leniency * 300f);
                                    if (Math.Abs(randomOffset.X) < 300)
                                        randomOffset.X = Main.rand.NextFloat(300f, 600f) * (Main.rand.NextBool() ? 1 : -1);

                                    Vector2 randomPosition = center + randomOffset;

                                    bool tooCloseToPlayer = false;
                                    for (int i = 0; i < Main.maxPlayers; i++)
                                    {
                                        if (Main.player[i].active && !Main.player[i].dead && Main.player[i].WithinRange(randomPosition, 200f))
                                        {
                                            tooCloseToPlayer = true;
                                            break;
                                        }
                                    }

                                    //Only count random pos scores if the player is far enough away
                                    if (!tooCloseToPlayer)
                                    {
                                        float randomPosScore = Hookstaff.ScoreCrabulonSpawnPosition(ref randomPosition, originOffset, size, Math.Min(0.8f, minimumTreshold));

                                        if (randomPosScore > bestScore)
                                        {
                                            bestScore = randomPosScore;
                                            bestPosition = randomPosition;
                                        }
                                    }

                                    //Slowly become more acceptant of less than filled spots
                                    minimumTreshold -= 1 / 200f;

                                    //if theres no good spots, just despawn
                                    if (minimumTreshold < 0.5f)
                                    {
                                        NPC.active = false;
                                        return;
                                    }
                                }

                                //pop out at the new best position
                                NPC.Bottom = bestPosition - Vector2.UnitY * 60f;
                                ActionTimer = 1f;
                                AIState = ActionState.ScurryingBackUp;

                                break;
                            }
                    }
                }
            }

            else
            {
                NPC.dontTakeDamage = false;
                Vector2 collisionCenter = NPC.Bottom - Vector2.UnitX - Vector2.UnitY * 26;
                bool onTheGround = Collision.SolidCollision(collisionCenter - Vector2.UnitY * 8f, 2, 8);
                bool inTheGround = Collision.SolidCollision(collisionCenter - Vector2.UnitY * 18f - Vector2.UnitX * NPC.width * 0.5f, (int)NPC.width, 16) ;
                float riseSpeed = AIState == ActionState.ScurryingBackUp ? MathF.Pow(ActionTimer, 1.6f) * 1f + 0.7f : 0.3f;

                if (inTheGround)
                    NPC.velocity.Y = -riseSpeed;
                else if (onTheGround)
                    NPC.velocity.Y = 0f;
                else
                    NPC.velocity.Y = 0.6f;

                //Tick down faster if the player is closer
                float tickDownSpeed = 1f - Utils.GetLerpValue(100f, 10f, NPC.Distance(Main.player[NPC.target].Center), true) * 0.95f;
                if (AIState == ActionState.ScurryingBackUp)
                    tickDownSpeed = 1;

                ActionTimer -= 1 / (60f * tickDownSpeed);

                if (AIState == ActionState.ScurryingBackUp)
                {
                    if (ActionTimer < 0)
                        ActionTimer = 0;
                    if (!inTheGround && ActionTimer == 0)
                    {
                        ActionTimer = 1;
                        AIState = ActionState.StayStill;
                    }
                }

                if (ActionTimer <= -9f && AIState == ActionState.StayStill)
                {
                    ActionTimer = 1f;
                    AIState = ActionState.ScurryDownToRelocate;
                }

                if (AIState == ActionState.StayStill || AIState == ActionState.MoveAround)
                    Lighting.AddLight(NPC.Bottom, TorchID.Mushroom);
            }
        }

        public void DigEffects()
        {
            Point tilePos = NPC.Top.ToTileCoordinates();
            Point topLeft = tilePos - new Point(4, 2);
            Point bottomRight = tilePos + new Point(4, 5);

            for (int i = topLeft.X; i <= bottomRight.X; i++)
            {
                for (int j = topLeft.Y; j <= bottomRight.Y; j++)
                {
                    Tile stompedTile = Framing.GetTileSafely(i, j);
                    Tile stompedTileAbove = Framing.GetTileSafely(i, j - 1);
                    if (!stompedTile.HasUnactuatedTile)
                        continue;

                    bool solidTileAbove = stompedTileAbove.HasUnactuatedTile && Main.tileSolid[stompedTileAbove.TileType];
                    bool isHalfSolid = Main.tileSolidTop[stompedTile.TileType];

                    if (solidTileAbove || isHalfSolid || !Main.tileSolid[stompedTile.TileType])
                        continue;

                    int dustCount = WorldGen.KillTile_GetTileDustAmount(fail: true, stompedTile, i, j);

                    for (int k = 0; k < dustCount / 2; k++)
                    {
                        Dust tileBreakDust = Main.dust[WorldGen.KillTile_MakeTileDust(i, j, stompedTile)];
                        tileBreakDust.velocity.Y -= 8f;
                        tileBreakDust.velocity.X *= 0.4f;
                        tileBreakDust.velocity.Y *= Main.rand.NextFloat();
                        tileBreakDust.velocity.Y *= 0.75f;
                        tileBreakDust.scale += 0.07f;
                    }
                }
            }
        }

        private List<VisualProp> Props;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Props == null)
                return false;

            screenPos.Y += 14f;

            //When scurrying down, fade out
            if (ScurryingDown)
                drawColor *= ActionTimer;
            if (AIState == ActionState.ScurryingBackUp)
                drawColor *= 1 - ActionTimer;

            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;
            spriteBatch.Draw(bloom, NPC.Bottom - screenPos, null, Color.DodgerBlue with { A = 0 } * (drawColor.A / 255f) * 0.4f, 0f, bloom.Size() * 0.5f, NPC.scale,0, 0);

            Texture2D tex = ModContent.Request<Texture2D>(Texture + "Body").Value;
            spriteBatch.Draw(tex, NPC.Bottom - Vector2.UnitY * 6f - screenPos, null, drawColor, 0f, new Vector2(tex.Width / 2f, 0), NPC.scale, NPC.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            spriteBatch.Draw(tex, NPC.Bottom - Vector2.UnitY * 6f - screenPos, null, drawColor, 0f, new Vector2(tex.Width / 2f, 0), NPC.scale, NPC.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

            tex = TextureAssets.Npc[Type].Value;
            spriteBatch.Draw(tex, NPC.Bottom - screenPos, null, drawColor, 0f, new Vector2(tex.Width / 2f, tex.Height), NPC.scale, NPC.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

            foreach (VisualProp prop in Props)
                prop.Draw(spriteBatch, screenPos, drawColor);

            return false;
        }

        public override bool CheckDead()
        {
            NPC.dontTakeDamage = true;
            NPC.life = 1;
            ActionTimer = 1f;
            AIState = ActionState.ScurryDownToSpawnCrabulon;
            return false;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (projectile.minion || projectile.sentry || ProjectileID.Sets.MinionShot[projectile.type])
                return false;
            return null;
        }

        private class VisualProp
        {
            public PeekingCrabulon crabulon;
            public NPC NPC => crabulon.NPC;

            public readonly Vector2 offsetFromBase;
            public readonly float damping;
            public readonly float velocityCarryover;
            public readonly float maxAngleDeviation;

            public Asset<Texture2D> TextureAsset;
            public Asset<Texture2D> GlowmaskAsset;

            public readonly bool flipped;
            public readonly SpriteEffects spriteEffects;
            public SpriteEffects Flip {
                get {
                    if (NPC.direction == -1)
                        return spriteEffects == SpriteEffects.FlipHorizontally ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    return spriteEffects;
                }
            }


            public readonly Vector2 spriteOrigin;
            public Vector2 position;
            public Vector2 velocity;
            public Vector2 lastAnchor;
            public Color glowmaskColor = Color.White;
            public Vector2 Anchor => NPC.Bottom + offsetFromBase with { X = NPC.direction == -1 ? offsetFromBase.X * -1 : offsetFromBase.X } * NPC.scale;
            public Vector2 IdealPosition => Anchor -Vector2.UnitY * 26f;


            public VisualProp(PeekingCrabulon crab, Vector2 offsetFromCenter, float damping, float velocityCarry, float maxAngleDeviation, string textureName, Vector2 spriteOrigin, bool flipped = false)
            {
                crabulon = crab;
                offsetFromBase = offsetFromCenter;
                this.damping = damping;
                this.maxAngleDeviation = maxAngleDeviation;
                this.velocityCarryover = velocityCarry;
                this.flipped = flipped;
                this.spriteOrigin = spriteOrigin;

                spriteEffects = SpriteEffects.None;
                TextureAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName, AssetRequestMode.ImmediateLoad);
                GlowmaskAsset = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + textureName + "Glow", AssetRequestMode.ImmediateLoad);


                if (flipped)
                {
                    this.spriteOrigin.X = TextureAsset.Value.Width - this.spriteOrigin.X;
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }

                position = IdealPosition;
                velocity = NPC.velocity;
            }


            public void Update()
            {
                velocity += NPC.velocity * velocityCarryover;
                velocity = Vector2.Lerp(velocity, (IdealPosition - position) * 0.5f, 1 - damping);
                if (velocity.Length() < 0.03f)
                    velocity = Vector2.Zero;

                position += velocity;
                Vector2 anchor = Anchor;

                float distanceFromAnchor = anchor.Distance(position);
                if (distanceFromAnchor > 56f)
                {
                    distanceFromAnchor = 56f;
                    position = anchor + anchor.DirectionTo(position) * distanceFromAnchor;
                }
            }

            public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
            {
                float opacity = drawColor.A / 255f;

                Vector2 anchor = Anchor;
                float rotation = anchor.AngleTo(position);
                rotation = Math.Clamp((-MathHelper.PiOver2).AngleBetween(rotation), -maxAngleDeviation, maxAngleDeviation);

                Vector2 origin = spriteOrigin;
                if (NPC.direction == -1)
                    origin.X = TextureAsset.Value.Width - spriteOrigin.X;

                spriteBatch.Draw(TextureAsset.Value, anchor - screenPos, null, drawColor * opacity, rotation, origin, NPC.scale, Flip, 0);
                spriteBatch.Draw(GlowmaskAsset.Value, anchor - screenPos, null, glowmaskColor * opacity, rotation, origin, NPC.scale, Flip, 0);
            }
        }
    }
}

