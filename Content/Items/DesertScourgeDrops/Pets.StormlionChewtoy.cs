using CalamityFables.Particles;
using System;
using System.IO;
using Terraria.GameContent.Animations;
using Terraria.WorldBuilding;

namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    public class StormlionChewtoy : ModItem
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Chewtoy");
            Tooltip.SetDefault("Summons a tiny, bashful Desert Scourge to follow you");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.ZephyrFish);

            Item.shoot = ModContent.ProjectileType<TinyScourgePet>();
            Item.buffType = ModContent.BuffType<TinyScourgePetBuff>();

            Item.rare = ItemRarityID.Master;
            Item.master = true;

            // All master mode pets sell for 5 gold.
            // TODO -- Should this be made into some static/const value?
            Item.value = Item.sellPrice(0, 5, 0, 0);
        }

        //I guess setting bufftype doesnt work automatically for pet items..?
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
                player.AddBuff(Item.buffType, 3600);
            return true;
        }
    }

    public class TinyScourgePetBuff : ModBuff
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
            DisplayName.SetDefault("Juvenile Scourge");
            Description.SetDefault("Prone to tunnel vision");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            bool _ = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<TinyScourgePet>());
        }
    }

    public class TinyScourgePet : ModProjectile
    {
        public class Segment
        {
            public Vector2 Position;

            public float Rotation;
        }

        public enum AIState
        {
            FlyAroundOwner,
            DigInShallowGround,
        }

        public Segment[] Segments = new Segment[12];

        public AIState CurrentState {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float AITimer => ref Projectile.ai[1];

        public ref float AntiGravityCharge => ref Projectile.ai[2];

        public bool wasInGround;

        public Player Owner => Main.player[Projectile.owner];
        public float OwnerLifeRatio {
            get {
                if (!Owner.active || Owner.dead)
                    return 0f;

                return Owner.statLife / (float)Owner.statLifeMax2;
            }
        }

        public ref float FrameInterpolant => ref Projectile.localAI[0];

        public static readonly SoundStyle DigSound = new(SoundDirectory.DesertScourgeDrops + "TinyScourgeDig");

        // If there's no ground in under this many pixels down, then the scourge is forced to fly in the air instead of burrow in the ground.
        public const float MaxHeightBeforeFlying = 720f;
        public const float SEGMENT_DISTANCE = 12f;

        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tiny Scourge");
            Main.projFrames[Type] = 4;
            Main.projPet[Type] = true;

            // Set up trailing indices.
            ProjectileID.Sets.TrailingMode[Type] = 2;

            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] =
                ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], int.MaxValue)
                .WithCode(DelegateMethods.CharacterPreview.WormPet);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeOfCthulhuPet);
            Projectile.aiStyle = -1;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.hide = true;

            for (int i = 0; i < Segments.Length; i++)
            {
                Segments[i] = new();
                Segments[i].Position = Projectile.Center + SEGMENT_DISTANCE * i * Vector2.UnitY;
            }
        }

        public override void AI()
        {
            CheckActive();

            // Teleport to the owner if very far away from them.
            if (!Projectile.WithinRange(Owner.Center, 2000f))
            {
                Projectile.Center = Owner.Center;
                Projectile.velocity = Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY * 3f;
                Projectile.netUpdate = true;
            }

            bool inGround = FablesUtils.TileCollision(Projectile.position, Projectile.width, Projectile.height, out bool onlyInsideTopSurfaces);
            if (!inGround)
            {
                inGround = Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height);
                if (inGround)
                    onlyInsideTopSurfaces = true;
            }
            if (!inGround && Owner.Distance(Projectile.Center) > 300 && Projectile.Center.Y >= Owner.Center.Y)
                inGround = true;

            //Recharge the antigravity
            if (inGround)
                AntiGravityCharge = Math.Min(AntiGravityCharge + 0.03f, 1);

            // Perform AI-specific behaviors.
            switch (CurrentState)
            {
                case AIState.FlyAroundOwner:
                    DoBehavior_FlyAroundOwner(inGround, onlyInsideTopSurfaces);
                    break;
                case AIState.DigInShallowGround:
                    DoBehavior_DigInShallowGround(inGround, onlyInsideTopSurfaces);
                    break;
            }

            // Update segments.
            UpdateSegments();

            // Increment the AI timer.
            AITimer++;
            if (wasInGround != inGround)
            {
                wasInGround = inGround;
                Projectile.netUpdate = true;
            }
            // Make the frame interpolant gradually approach the owner's life ratio.
            FrameInterpolant = MathHelper.Lerp(FrameInterpolant, OwnerLifeRatio, 0.03f);
        }

        public void CirclePlayer(float radius = 1200f, float speed = 30f, bool inGround = false, bool onlyInPlatforms = false, float gravityDepletionMultiplier = 1f, Vector2? offsetFromPlayer = null)
        {
            Vector2 targetPosition = Owner.Center;
            if (offsetFromPlayer.HasValue)
                targetPosition += offsetFromPlayer.Value;

            int side = Vector2.Dot(Projectile.Center - targetPosition, -Vector2.UnitY.RotatedBy(Projectile.rotation)).NonZeroSign();

            //Ideal position is at the side of the player, making it coil around them
            Vector2 goalPosition = targetPosition + Projectile.SafeDirectionFrom(targetPosition).RotatedBy(side) * radius;
            Vector2 goalVelocity = (goalPosition - Projectile.Center) / speed;
            Projectile.velocity += ((goalVelocity - Projectile.velocity) / speed) * (0.5f + 0.5f * Utils.GetLerpValue(0f, 1f, AntiGravityCharge));

            float speedLimit = 5f;
            if (!inGround)
            {
                //This greatly boosts the speed at which the scourge emerges when it goes out of the sand
                if (AntiGravityCharge == 1)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity * 2f, 0.1f);
                    Projectile.velocity *= 2.6f;
                    AntiGravityCharge -= 0.001f;
                }

                float antiGravityDepletion = 0.02f * Utils.GetLerpValue(500, 0f, Projectile.Center.Y - targetPosition.Y, true);
                AntiGravityCharge -= antiGravityDepletion * gravityDepletionMultiplier;
                if (AntiGravityCharge < 0)
                    AntiGravityCharge = 0;

                //Start to fall as the antigrav charge depletes
                Projectile.velocity.Y += 1f * (1 - AntiGravityCharge);
            }

            else
            {
                if (!onlyInPlatforms)
                {
                    int maxSurfaceDistance = 70;
                    float dustProbability = 0.03f;
                    TelegraphSand(150f, 1f, dustProbability, maxSurfaceDistance);
                }

                if (Projectile.velocity.Length() < speedLimit)
                    Projectile.velocity *= 1.05f;
                else
                    Projectile.velocity *= 0.98f;
            }
        }

        public void UpdateSegments()
        {
            float segmentOffset = Projectile.scale * 12f;
            Vector2 rotationGoal = Vector2.Zero;
            Segments[0].Position = Projectile.Center;
            for (int i = 1; i < Segments.Length; i++)
            { 
                if (i > 1)
                {
                    Vector2 idealRotation = Segments[i - 1].Position - Segments[i - 2].Position; //Direction the earlier segment took towards the even-earlier segment
                    rotationGoal = Vector2.Lerp(rotationGoal, idealRotation, 1 / 95f);
                }

                //Tilt the angle between the 2 segments by the rotation amount
                Vector2 directionFromPreviousSegment = (0.07f * rotationGoal + (Segments[i].Position - Segments[i - 1].Position).SafeNormalize(Vector2.Zero)).SafeNormalize(Vector2.Zero);
                float segmentSeparation = (SEGMENT_DISTANCE) * Projectile.scale;
                Segments[i].Position = Segments[i - 1].Position + directionFromPreviousSegment * segmentSeparation;
            }
        }

        public void CheckActive()
        {
            // Keep the projectile from disappearing as long as the owner isn't dead and has the pet buff.
            // If this check is failed the projectile will die on the next frame.
            if (!Owner.dead && Owner.HasBuff(ModContent.BuffType<TinyScourgePetBuff>()))
                Projectile.timeLeft = 2;
        }

        public void PrepareForDifferentAIState()
        {
            AITimer = 0f;
            Projectile.netUpdate = true;
        }

        public void DoBehavior_FlyAroundOwner(bool inGround, bool onlyInsidePlatforms)
        {
            // If possible, transition to a different AI state. This state should serve solely as a fallback when everything else
            // is impossible, since the ground behaviors are more interesting.
            // The 8 subtraction is done to give a buffer to prevent the scourge from going back and forth if the player hovers just at the line where it can begin burrowing.
            bool ableToReachGround = WorldUtils.Find(Owner.Center.ToTileCoordinates(), Searches.Chain(new Searches.Down((int)Math.Ceiling(MaxHeightBeforeFlying / 16f) - 8), new Conditions.IsSolid()), out _);
            if (ableToReachGround)
            {
                CurrentState = AIState.DigInShallowGround;
                PrepareForDifferentAIState();
                return;
            }

            float speed = 15f + Utils.GetLerpValue(500f, 1000f, Projectile.Distance(Owner.Center), true) * 10f;
            // Fly around the owner, similar to vanilla's master mode pets.
            if (!Projectile.WithinRange(Owner.Center, 200f))
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center).RotatedBy(0.15f) * speed, 0.03f);
            }

            if (Projectile.velocity.Length() < 6f)
                Projectile.velocity *= 1.1f;

            Projectile.velocity = Projectile.velocity.RotatedBy(0.05f * MathF.Sin(AITimer * 0.06f));

            // Decide rotation based on velocity.
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public void DoBehavior_DigInShallowGround(bool inGround, bool onlyInsidePlatforms)
        {
            // Begin flying if the owner is far from ground.
            bool ableToReachGround = WorldUtils.Find(Owner.Center.ToTileCoordinates(), Searches.Chain(new Searches.Down((int)Math.Ceiling(MaxHeightBeforeFlying / 16f)), new Conditions.IsSolid()), out _);
            if (!ableToReachGround)
            {
                CurrentState = AIState.FlyAroundOwner;
                PrepareForDifferentAIState();
                return;
            }

            // Perform ground impact effects if it's been entered or exited.
            if (wasInGround != inGround)
            {
                // Create impact sounds relative to how fast the scourge was moving.
                float impactInterpolant = Utils.GetLerpValue(11f, 19f, Projectile.velocity.Y, true);
                if (impactInterpolant > 0f)
                {
                    SoundEngine.PlaySound(DigSound with
                    {
                        Pitch = MathHelper.Lerp(-0.15f, -0.4f, impactInterpolant),
                        Volume = MathHelper.Lerp(0.75f, 1f, impactInterpolant)
                    }, Projectile.Center);
                }
            }

            CirclePlayer(200f + 40f * MathF.Sin(AITimer * 0.02f), 35f, inGround, onlyInsidePlatforms, 0.45f, -Vector2.UnitY * 80f);

            if (Projectile.velocity != Vector2.Zero)
                Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(AITimer * 4f) * 0.2f / Projectile.velocity.Length());

            if (Projectile.velocity.Length() > 10)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.One) * 10f;

            // Decide rotation based on velocity.
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public void TelegraphSand(float telegraphWidth, float dustSpeed, float dustProbability, int maxSurfaceDistance = 100)
        {
            int x = (int)(Projectile.Center.X / 16);
            int y = (int)(Projectile.Center.Y / 16);
            int halfWidth = (int)(telegraphWidth / 32);

            for (int i = x - halfWidth; i < x + halfWidth; i++)
            {
                for (int j = 0; j < maxSurfaceDistance; j++)
                {
                    Tile tile = Framing.GetTileSafely(i, y - j);
                    if ((!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]) && (tile.WallType == 0 || Main.wallHouse[tile.WallType]))
                    {
                        Tile tileBelow = Framing.GetTileSafely(i, y - j + 1);
                        if (!tileBelow.HasTile || !Main.tileSolid[tileBelow.TileType] || Main.tileSolidTop[tileBelow.TileType])
                            continue;

                        float sideness = (1 - Math.Abs(i - x) / (float)halfWidth);

                        float probability = dustProbability;
                        probability *= 1 - j / (float)maxSurfaceDistance;

                        for (int d = 0; d < 10; d++)
                        {
                            if (Main.rand.NextFloat() < probability * (float)Math.Pow(sideness, 0.5f))
                            {
                                Vector2 dustPos = new Vector2(i, y - j) * 16f;
                                dustPos += Vector2.UnitX * Main.rand.NextFloat(16f) + Vector2.UnitY * 16f;

                                Dust floorDust = Main.dust[WorldGen.KillTile_MakeTileDust(i, y - j + 1, tileBelow)];
                                floorDust.position = dustPos;
                                floorDust.velocity = -Vector2.UnitY * dustSpeed * Main.rand.NextFloat(0.5f, 1.2f);
                                floorDust.velocity = floorDust.velocity.RotatedByRandom(MathHelper.PiOver4);
                                floorDust.scale = Main.rand.NextFloat(0.6f, 2f) * sideness;

                                floorDust.velocity *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);
                                floorDust.scale *= 0.2f + 0.8f * (1 - j / (float)maxSurfaceDistance);

                            }
                        }
                        break;
                    }
                }
            }
        }


        // Draw behind tiles.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) =>
            FablesProjectile.DrawBehindTilesAlways.Add(index);

        // If you copypaste this projectile for a different pet, remember that you must use Main.EntitySpriteDraw for the critter shampoo to work on it.
        public override bool PreDraw(ref Color lightColor)
        {
            // Update the segments here in the draw method if this a preview dummy, since the AI method isn't called for preview dummies.
            if (Projectile.isAPreviewDummy)
            {
                Projectile.rotation = MathHelper.PiOver2;
                UpdateSegments();
            }

            // Acquire textures.
            Texture2D headTexture = TextureAssets.Projectile[Type].Value;
            Texture2D body1Texture = ModContent.Request<Texture2D>($"{Texture}_Body1").Value;
            Texture2D body2Texture = ModContent.Request<Texture2D>($"{Texture}_Body2").Value;
            Texture2D tailTexture = ModContent.Request<Texture2D>($"{Texture}_Tail").Value;

            // Calculate relative texture frames.
            float frameInterpolant = (1f - FrameInterpolant) * (Main.projFrames[Type] - 0.01f);
            float crossfadeInterpolant = frameInterpolant % 1f;
            int frame1 = (int)frameInterpolant;
            int frame2 = frame1;
            if (frame2 >= 1)
            {
                frame2--;
                crossfadeInterpolant = 1f - crossfadeInterpolant;
            }
            else
                frame2++;

            // Apply an easing curve to the crossfade interpolant so that it's more sharp at the ends.
            crossfadeInterpolant = FablesUtils.PolyInOutEasing(crossfadeInterpolant, 3);

            Rectangle headFrame1 = headTexture.Frame(1, Main.projFrames[Type], 0, frame1);
            Rectangle body1Frame1 = body1Texture.Frame(1, Main.projFrames[Type], 0, frame1);
            Rectangle body2Frame1 = body2Texture.Frame(1, Main.projFrames[Type], 0, frame1);
            Rectangle tailFrame1 = tailTexture.Frame(1, Main.projFrames[Type], 0, frame1);
            Rectangle headFrame2 = headTexture.Frame(1, Main.projFrames[Type], 0, frame2);
            Rectangle body1Frame2 = body1Texture.Frame(1, Main.projFrames[Type], 0, frame2);
            Rectangle body2Frame2 = body2Texture.Frame(1, Main.projFrames[Type], 0, frame2);
            Rectangle tailFrame2 = tailTexture.Frame(1, Main.projFrames[Type], 0, frame2);

            // Draw all non-head segments.
            for (int i = 1; i < Segments.Length; i++)
            {
                Texture2D segmentTexture = tailTexture;
                Rectangle segmentFrame1 = tailFrame1;
                Rectangle segmentFrame2 = tailFrame2;
                if (i < Segments.Length - 1)
                {
                    segmentTexture = i == 1 ? body1Texture : body2Texture;
                    segmentFrame1 = i == 1 ? body1Frame1 : body2Frame1;
                    segmentFrame2 = i == 1 ? body1Frame2 : body2Frame2;
                }

                Vector2 segmentDrawPosition = Segments[i].Position - Main.screenPosition;
                float rotation = (Segments[i - 1].Position - Segments[i].Position).ToRotation() + MathHelper.PiOver2;
                Color segmentLightColor = Lighting.GetColor((segmentDrawPosition + Main.screenPosition).ToTileCoordinates());
                Main.EntitySpriteDraw(segmentTexture, segmentDrawPosition, segmentFrame1, Projectile.GetAlpha(segmentLightColor) * (1f - crossfadeInterpolant), rotation, segmentFrame1.Size() * 0.5f, Projectile.scale, 0, 0);
                Main.EntitySpriteDraw(segmentTexture, segmentDrawPosition, segmentFrame2, Projectile.GetAlpha(segmentLightColor) * crossfadeInterpolant, rotation, segmentFrame2.Size() * 0.5f, Projectile.scale, 0, 0);
            }

            // Draw the head.
            lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            float headRotation = Projectile.rotation + MathHelper.PiOver2;
            if (Projectile.isAPreviewDummy)
                headRotation = Projectile.rotation;
            Vector2 headPosition = Projectile.Center +Projectile.rotation.ToRotationVector2() * 2f - Main.screenPosition;
            Main.EntitySpriteDraw(headTexture, headPosition, headFrame1, Projectile.GetAlpha(lightColor) * (1f - crossfadeInterpolant), headRotation, headFrame1.Size() * 0.5f, Projectile.scale, 0, 0);
            Main.EntitySpriteDraw(headTexture, headPosition, headFrame2, Projectile.GetAlpha(lightColor) * crossfadeInterpolant, headRotation, headFrame2.Size() * 0.5f, Projectile.scale, 0, 0);

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(wasInGround);
        public override void ReceiveExtraAI(BinaryReader reader) => wasInGround = reader.ReadBoolean();
    }
}
