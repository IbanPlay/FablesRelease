using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Achievements;
using Terraria.Graphics.Shaders;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Content.Items.BurntDesert
{
    public class BrambleStaff : ModItem
    {
        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromAngryTumblers;
        }

        private int peevedTumblerType = NPCID.NegativeIDCount - 1; //Cant just set it to -1 cuz lol negative IDs

        private void DropFromAngryTumblers(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.Tumbleweed || npc.type == peevedTumblerType)
                npcloot.Add(Type, new Fraction(1, 10));
        }

        public static readonly SoundStyle HitSound = new("CalamityFables/Sounds/TumbleweedHit", 2);
        public static readonly SoundStyle ReslotSound = new("CalamityFables/Sounds/BouncyReappear");


        public static float TUMBLEWEED_THROWN_DAMAGE_MULT = 1.35f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bramble Staff");
            Tooltip.SetDefault("Summons a pair of orbital tumbleweeds\n"
                + "Hitting a tumbleweed with a whip sends it flying forwards");
            Item.ResearchUnlockCount = 1;
            Item.staff[Type] = true;

            //Cache peeved tumbler ID from spirit so it can be used for the drop
            if (CalamityFables.SpiritEnabled && ModContent.TryFind("SpiritReforged/PeevedTumbler", out ModNPC peevedTumbler))
                peevedTumblerType = peevedTumbler.Type;
        }
        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.knockBack = 2f;
            Item.mana = 10;
            Item.width = 38;
            Item.height = 40;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 2, 50, 0);
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<BrambleMinion>();
            Item.buffType = ModContent.BuffType<BrambleMinionBuff>();
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //Set the tumbleweed's ai[0] to be the count of tumbleweeds the player already has
            if (player.altFunctionUse != 2)
            {
                player.AddBuff(Item.buffType, 2);

                if (player.maxMinions - player.slotsMinions >= 1f)
                {
                    //Spawn two of them
                    for (int minion = 0; minion < 2; minion++)
                    {
                        float rotation = 0;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile p = Main.projectile[i];
                            if (p.owner != player.whoAmI || !p.active || p.type != type || (float)Math.Floor(p.ai[0]) / 1 != 0)
                                continue;
                            rotation = p.ai[2];
                            break;
                        }

                        Projectile proj = Projectile.NewProjectileDirect(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, player.ownedProjectileCounts[type] + minion, 0, rotation);
                        proj.originalDamage = Item.damage;
                    }
                }
            }
            return false;
        }
    }

    public class BrambleMinion : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public static Asset<Texture2D> WhirlwindTexture;
        public float DistanceToPlayer => 130f + 60f * Utils.GetLerpValue(4, 8, Owner.ownedProjectileCounts[Type], true);

        //Unhinged coder uses the whole and fractional parts of a decimal number for different purposes
        public float CirclePosition {
            get {
                if (Owner.ownedProjectileCounts[Type] == 0)
                    return 0;
                return Projectile.ai[0] / (float)Owner.ownedProjectileCounts[Type];
            }
        }

        public float OrbitTimer {
            get {
                return Projectile.ai[2] * MathHelper.TwoPi;
            }
            set {
                Projectile.ai[2] = (value / MathHelper.TwoPi);
            }
        }

        public bool Orbiting {
            get => Projectile.ai[1] == 0;
            set => Projectile.ai[1] = 0;
        }
        public bool Bonked {
            get => Projectile.ai[1] >= 1;
            set => Projectile.ai[1] = 1;
        }

        public float BonkTimer {
            get => Projectile.ai[1] - 1;
            set => Projectile.ai[1] = value + 1;
        }

        public bool punchEffects;
        public float punchEffectTimer;
        public float sandstormApparitionTimer;

        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tumbleweed");
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.penetrate = -1;
            Projectile.minionSlots = 0.5f;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            #region active check
            if (Owner.dead || !Owner.active) // This is the "active check", makes sure the minion is alive while the Player is alive, and despawns if not
                Owner.ClearBuff(ModContent.BuffType<BrambleMinionBuff>());
            if (Owner.HasBuff(ModContent.BuffType<BrambleMinionBuff>()))
                Projectile.timeLeft = 2;
            #endregion

            float spinSpeed = 0.08f;
            float distanceToPlayer = DistanceToPlayer;
            Vector2 idealOrbitPosition = Owner.MountedCenter + (MathHelper.TwoPi * CirclePosition + OrbitTimer).ToRotationVector2() * distanceToPlayer;
            OrbitTimer += spinSpeed;

            if (Orbiting)
            {
                Projectile.numHits = 0;

                if (Projectile.DistanceSQ(Owner.Center) > 3000 * 3000)
                    Projectile.Center = idealOrbitPosition;

                Projectile.Center = Vector2.Lerp(Projectile.Center, idealOrbitPosition, 0.43f);
                Projectile.rotation += spinSpeed * 2f;
                Projectile.velocity = Vector2.Zero;

                punchEffects = false;
                if (CheckWhips(out Vector2 whipDirection))
                {
                    Projectile.velocity = whipDirection * 42f;
                    Bonked = true;
                    Projectile.netUpdate = true;
                }
            }

            if (Bonked)
            {
                if (!punchEffects)
                {
                    punchEffects = true;
                    SoundEngine.PlaySound(BrambleStaff.HitSound, Projectile.Center);
                    if (Projectile.owner == Main.myPlayer)
                        CameraManager.AddCameraEffect(new DirectionalCameraTug(Projectile.velocity * 0.05f, 2f, 25, easingDegree: 4, uniqueIdentity: "TumbleweedStaff"));
                    punchEffectTimer = 1f;
                }

                Projectile.rotation += 0.23f * Projectile.velocity.X.NonZeroSign();
                punchEffectTimer -= 1 / (60f * 0.3f);
                BonkTimer++;
                FlyingDustEffects();

                if (BonkTimer < 10)
                {
                    Vector2 normalizedVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    NPC potentialTarget = Main.npc.Where(x =>
                        x.active &&
                        x.CanBeChasedBy() &&
                        Vector2.Dot((x.Center - Projectile.Center).SafeNormalize(Vector2.UnitX), normalizedVelocity) > 0.4f)
                        .OrderBy(x => Projectile.DistanceSQ(x.Center)).FirstOrDefault();

                    //Homing
                    if (potentialTarget != null && potentialTarget.Distance(Projectile.Center) < 500)
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(potentialTarget.Center) * Projectile.velocity.Length(), 0.4f);

                    if (Projectile.Distance(Owner.Center) > 800f)
                        BonkTimer = 10;
                }

                else
                {
                    if (BonkTimer > 30)
                    {
                        Vector2 futurePosition = Vector2.Lerp(Projectile.Center, idealOrbitPosition, 0.1f + 0.9f * ((BonkTimer - 30f) / 30f));
                        Projectile.velocity = futurePosition - Projectile.Center;
                    }
                    else
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, (idealOrbitPosition - Projectile.Center) * 0.1f, 0.09f);

                    if (BonkTimer > 60 || Projectile.Distance(idealOrbitPosition) < 26f)
                    {
                        Orbiting = true;
                        SoundEngine.PlaySound(BrambleStaff.ReslotSound with { PitchVariance = 0.1f, Pitch = -0.4f, Volume = 0.8f }, Projectile.Center);
                    }
                }

            }

            CutThorns();

            if (CirclePosition == 0)
                DustVortexEffects(distanceToPlayer);
        }

        public void CutThorns()
        {
            if (Main.myPlayer != Projectile.owner || Owner.dontHurtNature)
                return;

            int minCutX = (int)(Projectile.position.X / 16f);
            int maxCutX = (int)((Projectile.position.X + (float)Projectile.width) / 16f) + 1;
            int minCutY = (int)(Projectile.position.Y / 16f);
            int maxCutY = (int)((Projectile.position.Y + (float)Projectile.height) / 16f) + 1;

            if (minCutX < 0)
                minCutX = 0;
            if (maxCutX > Main.maxTilesX)
                maxCutX = Main.maxTilesX;
            if (minCutY < 0)
                minCutY = 0;
            if (maxCutY > Main.maxTilesY)
                maxCutY = Main.maxTilesY;

            for (int i = minCutX; i < maxCutX; i++)
            {
                for (int j = minCutY; j < maxCutY; j++)
                {
                    if (Main.tile[i, j].HasTile && TileID.Sets.Conversion.Thorn[Main.tile[i, j].TileType] && WorldGen.CanCutTile(i, j, TileCuttingContext.AttackProjectile))
                    {
                        WorldGen.KillTile(i, j);
                        if (Main.netMode != NetmodeID.SinglePlayer)
                            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j);
                    }
                }
            }
        }

        public void DustVortexEffects(float distanceToPlayer)
        {
            //Transparent dust
            float dustCount = 4 + 8 * Utils.GetLerpValue(1, 3, Owner.ownedProjectileCounts[Type], true);
            for (int i = 0; i < dustCount; i++)
            {
                float distanceToCenter = 0.4f + 0.8f * (float)Math.Pow(Main.rand.NextFloat(), 0.4f);
                float dustRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 dustVelocity = (dustRotation + MathHelper.PiOver2).ToRotationVector2() * (distanceToCenter * 7f + 3f);

                Dust d = Dust.NewDustPerfect(Owner.MountedCenter + dustRotation.ToRotationVector2() * distanceToCenter * distanceToPlayer, DustID.Sand, Vector2.Zero, 160);
                d.velocity = dustVelocity;
                d.noGravity = true;
                d.fadeIn = 0f;


                d.shader = GameShaders.Armor.GetSecondaryShader(Owner.cMinion, Owner);
            }

            //Not transparent dust
            for (int i = 0; i < dustCount / 2f; i++)
            {
                float distanceToCenter = Main.rand.NextFloat();
                float realDistanceToCenter = 0.7f + 0.6f * distanceToCenter;
                float dustRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 dustVelocity = (dustRotation + MathHelper.PiOver2).ToRotationVector2() * (realDistanceToCenter * 3f + 2f);
                int dustType = Main.rand.NextBool(5) ? 232 : DustID.Sand;

                Dust d = Dust.NewDustPerfect(Owner.MountedCenter + dustRotation.ToRotationVector2() * realDistanceToCenter * distanceToPlayer, dustType, Vector2.Zero, 20);

                d.scale = Main.rand.NextFloat(0.8f, 1.2f);

                //Downscale edge dust
                distanceToCenter = Math.Abs((distanceToCenter * 2) - 1);
                if (distanceToCenter > 0.5f)
                    d.scale *= 1 - ((distanceToCenter - 0.5f) * 2f);

                if (Main.rand.NextBool(6))
                {
                    dustVelocity *= -1;
                    d.scale *= 0.5f;
                }

                d.velocity = dustVelocity;
                d.noGravity = true;
                d.fadeIn = 0f;

                d.shader = GameShaders.Armor.GetSecondaryShader(Owner.cMinion, Owner);
            }
        }

        public void FlyingDustEffects()
        {
            //Transparent dust
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(28f, 28f), DustID.Sand, Vector2.Zero, 160, default, Main.rand.NextFloat(1f, 1.4f));
                d.velocity = -Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * 0.2f;
                d.noGravity = true;
                d.fadeIn = 0f;
            }

            if (Main.rand.NextBool())
            {
                //Not transparent dust
                int dustType = Main.rand.NextBool(5) ? 232 : DustID.Sand;
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), dustType, Vector2.Zero, 160, default, Main.rand.NextFloat(1f, 1.4f));
                d.velocity = -Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * 0.3f;
                d.noGravity = true;
                d.fadeIn = 0f;
            }
        }

        public bool CheckWhips(out Vector2 whipDirection)
        {
            whipDirection = Vector2.Zero;

            Rectangle generousHitbox = Projectile.Hitbox;
            generousHitbox.Inflate(19, 19);

            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile proj = Main.projectile[i];

                if (proj == null || !proj.active || proj.damage == 0 || proj.owner != Projectile.owner || !ProjectileID.Sets.IsAWhip[proj.type])
                    continue;

                if (proj.ModProjectile != null && proj.ModProjectile.CanDamage() == false)
                    continue;

                //No modproj check because IsAWhip overrides the collision cfheck, lmao
                if (Projectile.Colliding(proj.Hitbox, generousHitbox))
                {
                    whipDirection = proj.velocity.SafeNormalize(Vector2.One);
                    return true;
                }
            }

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Bonked)
            {
                modifiers.SourceDamage *= BrambleStaff.TUMBLEWEED_THROWN_DAMAGE_MULT; //Base boosted damage
                modifiers.FinalDamage *= (float)Math.Pow(0.7f, Projectile.numHits); //Exponential damage reduction per hit
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (CirclePosition == 0)
            {
                WhirlwindTexture = WhirlwindTexture ?? ModContent.Request<Texture2D>(Texture + "Storm");
                Texture2D whirlwindTex = WhirlwindTexture.Value;

                float opacityMultiplier = Utils.GetLerpValue(0, 3, Owner.ownedProjectileCounts[Type], true);
                opacityMultiplier *= sandstormApparitionTimer;
                if (sandstormApparitionTimer < 1)
                {
                    sandstormApparitionTimer += 0.04f;
                    if (sandstormApparitionTimer > 1)
                        sandstormApparitionTimer = 1;
                }

                float scaleMultiplier = 1 + (DistanceToPlayer / 130f - 1) * 0.5f;

                for (int i = 0; i < 3; i++)
                {
                    float modulino = (Main.GlobalTimeWrappedHourly + i / 3f) % 1;
                    float rotation = i / 3f * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 2.9f;
                    float scale = PolyInOutEasing(modulino, 2) * 0.25f + 0.6f;
                    scale *= scaleMultiplier;
                    Color centerColor = Lighting.GetColor(Owner.MountedCenter.ToTileCoordinates());

                    float opacity = 1f;
                    if (modulino < 0.5f)
                        opacity *= SineInEasing(modulino * 2f);
                    else
                        opacity *= PolyOutEasing(1 - (modulino - 0.5f) * 2f, 1.4f);
                    opacity *= 0.3f * opacityMultiplier;

                    centerColor *= opacity;

                    Main.EntitySpriteDraw(whirlwindTex, Owner.MountedCenter - Main.screenPosition, null, centerColor, rotation, whirlwindTex.Size() / 2f, scale, 0, 0);
                }
            }

            Texture2D tex = TextureAssets.Projectile[Type].Value;
            for (int i = 9; i >= 0; i--)
            {
                float progression = 1 - i / 10f;
                Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, null, lightColor * 0.5f * (float)Math.Pow(progression, 2f), Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0, 0);

            }

            float scaleBoost = 1f;
            if (punchEffectTimer > 0f)
                scaleBoost += 0.4f * (float)Math.Pow(punchEffectTimer, 2f);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale * 1.2f * scaleBoost, 0, 0);
            return false;
        }
    }

    public class BrambleMinionBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tumbleweed Storm");
            Description.SetDefault("The tumbleweeds will protect you");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override string Texture => AssetDirectory.DesertItems + Name;

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<BrambleMinion>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
