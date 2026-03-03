using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader.Utilities;
using static CalamityFables.Helpers.DropHelper;
using static CalamityFables.Core.AStarPathfinding;

namespace CalamityFables.Content.Debug
{
    public class ASTARCRAWLER : ModNPC
    {
        public override bool IsLoadingEnabled(Mod mod) => false;

        #region Setup and variables
        public enum FrameState
        {
            // Frame 0.
            Idle,

            // Frame 1.
            Falling,

            // Frames 6-10.
            Walking
        }

        public enum AIState
        {
            Rest,
            WalkingTowardsGoal
        }

        public int AITimer {
            get;
            set;
        }

        public int variantID;

        public AIState CurrentState {
            get => (AIState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public FrameState CurrentFrame {
            get => (FrameState)NPC.localAI[0];
            set => NPC.localAI[0] = (int)value;
        }

        public bool OnTopOfTiles => FablesUtils.SolidCollisionFix(NPC.TopLeft, NPC.width, NPC.height + 8, true);

        public ref float FrameIndex => ref NPC.localAI[1];

        public Player Target => Main.player[NPC.target];

        public string VariantName => (int)variantID switch
        {
            0 => "Rhino",
            1 => "Weevil",
            _ => "Hercules",
        };

        #region Sounds
        public static readonly SoundStyle AmbientSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerAmbient", 2);

        // HAHAHA!! That Geode Crawler got hit in the head with a coconut!
        public static readonly SoundStyle BonkSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerBonk");

        public static readonly SoundStyle DeathSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerDeath");

        public static readonly SoundStyle DigSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerDig");

        public static readonly SoundStyle GnawingSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerGnaw", 3);

        public static readonly SoundStyle HitSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerHit");

        public static readonly SoundStyle JumpSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerJump");

        public static readonly SoundStyle StartledSound = new("CalamityFables/Sounds/GeodeCrawler/GeodeCrawlerStartled");
        #endregion
        public override string Texture => AssetDirectory.UndergroundNPCs + "GeodeCrawler" + VariantName;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("ASTAR CRAWLER");

            Main.npcFrameCount[Type] = 16;

        }

        public override void SetDefaults()
        {
            Main.npcFrameCount[Type] = 16;

            AIType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 14;
            NPC.width = 154;
            NPC.height = 236;
            NPC.defense = 15;
            NPC.lifeMax = 42;
            NPC.knockBackResist = 0.04f;
            NPC.value = Item.buyPrice(0, 0, 4, 20);

            NPC.HitSound = HitSound with
            {
                SoundLimitBehavior = SoundLimitBehavior.IgnoreNew,
                Volume = 0.65f
            };
            NPC.DeathSound = DeathSound with
            {
                Volume = 0.5f
            };

            NPC.behindTiles = true;
            NPC.lavaImmune = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Use a random variant.
            variantID = Main.rand.Next(3);
            NPC.netUpdate = true;
        }

        #endregion

        public static Vector2 worldGoal;
        public static Point tileGoal;

        public PathfindingTraceback pathToFollow;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void AI()
        {
            NPC.TargetClosest();

            NPC.dontTakeDamage = false;

            NPC.velocity = Vector2.Zero;
            return;

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                tileGoal = new Point(Player.tileTargetX, Player.tileTargetY);
                worldGoal = tileGoal.ToWorldCoordinates();
                Dust.QuickDust(tileGoal, Color.Red);
            }

            Dust.QuickDust(tileGoal, Color.Red);

            // Reset things every frame. They may be altered in the behavior methods below.
            NPC.dontTakeDamage = false;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.knockBackResist = 0.04f;

            switch (CurrentState)
            {
                case AIState.Rest:
                    DoBehavior_Rest();
                    break;
                case AIState.WalkingTowardsGoal:
                    DoBehavior_WalkAround();
                    break;
            }

            Color glowColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.4f) % 1, 0.7f, 0.75f);
            Lighting.AddLight(NPC.Center, glowColor.ToVector3());
            AITimer++;
        }

        #region Behaviors
        public void DoBehavior_Rest()
        {
            // Disable natural knockback resistance. Hit knockback effects will be applied manually.
            NPC.knockBackResist = 0f;

            // Use resting frames.
            CurrentFrame = FrameState.Idle;

            // Sit in place.
            NPC.velocity.X *= 0.91f;

            // Wait a bit before moving around.
            if (AITimer >= 60f && Main.rand.NextBool(20) )
            {
                CurrentState = AIState.WalkingTowardsGoal;
                PrepareForDifferentState();
                return;
            }
        }

        public void DoBehavior_WalkAround()
        {
            if (AITimer % 20 == 1)
            {
                Point start = NPC.Center.ToTileCoordinates();
                Point end = tileGoal;

                SolidCreatureNavigHeight = 3;
                SolidCreatureNavigWallClimbCheckDown = 4;

                if (OffsetPositionsToValidNavigation(SolidCreatureNavigationRaycast, ref start, ref end, 10, 20))
                    IsThereAPath(start, end, GeodeCrawlerStride, SolidCreatureNavigationRaycast, 300f);
            }

            // Approach the destination.
            GroundMotion(worldGoal, 3.5f, 0.06f, 4, AITimer >= 10, NPC.width / 2f + 4f, out bool tallObstacleAhead);



            if (NPC.WithinRange(worldGoal, 20))
            {
                CurrentState = AIState.Rest;
                PrepareForDifferentState();
            }
        }

        public static List<AStarNeighbour> GeodeCrawlerStride = AStarNeighbour.BigStride(3);

        public static bool GeodeCrawlerPathfind(Point p, Point? from)
        {
            Tile t = Main.tile[p];
            bool solidTile = Main.tileSolid[t.TileType];
            bool platform = TileID.Sets.Platforms[t.TileType];

            //Can't navigate inside solid tiles
            if (t.HasUnactuatedTile && !t.IsHalfBlock && !platform && solidTile)
                return false;

            //Can navigate on half tiles and platforms just fine
            if (t.HasUnactuatedTile && (t.IsHalfBlock || platform) && solidTile)
                return true;

            for (int i = -1; i <= 1; i++)
                for (int j = 0; j <= 1; j++)
                {
                    //Only cardinal directions here
                    if (j * i != 0 || (j == 0 && i == 0))
                        continue;

                    //IF a neighboring tile is solid we can go on it
                    Tile adjacentTile = Main.tile[p.X + i, p.Y + j];
                    if (adjacentTile.HasUnactuatedTile && !adjacentTile.IsHalfBlock && (Main.tileSolid[adjacentTile.TileType] || (Main.tileSolidTop[adjacentTile.TileType] && adjacentTile.TileFrameY == 0)))
                        return true;
                }

            //Can fall straight down just fine
            if (from != null && p.X == from.Value.X && p.Y == from.Value.Y + 1)
                return true;

            return false;
        }

        public void GroundMotion(Vector2 goal, float speed, float acceleration, int tallObstacleCheckHeight, bool canJump, float tallObstacleHorizontalDistance, out bool tallObstacleAhead)
        {
            bool shortObstacleAhead = !Collision.CanHitLine(NPC.Center, 1, 1, NPC.Center + Vector2.UnitX * NPC.spriteDirection * 60f, 1, 1) || NPC.velocity.X == 0f;
            tallObstacleAhead = shortObstacleAhead;
            for (int dy = 0; dy < tallObstacleCheckHeight; dy++)
            {
                tallObstacleAhead &= !Collision.CanHitLine(NPC.Center - Vector2.UnitY * dy * 16f, 1, 1, NPC.Center + new Vector2(NPC.spriteDirection * tallObstacleHorizontalDistance, -dy * 16f), 1, 1);
                //Dust.QuickDustLine(NPC.Center - Vector2.UnitY * dy * 16f, NPC.Center + new Vector2(NPC.spriteDirection * tallObstacleHorizontalDistance, -dy * 16f), 20, Color.Red);
            }

            // Approach the destination.
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.SafeDirectionTo(goal).X * speed, acceleration);

            // Make a cute hop if there's a short obstacle ahead.
            if (shortObstacleAhead && !tallObstacleAhead && OnTopOfTiles && canJump)
            {
                if (NPC.velocity.Y >= 0f)
                    SoundEngine.PlaySound(JumpSound, NPC.Center);

                NPC.velocity.X = NPC.spriteDirection * 3f;
                NPC.velocity.Y = -6f;
                NPC.noTileCollide = true;
                NPC.netUpdate = true;
            }

            // Look in the direction of movement.
            if (NPC.velocity.X != 0f)
                NPC.spriteDirection = NPC.velocity.X.NonZeroSign();

            // Use walking frames if walking. Otherwise, use falling frames.
            CurrentFrame = FrameState.Walking;
            if (!OnTopOfTiles)
                CurrentFrame = FrameState.Falling;
        }

        #endregion

        public void PrepareForDifferentState()
        {
            AITimer = 0;
            NPC.netUpdate = true;
        }

        public override bool? CanFallThroughPlatforms()
        {
            return base.CanFallThroughPlatforms();
        }

        public override void FindFrame(int frameHeight)
        {
            switch (CurrentFrame)
            {
                case FrameState.Idle:
                    NPC.frameCounter = 0;
                    FrameIndex = 0f;
                    break;
                case FrameState.Falling:
                    NPC.frameCounter = 0;
                    FrameIndex = 1f;
                    break;
                case FrameState.Walking:
                    NPC.frameCounter++;

                    if (NPC.frameCounter >= 6)
                    {
                        NPC.frameCounter = 0;
                        FrameIndex++;
                    }
                    if (FrameIndex <= 4f || FrameIndex >= 11f)
                        FrameIndex = 6f;
                    break;
            }
            NPC.frame.Y = (int)(FrameIndex * frameHeight);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                drawColor = Color.White;

            // Draw the body.
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = NPC.Center - screenPos + NPC.GfxOffY();
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);

            return false;
        }
    }
}
