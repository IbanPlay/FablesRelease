using CalamityFables.Content.Boss.MushroomCrabBoss;
using CalamityFables.Content.Items.Cursed;
using CalamityFables.Cooldowns;
using CalamityFables.Particles;
using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    [AutoloadEquip(EquipType.Neck, EquipType.Back, EquipType.Front)]
    public class SerratedFiberCloak : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public static readonly SoundStyle ThrowSound = new(SoundDirectory.CrabulonDrops + "MyceliumHookThrow");

        public static float MAX_REACH = 460f;
        public static float THROW_SPEED = 18f;
        public static float NORMAL_GRAPPLE_SPEED_MULTIPLIER = 1.3f;
        public static float HOOKPOINT_GRAPPLE_SPEED_MULTIPLIER = 1.1f;
        public static int HOOKPOINT_DURATION = 15;
        public static float HOOKPOINT_GRAPPLE_RADIUS = 50f;

        public override void Load()
        {
            FablesPlayer.OverrideGrappleEvent += OverrideGrapple;
            FablesPlayer.PostUpdateEvent += ResetHookpointLife;
        }

        public static int HookpointDeployed = 0;
        private void ResetHookpointLife(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (HookpointDeployed <= 0)
                    player.RemoveCooldown(ShroomHookpointLifespan.ID);
                if (HookpointDeployed > 0)
                    HookpointDeployed--;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Serrated Fiber Cloak");
            Tooltip.SetDefault("Replaces your hook with three powerful mycelium hooks\n" +
                "Hold the grapple key down to create a temporary hookpoint, letting you slingshot yourself through the air at high speed\n" +
                "Only one hookpoint may exist at a time");
        }

        public override void SetDefaults()
        {
            Item.value = Item.buyPrice(0, 9, 0, 0);
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.width = 32;
            Item.height = 32;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.SetPlayerFlag(Name);
            if (hideVisual)
                player.SetPlayerFlag(Name + "HideHooks");

            //Spawns hooks
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SerratedMyceliumHook>()] < 3 && player.whoAmI == Main.myPlayer)
            {
                SpawnHook(player, Vector2.Zero, 3f);
            }
        }

        public override void UpdateVanity(Player player)
        {
            player.SetPlayerFlag(Name + "VanityHooks");
            //Spawns hooks
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SerratedMyceliumHook>()] < 3 && player.whoAmI == Main.myPlayer)
            {
                SpawnHook(player, Vector2.Zero, 3f);
            }
        }

        public void SpawnHook(Player player, Vector2 velocity, float ai0)
        {
            List<int> counts = new List<int>() { 0, 1, 2 };
            int hookType = ModContent.ProjectileType<SerratedMyceliumHook>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.owner == player.whoAmI && p.active && p.type == hookType && counts.Contains((int)p.ai[2]))
                {
                    counts.Remove((int)p.ai[2]);
                }
            }

            Projectile.NewProjectile(player.GetSource_Accessory(Item), player.MountedCenter, velocity, hookType, 0, 0f, player.whoAmI, ai0, 0, counts.FirstOrDefault());
        }

        private bool OverrideGrapple(Player player)
        {
            if (!player.GetPlayerFlag(Name))
                return false;

            int hookType = ModContent.ProjectileType<SerratedMyceliumHook>();

            if (!FablesPlayer.BasicGrappleChecks(player, hookType, true))
                return true;


            int hookCount = 0;
            int foundHook = -1;

            for (int i = 0; i < 1000; i++)
            {
                Projectile hook = Main.projectile[i];
                if (hook.active && hook.owner == player.whoAmI && hook.type == hookType)
                {
                    hookCount++;

                    //Searches for an idle hook
                    if (hook.ai[0] == 3)
                    {
                        foundHook = i;
                        break;
                    }
                }
            }


            Vector2 velocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.One) * THROW_SPEED;

            //Shoot an existing hook
            if (foundHook >= 0)
            {

                //Shoot projectile
                Projectile hook = Main.projectile[foundHook];
                hook.velocity = velocity;
                hook.ai[0] = 0;
                hook.ai[1] = 0;
                hook.Center = player.Center;

                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, foundHook);
                hook.netUpdate = true;

            }
            //If we somehow have less than 3 hooks, spawn a new one
            else if (hookCount < 3)
            {
                SpawnHook(player, velocity, 0f);
            }

            return true;
        }
    }


    public class SerratedMyceliumHook : ModProjectile
    {
        public override string Texture => AssetDirectory.Crabulon + "MyceliumVineHook";

        public Player Owner => Main.player[Projectile.owner];
        internal PrimitiveTrail TrailRenderer;

        public HookState State {
            get => (HookState)(int)Projectile.ai[0];
            set { Projectile.ai[0] = (int)value; }
        }

        public ref float Timer => ref Projectile.ai[1];

        public ref float Index => ref Projectile.ai[2];

        public VerletNet chain;

        public enum HookState
        {
            Thrown,
            Retracting,
            Grappling,
            Idle
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Serrated Thread");
            //Expand the draw distance. Should never happen really , but just in case the player basically walks away from the hook.
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;
            FablesProjectile.ModifyProjectileDyeEvent += DontGetDyed;
        }

        private void DontGetDyed(Projectile projectile, ref int dyeID)
        {
            if (projectile.type == Type)
                dyeID = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 3;
            Projectile.height = 3;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.aiStyle = ProjAIStyleID.Hook; //The projectile uses entirely custom AI, but for some reason terraria's only way to distinguish what is and isnt a hook is its ai style.
        }

        public bool keptHookPressed = true;

        public override bool? CanDamage() => false;

        public int attachmentIndex = 0;
        public bool playedThrowSound = false;
        public bool attachedToHook = false;

        public override bool PreAI()
        {
            Vector2 BetweenOwner = Owner.Center - Projectile.Center;

            bool accessoryEquipped = Owner.GetPlayerFlag("SerratedFiberCloak");
            if (!Owner.active || Owner.dead || (!accessoryEquipped && !Owner.GetPlayerFlag("SerratedFiberCloakVanityHooks")) || BetweenOwner.Length() > 1500)
            {
                Projectile.Kill();
                return false;
            }

            //Failsafe (acc swapping and such probably)
            if (!accessoryEquipped && State != HookState.Idle)
            {
                State = HookState.Idle;
                Owner.fullRotation = 0f;
                Timer = 0;
            }

            Projectile.timeLeft = 2;
            Projectile.rotation = BetweenOwner.ToRotation() + MathHelper.PiOver2;

            if (State == HookState.Thrown)
            {
                if (!playedThrowSound)
                {
                    playedThrowSound = true;
                    SoundEngine.PlaySound(SerratedFiberCloak.ThrowSound with { Volume = 0.7f }, Projectile.Center);
                }

                if (!Owner.controlHook)
                    keptHookPressed = false;

                //Retract if too far
                if (SerratedFiberCloak.MAX_REACH < BetweenOwner.Length() || Projectile.velocity.Length() < 1f)
                {
                    State = HookState.Retracting;
                    Timer = 0;

                    if (keptHookPressed && Owner.ownedProjectileCounts[ModContent.ProjectileType<MushSlingshot>()] == 0)
                        SummonHookPoint();
                }

                if (State == HookState.Thrown)
                    CheckForGrapplableTiles();

                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.MushroomSpray, -Projectile.velocity * 0.1f);
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1f, 1.4f);
                }
            }

            else if (State == HookState.Retracting)
            {
                float retractSpeed = 5f + 17f * MathF.Pow(Utils.GetLerpValue(0f, 20f, Timer, true), 2f);
                float retractAcceleration = 0.1f + 0.6f * Utils.GetLerpValue(15, 30, Timer, true);

                float movingTowardsPlayerness = Vector2.Dot(Projectile.velocity.SafeNormalize(Vector2.Zero), Projectile.SafeDirectionTo(Owner.Center));
                if (movingTowardsPlayerness < 0)
                {
                    retractSpeed += 13f;
                    retractAcceleration = MathHelper.Lerp(retractAcceleration, 1f, 0.14f);
                }

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, BetweenOwner.SafeNormalize(Vector2.One) * retractSpeed, retractAcceleration);

                if (BetweenOwner.Length() < 25f)
                    State = HookState.Idle;
            }

            else if (State == HookState.Grappling)
            {
                float lenghtToOwner = BetweenOwner.Length();
                Point tilePos = Projectile.Center.ToTileCoordinates();
                Tile tile = Main.tile[tilePos];

                bool canHook = attachedToHook || (tile.HasUnactuatedTile && tile.CanTileBeLatchedOnTo());

                if (lenghtToOwner > SerratedFiberCloak.MAX_REACH * 2f || (Owner.controlJump && Owner.releaseJump) || !canHook)
                {
                    Owner.fullRotation = 0;
                    State = HookState.Retracting;
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }

                Projectile.velocity = Vector2.Zero;

                if (Owner.grapCount < 10)
                {
                    Owner.grappling[Owner.grapCount] = Projectile.whoAmI;
                    Owner.grapCount++;
                }

                if (attachedToHook)
                {
                    if (lenghtToOwner < 10)
                    {
                        State = HookState.Idle;
                        Owner.velocity *= 1.1f;
                        Projectile.netUpdate = true;
                        Owner.fullRotation = 0;

                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 dustPosition = Owner.Center + Main.rand.NextVector2Circular(Owner.width, Owner.height * 0.6f);
                            dustPosition += Main.rand.NextFloat(-1f, 1f) * Owner.velocity;

                            Dust d = Dust.NewDustPerfect(dustPosition, DustID.MushroomSpray, Owner.velocity * Main.rand.NextFloat(0.5f, 2f));
                            if (Main.rand.NextBool(4))
                                d.velocity *= -0.3f;

                            d.noGravity = true;
                            d.scale = Main.rand.NextFloat(1.3f, 2.4f);
                        }

                        SoundEngine.PlaySound(LuminousMixture.BuffSound with { Volume = 0.4f }, Projectile.Center);

                        if (Projectile.owner == Main.myPlayer)
                            CameraManager.Shake += 10;
                    }
                }
            }

            else if (State == HookState.Idle)
            {
                attachedToHook = false;
                keptHookPressed = true;
                playedThrowSound = false;
                attachmentIndex = 0;

                Vector2 idealPosition = GetIdealPosition();

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(idealPosition) * Vector2.Distance(Projectile.Center, idealPosition) * 0.1f, 0.1f);
                if (Projectile.Distance(idealPosition) < 1f)
                    Projectile.Center = idealPosition;

                if (chain != null)
                    Projectile.rotation = chain.points[^2].position.AngleTo(chain.points[^1].position) - MathHelper.PiOver2;
            }

            if (!Main.dedServ)
            {
                Vector2 idealPosition = GetIdealPosition();
                Vector2 origin = Owner.MountedCenter - Vector2.UnitY * 10f;

                //Smaller than default hitbox
                if (Owner.height < 42)
                {
                    origin.Y += (42 - Owner.height);
                }

                Vector2 midPoint = Vector2.Lerp(origin, idealPosition, 0.5f);
                Vector2 rotatingPoint = origin - midPoint;


                if (chain == null)
                {
                    chain = new VerletNet();
                    VerletPoint[] points = new VerletPoint[30];

                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 position = midPoint + rotatingPoint.RotatedBy(i / 30f * MathHelper.Pi * Owner.direction);
                        points[i] = new VerletPoint(position, i == 0 || i == 29);
                    }

                    chain.AddChain(PrimWidthFunction, PrimColorFunction, points);
                }

                chain.extremities[0].position = origin;
                chain.extremities[1].position = Projectile.Center;

                midPoint = Vector2.Lerp(origin, Projectile.Center, 0.5f);
                rotatingPoint = origin - midPoint;
                for (int i = 0; i < 30; i++)
                {
                    Vector2 gravity = rotatingPoint.RotatedBy(i / 30f * MathHelper.Pi * Owner.direction);

                    chain.points[i].customGravity = gravity * 0.02f;
                }

                chain.Update(7, 0f, 0.9f);

                Vector2[] positions = new Vector2[30];
                Vector2 normal = Owner.MountedCenter.SafeDirectionTo(Projectile.Center).RotatedBy(MathHelper.PiOver2);
                float frequency = 1f;
                float offset = 0f;
                float wobbleStrenght = 0f;
                float fadeEdge = 0f;

                if (State == HookState.Thrown)
                {
                    offset = Timer * 0.3f;
                    frequency = 0.2f;
                    wobbleStrenght = 6f;
                    fadeEdge = 0.7f;
                }
                else if (State == HookState.Grappling)
                {

                    offset = Timer * 0.02f;
                    frequency = 1.4f;
                    wobbleStrenght = 12f * MathF.Pow(Utils.GetLerpValue(10, 0, Timer, true), 1.4f);
                }

                frequency /= (400f / Owner.Distance(Projectile.Center));

                if (State == HookState.Idle)
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        positions[i] = chain.points[i].position;
                    }
                }

                else
                {
                    for (int i = 0; i < positions.Length; i++)
                    {
                        Vector2 basePosition = Vector2.Lerp(Owner.MountedCenter, Projectile.Center, 1 - i / (positions.Length - 1f));
                        positions[i] = basePosition + normal * wobbleStrenght * MathF.Sin(frequency * i + offset) * (1f * (1 - fadeEdge) + fadeEdge * MathF.Sin(MathHelper.Pi * i / (positions.Length - 1f)));
                    }
                }

                TrailRenderer = TrailRenderer ?? new PrimitiveTrail(30, PrimWidthFunction, PrimColorFunction);
                TrailRenderer.SetPositions(positions, FablesUtils.RigidPointRetreivalFunction);
            }

            Timer++;
            return false;
        }

        public override void GrapplePullSpeed(Player player, ref float speed)
        {
            float hookProgress = Utils.GetLerpValue(SerratedFiberCloak.MAX_REACH, 0, player.Distance(Projectile.Center), true);

            //Accelerate directly as we get closer
            if (attachedToHook)
            {
                speed *= 1f + 0.8f * (float)Math.Pow(hookProgress, 1.3f);
                speed *= SerratedFiberCloak.HOOKPOINT_GRAPPLE_SPEED_MULTIPLIER;
            }
            //Speeds up near the middle, slows down on the edges
            else
            {
                float sinProgress = MathF.Sin(hookProgress * MathHelper.Pi);
                sinProgress = Math.Max(0, sinProgress);

                speed *= 0.6f + 0.8f * (float)Math.Pow(sinProgress, 0.7f);
                speed *= SerratedFiberCloak.NORMAL_GRAPPLE_SPEED_MULTIPLIER;
            }
        }

        public Vector2 GetIdealPosition()
        {
            Vector2 idealOffset = -Vector2.UnitY * 70f - Vector2.UnitX * Owner.Directions * 20f;
            idealOffset = idealOffset.RotatedBy(((1 - Index / 2f) * 0.8f + 0.2f) * Owner.direction);

            Vector2 rotatoOffset = Vector2.UnitX.RotatedBy(Timer * 0.04f + Index * 0.7f) * 18f;
            rotatoOffset.Y *= 0.6f;

            idealOffset += rotatoOffset;

            return Owner.MountedCenter + idealOffset;
        }

        public void CheckForGrapplableTiles()
        {
            //Check for the slingshot hook
            int slingshotType = ModContent.ProjectileType<MushSlingshot>();
            if (Owner.ownedProjectileCounts[slingshotType] > 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == slingshotType && p.timeLeft > 40)
                    {
                        if (Projectile.WithinRange(p.Center, SerratedFiberCloak.HOOKPOINT_GRAPPLE_RADIUS))
                        {
                            Vector2 prevVelocity = Projectile.velocity;
                            OnGrapple(p.Center, 0, 0);
                            GrappleToHookPoint(p, prevVelocity);
                            break;
                        }

                    }
                }
            }

            Vector2 hitboxStart = Projectile.Center - new Vector2(5f);
            Vector2 hitboxEnd = Projectile.Center + new Vector2(5f);

            Point topLeftTile = (hitboxStart - new Vector2(16f)).ToTileCoordinates();
            Point bottomRightTile = (hitboxEnd + new Vector2(32f)).ToTileCoordinates();

            for (int x = topLeftTile.X; x < bottomRightTile.X; x++)
            {
                for (int y = topLeftTile.Y; y < bottomRightTile.Y; y++)
                {
                    Vector2 worldPos = new Vector2(x * 16f, y * 16f);
                    Point tilePos = new Point(x, y);

                    //Ignore tiles that arent being collided with
                    if (!(hitboxStart.X + 10f > worldPos.X) || !(hitboxStart.X < worldPos.X + 16f) || !(hitboxStart.Y + 10f > worldPos.Y) || !(hitboxStart.Y < worldPos.Y + 16f))
                        continue;

                    Tile tile = Main.tile[tilePos];

                    if (!tile.HasUnactuatedTile || !tile.CanTileBeLatchedOnTo() || Owner.IsBlacklistedForGrappling(tilePos))
                        continue;

                    OnGrapple(worldPos, x, y);

                    break;
                }

                if (State == HookState.Grappling)
                    break;
            }
        }

        public void OnGrapple(Vector2 grapplePos, int x, int y)
        {
            //Hook onto the tile
            Projectile.velocity = Vector2.Zero;
            State = HookState.Grappling;
            Timer = 0;
            Projectile.Center = grapplePos + Vector2.One * 8f;
            //effects

            //if (x != 0 && y != 0)
                WorldGen.KillTile(x, y, fail: true, effectOnly: true);
            SoundEngine.PlaySound(SoundID.Dig, grapplePos);
            SoundEngine.PlaySound(SoundID.Item174 with { Pitch = -0.4f }, grapplePos);

            Vector2 direction = Projectile.Center.SafeDirectionTo(Owner.MountedCenter);
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(Vector2.Lerp(Projectile.Center, Owner.Center, Main.rand.NextFloat(0.7f)) + Main.rand.NextVector2Circular(7f, 7f), DustID.MushroomSpray, direction);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(1f, 1.4f);
            }

            if (x != 0 && y != 0)
            {
                Rectangle? tileVisualHitbox = WorldGen.GetTileVisualHitbox(x, y);
                if (tileVisualHitbox.HasValue)
                    Projectile.Center = tileVisualHitbox.Value.Center.ToVector2();
            }

            //Update the attachment indices of all the hooks, and unhook the oldest grappled one if necessary
            attachmentIndex++;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == Type && p.ai[0] == 2)
                {
                    if (p.whoAmI == Projectile.whoAmI)
                        continue;

                    SerratedMyceliumHook hookProjectile = p.ModProjectile as SerratedMyceliumHook;
                    hookProjectile.attachmentIndex++;
                    if (hookProjectile.attachmentIndex > 2)
                    {
                        hookProjectile.attachmentIndex = 0;
                        p.ai[0] = 1;
                        p.ai[1] = 20;

                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.whoAmI);
                    }
                }
            }

            if (Owner.grapCount < 10)
            {
                Owner.grappling[Owner.grapCount] = Projectile.whoAmI;
                Owner.grapCount++;
            }

            if (Main.myPlayer == Owner.whoAmI)
            {
                Projectile.netSpam = 0;
                Projectile.netUpdate = true;
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Owner.whoAmI);
            }
        }

        public void SummonHookPoint()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 previousVelocity = Projectile.velocity; 
            OnGrapple(Projectile.Center, 0, 0);

            Projectile hook = null;
            hook = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, previousVelocity.SafeNormalize(Vector2.Zero), ModContent.ProjectileType<MushSlingshot>(), 0, 0, Main.myPlayer, 0, 0, (Owner.Center.X - Projectile.Center.X).NonZeroSign());
            GrappleToHookPoint(hook, previousVelocity);
        }

        public void GrappleToHookPoint(Projectile hookPoint, Vector2 previousVelocity)
        {
            attachedToHook = true;

            if (hookPoint != null && Main.myPlayer == Owner.whoAmI)
            {
                hookPoint.ai[1] = 0;
                hookPoint.ai[2] = (Owner.Center.X - Projectile.Center.X).NonZeroSign();
                hookPoint.velocity = previousVelocity.SafeNormalize(Vector2.Zero);
                hookPoint.netUpdate = true;
                new SerratedFiberCloakGrapplePacket(Owner).Send();
            }

            //Owner.GetModPlayer<FablesPlayer>().AddSpinEffect(new BasicPirouetteEffect(20));

            //Unhook all other hooks
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == Type && p.ai[0] != 3 && p.whoAmI != Projectile.whoAmI)
                {
                    p.ai[0] = 1;
                    p.ai[1] = 50;
                    p.netUpdate = false;
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, i);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Owner.fullRotation = 0;
        }

        public Color primColorMult;
        public Color primColorMult2;

        public float PrimWidthFunction(float completionRatio)
        {
            return 9f;
        }
        public Color PrimColorFunction(float completionRatio)
        {
            if (State == HookState.Idle)
                completionRatio = 1 - completionRatio;

            return Color.Lerp(primColorMult, primColorMult2, completionRatio);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            Projectile.hide = false;
            if (Owner.isLockedToATile)
            {
                behindNPCsAndTiles.Add(index);
                Projectile.hide = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (chain == null)
                return false;

            if (State == HookState.Idle && (Owner.GetPlayerFlag("SerratedFiberCloakHideHooks") || Owner.GetModPlayer<SporeWarpPlayer>().fleshWeavingTime > 0))
                return false;

            primColorMult = lightColor;
            primColorMult2 = Lighting.GetColor(Owner.Center.ToTileCoordinates());


            if (State == HookState.Retracting)
            {
                float opacityReduction = 0.6f;
                lightColor *= opacityReduction;
                primColorMult *= opacityReduction;
                primColorMult2 *= opacityReduction;
            }

            Texture2D chainTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "MyceliumVine").Value;
            Effect effect = AssetDirectory.PrimShaders.TextureMap;
            effect.Parameters["sampleTexture"].SetValue(chainTex);

            float lenght = chain.chainLenghts[0];
            if (State == HookState.Thrown || State == HookState.Retracting)
                lenght = Vector2.Distance(Owner.MountedCenter, Projectile.Center) / 2f;

            effect.Parameters["repeats"].SetValue(2f / (float)chainTex.Width * lenght);
            effect.Parameters["scroll"].SetValue(Index * 0.3f - Timer * 0.006f);
            TrailRenderer?.Render(effect, -Main.screenPosition);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, chain.extremities[1] - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0, 0);

            return false;
        }

        public override bool PreDrawExtras() => false; //Prevents vanilla chain drawing from taking place


        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(attachedToHook);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            attachedToHook = reader.ReadBoolean();
        }
    }

    public class MushSlingshot : ModProjectile
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "MushSlingshot";
        public Player Owner => Main.player[Projectile.owner];

        public Asset<Texture2D> CapTexture;


        public ref float HookAnimation => ref Projectile.ai[1];
        public int SpawnDirection => (int)Projectile.ai[2];
        public float spawnEffectTimer = 0f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mushroom Hook");
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 * SerratedFiberCloak.HOOKPOINT_DURATION;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;

        public DampedVelocityTracker pendulum;

        public override void AI()
        {
            if (Main.myPlayer == Projectile.owner && !Main.LocalPlayer.dead)
            {
                SerratedFiberCloak.HookpointDeployed = 2;
                
                if (!Owner.HasCooldown(ShroomHookpointLifespan.ID))
                {
                    CooldownInstance lifespanCooldown = Owner.AddCooldown(ShroomHookpointLifespan.ID, 60 * SerratedFiberCloak.HOOKPOINT_DURATION);
                    lifespanCooldown.timeLeft = Projectile.timeLeft;
                }
                else if (Owner.FindCooldown(ShroomHookpointLifespan.ID, out var cdLifespan))
                    cdLifespan.timeLeft = Projectile.timeLeft;
            }

            if (HookAnimation == 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustPos = Main.rand.NextVector2Circular(20f, 10f);
                    Dust d = Dust.NewDustPerfect(Projectile.Top + dustPos, DustID.MushroomSpray, dustPos.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 4f));
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(1f, 2f);

                    d.noLight = true;
                    d.type = Main.rand.NextBool(5) ? DustID.MushroomTorch : DustID.MushroomSpray;
                }
            }

            HookAnimation += 1 / (60f * 0.4f);
            if (HookAnimation > 1f)
                HookAnimation = 1;

            spawnEffectTimer += 1 / (60f * 0.4f);
            if (spawnEffectTimer > 1f)
                spawnEffectTimer = 1;

            float animatedRotation = MathF.Pow(1 - HookAnimation, 2f) * 1.6f * SpawnDirection;

            if (pendulum is null)
            {
                pendulum = new DampedVelocityTracker(0.2f, 0.06f, 2.5f);
                pendulum.value.X = animatedRotation;
            }

            if (HookAnimation > 0.4f)
                pendulum.Update(Vector2.Zero);
            else
            {
                Vector2 newValue = Vector2.UnitX * animatedRotation;
                pendulum.velocity = newValue - pendulum.value;
                pendulum.value = newValue;
            }

            //Stay alive if hooked on
            if (HookAnimation < 1)
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 40);
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Players can't grapple on other people's hook points, so draw them half transparent
            if (Main.myPlayer != Projectile.owner)
                lightColor *= 0.4f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly);

            lightColor *= Utils.GetLerpValue(0, 40, Projectile.timeLeft, true);

            Texture2D hookTex = TextureAssets.Projectile[Type].Value;
            CapTexture ??= ModContent.Request<Texture2D>(Texture + "Cap");
            Texture2D capTex = CapTexture.Value;

            Vector2 drawPosition = Projectile.Top;
            float rotation = Projectile.rotation;
            float scale = Projectile.scale + MathF.Pow(1 - spawnEffectTimer, 1.5f) * 0.4f;

            //LUse the hook timer if spawn effect is not there
            if (spawnEffectTimer == 1)
                scale += MathF.Pow(1 - HookAnimation, 1.5f) * 0.2f;


            if (pendulum != null)
                rotation += pendulum.value.X;
            drawPosition += MathF.Sin(MathHelper.Pi * HookAnimation) * Projectile.velocity * 25f;

            Main.EntitySpriteDraw(hookTex, drawPosition - Main.screenPosition, null, lightColor, rotation, new Vector2(hookTex.Width / 2f, 4), scale, 0);
            
            
            Main.EntitySpriteDraw(capTex, drawPosition - Main.screenPosition, null, lightColor, rotation * 0.3f, new Vector2(capTex.Width / 2f + 2, capTex.Height - 4), scale, 0);
            
            if (HookAnimation < 1)
                Main.EntitySpriteDraw(capTex, drawPosition - Main.screenPosition, null, Color.RoyalBlue with { A = 0 } * (1 - HookAnimation), rotation * 0.3f, new Vector2(capTex.Width / 2f + 2, capTex.Height - 4), scale * (1 + (1 - HookAnimation) * 0.3f), 0);
            return false;
        }
    }

    [Serializable]
    public class SerratedFiberCloakGrapplePacket : Module
    {
        public readonly byte whoAmI;
        public SerratedFiberCloakGrapplePacket(Player player)
        {
            whoAmI = (byte)player.whoAmI;
        }

        protected override void Receive()
        {
            Player player = Main.player[whoAmI];
            player.GetModPlayer<FablesPlayer>().AddSpinEffect(new BasicPirouetteEffect(20));

            if (Main.netMode == NetmodeID.Server)
            {
                Send(-1, whoAmI, false);
                return;
            }
        }
    }
}