using Terraria.DataStructures;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class DesertScourgeHitbox : ModNPC, ICustomDeathMessages
    {
        public override string Texture => AssetDirectory.Invisible;
        public int BrainIndex => (int)NPC.ai[0];
        public int NextSegmentIndex => (int)NPC.ai[1];
        public int SegmentIndex => (int)NPC.ai[2];

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Desert Scourge");
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = DesertScourge.Stat_LifeMax;
            NPC.defense = DesertScourge.Stat_Defense;
            NPC.damage = DesertScourge.Stat_ContactDamage;
            NPC.width = 60;
            NPC.height = 60;

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.value = Item.buyPrice(0, 5, 0, 0);
            NPC.behindTiles = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;

            NPC.netAlways = true;
            NPC.dontCountMe = true;
            NPC.frame = new Rectangle(0, 0, NPC.width, NPC.height);
            NPC.dontTakeDamage = true;
        }


        public override bool CheckActive() => false;
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) => DesertScourge.ScaleExpertStats(NPC, numPlayers, balance);
        public override void AI()
        {
            NPC.frame = new Rectangle(0, 0, NPC.width, NPC.height);

            if (BrainIndex > 0)
                NPC.realLife = BrainIndex;

            bool shouldDespawn = true;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[BrainIndex].active && Main.npc[BrainIndex].type == ModContent.NPCType<DesertScourge>())
                {
                    shouldDespawn = false;
                    break;
                }
            }

            if (shouldDespawn)
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }

            NPC.chaseable = Main.npc[BrainIndex].chaseable;
            NPC.dontTakeDamage = Main.npc[BrainIndex].dontTakeDamage;
            NPC.damage = Main.npc[BrainIndex].damage;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            bool canHit = Main.npc[BrainIndex].ModNPC.CanHitPlayer(target, ref cooldownSlot);
            if (!canHit)
                return canHit;

            float tailPosition = (SegmentIndex / ((float)DesertScourge.SegmentCount - 1));
            if (tailPosition > DesertScourge.NoHurtingTailTipStart)
                return false;

            float sizeReduction = 0.5f + 0.5f * (float)Math.Pow(tailPosition, 0.6f);
            sizeReduction *= DesertScourge.SegmentHurtboxSizeMultiplier(Main.npc[BrainIndex]);
            return Collision.CheckAABBvAABBCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(), NPC.Center - NPC.Hitbox.Size() * 0.5f * sizeReduction, NPC.Hitbox.Size() * sizeReduction);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            Main.npc[BrainIndex].VanillaSpoofPlayerHitIFrames(player);

            DesertScourge scourgeHead = (Main.npc[BrainIndex].ModNPC as DesertScourge);
            scourgeHead.RecursivelySpoofPlayerHitIFrames(Main.npc[scourgeHead.nextHitbox], player);

            scourgeHead.TakeSegmentDamage(SegmentIndex, hit.Damage);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC head = Main.npc[BrainIndex];
            DesertScourge scourgeHead = (head.ModNPC as DesertScourge);

            head.VanillaSpoofProjectileHitIFrames(projectile);
            scourgeHead.RecursivelySpoofProjectileIframes(Main.npc[scourgeHead.nextHitbox], projectile);

            float fleshDamageMultiplier = 1f;

            if (hit.Damage > 40 && projectile.aiStyle == 16 && Main.rand.NextBool(2) && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
            {
                for (int i = 0; i < 3; i++)
                {
                    int goreType = Mod.Find<ModGore>("ScourgeBeefGore" + Main.rand.Next(1, 5).ToString()).Type;
                    Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center + Main.rand.NextVector2Circular(30f, 30f), -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(1f, 2f), goreType);
                    gore.timeLeft = 7;
                    gore.alpha = Main.rand.Next(30, 50);
                }

                fleshDamageMultiplier = 4f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC meatball = NPC.NewNPCDirect(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ScourgeFlesh>(), NPC.whoAmI);
                    meatball.velocity = Main.rand.NextVector2Circular(3f, 3f);
                }
            }

            scourgeHead.TakeSegmentDamage(SegmentIndex, (int)(hit.Damage * fleshDamageMultiplier));
        }
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            DesertScourge.ApplyMeleeWeakness(ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.IsContactMelee())
                DesertScourge.ApplyMeleeWeakness(ref modifiers);
        }

        public override bool? CanBeHitByProjectile(Projectile projectile) => Main.npc[BrainIndex].VanillaCanBeHitByProjectile(projectile);
        public override bool? CanBeHitByItem(Player player, Item item) => Main.npc[BrainIndex].VanillaCanBeHitByPlayer(player);

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            if (Main.npc[BrainIndex].type != ModContent.NPCType<DesertScourge>())
            {
                return false;
            }

            DesertScourge scourgeHead = (Main.npc[BrainIndex].ModNPC as DesertScourge);
            return scourgeHead.CustomDeathMessage(player, ref customDeath);
        }
    }
}
