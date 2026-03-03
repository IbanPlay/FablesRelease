using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Drawing;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class GroundBeef : ModNPC
    {
        public override string Texture => AssetDirectory.DesertScourge + Name;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;

        public bool Falling {
            get => NPC.ai[0] == 0 && !NPC.IsABestiaryIconDummy;
            set => NPC.ai[0] = value ? 0 : 1;
        }

        public bool Poisoned {
            get => NPC.ai[1] != 0;
            set => NPC.ai[1] = value ? 1 : 0;
        }

        public ref float TimeSpent => ref NPC.ai[2];
        public float squishy = 1f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rotting Meat");
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.damage = DesertScourge.ChungryLunge_MeatDamage;
            NPC.lifeMax = DesertScourge.ChungryLunge_MeatHealth;

            NPC.width = 40;
            NPC.height = 40;
            NPC.aiStyle = -1;
            AIType = -1;

            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.canGhostHeal = false;
            NPC.chaseable = false;
            NPC.dontTakeDamage = true;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath21;

            NPC.direction = Main.rand.NextBool() ? 1 : -1;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            int associatedNPCType = ModContent.NPCType<DesertScourge>();
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[associatedNPCType], quickUnlock: true);

            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
                new FlavorTextBestiaryInfoElement("Mods.CalamityFables.BestiaryEntries.GroundBeef")
            });
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = DesertScourge.ChungryLunge_MeatHealth;
        }

        public override bool? CanFallThroughPlatforms() => true;

        public virtual int FrameWidth => 80;
        public virtual int FrameHeight => 42;

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
                squishy = MathHelper.Lerp(squishy, 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + NPC.whoAmI * 0.4f), 0.2f);

            if (!Falling)
                NPC.frame.Y = 0;
            else
            {
                NPC.frameCounter += 0.1f;
                NPC.frame.Y = 1 + (int)((NPC.frameCounter) % 4);
            }
            NPC.frame = new Rectangle(0, NPC.frame.Y * FrameHeight, FrameWidth, FrameHeight - 2);
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<DesertScourge>()))
            {
                DevourEffects(-Vector2.UnitY * 0.4f);
                NPC.HitEffect();
                NPC.life = 0;
                NPC.dontTakeDamage = false;
                NPC.checkDead();
                return;
            }

            if (Falling)
            {
                squishy = 1f;
                NPC.rotation += NPC.velocity.X * 0.047f;
                NPC.velocity.Y += 0.2f;
                if (NPC.velocity.Y > 16f)
                    NPC.velocity.Y = 16;

                DoDust();
                if (!Main.dedServ)
                {
                    ManageCache();
                    ManageTrail();
                }

                if (NPC.collideY || Collision.SolidCollision(NPC.position + Vector2.UnitY * 4f, NPC.width, NPC.height, false))
                {
                    NPC.frame.Y = 0;
                    NPC.rotation = 0;
                    Falling = false;
                    squishy = 1.7f;
                    SoundEngine.PlaySound(SoundID.DD2_OgreSpit, NPC.Center);
                    NPC.Resize(80, 36);
                }
                return;
            }

            TimeSpent++;
            NPC.velocity.X = 0;
            NPC.rotation = 0;
            NPC.dontTakeDamage = false;
            NPC.noGravity = false;
            NPC.chaseable = true;
            NPC.damage = 0;
            HandlePoisonChecks();
            DoPoisonDust();

            if (Main.rand.NextBool(100))
            {
                ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.PooFly, new ParticleOrchestraSettings
                {
                    PositionInWorld = Main.rand.NextVector2FromRectangle(NPC.Hitbox) + new Vector2(Main.rand.NextFloat(-20f, 20f), -5f)
                });
            }

            NPC.frame.Y = 0;

            if (Poisoned)
            {
                NPC.dontTakeDamage = true;
            }

            squishy = MathHelper.Lerp(squishy, 1f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + NPC.whoAmI * 0.4f), 0.2f);
            if (NPC.IsABestiaryIconDummy)
                squishy = 1f;
        }

        public virtual void DoDust()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? 5 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.velocity = -NPC.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 2f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            if (Main.rand.NextBool(4))
            {
                int goreType = Mod.Find<ModGore>("GroundBeefGore" + Main.rand.Next(1, 6).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center + Main.rand.NextVector2Circular(10f, 10f), NPC.velocity * 0.4f, goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 100);
                gore.scale *= 0.5f;
            }
        }

        public void DoPoisonDust()
        {
            if (!Poisoned)
                return;

            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 256, 0, 0, Main.rand.Next(20, 100));
                dusty.scale = Main.rand.NextFloat(0.8f, 2.5f);
                dusty.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 2f);
                dusty.noLight = true;
                dusty.noGravity = true;
                dusty.rotation = Main.rand.NextFloat(MathHelper.Pi);
            }
        }

        public void ManageCache()
        {
            //Initialize the cache
            if (cache == null)
            {
                cache = new List<Vector2>();

                for (int i = 0; i < 16; i++)
                {
                    cache.Add(NPC.Center + NPC.velocity);
                }
            }

            cache.Add(NPC.Center + NPC.velocity);
            while (cache.Count > 16)
                cache.RemoveAt(0);
        }

        public virtual void ManageTrail()
        {
            TrailDrawer ??= new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, NPC.Center + NPC.velocity, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = NPC.Center + NPC.velocity * 1.05f;
        }

        public virtual float WidthFunction(float completion)
        {
            return 11f * (float)Math.Pow(completion, 0.6f) * (0.85f + 0.15f * Utils.GetLerpValue(12f, 5f, NPC.velocity.Length(), true));
        }

        public virtual Color ColorFunction(float completion)
        {
            return Color.Lerp(Color.Sienna, Color.Crimson, (float)Math.Pow(completion, 3f)) * completion;
        }


        public void HandlePoisonChecks()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                if (!Poisoned && NPC.Hitbox.Contains(Main.MouseWorld.ToPoint()) && Main.LocalPlayer.HasItem(ItemID.JungleSpores) && Main.LocalPlayer.Distance(NPC.Center) < 450)
                {
                    Main.LocalPlayer.cursorItemIconEnabled = true;
                    Main.LocalPlayer.cursorItemIconID = ItemID.JungleSpores;
                    Main.LocalPlayer.cursorItemIconText = "";
                    NPC.ShowNameOnHover = false;

                    if (Main.mouseRight && Main.mouseRightRelease && Main.LocalPlayer.Distance(NPC.Center) < 300)
                    {
                        Main.LocalPlayer.ConsumeItem(ItemID.JungleSpores);
                        new GroundBeefPoisoningPacket(NPC).Send(-1, -1, true);
                    }
                }

                else
                    NPC.ShowNameOnHover = true;
            }
        }

        public void RumblingSandEffects(float speed)
        {
            for (int d = 0; d < 2; d++)
            {
                Dust dus = Dust.NewDustPerfect(NPC.BottomLeft + Vector2.UnitX * NPC.width * Main.rand.NextFloat(), DustID.Sand, -Vector2.UnitY * speed * Main.rand.NextFloat(0.5f, 1.2f), 0);
                dus.noGravity = false;
                dus.velocity = dus.velocity.RotatedByRandom(MathHelper.PiOver4);
            }
        }

        public virtual void DevourEffects(Vector2 blastVelocity)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 20; i++)
            {
                int goreType = Mod.Find<ModGore>("GroundBeefGore" + Main.rand.Next(1, 6).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center + Main.rand.NextVector2Circular(10f, 10f), -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 8f), goreType);
                gore.timeLeft = 5;
                gore.alpha = Main.rand.Next(30, 100);
                gore.scale *= Main.rand.NextFloat(0.3f, 1f);
                gore.sticky = true;
            }
            for (int i = 0; i < 40; i++)
            {
                Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? 5 : 105, 0, 0, 20);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.6f);
                dusty.velocity = blastVelocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1.2f, 3f);
                dusty.noLight = true;
                dusty.noGravity = false;
            }

            if (Poisoned)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust bubel = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 256, 0, 0, Main.rand.Next(20, 100));
                    bubel.scale = Main.rand.NextFloat(1.5f, 3.5f);
                    bubel.velocity = blastVelocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1.2f, 12f);
                    bubel.noLight = true;
                    bubel.noGravity = true;
                    bubel.rotation = Main.rand.NextFloat(MathHelper.Pi);
                }
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            //REsist explosives
            if (projectile.aiStyle == 16)
                modifiers.FinalDamage *= DesertScourge.ChungryLunge_MeatExplosionResist;
        }

        public override void OnKill()
        {
            if (NPC.hide)
                return;

            int closestPlayer = Player.FindClosest(NPC.Center, 1, 1);
            if (Main.rand.NextBool(4) && Main.player[closestPlayer].statLife < Main.player[closestPlayer].statLifeMax2)
                Item.NewItem(NPC.GetSource_Loot(), (int)NPC.position.X, (int)NPC.position.Y, NPC.width, NPC.height, ItemID.Heart);
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
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, hit.HitDirection, -1f, 0, default, 1f);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            if (Falling)
            {
                Effect effect = Scene["GlorpTrail"].GetShader().Shader;
                effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.2f + NPC.whoAmI * 0.1f);
                effect.Parameters["repeats"].SetValue(3f);
                effect.Parameters["voronoi"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "Voronoi").Value);
                effect.Parameters["noiseOverlay"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "PrettyManifoldNoise").Value);
                TrailDrawer?.Render(effect, -screenPos);
            }

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Rectangle frame = NPC.frame;
            Vector2 scale = new Vector2(squishy, 2f - squishy);
            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
            Vector2 position = NPC.Bottom + Vector2.UnitY * 2f;
            SpriteEffects flip = NPC.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            if (Falling)
            {
                position = NPC.Center;
                origin = frame.Size() / 2;
            }
            if (Poisoned)
                lightColor = lightColor.MultiplyRGB(Color.Lerp(Color.Khaki, Color.LimeGreen, 0.4f));

            if (NPC.IsABestiaryIconDummy)
                position += Vector2.UnitY * 16f;

            spriteBatch.Draw(texture, position - screenPos, frame, lightColor, NPC.rotation, origin, scale * NPC.scale, flip, 0);
            return false;
        }
    }

    [Serializable]
    public class GroundBeefPoisoningPacket : Module
    {
        byte whoAmI;
        int beef;

        public GroundBeefPoisoningPacket(NPC beef)
        {
            this.beef = beef.whoAmI;
            whoAmI = (byte)Main.myPlayer;
        }

        protected override void Receive()
        {
            NPC npc = Main.npc[beef];
            SoundEngine.PlaySound(SoundID.Item87, npc.Center);
            npc.ai[1] = 1;
            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    public class ScourgeFlesh : GroundBeef
    {
        public override string Texture => AssetDirectory.DesertScourge + Name;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Scourge Meat");
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.damage = 0;
            NPC.lifeMax = DesertScourge.ChungryLunge_ScourgeMeatHealth;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            NPC.lifeMax = DesertScourge.ChungryLunge_ScourgeMeatHealth;
        }


        public override int FrameWidth => 82;
        public override int FrameHeight => 42;

        public override void DoDust()
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? 5 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.velocity = -NPC.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 2f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            if (Main.rand.NextBool(4))
            {
                int goreType = Mod.Find<ModGore>("ScourgeBeefGore" + Main.rand.Next(5, 9).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center + Main.rand.NextVector2Circular(10f, 10f), NPC.velocity * 0.4f, goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 100);
                gore.scale *= 0.5f;
            }
        }

        public override void DevourEffects(Vector2 blastVelocity)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 20; i++)
            {
                int goreType = Mod.Find<ModGore>("ScourgeBeefGore" + Main.rand.Next(1, 9).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(NPC.GetSource_FromAI(), NPC.Center + Main.rand.NextVector2Circular(10f, 10f), -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(3f, 8f), goreType);
                gore.timeLeft = 5;
                gore.alpha = Main.rand.Next(30, 100);
                gore.scale *= Main.rand.NextFloat(0.3f, 1f);
                gore.sticky = true;
            }
            for (int i = 0; i < 40; i++)
            {
                Dust dusty = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Main.rand.NextBool() ? 5 : 105, 0, 0, 20);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.6f);
                dusty.velocity = blastVelocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1.2f, 3f);
                dusty.noLight = true;
                dusty.noGravity = false;
            }

            if (Poisoned)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust bubel = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 256, 0, 0, Main.rand.Next(20, 100));
                    bubel.scale = Main.rand.NextFloat(1.5f, 3.5f);
                    bubel.velocity = blastVelocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(1.2f, 12f);
                    bubel.noLight = true;
                    bubel.noGravity = true;
                    bubel.rotation = Main.rand.NextFloat(MathHelper.Pi);
                }
            }
        }
    }
}
