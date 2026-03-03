using CalamityFables.Content.Items.Cursed;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    [AutoloadEquip(EquipType.Head)]
    public class PontiffsPiper : ModItem
    {

        public static readonly SoundStyle ShootSound = new SoundStyle("CalamityFables/Sounds/PontiffsPiperShoot", 4) { MaxInstances = 0 };
        public static readonly SoundStyle TootSound = new SoundStyle("CalamityFables/Sounds/PontiffPiperSing", 5) { MaxInstances = 0 };
        public static readonly SoundStyle UnsummonSound = new SoundStyle("CalamityFables/Sounds/PontiffPiperDie");
        public static readonly SoundStyle BuffMinionSound = new SoundStyle("CalamityFables/Sounds/PontiffsPiperAuraCreate");
        public static readonly SoundStyle BuffProjectileFireSound = new SoundStyle("CalamityFables/Sounds/PontiffsPiperAuraFire");

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromBloodMoons;

            if (Main.netMode != NetmodeID.Server)
            {
                EquipLoader.AddEquipTexture(Mod, AssetDirectory.EarlyGameMisc + Name + "_HeadVanity", EquipType.Head, name: "PontiffsPiperVanity");
            }
        }

        private void DropFromBloodMoons(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.BloodZombie || npc.type == NPCID.Drippler)
            {
                npcloot.Add(Type, new Fraction(1, 100));
            }
        }

        public static int NOTEDAMAGE = 18;
        public static float MAXBUFFCOOLDOWN = 10f;
        public static float MINBUFFCOOLDOWN = 3f;

        public static float MINIONBUFFTIME = 6f;
        public static float MINIONBUFFFIRECOOLDOWN = 0.8f;
        public static int MINIONBUFFDAMAGE = 10;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Pontiff's Piper");
            Tooltip.SetDefault("Summons a bagpipe squid ontop of your head\n" +
                "The bagpipe squid can empower other minions, and will gain power as your health lowers\n" +
                "Increases your max number of minions by 1\n" +
                "[c/a97171:'...An engraving is found on the bottom of the plate, which reads “To Giovanni Battista Pamphili: My utmost apologies“']");

            if (Main.netMode == NetmodeID.Server)
                return;

            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHatHair[equipSlot] = true;

            equipSlot = EquipLoader.GetEquipSlot(Mod, "PontiffsPiperVanity", EquipType.Head);
            ArmorIDs.Head.Sets.DrawHatHair[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 2, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.defense = 2;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<PontiffsPipePlayer>().pontiffPiped = true;
            player.maxMinions++;
            if (!Main.gameMenu && Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[ProjectileType<PontiffsPiperSquid>()] == 0)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ProjectileType<PontiffsPiperSquid>(), 0, 0, player.whoAmI);
            }
        }

        public override bool IsVanitySet(int head, int body, int legs) => head == EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);

        public override void PreUpdateVanitySet(Player player)
        {
            player.GetModPlayer<PontiffsPipePlayer>().vanityHat = true;
            if (!Main.gameMenu && Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[ProjectileType<PontiffsPiperSquid>()] == 0)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center, Vector2.Zero, ProjectileType<PontiffsPiperSquid>(), 0, 0, player.whoAmI);
            }
        }
    }

    public class PontiffsPipePlayer : ModPlayer
    {
        public bool pontiffPiped;
        public bool vanityHat;
        public override void ResetEffects()
        {
            pontiffPiped = false;
            vanityHat = false;
        }

        public override void FrameEffects()
        {
            if (vanityHat)
                Player.head = Player.head = EquipLoader.GetEquipSlot(Mod, "PontiffsPiperVanity", EquipType.Head);
        }
    }

    public class PontiffsPiperSquid : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;
        public Player Owner => Main.player[Projectile.owner];
        public ref float FireTimer => ref Projectile.ai[0];
        public ref float BuffFireTimer => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Piper");
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 100;
            Projectile.minionSlots = 0;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Owner.dead || !Owner.active) // This is the "active check", makes sure the minion is alive while the Player is alive, and despawns if not
            {
                Projectile.Kill();
                return;
            }

            var modPlayer = Owner.GetModPlayer<PontiffsPipePlayer>();
            if (modPlayer.pontiffPiped || modPlayer.vanityHat)
                Projectile.timeLeft = 2;

            Projectile.Center = Owner.Center - Vector2.UnitY * 30f * Owner.gravDir;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 10)
            {
                Projectile.frame = ++Projectile.frame % 6;
                Projectile.frameCounter = 0;
            }

            if (!modPlayer.pontiffPiped || Owner.GetModPlayer<PeculiarPotPlayer>().Potted)
                return;

            AttackLogic();
            BuffLogic();
        }

        public void AttackLogic()
        {
            bool hasTarget = Owner.HasMinionAttackTargetNPC;
            IEnumerable<NPC> nearbyTargets = new NPC[1];

            if (!hasTarget)
            {
                nearbyTargets = Main.npc.Where(n => n.active && n.CanBeChasedBy(Projectile) && !n.CountsAsACritter && !n.friendly && !n.townNPC && n.Distance(Projectile.Center) < 600 + n.HalfDiagonalLenght() && Collision.CanHit(Projectile.Center, 1, 1, n.Center, 1, 1));
                if (nearbyTargets.Count() > 0)
                    hasTarget = true;
            }

            if (FireTimer >= 1 || hasTarget)
            {
                //Only charges up towards firing if not already set to fire a buffing shot
                if (FireTimer < 1 && BuffFireTimer < 1f)
                    FireTimer += 1 / (60f * 0.5f);

                if (FireTimer >= 1)
                {
                    float fireTime = 1 / (60f * 1f);
                    FireTimer += fireTime;
                    Projectile.frame = (int)Math.Min(6 + (FireTimer - 1) * 6f, 11);

                    if (FireTimer < 1.3 && FireTimer + fireTime >= 1.3f)
                    {
                        int nearestTargetIndex = -1;
                        if (Owner.HasMinionAttackTargetNPC)
                            nearestTargetIndex = Owner.MinionAttackTargetNPC;
                        else
                        {
                            NPC nearestTarget = nearbyTargets.OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
                            nearestTargetIndex = nearestTarget == null ? -1 : nearestTarget.whoAmI;
                        }

                        FireProjectile(nearestTargetIndex);
                    }

                    if (FireTimer >= 2)
                        FireTimer = 0;
                }
            }
        }

        public void FireProjectile(int nearestTargetIndex)
        {
            SoundEngine.PlaySound(PontiffsPiper.ShootSound, Projectile.Center);

            if (Main.myPlayer == Owner.whoAmI)
            {
                Vector2 projectileVelocity = new Vector2(3f * Owner.direction, -3f * Owner.gravDir);
                Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center + projectileVelocity * 3f, projectileVelocity, ProjectileType<PontiffsPiperNotes>(), PontiffsPiper.NOTEDAMAGE, 0, Owner.whoAmI, 0, nearestTargetIndex);
                proj.originalDamage = PontiffsPiper.NOTEDAMAGE;
                proj.netUpdate = true;
            }
        }

        public bool IsProjectileMinion(Projectile p)
        {
            return p.active && p.minion && p.owner == Projectile.owner && !ProjectileID.Sets.MinionShot[p.type];
        }

        public void BuffLogic()
        {
            //Only buff minions if the player has more than one minion out
            bool hasMinionsToBuff = Main.projectile.Where(IsProjectileMinion).Count() > 1;
            bool alreadyBuffingMinion = Owner.ownedProjectileCounts[ProjectileType<PontiffsPiperBuffNotes>()] > 0;

            if (BuffFireTimer >= 1f || (hasMinionsToBuff && !alreadyBuffingMinion))
            {
                float chargeTime = 1 / (60f * MathHelper.Lerp(PontiffsPiper.MINBUFFCOOLDOWN, PontiffsPiper.MAXBUFFCOOLDOWN, Utils.GetLerpValue(0.1f, 1f, Owner.statLife / (float)Owner.statLifeMax, true)));

                //Only charges up until reaching 1, or if not firing
                if (BuffFireTimer < 1f)
                    BuffFireTimer += chargeTime;
                else
                {
                    float fireTime = 1 / (60f * 1f);

                    BuffFireTimer += fireTime;
                    Projectile.frame = (int)Math.Min(6 + (BuffFireTimer - 1) * 6f, 11);

                    if (BuffFireTimer < 1.3 && BuffFireTimer + fireTime >= 1.3f)
                    {
                        SoundEngine.PlaySound(PontiffsPiper.TootSound, Projectile.Center);

                        if (Main.myPlayer == Owner.whoAmI)
                        {
                            Vector2 projectileVelocity = new Vector2(3f * Owner.direction, -3f * Owner.gravDir);
                            Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center + projectileVelocity * 3f, projectileVelocity, ProjectileType<PontiffsPiperBuffNotes>(), 0, 0, Owner.whoAmI, -1);
                        }
                    }

                    if (BuffFireTimer >= 2)
                        BuffFireTimer = 0;
                }
            }

            else if (alreadyBuffingMinion)
            {
                BuffFireTimer = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            //Ring of small dust
            for (int i = 0; i < 8; i++)
            {
                Vector2 dustOffset = Vector2.UnitY.RotatedBy(i / 8f * MathHelper.TwoPi);
                Dust.NewDustPerfect(Projectile.Center + dustOffset * 10f, 5, dustOffset * Main.rand.NextFloat(0.2f, 0.7f), 30, default(Color), 1f);
            }

            for (int i = 0; i < 6; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f) * 10f, 5, -Vector2.UnitY * Main.rand.NextFloat(0.2f, 3.2f), 0, default(Color), 1f);
            }

            SoundEngine.PlaySound(PontiffsPiper.UnsummonSound, Owner.Center);
        }

        //Drawing is done on the player layer
        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class PontiffsPiperNotes : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public NPC Target {
            get {
                if (Projectile.ai[1] < 0 || Projectile.ai[1] > Main.maxNPCs)
                    return null;

                return Main.npc[(int)Projectile.ai[1]];
            }
            set {
                if (value == null)
                    Projectile.ai[1] = -1;
                else
                    Projectile.ai[1] = value.whoAmI;
            }
        }

        public int variant = 0;
        public float wobble = 1f;

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        public virtual Color GlowColor => Color.Goldenrod;

        public int HomingWaitTime => 20;
        public int AimingRampupTime => 140;
        public int Lifetime => 600;

        public float StartStopTimer {
            get {
                if (Projectile.timeLeft < Lifetime - HomingWaitTime)
                    return 1f;

                return (Lifetime - Projectile.timeLeft) / (float)HomingWaitTime;
            }
        }

        public float HomeInTimer {
            get {
                float extraTime = 5;
                if (Lifetime - Projectile.timeLeft < HomingWaitTime + extraTime)
                    return 0f;

                return Math.Min(1f, (Lifetime - Projectile.timeLeft - HomingWaitTime - extraTime) / (float)(AimingRampupTime - HomingWaitTime - extraTime));
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Toot");

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
            Projectile.timeLeft = Lifetime;
            variant = Main.rand.Next(4);
            wobble = 1f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.X * 0.05f;

            //Blast off.
            if (Projectile.timeLeft == Lifetime)
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDustPerfect(dustCenter, 5, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(1f, 1.3f));
                }
            }

            if (StartStopTimer < 1f)
                Projectile.velocity *= 1f - (float)Math.Pow(StartStopTimer, 3f) * 0.7f;

            else
            {
                if (Target != null)
                {
                    if (!Target.active || Target.dontTakeDamage)
                    {
                        Target = null;
                        return;
                    }

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center) * 16.01f, 0.02f + 0.2f * (float)Math.Pow(HomeInTimer, 0.8f));
                }

                else
                {
                    if (Owner.HasMinionAttackTargetNPC)
                        Target = Main.npc[Owner.MinionAttackTargetNPC];

                    else if (Projectile.timeLeft % 10 == 0)
                    {
                        var nearbyTargets = Main.npc.Where(n => n.active && n.CanBeChasedBy(Projectile) && !n.CountsAsACritter && !n.friendly && !n.townNPC && n.Distance(Projectile.Center) < 600 + n.HalfDiagonalLenght());
                        if (nearbyTargets.Count() > 0)
                            Target = nearbyTargets.OrderBy(n => n.Distance(Projectile.Center)).FirstOrDefault();
                    }

                    if (Target == null || !Target.active || Target.dontTakeDamage)
                        Projectile.velocity *= 0.9f;
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle frame = tex.Frame(1, 4, 0, variant, 0, -2);
            Vector2 origin = frame.Size() / 2f;

            float wobbleSquish = (float)(Math.Sin(wobble * MathHelper.Pi * 5f)) * 0.3f;
            Vector2 scale = Vector2.Lerp(Vector2.One, new Vector2(1 + wobbleSquish, 1 + (1 - wobbleSquish)), wobble) * Projectile.scale;
            if (wobble > 0)
                wobble -= 1 / (60f * 0.4f);

            float opacityMult = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * opacityMult, Projectile.rotation, origin, scale, 0, 0);

            if (StartStopTimer < 1f)
            {
                float glowOpacity = 1 - (float)Math.Pow(StartStopTimer, 0.5f);
                Color glowColor = GlowColor with { A = 0 } * glowOpacity;

                Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, glowColor * opacityMult, Projectile.rotation, origin, scale, 0, 0);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath12, Projectile.Center);

            int numParticles = Main.rand.Next(4, 7);
            for (int i = 0; i < numParticles; i++)
            {
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(MathHelper.Pi / 6f) * (1f + 8f * (float)Math.Pow(Main.rand.NextFloat(), 4f));
                Dust.NewDustPerfect(Projectile.Center, 5, velocity, Scale: Main.rand.NextFloat(1f, 1.3f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }
    }

    public class PontiffsPiperBuffNotes : PontiffsPiperNotes, IDrawPixelated
    {
        public struct MusicSheetNote
        {
            public Rectangle frame;
            public int variant;
            public int elevation;
            public float completionAroundMusic;
            public float minSpacing;

            public MusicSheetNote(float completion)
            {
                variant = completion == 0 ? 0 : Main.rand.Next(1, 5);
                frame = new Rectangle(0, 38 * variant, 20, 36);
                elevation = completion == 0 ? 0 : Main.rand.Next(-14, 3);
                completionAroundMusic = completion;
                switch (variant)
                {
                    case 0:
                        minSpacing = 14;
                        break;
                    case 1:
                        minSpacing = 6;
                        break;
                    case 2:
                        minSpacing = 14;
                        break;
                    case 3:
                        minSpacing = 16;
                        break;
                    case 4:
                    default:
                        minSpacing = 14;
                        break;
                }
            }
        }

        public List<MusicSheetNote> melody;
        public float auraRadius;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public override bool? CanDamage() => false;
        public override Color GlowColor => Color.Crimson;

        public PrimitiveClosedLoop loopDrawer;
        public float musicLoopApparitionTime = 0f;

        public bool IsAuraMode {
            get => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxProjectiles;
        }

        public ref float FireTimer => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = -1;
        }

        public bool IsProjectileMinion(Projectile p)
        {
            return p.active && p.minion && p.owner == Projectile.owner && !ProjectileID.Sets.MinionShot[p.type] && p.type != ProjectileType<PontiffsPiperSquid>() && !FablesSets.SubMinion[p.type];
        }

        public override void AI()
        {
            //Blast off.
            if (Projectile.timeLeft == Lifetime)
            {
                Vector2 dustCenter = Projectile.Center + Projectile.velocity * 1f;

                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDustPerfect(dustCenter, 5, Projectile.velocity.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0.6f, 1f), Scale: Main.rand.NextFloat(1f, 1.3f));
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Crimson.ToVector3());

            if (!IsAuraMode)
            {
                Projectile.rotation = Projectile.velocity.X * 0.05f;

                //Start by slowing down
                if (StartStopTimer < 1f)
                    Projectile.velocity *= 1f - (float)Math.Pow(StartStopTimer, 3f) * 0.7f;

                else if (HomeInTimer > 0f)
                {
                    var ownerMinions = Main.projectile.Where(IsProjectileMinion);
                    if (ownerMinions.Count() == 0)
                    {
                        Projectile.Kill();
                        return;
                    }

                    Projectile target = ownerMinions.OrderBy(p => p.Distance(Projectile.Center)).FirstOrDefault();
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 16.01f, 0.02f + 0.2f * (float)Math.Pow(HomeInTimer, 0.8f));

                    if (Projectile.Distance(target.Center) < 10f)
                    {
                        Projectile.ai[0] = target.whoAmI;
                        Projectile.rotation = 0f;
                        SoundEngine.PlaySound(PontiffsPiper.BuffMinionSound, Projectile.Center);
                        Projectile.timeLeft = (int)(PontiffsPiper.MINIONBUFFTIME * 60f);
                        if (Main.netMode == NetmodeID.MultiplayerClient && Main.myPlayer == Projectile.owner)
                            new PontiffsPiperBuffAuraPacket(Projectile).Send(-1, -1, false);
                    }
                }
            }

            else
            {
                Projectile anchor = Main.projectile[(int)Projectile.ai[0]];
                if (Main.myPlayer == Projectile.owner && !anchor.active || anchor.owner != Projectile.owner)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.rotation += 0.04f + 0.26f * (1 - musicLoopApparitionTime);
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = anchor.Center;

                auraRadius = anchor.HalfDiagonalLenght() + 30f;

                if (!Main.dedServ)
                {
                    if (loopDrawer == null)
                        loopDrawer = new PrimitiveClosedLoop(50, WidthFunction, ColorFunction);
                    loopDrawer.SetPositionsCircle(Projectile.Center, auraRadius, Projectile.rotation);
                }

                FireTimer += 1 / (60f * PontiffsPiper.MINIONBUFFFIRECOOLDOWN * (0.25f + 0.75f * Utils.GetLerpValue(0.1f, 1f, Owner.statLife / (float)Owner.statLifeMax, true)));


                if (FireTimer > 1f)
                {
                    var nearbyTargets = Main.npc.Where(n => n.active && n.CanBeChasedBy(Projectile) && !n.CountsAsACritter && !n.friendly && !n.townNPC && n.Distance(Projectile.Center) < 800 + n.HalfDiagonalLenght() && Collision.CanHitLine(Projectile.Center, 1, 1, n.Center, 1, 1));
                    if (nearbyTargets.Count() == 0)
                        return;

                    FireTimer = 0f;
                    if (Main.myPlayer == Owner.whoAmI)
                    {
                        Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ProjectileType<PontiffsPiperBloodShot>(), PontiffsPiper.MINIONBUFFDAMAGE, 2, Main.myPlayer);
                        proj.originalDamage = PontiffsPiper.MINIONBUFFDAMAGE;
                    }
                }
            }
        }

        public float WidthFunction(float completion)
        {
            return 14f;
        }

        public Color colorMult;
        public Color ColorFunction(float completion)
        {
            return colorMult;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!IsAuraMode)
                base.PreDraw(ref lightColor);
            else
                DrawMusicNotes();

            return false;
        }

        public void DrawMusicNotes()
        {
            if (auraRadius == 0)
                return;

            if (melody == null)
            {
                melody = new List<MusicSheetNote>();
                float melodyProgress = 0f;
                float totalCircumference = auraRadius * 2f * MathHelper.Pi;

                while (melodyProgress < totalCircumference * 0.95f)
                {
                    melody.Add(new MusicSheetNote(melodyProgress / totalCircumference));
                    melodyProgress += melody[melody.Count - 1].minSpacing + Main.rand.NextFloat(0f, 20f);
                }
            }

            Texture2D notesShadows = (Request<Texture2D>(AssetDirectory.EarlyGameMisc + "PontiffsPiperNoteSilouettes").Value);
            Texture2D notes = (Request<Texture2D>(AssetDirectory.EarlyGameMisc + "PontiffsPiperNoteSmall").Value);

            DrawNoteRing(notesShadows, Color.Maroon * 0.7f);
            DrawNoteRing(notes, Color.White);
        }

        public void DrawNoteRing(Texture2D noteTex, Color color)
        {
            foreach (MusicSheetNote note in melody)
            {
                float rotation = Projectile.rotation + note.completionAroundMusic * MathHelper.TwoPi + 0.03f;
                Vector2 position = Projectile.Center + rotation.ToRotationVector2() * (auraRadius + note.elevation);
                Vector2 origin = new Vector2(2f, note.frame.Height / 2f);

                Main.EntitySpriteDraw(noteTex, position - Main.screenPosition, note.frame, color * musicLoopApparitionTime, rotation + MathHelper.PiOver2 + 0.1f, origin, 1f, 0, 0);
            }
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (!IsAuraMode || loopDrawer == null)
                return;

            if (Projectile.timeLeft > 40f)
            {
                musicLoopApparitionTime += 1 / (60f * 0.15f);
                if (musicLoopApparitionTime > 1f)
                    musicLoopApparitionTime = 1f;
            }
            else
                musicLoopApparitionTime = Projectile.timeLeft / 40f;


            Effect effect = AssetDirectory.PrimShaders.TextureMap;
            effect.Parameters["scroll"].SetValue(0f);
            effect.Parameters["repeats"].SetValue(5f);
            effect.Parameters["sampleTexture"].SetValue(Request<Texture2D>(AssetDirectory.EarlyGameMisc + "PontiffsPiperBar").Value);

            colorMult = Color.White * musicLoopApparitionTime;
            //for (int i = 0; i < 4; i++)
            //    loopDrawer?.Render(effect, -Main.screenPosition + (i / 4f * MathHelper.TwoPi).ToRotationVector2() * 2f );
            loopDrawer?.Render(effect, -Main.screenPosition + Vector2.UnitY * 2f);

            colorMult = Color.White with { A = 0 } * musicLoopApparitionTime * 0.02f;
            loopDrawer?.Render(effect, -Main.screenPosition);
        }
    }

    [Serializable]
    public class PontiffsPiperBuffAuraPacket : Module
    {
        byte whoAmI;
        int identity;
        int minionIdentity;
        int minionIndex;

        public PontiffsPiperBuffAuraPacket(Projectile proj)
        {
            whoAmI = (byte)proj.owner;
            identity = proj.identity;
            minionIdentity = Main.projectile[(int)proj.ai[0]].identity;
            minionIndex = (int)proj.ai[0];
        }

        protected override void Receive()
        {
            Projectile proj = null;
            int buffedMinion = -1;

            //Fast check in case the minion's index is the same across clients
            if (Main.projectile[minionIndex].identity == minionIdentity)
                buffedMinion = minionIndex;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].owner == whoAmI && Main.projectile[i].identity == identity)
                {
                    proj = Main.projectile[i];

                    if (buffedMinion != -1)
                        break;
                }

                if (buffedMinion == -1 && Main.projectile[i].owner == whoAmI && Main.projectile[i].identity == minionIdentity)
                {
                    buffedMinion = i;
                    if (proj != null)
                        break;
                }
            }

            if (proj == null || buffedMinion == -1)
                return;

            proj.ai[0] = buffedMinion;
            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    public class PontiffsPiperBloodShot : ModProjectile, IDrawPixelated
    {
        public DrawhookLayer layer => DrawhookLayer.AboveTiles;
        public override string Texture => AssetDirectory.Particles + "StreakBloom";
        public Player Owner => Main.player[Projectile.owner];

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;
        public bool playedFireSound = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Shot");
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minion = true;
            Projectile.minionSlots = 0;
            Projectile.ArmorPenetration = 5;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = 0.55f + 0.4f * (float)Math.Pow(Projectile.timeLeft / 60f, 0.5f);

            if (!Main.dedServ)
            {
                ManageCache();
                ManageTrail();

                if (!playedFireSound)
                {
                    playedFireSound = true;
                    SoundEngine.PlaySound(PontiffsPiper.BuffProjectileFireSound, Projectile.Center);
                }
            }

            if (Main.rand.NextBool())
            {
                Vector2 dustCenter = Projectile.Center - Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(4f, 4f);

                Vector2 velocityDirection = Projectile.velocity.SafeNormalize(Vector2.Zero);
                dustCenter += velocityDirection.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1f, 1f) * 4f;

                int dustCount = Main.rand.Next(1, 3);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustSpeed = -velocityDirection.RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(0f, 2f);
                    Dust.NewDustPerfect(dustCenter, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(0.7f, 1f));
                }
            }

            var target = Main.npc.Where(n => n.active &&
            n.CanBeChasedBy(Projectile) && !n.townNPC && !n.CountsAsACritter &&
            Vector2.Distance(n.Center, Projectile.Center) < 800
            // &&
            //Vector2.Dot(Projectile.DirectionTo(n.Center), Projectile.velocity.SafeNormalize(Vector2.Zero)) > 0.5f
            ).OrderBy(n => Vector2.Distance(n.Center, Projectile.Center)).FirstOrDefault();
            if (target != default && target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.One) * 17f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.15f);
            }

            else
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streakBloomTex = TextureAssets.Projectile[Type].Value;

            Color bloomColor = Color.Red with { A = 120 };
            Vector2 bloomSquish = new Vector2(1f, 0.7f) * Projectile.scale;
            Main.EntitySpriteDraw(streakBloomTex, Projectile.Center - Main.screenPosition, null, bloomColor, Projectile.rotation, streakBloomTex.Size() / 2, bloomSquish, 0, 0);
            return false;
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();
                for (int i = 0; i < 15; i++)
                {
                    cache.Add(Projectile.Center);
                }
            }
            cache.Add(Projectile.Center + Projectile.velocity);

            while (cache.Count > 15)
            {
                cache.RemoveAt(0);
            }
        }

        public void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, Projectile.Center, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity * 2f;
        }

        internal Color colorUsed;
        internal Color ColorFunction(float completionRatio)
        {
            float fadeOpacity = (float)Math.Sqrt(completionRatio);
            return colorUsed * fadeOpacity;
        }
        internal float WidthFunction(float completionRatio)
        {
            return 3f * completionRatio;
        }


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            colorUsed = Color.Red with { A = 100 };
            TrailDrawer?.Render(null, -Main.screenPosition);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector2 dustCenter = target.Center + Main.rand.NextVector2Circular(2f, 2f);
                dustCenter += Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1f, 1f) * 7f;

                int dustCount = Main.rand.Next(2, 7);
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustSpeed = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4 * 0.56f) * Main.rand.NextFloat(4f, 8f);
                    Dust.NewDustPerfect(dustCenter, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(1.3f, 2f));
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (timeLeft == 60)
                return;

            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.NPCDeath11 with { MaxInstances = 0 }, Projectile.Center);

            if (!Main.dedServ && cache != null && cache.Count > 1)
            {
                GhostTrail clone = new GhostTrail(cache, TrailDrawer, 0.25f);
                clone.ShrinkTrailLenght = true;
                clone.Pixelated = true;
                clone.DrawLayer = DrawhookLayer.AboveTiles;
                GhostTrailsHandler.LogNewTrail(clone);
            }

            int dustCount = Main.rand.Next(2, 6);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpeed = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0f, 2f);
                dustSpeed += Projectile.velocity * 0.5f;
                Dust.NewDustPerfect(Projectile.Center, DustID.Blood, dustSpeed, Scale: Main.rand.NextFloat(1f, 1.4f));
            }
        }
    }
}
