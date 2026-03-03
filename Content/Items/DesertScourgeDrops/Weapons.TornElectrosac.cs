using CalamityFables.Content.Boss.DesertWormBoss;
using CalamityFables.Content.Dusts;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.UI.Chat;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.DesertScourgeDrops
{
    [ReplacingCalamity("AquaticDischarge")]
    public class TornElectrosac : ModItem //Balling subclass will be real one day...
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        public static int BASE_ATTACK_SPEED = 14;
        public static float THROW_BLAST_DAMAGE_MULTIPLIER = 3f;
        public static float CHARGE_TIME = 3f;
        public static int THROW_TIME = 30;
        public static float BLAST_RADIUS = 85;

        internal List<TooltipParticleData> ballerParticles = new();


        public static readonly SoundStyle BounceSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "ElectrosacBounce", 4) { MaxInstances = 0, PitchVariance = 0.35f, Volume = 0.5f };
        //public static readonly SoundStyle ElectricBounceSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "ElectrosacBounceElectric", 4) { MaxInstances = 0, PitchVariance = 0.3f };
        public static readonly SoundStyle ThrowSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "ElectrosacThrow");
        public static readonly SoundStyle ReturnSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "ElectrosacReturn");
        public static readonly SoundStyle ChargeSound = new SoundStyle(SoundDirectory.DesertScourgeDrops + "ElectrosacCharge");

        public static LocalizedText FullyChargedDamageText;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Torn Electrosac");
            Tooltip.SetDefault("A surprisingly bouncy organ that can be dribbled around\n" +
                "Builds up charge passively, which gets released when throwing the ball\n" +
                "The throw deals more damage as the electrosac reaches further\n" +
                "'We Ball!!!'");
            Item.ResearchUnlockCount = 1;

            FullyChargedDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.TornElectrosacFullyChargedDamage");
        }

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.useTime = Item.useAnimation = BASE_ATTACK_SPEED;
            Item.width = 36;
            Item.height = 48;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 5f;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ModContent.ProjectileType<TornElectrosacBall>();
            Item.autoReuse = true;
            Item.shootSpeed = 11;
        }

        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] == 0 && player.whoAmI == Main.myPlayer)
                TrySpawnBall(player);
        }

        public void TrySpawnBall(Player player)
        {
            if (!CombinedHooks.CanUseItem(player, Item) || !CombinedHooks.CanShoot(player, Item))
                return;

            //We don't actually care about those, but we need them to call the hook
            int type = Item.shoot;
            Vector2 position = player.Center;
            Vector2 velocity = Vector2.Zero;

            //Those are the 2 we want
            int damage = player.GetWeaponDamage(Item);
            float knockback = player.GetWeaponKnockback(Item);
            CombinedHooks.ModifyShootStats(player, Item, ref position, ref velocity, ref type, ref damage, ref knockback);

            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, Item.shoot, damage, knockback, player.whoAmI, ai2: Item.scale);
        }

        //Clamp throw speed
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (velocity.Length() > Item.shootSpeed * 1.5f)
                velocity = velocity.ClampMagnitude(Item.shootSpeed * 1.5f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.ownedProjectileCounts[Item.shoot] == 0)
                return false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.owner != player.whoAmI || !p.active || p.type != Item.shoot)
                    continue;

                //If the ball is balling, throw the ball
                if (p.ai[0] == 0)
                {
                    //Set the damage and knockback to the new values if it changed
                    p.damage = damage;
                    p.knockBack = knockback;

                    //Thrown balls collide with tiles
                    p.tileCollide = true;
                    p.ownerHitCheck = false;

                    //Set the throw velocity, set the throw variable, and set its timeleft
                    p.Center = player.Center + velocity;
                    p.velocity = velocity;
                    p.ai[0] = 1;
                    p.timeLeft = THROW_TIME + 2;

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        new TornElectrosacThrowPacket(p).Send(-1, -1, false);
                    //NETUPDATE DOES NOTHING IF ITS NOT USED IN AI :)
                    //p.netUpdate = true;
                    //p.netSpam = 0;
                }
            }

            return false;
        }

        public override bool MeleePrefix() => true;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");

            if (damageIndex < 0)
                return;

            int baseDamage = Main.LocalPlayer.GetWeaponDamage(Item, true);
            TooltipLine ThrowingForContent = new TooltipLine(Mod, "CalamityFables:ExtraDamage", FullyChargedDamageText.Format((int)(baseDamage * THROW_BLAST_DAMAGE_MULTIPLIER)));
            ThrowingForContent.OverrideColor = Color.Lerp(Color.White, Color.Turquoise, (float)Math.Pow(0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));
            tooltips.Insert(damageIndex + 1, ThrowingForContent);
        }

        public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Name == "Tooltip3" && line.Mod == "Terraria")
            {
                Vector2 basePosition = new Vector2(line.X, line.Y);

                Color colorStartBurn = new Color(252, 73, 3);
                Color colorEndBurn = new Color(255, 160, 40);
                Color colorStartElectro = new Color(55, 176, 251);
                Color colorEndElectro = new Color(66, 144, 212);
                Color outlineColor = MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.4f, colorStartBurn, colorEndBurn, colorEndElectro, colorStartElectro);


                Vector2 textSize = line.Font.MeasureString(line.Text);
                Texture2D glo = ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value;
                Vector2 gloOrigin = new Vector2(0, glo.Height * 0.2f);
                Vector2 gloScale = new Vector2(textSize.X / (float)glo.Width, textSize.Y / (float)(glo.Height * 0.6f));

                Main.spriteBatch.Draw(glo, basePosition - Vector2.UnitY * 2f, null, outlineColor with { A = 0 } * 0.4f, 0f, gloOrigin, gloScale, SpriteEffects.None, 0f);

                // Bg outline
                for (int i = 0; i < 8; i++)
                {
                    Vector2 outlinePos = basePosition + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (2f);
                    ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, line.Text, outlinePos, outlineColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
                }

                // Draw the main inner text.
                Color mainTextColor = Color.Lerp(outlineColor, Color.Black, 0.8f);
                ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, line.Text, basePosition, mainTextColor, line.Rotation, line.Origin, line.BaseScale);

                for (int i = -4; i <= 4; i++)
                {
                    if (i == 0)
                        continue;

                    float alternance = ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f);
                    alternance = PolyInOutEasing(alternance, 3f);

                    Vector2 displace = 5f * Vector2.UnitX * i / 4f * alternance;
                    Color gloColor = Color.DodgerBlue with { A = 0 } * (1 - alternance);
                    ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, line.Text, basePosition + displace, gloColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
                }


                if (Main.rand.NextBool(15))
                {
                    int lifetime = (int)Main.rand.Next(50, 70);
                    float scale = Main.rand.NextFloat(0.5f, 1.2f);
                    Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(0f, 0.1f);
                    Color color = outlineColor * 1.5f;
                    color.A = 0;
                    ballerParticles.Add(new TooltipParticleData(line, velocity, 0f, Main.rand.NextFloat(-0.03f, 0.03f), scale, color, lifetime, TooltipParticleData.GrowShrinkScale));
                }


                TooltipParticleData.SimulateParticles(ballerParticles);
                TooltipParticleData.DrawParticles(ballerParticles, ModContent.Request<Texture2D>(AssetDirectory.Particles + "ThinSparkle").Value, line);

                return false;
            }
            return true;
        }
    }

    [Serializable]
    public class TornElectrosacThrowPacket : Module
    {
        int projectileIdentity;
        byte whoAmI;

        int damage;
        float knockback;
        Vector2 velocity;

        public TornElectrosacThrowPacket(Projectile proj)
        {
            projectileIdentity = proj.identity;
            whoAmI = (byte)proj.owner;

            damage = proj.damage;
            knockback = proj.knockBack;
            velocity = proj.velocity;
        }

        protected override void Receive()
        {
            Projectile proj = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].owner == whoAmI && Main.projectile[i].identity == projectileIdentity)
                {
                    proj = Main.projectile[i];
                    break;
                }
            }

            if (proj == null)
                return;

            proj.damage = damage;
            proj.knockBack = knockback;
            proj.Center = Main.player[proj.owner].Center + velocity;
            proj.velocity = velocity;
            //Thrown balls collide with tiles
            proj.tileCollide = true;
            proj.ownerHitCheck = false;
            proj.timeLeft = TornElectrosac.THROW_TIME + 2;
            proj.ai[0] = 1;

            if (Main.dedServ)
                Send(-1, whoAmI, false);
        }
    }

    public class TornElectrosacBall : ModProjectile
    {
        public override string Texture => AssetDirectory.DesertScourgeDrops + Name;

        internal List<Vector2> throwCache;
        internal PrimitiveTrail ThrowTrail;

        internal PrimitiveClosedLoop FullChargeLoop;

        internal List<Vector2> fullCache;

        public bool Balling
        {
            get => Projectile.ai[0] == 0;
            set
            {
                if (value)
                {
                    Projectile.ai[0] = 0;
                    Projectile.tileCollide = false;
                    Projectile.extraUpdates = 0;
                    Projectile.velocity = Vector2.Zero;
                }
            }
        }
        public bool Thrown
        {
            get => Projectile.ai[0] == 1;
            set => Projectile.ai[0] = value ? 1 : 0;
        }

        public ref float Timer => ref Projectile.ai[1];
        public float Charge => Math.Min(Timer / (TornElectrosac.CHARGE_TIME * 60), 1);

        public float Scale => Projectile.ai[2];

        public ref float BounceMode => ref Projectile.localAI[0];

        public float throwDirection; //Visuals
        public float flashTime = 0f;
        public float bloingTime = 0f;
        public float throwTrailFade = 0f;
        public float throwTrailBurn = 0f;
        public float fullChargeFlashTimer = 0f;

        public bool FlyingAway => Thrown && Projectile.timeLeft > 2;
        public float CineticForce => (Projectile.timeLeft - 2) / (float)TornElectrosac.THROW_TIME;

        public bool CanStayAlive => !Owner.CCed && Owner.active && !Owner.dead && Owner.HeldItem.type == ModContent.ItemType<TornElectrosac>() && CombinedHooks.CanShoot(Owner, Owner.HeldItem) && CombinedHooks.CanUseItem(Owner, Owner.HeldItem);

        public Player Owner => Main.player[Projectile.owner];

        public Vector2 DribbleHorizontalOffset
        {
            get
            {
                Vector2 offset = Vector2.UnitX * Owner.direction * (16f + Utils.GetLerpValue(0f, 10f, Math.Abs(Owner.velocity.X)) * 35f);
                return offset;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("BALLING SO HARD");
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 600)
                BounceMode = 1;

            // Resize if scale doesnt match
            if (Projectile.scale != Scale)
            {
                Vector2 newSize = Projectile.Size * (Scale / Projectile.scale);
                Projectile.Resize((int)newSize.X, (int)newSize.Y);
                Projectile.scale = Scale;
            }

            //When thrown, act sorta like a holdout
            if (Thrown)
            {
                if (Projectile.timeLeft <= 2)
                    Projectile.timeLeft = 2;

                Owner.heldProj = Projectile.whoAmI;

                if (Owner.itemTime <= 2)
                    Owner.SetDummyItemTime(2);
                ThrownAI();
            }

            else if (Balling)
            {
                Projectile.ownerHitCheck = true;
                Projectile.localNPCHitCooldown = Owner.GetHeldItemUseTime() ?? TornElectrosac.BASE_ATTACK_SPEED;

                //Charge up
                float previousCharge = Charge;
                Timer++;
                if (previousCharge < 1 && Charge == 1)
                    ReachFullChargeEffects();

                //When balling, survive only if the players held item is the ball, theyre not dead or stunned, etc etc
                if (CanStayAlive)
                {
                    Projectile.timeLeft = 2;
                    Owner.heldProj = Projectile.whoAmI;
                }
                else if (Projectile.timeLeft > 2)
                    Projectile.timeLeft = 2;

                BallingAI();
            }

            TickDownCounters();
            ManageCaches();
            ManageTrails();
        }

        public void TickDownCounters()
        {
            fullChargeFlashTimer -= 1 / (120f * 0.2f); //Timer for when the ball is fully charged and glows and all

            if (Thrown)
            {
                bloingTime -= 1 / (60f * 0.3f); //Timer for the ball being frozen after bouncing off tiles
                flashTime -= 1 / (60f * 0.25f); //Timer for the ball itself glowing at the start of the throw
                throwTrailFade = 1f; //When thrown, the trail shouldn't dissapear at all.

                if (!FlyingAway)
                    throwTrailBurn -= 1 / (60f * 0.75f); //The trail fades from electric blue to a fire red as it returns back to the player.
                else
                    throwTrailBurn = 1f;
            }

            else
            {
                throwTrailFade -= 1 / (60f * 0.3f); //Timer for the trail from the throw dissapearing off
            }
        }

        #region Balling
        public void BallingAI()
        {
            Vector2 oldPosition = Projectile.Center;
            Vector2 idealPosition = Owner.Center + (Owner.gfxOffY + 8) * Vector2.UnitY * Owner.gravDir;
            idealPosition += DribbleHorizontalOffset;

            bool bounced = false;
            float dribbleSpeed = TornElectrosac.BASE_ATTACK_SPEED / (Owner.GetHeldItemUseTime() ?? TornElectrosac.BASE_ATTACK_SPEED) * 1.1f;

            if (Owner.velocity.Y != 0)
                idealPosition += CycleAroundLoopDribble(dribbleSpeed, out bounced);
            else
            {
                if (BounceMode < 0)
                    idealPosition += RegularUpDownDribble(dribbleSpeed, out bounced);
                else
                    idealPosition += InfinityLoopDribble(dribbleSpeed, out bounced);
            }

            //Do a bounce sound on the floor
            if (bounced && Owner.velocity.Y == 0)
                BounceEffects();

            bool canUseNormalDribble = Math.Abs(Owner.velocity.X) < 2.5f && Owner.velocity.Y == 0;
            if (BounceMode < 0)
                canUseNormalDribble = Math.Abs(Owner.velocity.X) < 6.5f;

            if (canUseNormalDribble)
            {
                if (Owner.velocity.Y == 0)
                {
                    float transitionTime = 2f;
                    BounceMode -= 1 / (120f * transitionTime);
                }
                else
                {
                    float transitionTime = 0.4f;
                    BounceMode += 1 / (120f * transitionTime);
                }

                if (BounceMode < -1f)
                    BounceMode = -1f;
            }
            else
                BounceMode = 1;

            //Rotate the ball
            if (Projectile.Center.Distance(Owner.Center) < 50f)
                Projectile.Center = Projectile.Center.MoveTowards(idealPosition, 29f);
            else
                Projectile.Center = idealPosition;

            Projectile.rotation = oldPosition.AngleTo(Projectile.Center);

            if (Charge == 1 && fullChargeFlashTimer <= 0f && !Main.rand.NextBool(3))
            {
                Vector2 dustyDisplacement = Main.rand.NextVector2CircularEdge(6f, 6f);
                Vector2 dustVelocity = dustyDisplacement * Main.rand.NextFloat(0f, 0.1f) + (Projectile.Center - oldPosition) * 0.1f;

                Dust zusty = Dust.NewDustPerfect(Projectile.Center + dustyDisplacement, DustID.DungeonSpirit, dustVelocity, 200, Scale: Main.rand.NextFloat(0.9f, 1.2f));
                zusty.noGravity = true;
            }
        }

        public void ReachFullChargeEffects()
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 dustyDisplacement = Main.rand.NextVector2CircularEdge(52f, 52f) * Main.rand.NextFloat(0.8f, 1f);
                Dust dusty = Dust.NewDustPerfect(Projectile.Center + dustyDisplacement, DustID.UltraBrightTorch, -dustyDisplacement * 0.05f, 200, Scale: Main.rand.NextFloat(0.9f, 1.5f));
                dusty.noGravity = true;

                dustyDisplacement = Main.rand.NextVector2CircularEdge(22f, 22f);
                Dust zusty = Dust.NewDustPerfect(Projectile.Center + dustyDisplacement, DustID.UltraBrightTorch, dustyDisplacement * 0.24f, 200, Scale: 0.9f);
                zusty.noGravity = true;
            }

            fullChargeFlashTimer = 1f;

            if (Owner.whoAmI == Main.myPlayer)
                SoundEngine.PlaySound(TornElectrosac.ChargeSound, Projectile.Center);
        }

        public void BounceEffects()
        {
            float bounceVolumeMult = 0.4f;
            if (BounceMode < 0f && Owner.velocity.X == 0)
                bounceVolumeMult *= 0.2f + 0.8f * (BounceMode + 1);

            SoundEngine.PlaySound(TornElectrosac.BounceSound with { Volume = bounceVolumeMult, PitchVariance = 0.3f }, Projectile.Center);

            Vector2 ballBottom = Owner.gravDir == 1 ? Projectile.Bottom : (Projectile.Top - Vector2.UnitY * 8f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 dustPosition = ballBottom + Vector2.UnitX * Main.rand.NextFloat(-Projectile.width, Projectile.width) * 0.4f;
                Vector2 dustSpeed = dustPosition.SafeDirectionFrom(ballBottom + Vector2.UnitY) * Main.rand.NextFloat(0.2f, 3f);
                Dust.NewDustPerfect(dustPosition, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(0.3f, 1.2f));
            }
        }


        public Vector2 RegularUpDownDribble(float dribbleSpeed, out bool bounced)
        {
            dribbleSpeed *= 0.4f;
            float oscillateTimer = Timer * dribbleSpeed;
            float upDownBounce = 10f * (float)Math.Sin(oscillateTimer); //Bouncing the ball up and down

            bounced = Math.Cos(oscillateTimer) > 0 && Math.Cos(oscillateTimer + dribbleSpeed) <= 0; //If the ball goes from falling to going up, bounce (Cos is the derative of Sin)

            float sideways = (DribbleHorizontalOffset.X - Owner.direction * 8f) * upDownBounce / (10f * Owner.gravDir) * (0.3f + 0.7f * Utils.GetLerpValue(0f, 3f, Math.Abs(Owner.velocity.X), true));

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ((float)Math.Sin(oscillateTimer) - MathHelper.PiOver4) * Owner.direction);
            return new Vector2(sideways + Owner.direction * 2f, upDownBounce);
        }


        public Vector2 InfinityLoopDribble(float dribbleSpeed, out bool bounced)
        {
            dribbleSpeed *= 0.5f;
            float oscillateTimer = Timer * dribbleSpeed;
            float upDownBounce = 10f * (float)Math.Sin(oscillateTimer); //Bouncing the ball up and down

            bounced = Math.Cos(oscillateTimer) > 0 && Math.Cos(oscillateTimer + dribbleSpeed) <= 0; //If the ball goes from falling to going up, bounce (Cos is the derative of Sin)

            float sidewaysDribble = 30f * (float)Math.Sin(oscillateTimer * 0.5f);

            if (Owner.direction == sidewaysDribble.NonZeroSign())
                sidewaysDribble *= 0.7f;
            else
                sidewaysDribble *= 1.3f;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ((float)Math.Sin(oscillateTimer * 0.5f + MathHelper.Pi) - MathHelper.PiOver4) * Owner.direction);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ((float)Math.Sin(oscillateTimer * 0.5f) - MathHelper.PiOver4) * Owner.direction);

            return new Vector2(sidewaysDribble, upDownBounce);
        }

        public Vector2 CycleAroundLoopDribble(float dribbleSpeed, out bool bounced)
        {
            dribbleSpeed *= 0.55f;
            float oscillateTimer = Timer * dribbleSpeed;
            float upDownBounce = 10f * (float)Math.Sin(oscillateTimer * 0.66f); //Bouncing the ball up and down
            float sidewaysDribble = 30f * (float)Math.Sin(oscillateTimer * 1.2f);

            bounced = false;

            Projectile.soundDelay--;
            if (Projectile.soundDelay < 0f)
            {
                Projectile.soundDelay = 16;
                SoundEngine.PlaySound(SoundDirectory.CommonSounds.LouderItem7 with { MaxInstances = 0, PitchVariance = 0.4f, Volume = 0.3f }, Projectile.Center);
            }

            if (Owner.direction == sidewaysDribble.NonZeroSign())
                sidewaysDribble *= 0.7f;
            else
                sidewaysDribble *= 0.3f;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ((float)Math.Sin(oscillateTimer * 0.5f + MathHelper.Pi) - MathHelper.PiOver4) * Owner.direction);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, ((float)Math.Sin(oscillateTimer * 0.5f) - MathHelper.PiOver4) * Owner.direction);

            return new Vector2(sidewaysDribble, upDownBounce) - Vector2.UnitY * 4f - Vector2.UnitX * Owner.direction * 7f;
        }
        #endregion

        #region Throw
        public void ThrownAI()
        {
            if (CineticForce == 1)
                StartThrowEffects();

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, throwDirection - ((float)Math.Pow(CineticForce, 2) * MathHelper.PiOver2 - 0.5f) * Owner.direction);

            //Accelerate back towards the player
            if (!FlyingAway)
            {
                Projectile.tileCollide = false;
                float distanceToPlayer = (Owner.Center - Projectile.Center).Length();
                if (distanceToPlayer > 3000f)
                {
                    Projectile.Kill();
                    return;
                }

                Vector2 towardsOwner = Projectile.SafeDirectionTo(Owner.Center);
                float speed = Projectile.velocity.Length();
                float dot = Vector2.Dot(towardsOwner, Projectile.velocity.SafeNormalize(Vector2.UnitY));

                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(Projectile.AngleTo(Owner.Center), 0.13f).ToRotationVector2() * speed;


                //Accelerate back towards the player
                Projectile.velocity += towardsOwner;
                if (dot <= 0)
                    Projectile.velocity += towardsOwner;

                //Clamp the max magnitude of the velocity
                float maxVelocity = 18f;
                if (bloingTime > 0f)
                    maxVelocity *= (float)Math.Pow(1 - bloingTime, 3f);

                if (Projectile.velocity.Length() > maxVelocity && distanceToPlayer < 1000f)
                    Projectile.velocity = Projectile.velocity.ClampMagnitude(maxVelocity);

                //if the ball boomerangs back to the player
                if (dot <= 0 && Vector2.Dot(towardsOwner, Projectile.velocity.SafeNormalize(Vector2.UnitY)) > 0 && distanceToPlayer > 30f)
                {
                    SoundEngine.PlaySound(SoundID.Item7, Projectile.Center);
                    SoundEngine.PlaySound(TornElectrosac.ReturnSound, Projectile.Center);

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 dustyDisplacement = Main.rand.NextVector2CircularEdge(52f, 52f) * Main.rand.NextFloat(0.8f, 1f);
                        Dust dusty = Dust.NewDustPerfect(Projectile.Center + dustyDisplacement, DustID.Torch, -dustyDisplacement * 0.05f, 200, Scale: Main.rand.NextFloat(0.9f, 1.5f));
                        dusty.noGravity = true;

                        Dust zusty = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(22f, 22f), DustID.Torch, Vector2.Zero, 200, Scale: 0.9f);
                        zusty.noGravity = true;
                    }
                }

                if (Projectile.Distance(Owner.Center) < 30f && bloingTime <= 0f)
                    Balling = true;
            }

            if (Main.rand.NextBool(3))
            {
                int dustType = DustID.UltraBrightTorch;
                if (Main.rand.NextFloat() > throwTrailBurn)
                    dustType = DustID.Torch;

                Dust dusty = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.width / 2f), dustType, Projectile.velocity * 0.5f, 200, Scale: 0.9f);
                dusty.noGravity = true;

                Dust zusty = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.width / 2f), dustType, Vector2.Zero, 200, Scale: 0.9f);
                zusty.noGravity = true;
            }
        }

        public void StartThrowEffects()
        {
            if (throwTrailFade < 0.5f)
                ResetThrowCache();
            Projectile.localNPCImmunity = new int[200]; // Clear local immunities so dribbling wont interfere
            Projectile.localNPCHitCooldown++;
            Projectile.extraUpdates = 1;
            throwDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation() - MathHelper.PiOver2;
            SoundEngine.PlaySound(TornElectrosac.ThrowSound with { Volume = 1.2f }, Projectile.Center);
            flashTime = 1f;
            BounceMode = 1;
        }
        #endregion

        #region Hit/Bounce stuff
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Thrown)
            {
                Projectile.tileCollide = false;
                Projectile.timeLeft = 2;
                Projectile.position += Projectile.velocity * 2f;
                Projectile.velocity = -oldVelocity * 0.3f;
                bloingTime = 1f;
            }

            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(TornElectrosac.BounceSound, Projectile.Center);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            //When balling, turn the hitbox into an ellipse
            if (Balling)
            {
                Vector2 shortestVectorFromPlayerToTarget = targetHitbox.ClosestPointInRect(Owner.MountedCenter + DribbleHorizontalOffset) - (Owner.MountedCenter + DribbleHorizontalOffset);
                shortestVectorFromPlayerToTarget.Y /= 0.8f; // Makes the hit area an ellipse. Vertical hit distance is smaller due to this math.
                float hitRadius = 65f; // The length of the semi-major radius of the ellipse (the long end)
                return shortestVectorFromPlayerToTarget.Length() <= hitRadius;
            }

            return AABBvCircle(targetHitbox, Projectile.Center, Projectile.width * 1.4f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            //Always knock enemies back while balling
            if (Balling)
            {
                modifiers.HitDirectionOverride = (target.Center.X - Owner.Center.X).NonZeroSign();
            }
            if (Thrown)
                modifiers.FinalDamage *= Utils.GetLerpValue(1f, 0.5f, CineticForce) * 0.76f + 0.24f;

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Thrown)
            {
                if (Projectile.tileCollide)
                {
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = 2;
                    Projectile.velocity = -Projectile.velocity;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(TornElectrosac.BounceSound, Projectile.Center);
                }

                if (Charge == 1)
                {
                    int blast_damage = (int)(hit.SourceDamage * TornElectrosac.THROW_BLAST_DAMAGE_MULTIPLIER);
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MiniElectroblast>(), blast_damage, 0, Main.myPlayer, TornElectrosac.BLAST_RADIUS);
                    Timer = 0;
                    Projectile.netUpdate = true;

                    CameraManager.Shake += 15;
                }
            }

            BounceMode = 1;
        }
        #endregion

        #region Prims
        public void ManageCaches()
        {
            //Initialize the caches
            if (throwCache == null)
                ResetThrowCache();
            if (fullCache == null)
            {
                fullCache = new List<Vector2>();
                for (int i = 0; i < 30; i++)
                {
                    fullCache.Add(Projectile.Center);
                }
            }

            fullCache.Add(Projectile.Center);

            if (Thrown)
                throwCache.Add(Projectile.Center + Projectile.velocity);
            else
                throwCache.Add(Owner.Center);

            while (throwCache.Count > 40)
                throwCache.RemoveAt(0);
            while (fullCache.Count > 40)
                fullCache.RemoveAt(0);
        }

        public void ResetThrowCache()
        {
            throwCache = new List<Vector2>();
            for (int i = 0; i < 40; i++)
            {
                throwCache.Add(Projectile.Center + Projectile.velocity);
            }
        }

        public void ManageTrails()
        {
            if (Main.dedServ)
                return;

            ThrowTrail = ThrowTrail ?? new PrimitiveTrail(30, ThrowWidthFunction, ThrowColorFunction);
            ThrowTrail.SetPositionsSmart(throwCache, Projectile.Center + Projectile.velocity, RigidPointRetreivalFunction);
            ThrowTrail.NextPosition = Projectile.Center + Projectile.velocity;

            FullChargeLoop = FullChargeLoop ?? new PrimitiveClosedLoop(50, ChargeLoopWidthFunction, ChargeLoopColorFunction);
            FullChargeLoop.SetPositionsCircle(Projectile.Center, 30f - 20f * (float)Math.Pow(fullChargeFlashTimer, 2f));
        }

        internal float ThrowWidthFunction(float completionRatio)
        {
            float baseWidth = (15f * (float)Math.Pow(completionRatio, 0.3f));  //Width tapers off at the end
            baseWidth *= (1 + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth * throwTrailFade; //Width fades off
        }

        internal Color ThrowColorFunction(float completionRatio)
        {
            Color colorStartBurn = new Color(252, 73, 3);
            Color colorEndBurn = new Color(255, 160, 40);
            Color colorStartElectro = new Color(55, 176, 251);
            Color colorEndElectro = new Color(66, 144, 212);

            Color colorStart = Color.Lerp(colorStartBurn, colorStartElectro, throwTrailBurn);
            Color colorEnd = Color.Lerp(colorEndBurn, colorEndElectro, throwTrailBurn);

            Color color = Color.Lerp(colorStart * 0.05f, colorEnd, completionRatio);
            color *= (float)Math.Pow(completionRatio, 1.2f);

            color *= throwTrailFade;
            return color;
        }

        internal float ChargeLoopWidthFunction(float completionRatio)
        {
            float baseWidth = 5f * fullChargeFlashTimer;  //Width tapers off at the end
            baseWidth *= (1 + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7) * 0.3f); //Width oscillates
            return baseWidth;
        }

        internal Color ChargeLoopColorFunction(float completionRatio)
        {
            Color colorStartElectro = new Color(55, 176, 251);
            Color colorEndElectro = new Color(66, 144, 212);

            Color color = Color.Lerp(colorStartElectro, colorEndElectro, 1 - fullChargeFlashTimer);
            return color;
        }
        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            #region Prims
            //Throw Cache
            if (throwTrailFade > 0f && CineticForce < 1)
            {
                Effect effect = AssetDirectory.PrimShaders.IntensifiedTextureMap;
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "FireTrail").Value);
                ThrowTrail?.Render(effect, -Main.screenPosition);
            }

            //Ring around the thing when its fully charged
            if (fullChargeFlashTimer > 0f)
            {
                Effect effect = AssetDirectory.PrimShaders.IntensifiedTextureMap;
                effect.Parameters["repeats"].SetValue(4);
                effect.Parameters["scroll"].SetValue(Main.GlobalTimeWrappedHourly * 3f);
                effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "BasicTrail").Value);
                FullChargeLoop?.Render(effect, -Main.screenPosition);
            }
            #endregion

            //BLoom on throw
            if (Thrown)
            {
                Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

                Vector2 bloomPosition = Projectile.Center - Main.screenPosition;

                Color alphaElectro = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.Aqua, Color.SlateGray, Color.RoyalBlue) with { A = 0 };
                Color alphaFire = MulticolorLerp((Main.GlobalTimeWrappedHourly * 0.8f + Projectile.whoAmI * 0.1f) % 1, Color.DarkOrange, Color.OrangeRed, Color.Gold) with { A = 0 };

                Color glowColor = Color.Lerp(alphaFire, alphaElectro, throwTrailBurn);

                Main.EntitySpriteDraw(bloom, bloomPosition, null, glowColor * 0.1f, 0, bloom.Size() / 2f, Projectile.scale * 0.65f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(bloom, bloomPosition, null, glowColor * 0.06f, 0, bloom.Size() / 2f, Projectile.scale * 0.25f, SpriteEffects.None, 0);
            }

            Texture2D overlay = ModContent.Request<Texture2D>(Texture + "Silouette").Value;
            Texture2D outline = ModContent.Request<Texture2D>(Texture + "Outline").Value;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float ballRotation = Projectile.velocity.ToRotation();
            if (Balling && fullCache != null)
                ballRotation = Projectile.rotation;

            //Afterimages
            if (Balling && fullCache != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 oldPos = fullCache[fullCache.Count - 1 - i];
                    float opacity = 0.6f * (float)Math.Pow(1 - i / 10f, 2f);
                    float scaleMult = 1f - 0.2f * (i / 10f);

                    Color afterImageColor = lightColor;
                    if (Charge == 1)
                    {
                        byte afterImageAlpha = (byte)(20 + opacity * 200);
                        float colorLerper = Math.Clamp(1 - fullChargeFlashTimer, 0, 1) * (float)Math.Pow(1 - i / 10f, 0.3f);
                        afterImageColor = Color.Lerp(afterImageColor, Color.DodgerBlue with { A = afterImageAlpha }, colorLerper * 0.7f);
                    }

                    Main.EntitySpriteDraw(texture, oldPos - Main.screenPosition, null, afterImageColor * opacity, ballRotation, texture.Size() / 2, Projectile.scale * scaleMult, 0, 0);
                }
            }

            Vector2 scale = Vector2.One;
            //Squish on throw
            if (Thrown)
            {
                float squishy = Utils.GetLerpValue(0f, 37f, Projectile.velocity.Length(), true);
                scale = new Vector2(1 + squishy * 0.5f, 1 - squishy * 0.3f);
                if (bloingTime > 0f)
                {
                    scale.X *= 1 - bloingTime * 0.31f;
                    scale.Y *= 1 + bloingTime * 0.5f;
                }
            }

            //Outline and streak for the flash
            if (fullChargeFlashTimer > 0f)
            {
                Main.EntitySpriteDraw(outline, Projectile.Center - Main.screenPosition, null, (Color.RoyalBlue with { A = 120 }) * fullChargeFlashTimer, ballRotation, outline.Size() / 2, scale * Projectile.scale, 0, 0);

                Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;
                Vector2 lensFlareScale = new Vector2(2f - 1.6f * (float)Math.Pow(1 - fullChargeFlashTimer, 0.3f), 2f + 3f * (1 - fullChargeFlashTimer));

                Color lensFlareColor = Color.Lerp(Color.RoyalBlue, Color.White, (float)Math.Pow(fullChargeFlashTimer, 3f));
                Main.EntitySpriteDraw(lensFlare, Projectile.Center - Main.screenPosition, null, (lensFlareColor with { A = 120 }) * fullChargeFlashTimer, MathHelper.PiOver2, lensFlare.Size() / 2, lensFlareScale * Projectile.scale, 0, 0);
            }

            //Draw the ball (duh)
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, ballRotation, texture.Size() / 2, scale * Projectile.scale, 0, 0);

            //Draw the tail of the organ
            if (fullCache != null)
            {
                Texture2D tail = ModContent.Request<Texture2D>(AssetDirectory.DesertScourgeDrops + "TornElectrosacTail").Value;
                Vector2 tailOrigin = new Vector2(tail.Width * 0.5f, tail.Height - 1f);
                Vector2 tailPosition = Projectile.Center - ballRotation.ToRotationVector2() * scale.X * Projectile.scale * 10f;
                float tailRotation = Projectile.Center.AngleTo(fullCache[fullCache.Count - 2]) + MathHelper.PiOver2;
                Main.EntitySpriteDraw(tail, tailPosition - Main.screenPosition, null, lightColor, tailRotation, tailOrigin, Projectile.scale, 0, 0);
            }

            #region Glow overlays
            //Glow effect on throw
            if (Thrown && flashTime > 0f)
            {
                Color color = Color.Lerp(Color.White, Color.MediumTurquoise, 1 - (flashTime - 0.6f) / 0.4f);
                if (flashTime < 0.6f)
                    color = Color.Lerp(Color.MediumTurquoise, Color.DodgerBlue, (float)Math.Pow(1 - flashTime / 0.6f, 0.5f));

                Main.EntitySpriteDraw(overlay, Projectile.Center - Main.screenPosition, null, color * flashTime, Projectile.velocity.ToRotation(), texture.Size() / 2, scale * Projectile.scale, 0, 0);
            }

            //Full charge effects
            if (fullChargeFlashTimer > 0f)
            {
                Color color = Color.Lerp(Color.RoyalBlue, Color.White, fullChargeFlashTimer) with { A = (byte)(120 + 100 * fullChargeFlashTimer) };
                Main.EntitySpriteDraw(overlay, Projectile.Center - Main.screenPosition, null, color * (float)Math.Pow(fullChargeFlashTimer, 0.5f), Projectile.velocity.ToRotation(), texture.Size() / 2, scale * Projectile.scale, 0, 0);
            }
            #endregion

            return false;
        }
        #endregion
    }

    public class MiniElectroblast : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("BALLIN' BLAST");
        }

        public static int BlastTime => 28;
        public float Completion => (BlastTime - Projectile.timeLeft) / (float)BlastTime;

        public float BlastRadius => Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 170;
            Projectile.height = 170;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = BlastTime;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 46;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => AABBvCircle(targetHitbox, Projectile.Center, BlastSizeMultiplier * BlastRadius);

        public override void AI()
        {
            Color[] prettyColors = new Color[] { Color.DodgerBlue, Color.HotPink, Color.Orange };
            Lighting.AddLight(Projectile.Center, new Vector3(200, 210, 400) * 0.02f * (1 - Completion));

            if (Projectile.timeLeft == BlastTime)
            {
                //SoundEngine.PlaySound(SoundID.Item93 with { Volume = 1f }, Projectile.Center);
                //SoundEngine.PlaySound(SoundID.NPCHit44 with { Volume = 1f }, Projectile.Center);


                SoundEngine.PlaySound(DesertScourge.PreyBelchStormDebrisImpactSound, Projectile.Center);

                int tinyDustCount = (int)(10 + 30 * Utils.GetLerpValue(100, 400, Projectile.width, true));

                for (int i = 0; i < tinyDustCount / 2; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.width / 2);
                    Dust dus = Dust.NewDustPerfect(dustPos, 156, (dustPos - Projectile.Center) * 0.1f, 30);
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                }

                for (int i = 0; i < tinyDustCount; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.width / 2);
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();
                    Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - Projectile.Center) * 0.1f, Main.rand.Next(30, 60));
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                }
            }

            if (Main.rand.NextBool(2) && Completion < 0.5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.width / 2) * BlastSizeMultiplier;
                    int dusType = Main.rand.NextBool() ? ModContent.DustType<ElectroDust>() : ModContent.DustType<ElectroDustUnstable>();

                    Dust dus = Dust.NewDustPerfect(dustPos, dusType, (dustPos - Projectile.Center) * 0.1f, 30);
                    dus.noGravity = true;
                    dus.scale = Main.rand.NextFloat(0.8f, 1.2f) * (1 - (Completion / 0.5f));

                    dus.velocity = (dustPos - Projectile.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 4f);

                    dus.customData = Main.rand.Next(prettyColors);
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (target.Center.X - Main.player[Projectile.owner].Center.X).NonZeroSign();
        }


        public CurveSegment ExpandFast = new CurveSegment(PolyOutEasing, 0f, 0f, 1f, 3);
        public CurveSegment Unbounce = new CurveSegment(SineInEasing, 0.8f, 1f, -0.2f);
        public CurveSegment Shrink = new CurveSegment(SineInOutEasing, 0.85f, 0.8f, -0.8f);

        internal float BlastSizeMultiplier => PiecewiseAnimation(Completion, new CurveSegment[] { ExpandFast, Unbounce });

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = Scene["ElectroOrb"].GetShader().Shader;
            Texture2D pebbleNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "PebblesNoise").Value;
            Texture2D zapNoise = ModContent.Request<Texture2D>(AssetDirectory.Noise + "LightningNoise").Value;
            Texture2D ligjht = AssetDirectory.CommonTextures.PixelBloomCircle.Value;
            Texture2D bloom = AssetDirectory.CommonTextures.BloomCircle.Value;

            float size = BlastSizeMultiplier * BlastRadius;
            float coreRadiusPercent = Math.Max(20f / size, 0.2f);
            float colorMult = (float)Math.Pow(1 - Completion, 0.2f);
            float coreColorMult = (float)Math.Pow(1 - Completion, 0.05f);

            Vector2 resolution = new Vector2(BlastRadius);
            Main.spriteBatch.Draw(ligjht, Projectile.Center - Main.screenPosition, null, Color.Black * 0.4f * colorMult, 0, ligjht.Size() / 2f, 1.3f * size / (float)ligjht.Width, SpriteEffects.None, 0f);

            Vector4 coreColor = new Vector4(0.9f, 1.0f, 1.2f, 0.9f) * coreColorMult;
            Vector4 zapColor = new Vector4(0.1f, 0.16f, 0.26f, 1f) * coreColorMult;
            Vector4 edgeColor = new Vector4(0.3f, 0.5f, 0.85f, 1.0f) * colorMult;

            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.24f);
            effect.Parameters["zapTexture"].SetValue(zapNoise);
            effect.Parameters["resolution"].SetValue(resolution);
            effect.Parameters["coreColor"].SetValue(coreColor);
            effect.Parameters["edgeColor"].SetValue(edgeColor);
            effect.Parameters["zapColor"].SetValue(zapColor);
            effect.Parameters["blowUpSize"].SetValue(0.4f);
            effect.Parameters["maxRadius"].SetValue((0.95f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly)) * BlastSizeMultiplier);
            effect.Parameters["coreSolidRadius"].SetValue(coreRadiusPercent);
            effect.Parameters["coreFadeRadius"].SetValue(coreRadiusPercent * 0.25f);
            effect.Parameters["fresnelStrenght"].SetValue(7f + 7f * (float)Math.Pow(Completion, 4f));

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(pebbleNoise, Projectile.Center - Main.screenPosition, null, Color.White, 0, pebbleNoise.Size() / 2f, (BlastRadius * 2f) / (float)pebbleNoise.Width, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, (Color.RoyalBlue with { A = 0 }) * coreColorMult, 0, bloom.Size() / 2f, 1.6f * size / (float)bloom.Width, SpriteEffects.None, 0f);

            return false;
        }
    }
}