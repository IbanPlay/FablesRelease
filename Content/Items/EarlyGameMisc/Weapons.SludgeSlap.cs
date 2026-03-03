using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace CalamityFables.Content.Items.EarlyGameMisc
{
    public class SludgeSlap : ModItem
    {
        public static readonly SoundStyle SludgeSlapSwing = new SoundStyle("CalamityFables/Sounds/StickyHandWhip", 2);
        public static readonly SoundStyle SludgeSlapCrack = new SoundStyle("CalamityFables/Sounds/StickyHandCrack", 3) { Volume = 1.25f };
        public static readonly SoundStyle SludgeSlapClap = new SoundStyle("CalamityFables/Sounds/StickyHandClap", 2);

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        internal static LocalizedText SlimelingDamageText;

        public static float SLAP_KNOCKBACK_MULT = 8;
        public static float MULTIHIT_PENALTY = 0.4f;
        public static float WHIP_OPACITY = 0.6f;
        public static float SLIMELING_DAMAGE_MULT = 1 / 3f;
        public static float SLIMELING_KNOCKBACK_MULT = 0.4f;
        public static float SLIME_OPACITY = 0.8f;
        public static float TIP_STRIKE_SLIMELING_CHANCE = 1f;
        public static int SLIMELING_CAP = 100;
        public static int SLIMELING_LIFETIME = 1800;

        public static readonly int DefaultSlimelingDamage = (int)(25 * SLIMELING_DAMAGE_MULT);
        public static readonly float DefaultSlimelingKnockback = 2.5f * SLIMELING_KNOCKBACK_MULT;

        public override void Load()
        {
            FablesNPC.ModifyNPCLootEvent += DropFromKS;
            FablesItem.ModifyItemLootEvent += DropFromKSBossBag;
            ItemID.Sets.BonusAttackSpeedMultiplier[Type] = 0.5f;
        }

        public override void SetStaticDefaults()
        {
            SlimelingDamageText = Mod.GetLocalization("Extras.ItemTooltipExtras.SludgeSlapSlimelingDamage");
        }

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<SludgeSlapProjectile>(), 25, 2.5f, 5, 40);

            Item.autoReuse = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(silver: 50);
            Item.channel = true;

            Item.UseSound = SludgeSlapSwing;
        }

        public override bool CanUseItem(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return true;
            return player.ownedProjectileCounts[Item.shoot] == 0;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int damageIndex = tooltips.FindIndex(tooltip => tooltip.Name == "Damage" && tooltip.Mod == "Terraria");
            int slimelingDamage = (int)(Main.LocalPlayer.GetWeaponDamage(Item, true) * SLIMELING_DAMAGE_MULT);
            TooltipLine SlimelingDamageLine = new TooltipLine(Mod, "CalamityFables:SlimelingDamage", SlimelingDamageText.Format(slimelingDamage));
            SlimelingDamageLine.OverrideColor = Color.Lerp(Color.White, Color.DodgerBlue, (float)Math.Pow(0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly * 2f), 4f));
            tooltips.Insert(damageIndex + 1, SlimelingDamageLine);
        }

        public override bool MeleePrefix() => true; // Whips gotta have legendary!!!

        // Drop from King Slime at a 25% chance
        private void DropFromKSBossBag(Item item, ItemLoot itemLoot)
        {
            if (item.type == ItemID.KingSlimeBossBag)
                itemLoot.Add(Type, new Fraction(1, 3));
        }

        private void DropFromKS(NPC npc, NPCLoot npcloot)
        {
            if (npc.type == NPCID.KingSlime)
            {
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.Add(Type, new Fraction(1, 4));
                npcloot.Add(notExpertRule);
            }
        }
    }

    public class SludgeSlapProjectile : BaseWhip, IDrawPixelated
    {
        //Data storing class for the hit of a whip
        public class SludgeSlapWhipHitNPCData : CustomGlobalData
        {
            public int SlimelingDamage = SludgeSlap.DefaultSlimelingDamage;
            public float SlimelingKnockback = SludgeSlap.DefaultSlimelingKnockback;

            public SludgeSlapWhipHitNPCData(Projectile proj)
            {
                SlimelingDamage = (int)(proj.damage * SludgeSlap.SLIMELING_DAMAGE_MULT);
                SlimelingKnockback = proj.knockBack * SludgeSlap.SLIMELING_KNOCKBACK_MULT;
            }
        }

        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public SludgeSlapProjectile() : base("Sludge Slap", 20, 0.6f) { }

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public bool ShoulDrawPixelated => DrawTrail;

        public ref float TipStrikes => ref Projectile.ai[1];
        public bool DrawTrail => AnimProgress > 0.5f && AnimProgress < 0.8f;
        public bool FullyExtended => AnimProgress > 0.63f && AnimProgress < 0.71f;

        internal PrimitiveTrail ChainTrail;
        internal PrimitiveSliceTrail SliceTrail;
        internal List<Vector2> CacheEnd;
        internal List<Vector2> CacheStart;

        public override void SafeSetDefaults()
        {
            Projectile.localNPCHitCooldown = 20 * Projectile.MaxUpdates;
            yFrames = 4;
            CrackSound = SludgeSlap.SludgeSlapCrack;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void ArcAI()
        {
            // Create slimeglobs while the whip trail is being drawn. prevents early whip animation weirdness
            if (DrawTrail && CacheStart != null && Main.rand.NextBool(10))
            {
                Vector2 trailVector = EndPoint - CacheStart.Last();
                Vector2 particleVelocity = trailVector.SafeNormalize(Vector2.Zero).RotatedByRandom(0.1f) * Main.rand.NextFloat(4, 5);

                SpawnDroplet(EndPoint, particleVelocity, Main.rand.NextFloat(0.6f, 0.8f), Main.rand.Next(20, 30));
            }

            // Player swing animation, animated here to simplify
            Player.CompositeArmStretchAmount stretchAmount = Player.CompositeArmStretchAmount.Full;
            if (AnimProgress < 0.25)
                stretchAmount = Player.CompositeArmStretchAmount.Quarter;
            else if (AnimProgress < 0.7)
                stretchAmount = Player.CompositeArmStretchAmount.ThreeQuarters;
            Owner.SetCompositeArmFront(true, stretchAmount, ArmRotationForAnimation());

            ManageTipperCache();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Tipper effects, can only trigger once per swing
            // Ignores multihit penalty, increases knockback, and spawns a slimeling
            if (BigSlap(target))
            {
                TipStrikes++;

                // Increase knockback by modifier
                modifiers.Knockback *= SludgeSlap.SLAP_KNOCKBACK_MULT;

                // Summon slimeling
                if (Main.rand.NextFloat() < SludgeSlap.TIP_STRIKE_SLIMELING_CHANCE && target.CanBeChasedBy() && Owner.ownedProjectileCounts[SludgeSlapNPCDebuff.SlimelingType] < SludgeSlap.SLIMELING_CAP)
                {
                    Vector2 slimeballVelocity = target.Center - Owner.Center;
                    slimeballVelocity.Normalize();
                    slimeballVelocity = slimeballVelocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(8, 10f);

                    int damage = (int)(Projectile.damage * SludgeSlap.SLIMELING_DAMAGE_MULT);
                    float knockback = Projectile.knockBack * SludgeSlap.SLIMELING_KNOCKBACK_MULT;
                    Projectile.NewProjectile(Owner.GetSource_OnHit(target), target.Center, slimeballVelocity, SludgeSlapNPCDebuff.SlimelingType, damage, knockback, Owner.whoAmI);
                }

                SoundEngine.PlaySound(SludgeSlap.SludgeSlapClap, EndPoint);
                SpawnSlapParticles(EndPoint, Projectile.velocity);
                new SlapParticlesPacket(Projectile).Send();
            }
            else
            {
                // Multihit damage penalty, like vanilla whips
                // Done in a weird way so slaps will always do full damage
                modifiers.SourceDamage *= (float)Math.Pow(1 - SludgeSlap.MULTIHIT_PENALTY, Projectile.numHits - TipStrikes);
            }

            // Add debuff, but only to 1 target
            if (Projectile.numHits < 1)
            {
                target.AddBuff(ModContent.BuffType<SludgeSlapNPCDebuff>(), 240);
                //Assigns data that saves the projectile's damage values to the NPC
                target.SetNPCData(new SludgeSlapWhipHitNPCData(Projectile));
            }
        }

        /// <summary>
        /// Checks if target hitbox intersects with endpoint for tipper
        /// </summary>
        public bool BigSlap(NPC target)
        {
            Rectangle tipperHitbox = Projectile.Hitbox;
            tipperHitbox.Location = new Point((int)(EndPoint.X - Projectile.Hitbox.Width / 2), (int)(EndPoint.Y - Projectile.Hitbox.Height / 2));
            return tipperHitbox.Intersects(target.Hitbox) && FullyExtended && TipStrikes == 0;
        }

        #region Whip Animation
        public float ArmRotationForAnimation()
        {
            float animRot = 7f * (float)Math.PI / 6f * (float)Math.Exp(-9 * Math.Pow(AnimProgress - 0.2f, 2)) + ((float)Math.PI / 6f);
            float directionalOffset = Projectile.direction > 0 ? 0 : MathHelper.Pi;
            return animRot * -Projectile.direction + Projectile.velocity.ToRotation() + directionalOffset;
        }

        // Recreates control points to make handle movement smoother and fit with the player animation. Kinda scuffed but it works
        // IL editing arm position would achieve the same thing and I tried but I couldn't get it to work
        public override void CustomizeWhipControlPoints(List<Vector2> controlPoints)
        {

            controlPoints.Clear();
            float timeModified = AnimProgress * 1.5f;
            float segmentOffset = MathHelper.Pi * 10f * (1f - timeModified) * -Projectile.spriteDirection / Segments;
            float tLerp = 0f;

            if (timeModified > 1f)
            {
                tLerp = (timeModified - 1f) / 0.5f;
                timeModified = MathHelper.Lerp(1f, 0f, tLerp);
            }

            //vanilla code
            Item heldItem = Owner.HeldItem;
            float realRange = ContentSamples.ItemsByType[heldItem.type].useAnimation * 2 * AnimProgress * Owner.whipRangeMultiplier;
            float num8 = Projectile.velocity.Length() * realRange * timeModified * Range / Segments;
            Vector2 playerArmPosition = Owner.MountedCenter + new Vector2(0, 12).RotatedBy(ArmRotationForAnimation());   // Arm position change
            Vector2 firstPos = playerArmPosition;
            float num10 = 0f - MathHelper.PiOver2;
            Vector2 midPos = firstPos;
            float num11 = 0f + MathHelper.PiOver2 + MathHelper.PiOver2 * Projectile.spriteDirection;
            Vector2 lastPos = firstPos;
            float num12 = 0f + MathHelper.PiOver2;
            controlPoints.Add(playerArmPosition);

            for (int i = 0; i < Segments; i++)
            {
                float num14 = segmentOffset * (i / (float)Segments);
                Vector2 nextFirst = firstPos + num10.ToRotationVector2() * num8;
                Vector2 nextLast = lastPos + num12.ToRotationVector2() * (num8 * 2f);
                Vector2 nextMid = midPos + num11.ToRotationVector2() * (num8 * 2f);
                float num15 = 1f - timeModified;
                float num16 = 1f - num15 * num15;
                var value3 = Vector2.Lerp(nextLast, nextFirst, num16 * 0.9f + 0.1f);
                var value4 = Vector2.Lerp(nextMid, value3, num16 * 0.7f + 0.3f);
                Vector2 spinningpoint = playerArmPosition + (value4 - playerArmPosition) * new Vector2(1f, 1.5f);
                float num17 = tLerp;
                num17 *= num17;
                Vector2 item = spinningpoint.RotatedBy(Projectile.rotation + 4.712389f * num17 * Projectile.spriteDirection, playerArmPosition);
                controlPoints.Add(item);
                num10 += num14;
                num12 += num14;
                num11 += num14;
                firstPos = nextFirst;
                lastPos = nextLast;
                midPos = nextMid;
            }
        }

        #endregion

        #region Prims
        public void ManageTipperCache()
        {
            if (Main.dedServ)
                return;

            float rotation = (PointsForCollision[^2] - EndPoint).ToRotation() - MathHelper.PiOver2;
            Vector2 handEnd = EndPoint + new Vector2(0, -20).RotatedBy(rotation);
            Vector2 chainPos = PointsForCollision[^4];

            // Cache tip of the hand
            CacheEnd ??= [];
            CacheEnd.Add(handEnd);
            while (CacheEnd.Count > 8)
                CacheEnd.RemoveAt(0);

            // Cache along chain near the end of the whip
            CacheStart ??= [];
            CacheStart.Add(chainPos);
            while (CacheStart.Count > 8)
                CacheStart.RemoveAt(0);

            SliceTrail ??= new PrimitiveSliceTrail(20, TipperLightingFunction);
            SliceTrail.SetPositions(CacheEnd, CacheStart, FablesUtils.SmoothBezierPointRetreivalFunction);
        }

        public Color TipperLightingFunction(float factorAlongTrail)
        {
            Vector2 position = CacheEnd[(int)(factorAlongTrail * (CacheEnd.Count - 1))];
            Color lighting = Lighting.GetColor(position.ToTileCoordinates());

            return new Color(50, 100, 255).MultiplyRGB(lighting);
        }

        public void ManageChainPrims(List<Vector2> points)
        {
            ChainTrail ??= new PrimitiveTrail(20, f => 3, ChainLightingFunction);
            ChainTrail.SetPositionsSmart(points, Projectile.Center);
        }

        public Color ChainLightingFunction(float factorAlongTrail)
        {
            Vector2 position = PointsForCollision[(int)(20 * factorAlongTrail)];
            Color lighting = Lighting.GetColor(position.ToTileCoordinates());

            return lighting * SludgeSlap.WHIP_OPACITY;
        }
        #endregion

        #region Drawcode
        public override void DrawMidSegments(Texture2D texture, List<Vector2> points, SpriteEffects effects)
        {
            // Literally cant be managed anywhere else, the chain wont be in the right place. Doesnt break on pause tho
            ManageChainPrims(points);

            // Required restart to immediate for player rendering
            Main.spriteBatch.End();

            Effect effect = AssetDirectory.PrimShaders.TextureMap;
            effect.Parameters["repeats"].SetValue(1);
            effect.Parameters["scroll"].SetValue(0);
            effect.Parameters["sampleTexture"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + Name + "Chain").Value);
            ChainTrail?.Render(effect, -Main.screenPosition);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override void DrawLastSegment(Texture2D texture, Vector2 position, Vector2 nextPosition, SpriteEffects effects)
        {
            // Change frame end depending on whip anim progress
            int frameNum = FullyExtended ? 1 : AnimProgress < 0.67 ? 2 : 3;
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, frameNum);

            // Changes scale at very beginning and end of anim to look less scuffed
            float scale = (float)Math.Min(2.5f - Math.Abs(5 * AnimProgress - 2.5f), 1);

            // Origin to be at end of frame
            Vector2 origin = new(whipFrame.Width * 0.5f, 0);
            Color color = Projectile.GetAlpha(Lighting.GetColor(position.ToTileCoordinates()));
            float rotation = (nextPosition - position).ToRotation() - MathHelper.PiOver2;

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, whipFrame, color * SludgeSlap.WHIP_OPACITY, rotation, origin, Projectile.scale * scale, effects, 0);
        }

        // Draw slice trail at the end of the whip
        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Effect effect = Scene["SlicePrimitive"].GetShader().Shader;
            effect.Parameters["edgeSize"].SetValue(0.07f);
            effect.Parameters["edgeSizePower"].SetValue(2f);
            effect.Parameters["edgeTransitionSize"].SetValue(0.03f);
            effect.Parameters["edgeTransitionOpacity"].SetValue(0.2f);
            effect.Parameters["edgeColorMultiplier"].SetValue(new Vector4(1.25f, 1.25f, 1f, 1f));
            effect.Parameters["horizontalPower"].SetValue(1f + AnimProgress * 3f);
            effect.Parameters["verticalPower"].SetValue(1.2f);
            SliceTrail?.Render(effect, -Main.screenPosition);
        }
        #endregion

        #region Particles

        public static void SpawnDroplet(Vector2 position, Vector2 velocity, float scale, int lifetime)
        {
            Color color = new Color(59, 114, 230) * SludgeSlap.WHIP_OPACITY;
            Color outlineColor = new Color(20, 62, 148) * SludgeSlap.WHIP_OPACITY;

            Particle drops = new PrimitiveStreak(position, velocity, color, color, 3f * scale, 0, 4, lifetime, outlineColor, outlineColor, true)
            {
                TrailTip = new TriangularTip(4),
                Acceleration = Vector2.Zero,
                Collision = true
            };

            ParticleHandler.SpawnParticle(drops);
        }

        public static void SpawnSlapParticles(Vector2 endPoint, Vector2 velocity)
        {
            Color dustColor = Color.DodgerBlue;
            Color highlight = Color.White;
            float handSize = Main.rand.NextFloat(0.9f, 1.2f);

            Particle hand = new SlapHandParticle(endPoint, dustColor, highlight, null, 0.8f, handSize);
            ParticleHandler.SpawnParticle(hand);

            for (int i = 0; i < Main.rand.Next(3, 5); i++)
            {
                // Least complex particle spawn
                Vector2 trailVector = velocity;
                trailVector.Normalize();
                Vector2 particleVelocity = trailVector.SafeNormalize(Vector2.Zero).RotatedByRandom(1.25f) * Main.rand.NextFloat(5, 10);
                float width = Main.rand.NextFloat(2.5f, 3.5f);

                TrailColorFunction colorFunction(Func<float, Color> lightColorFunc, float lifetimeCompletion, bool outline = false)
                {
                    return (progress) =>
                    {
                        Color startColor = outline ? Color.DodgerBlue : Color.White;
                        Color fadeColor = (outline ? new Color(20, 62, 148) : new Color(59, 114, 230)) * SludgeSlap.WHIP_OPACITY;

                        float fadeEasing = FablesUtils.PolyInOutEasing(lifetimeCompletion);
                        Color color = Color.Lerp(startColor, fadeColor, fadeEasing);

                        // Opacity
                        color *= 1f - MathF.Pow(Utils.GetLerpValue(0.5f, 1f, lifetimeCompletion, true), 2);

                        // No light at start, fade to light color
                        if (lightColorFunc != null)
                        {
                            Color lightColor = color.MultiplyRGBA(lightColorFunc(progress));
                            color = Color.Lerp(color, lightColor, fadeEasing);
                        }

                        return color;
                    };
                }

                Particle drops = new PrimitiveStreak(endPoint, particleVelocity, colorFunction, PrimitiveStreak.ConvertWidthFunction(f => width * f), 4, Main.rand.Next(30, 50))
                {
                    Outline = true,
                    TrailTip = new TriangularTip(4),
                    Acceleration = Vector2.Zero,
                };

                ParticleHandler.SpawnParticle(drops);
            }

            // Vanilla dust
            for (int i = 0; i < Main.rand.Next(10, 15); i++)
            {
                Vector2 dustVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3, 5);
                Dust magicDust = Dust.NewDustPerfect(endPoint + Main.rand.NextVector2Circular(10, 10), DustID.MagicMirror, dustVelocity);
                magicDust.noGravity = true;
            }
        }

        #endregion
    }

    [Serializable]
    public class SlapParticlesPacket(Projectile projectile) : Module
    {
        public byte WhoAmI = (byte)Main.myPlayer;
        public byte Identity = (byte)projectile.identity;

        protected override void Receive()
        {
            if (Main.netMode == NetmodeID.Server)
                Send(-1, WhoAmI, false);
            else
                foreach (Projectile projectile in Main.ActiveProjectiles)
                    if (projectile.identity == Identity && projectile.ModProjectile is SludgeSlapProjectile sludgeSlap)
                    {
                        SoundEngine.PlaySound(SludgeSlap.SludgeSlapClap, sludgeSlap.EndPoint);
                        SludgeSlapProjectile.SpawnSlapParticles(sludgeSlap.EndPoint, projectile.velocity);
                    }
        }
    }

    public class SludgeSlapNPCDebuff : ModBuff
    {
        public override string Texture => AssetDirectory.Buffs + "GelIcon";

        public static int SlimelingType;

        public override void Load()
        {
            FablesProjectile.OnHitNPCEvent += HitEffect;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sludged");
            Description.SetDefault("Covered in slimy sludge");
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;      
            BuffID.Sets.CanBeRemovedByNetMessage[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.SetNPCFlag(Name);
            
            // Droplet particles
            if (Main.rand.NextBool(30))
            {
                Vector2 particlePosition = Main.rand.NextVector2FromRectangle(npc.Hitbox);
                SludgeSlapProjectile.SpawnDroplet(particlePosition, Vector2.Zero, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 45));
            }
        }

        private void HitEffect(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Only works with minion attacks
            if (!projectile.minion && !ProjectileID.Sets.MinionShot[projectile.type] && !projectile.sentry && !ProjectileID.Sets.SentryShot[projectile.type])
                return;

            // Spawns slimeling if target has the debuff and projectile is a summon that isnt a slimeling
            if (target.GetNPCFlag(Name) && projectile.type != SlimelingType && target.CanBeChasedBy()) 
            {
                Vector2 slimeballVector = Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)) * -Main.rand.NextFloat(6, 8);

                // Retrieve damage and knockback from the data we wrote
                int damage = SludgeSlap.DefaultSlimelingDamage;
                float knockback = SludgeSlap.DefaultSlimelingKnockback;
                if (target.GetNPCData<SludgeSlapProjectile.SludgeSlapWhipHitNPCData>(out var whipHitData))
                {
                    damage = whipHitData.SlimelingDamage;
                    knockback = whipHitData.SlimelingKnockback;
                }

                // Summon Slimeling
                Projectile.NewProjectile(projectile.GetSource_OnHit(target), target.Center, slimeballVector, ModContent.ProjectileType<SludgeSlapSlimeling>(), damage, knockback, projectile.owner);
                target.RequestBuffRemoval(ModContent.BuffType<SludgeSlapNPCDebuff>());
            }
        }
    }

    public class SludgeSlapSlimelingBuff : ModBuff
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;

        #region Storing the time having had the buff for the crown visuals
        public class SludgeSlapCrownTimeData : CustomGlobalData
        {
            public int SlimelingBuffAge = 0;
        }

        public override void Load()
        {
            FablesPlayer.PostUpdateMiscEffectsEvent += CheckForBuffDeletion;
        }

        private void CheckForBuffDeletion(Player player)
        {
            if (!player.GetPlayerFlag(Name) && player.GetPlayerData(out SludgeSlapCrownTimeData crownAge) && crownAge.SlimelingBuffAge > 0)
            {
                crownAge.SlimelingBuffAge = 0;

                // Create particle that appears when the crown dissapears
                // Spawned here since otherwise it can't be spawned when the buff icon is right clicked
                Vector2 particlePosition = player.Center + new Vector2(0, -28);
                Vector2 particleVelocity = new Vector2(0, -3).RotatedBy(Main.rand.NextFloat(0.45f, 0.9f) * -player.direction);

                Particle crownParticle = new GelKingCrownParticle(particlePosition, particleVelocity);
                ParticleHandler.SpawnParticle(crownParticle);
            }
        }
        #endregion

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Slimelings");
            Description.SetDefault("Loyal yet puny minions");
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Checks if the player with this buff has the minion
            // Renews the buff's time left and resets the player flag, removes the buff otherwise
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SludgeSlapSlimeling>()] > 0)
            {
                int buffTime = 0;

                // Find slimeling with highest time left, that will be the time displayed on the buff icon
                foreach(Projectile projectile in Main.ActiveProjectiles)
                    if (projectile.ModProjectile is SludgeSlapSlimeling && projectile.owner == player.whoAmI && projectile.timeLeft > buffTime)
                    {
                        buffTime = projectile.timeLeft;
                        if (projectile.timeLeft >= SludgeSlap.SLIMELING_LIFETIME)
                            break;
                    }

                player.buffTime[buffIndex] = buffTime;
                player.SetPlayerFlag(Name);

                //Keep track of how long the player has had the buff
                if (!player.GetPlayerData(out SludgeSlapCrownTimeData crownAge))
                {
                    crownAge = new SludgeSlapCrownTimeData();
                    player.SetPlayerData(crownAge);
                }
                crownAge.SlimelingBuffAge++;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // Change icon frame depending on owned slimelings
            int slimelings = Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<SludgeSlapSlimeling>()];
            int frame = (int)Math.Min(slimelings * 0.4f, 2);

            Texture2D texture = drawParams.Texture;
            Rectangle iconFrame = texture.Frame(3, 1, frame, 0);

            Main.spriteBatch.Draw(texture, drawParams.Position, iconFrame, drawParams.DrawColor, 0f, default, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class SludgeSlapSlimeling : ModProjectile, IDrawPixelated
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + Name;
        public Asset<Texture2D> GoopTexture;

        public DrawhookLayer layer => DrawhookLayer.AboveNPCs;
        public bool ShoulDrawPixelated => true;

        internal PrimitiveTrail Trail;
        public List<Vector2> Cache;
        internal int TrailLayer = 0;

        public Player Owner => Main.player[Projectile.owner];

        public enum AIState
        {
            Slimeball,
            Fighting,
            CatchupFlight
        }

        public AIState CurrentAIState
        {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        internal int GoopTrailFrame = 0;
        internal int GoopTrailFrameCounter = 0;

        private bool AddBuff = true;

        public override void Load() => FablesProjectile.ModifyProjectileDyeEvent += ModifyProjectileDye;

        public override void SetStaticDefaults()
        {
            FablesSets.SubMinion[Type] = true;
            Main.projFrames[Type] = 4;
            Main.projPet[Type] = true;

            SludgeSlapNPCDebuff.SlimelingType = Type;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 18;
            Projectile.penetrate = 3;
            Projectile.netImportant = true;
            Projectile.timeLeft = SludgeSlap.SLIMELING_LIFETIME;
            Projectile.minion = true;
            Projectile.friendly = true;

            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        #region AI
        public override void AI()
        {
            // Ensures minion buff has been added on spawn
            if (AddBuff)
            {
                Owner.AddBuff(ModContent.BuffType<SludgeSlapSlimelingBuff>(), 18000);
                AddBuff = false;
            }

            if (Owner.dead || !Owner.HasBuff<SludgeSlapSlimelingBuff>())
                Projectile.Kill();

            if (Projectile.timeLeft < 20)
                Projectile.scale = MathF.Pow(Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true), 0.5f);

            // Always maintain cache
            ManageTrail();

            // Slimeball AI
            if (CurrentAIState == AIState.Slimeball)
            {
                Projectile.tileCollide = true;
                Projectile.shouldFallThrough = true;

                if (Collision.SolidCollision(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height))
                    CurrentAIState = AIState.Fighting;

                if (Projectile.velocity.Y < 10f)
                    Projectile.velocity.Y += 0.26f;
                Projectile.velocity.X *= 0.98f;

                if (Projectile.velocity != Vector2.Zero)
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                ManageFlyingFrames();
            }
            // Catchup flight AI
            else if (CurrentAIState == AIState.CatchupFlight)
            {
                FlyBackToPlayer();

                if (Projectile.velocity != Vector2.Zero)
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                ManageFlyingFrames();          
            }
            // Fighting AI
            else
            {
                Fighting();
                ManageFightingFrames();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (CurrentAIState == AIState.Slimeball)
                CurrentAIState = AIState.Fighting;
            return true;
        }

        public void FlyBackToPlayer()
        {
            Projectile.tileCollide = false;
            Projectile.shouldFallThrough = true;

            //Ideal position is above the player
            Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * 30f - Vector2.UnitX * Owner.direction * 30;
            idealPosition += Vector2.UnitX.RotatedBy(Projectile.minionPos * 0.4f) * 30f;
            float distanceToIdealPosition = (Projectile.Center - idealPosition).Length();

            Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.045f;

            // Max velocity is either 6.5 or the speed of the player, so it never has trouble catching up
            float maxVelocity = Math.Max(Owner.velocity.Length(), 6.5f);
            if (goalVelocity.Length() > maxVelocity)
                goalVelocity = goalVelocity.ClampMagnitude(maxVelocity);

            if (distanceToIdealPosition > 60f)
            {
                Projectile.velocity.X += goalVelocity.X * 0.1f;
                Projectile.velocity.Y += goalVelocity.Y * 0.06f;

                //A low lerp leads to curvier movement , which is nice when its hovering around its destination
                float lerpStrength = 0.01f + Utils.GetLerpValue(100, 300, distanceToIdealPosition, true) * 0.07f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, lerpStrength);
            }

            // Stop flying
            if (Projectile.WithinRange(Owner.Center, 200) && Owner.velocity.Y == 0f && Projectile.Bottom.Y <= Owner.Bottom.Y && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                CurrentAIState = AIState.Fighting;

            // Teleport to the player if way too far
            if (distanceToIdealPosition > 2000)
                Projectile.Center = Owner.Center;
        }

        public void Fighting()
        {
            Projectile.tileCollide = true;
            Projectile.shouldFallThrough = Owner.Bottom.Y - 12f > Projectile.Bottom.Y;

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            float distanceToPlayerX = Math.Abs(Owner.Center.X - Projectile.Center.X);
            float distanceToPlayerY = Math.Abs(Owner.Center.Y - Projectile.Center.Y);

            Vector2 idealPosition = Owner.Center - Vector2.UnitX * Owner.Directions * (25 + Owner.width + 25 * Projectile.minionPos);
            float distanceToIdealPos = Math.Abs(Projectile.Center.X - idealPosition.X);

            //Vanilla uses AI_067_CustomEliminationCheck_Pirates as the elimination check, but it always returns true. How weird
            int target = -1;
            int maxTargetRange = 800;
            Projectile.Minion_FindTargetInRange(maxTargetRange, ref target, true);
            NPC targetNPC = target != -1 ? Main.npc[target] : null;

            // Edge case mostly for when its spawned
            if (FablesUtils.FullSolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                CurrentAIState = AIState.CatchupFlight;
                return;
            }

            // If not targeting anything, its allowed to go into flight mode to catch up
            if (targetNPC is null)
            {
                // Teleport to the player if way too far
                if (distanceToPlayer > 2000)
                    Projectile.Center = Owner.Center;
                // Catch up to the player whle flying if too far
                else if (distanceToPlayer > 500 || distanceToPlayerY > 300 || Owner.rocketDelay2 > 0)
                {
                    CurrentAIState = AIState.CatchupFlight;
                    Projectile.netUpdate = true;

                    // Abruptly change velocity to go to the player
                    if (Math.Sign(Projectile.velocity.Y) != Math.Sign((Owner.Center - Projectile.Center).Y))
                        Projectile.velocity.Y = 0;
                }
            }
            // Target and attack enemies
            else
            {
                idealPosition = targetNPC.Center;
                Projectile.shouldFallThrough = targetNPC.Center.Y > Projectile.Bottom.Y;

                if (Projectile.IsInRangeOfMeOrMyOwner(targetNPC, maxTargetRange, out var _, out var _, out var _))
                {
                    if (Vector2.Distance(Projectile.Center, targetNPC.Center) < 50)
                    {
                        Projectile.velocity = Projectile.velocity.ClampMagnitude(10);
                        Projectile.netUpdate = true;
                    }

                    // Bounce on the enemies if they're in the air
                    Rectangle shortenedHitbox = targetNPC.Hitbox;
                    shortenedHitbox.Inflate(0, -8);
                    if (Projectile.Hitbox.Intersects(shortenedHitbox) && Projectile.velocity.Y >= 0f)
                    {
                        Projectile.velocity.Y = -10;
                        Projectile.velocity.X += Math.Max(Math.Abs(Projectile.velocity.X * 0.2f), 5) * Projectile.velocity.X.NonZeroSign();

                        targetNPC.velocity.Y += 4f * targetNPC.knockBackResist;
                    }
                }
                idealPosition.X += 20 * (targetNPC.Center.X - Projectile.Center.X).NonZeroSign();
            }

            // Bounce
            bool canWaterBounce = !Projectile.shouldFallThrough && Projectile.velocity.Y >= 0 && Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height + 10);
            if (Projectile.velocity.Y == 0 || canWaterBounce)
            {
                float bounceStrength = MathF.Sin(Projectile.minionPos * 0.3f) + 6;
                bounceStrength += Utils.GetLerpValue(100, 300, distanceToIdealPos, true) * 6;

                // Check ahead for obstacles and increase the jump strenght if so
                if (bounceStrength < 11)
                {
                    // Jump over obstacles
                    Point tilePosition = Projectile.Center.ToTileCoordinates();
                    tilePosition.X += Projectile.spriteDirection * 2;
                    tilePosition.X += (int)Projectile.velocity.X;

                    tilePosition.Y -= (int)(Utils.GetLerpValue(4, 7, bounceStrength, true) * 2.9f);

                    for (int i = 0; i < 4; i++)
                    {
                        if (!WorldGen.InWorld(tilePosition.X, tilePosition.Y) || bounceStrength > 11)
                            break;

                        Tile t = Main.tile[tilePosition];
                        if (WorldGen.SolidTile(t))
                            bounceStrength += 2.5f;

                        tilePosition.Y--;
                    }
                }

                // Bounce higher to reach the target
                if (targetNPC is not null)
                {
                    float enemyBounceStrength = (float)Math.Sqrt(Math.Abs(Projectile.Center.Y - targetNPC.Center.Y) * 2f * 0.52f);
                    if (enemyBounceStrength > 25)
                        enemyBounceStrength = 25;
                    bounceStrength = Math.Max(enemyBounceStrength, bounceStrength);
                }
                // Stop bouncing when the ideal position has been reached
                else if (Owner.velocity.X == 0 && Projectile.velocity.X == 0)
                    bounceStrength = 0;

                Projectile.velocity.Y = -bounceStrength;
            }

            // Go towards the target
            if (Math.Abs(Projectile.Center.X - idealPosition.X) > 10)
            {
                int direction = (idealPosition.X - Projectile.Center.X).NonZeroSign();
                float acceleration = 0.04f;

                if (Projectile.velocity.X.NonZeroSign() != direction)
                    acceleration *= 2f;

                float speed = Math.Max(Math.Abs(Owner.velocity.X), 6.5f);
                if (targetNPC is not null)
                    speed = 12;

                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, speed * direction, acceleration);
            }
            // Slow down rapidly and stop
            else
            {
                Projectile.velocity.X *= 0.94f;
                if (Math.Abs(Projectile.velocity.X) < 0.1f)
                    Projectile.velocity.X = 0;
            }

            // Gravity
            if (Projectile.velocity.Y < 10)
                Projectile.velocity.Y += 0.52f;

            Projectile.rotation = Math.Clamp(Projectile.velocity.X * 0.04f * Utils.GetLerpValue(0, 5f, Projectile.velocity.Y), -0.3f, 0.3f);
        }
        #endregion

        public override void OnKill(int timeLeft)
        {
            // Spawn droplets if it dies by hitting enemies
            if (Projectile.penetrate <= 0)
            {
                for (int i = 0; i < Main.rand.Next(4, 6); i++)
                {
                    Vector2 particleVelocity = Vector2.UnitY.RotateRandom(MathHelper.PiOver2) * -Main.rand.NextFloat(3.5f, 7);
                    SludgeSlapProjectile.SpawnDroplet(Projectile.Center, particleVelocity, Main.rand.NextFloat(1, 1.2f), Main.rand.Next(20, 30));
                }
            }
        }

        public override bool MinionContactDamage() => CurrentAIState != AIState.Slimeball;

        public void ManageFlyingFrames()
        {
            // Cycles projectile frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame >= 4)
                Projectile.frame = 0;

            // Cycles trail frames
            GoopTrailFrameCounter++;
            if (GoopTrailFrameCounter > 6)
            {
                GoopTrailFrameCounter = 0;
                GoopTrailFrame++;
            }
            if (GoopTrailFrame >= 6)
                GoopTrailFrame = 0;
        }

        public void ManageFightingFrames()
        {
            // Cycles projectile frames while not moving
            if (Projectile.velocity.X == 0)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 12)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
                if (Projectile.frame >= 2)
                    Projectile.frame = 0;
            }
            // Otherwise, using frame 1 all the time unless Y velocity is negative (going up)
            else
            {
                Projectile.frame = 0;
                if (Projectile.velocity.Y < 0)
                    Projectile.frame = 1;              
            }
        }

        private void ModifyProjectileDye(Projectile projectile, ref int dyeID)
        {
            // Prevent slimelings from being dyed
            if (projectile.type == Type)
                dyeID = 0;
        }

        #region Prims
        public void ManageTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 position = Projectile.Center + Projectile.velocity;

            Cache ??= [];

            Cache.Add(position);
            while (Cache.Count > 10)
                Cache.RemoveAt(0);

            // Fade out length while fighting
            if (CurrentAIState == AIState.Fighting)
            {
                int removed = 0;
                while (Cache.Count > 0 && removed < 2)
                {
                    Cache.RemoveAt(0);
                    removed++;
                }
            }

            Trail ??= new PrimitiveTrail(20, WidthFunction, ColorFunction);
            Trail.SetPositionsSmart(Cache, position, FablesUtils.SetLengthRetrievalFunction(64));
        }

        private float WidthFunction(float factorAlongTrail) => (TrailLayer == 0 ? 11 : 9) * Projectile.scale;

        private Color ColorFunction(float factorAlongTrail)
        {
            Vector2 position = Cache[(int)(factorAlongTrail * (Cache.Count - 1))];
            Color lighting = Lighting.GetColor(position.ToTileCoordinates());

            // Change colors depending on context
            Color color = TrailLayer switch
            {
                0 => new Color(15, 38, 87),
                1 => new Color(20, 62, 148),
                _ => new Color(36, 85, 186)
            };
            return color.MultiplyRGB(lighting) * SludgeSlap.SLIME_OPACITY;
        }
        #endregion

        #region Visuals
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(2, Main.projFrames[Type], CurrentAIState != AIState.Fighting ? 1 : 0, Projectile.frame);
            Vector2 origin = (frame.Size() - new Vector2(2, 2)) / 2;

            Vector2 drawPosition = Projectile.Center;
            if (CurrentAIState == AIState.Fighting)
                drawPosition.Y += 2;

            Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition, frame, lightColor * SludgeSlap.SLIME_OPACITY, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (Trail is null || Cache.Count < 2)
                return;

            GoopTexture ??= ModContent.Request<Texture2D>(AssetDirectory.EarlyGameMisc + "GoopTextureMap");
            Rectangle goopFrame = GoopTexture.Frame(1, 6, 0, GoopTrailFrame);

            Effect effect = AssetDirectory.PrimShaders.StretchedTextureMap;
            effect.Parameters["frame"].SetValue(new Vector4(goopFrame.X, goopFrame.Y, goopFrame.Width, goopFrame.Height));
            effect.Parameters["textureResolution"].SetValue(GoopTexture.Size());
            effect.Parameters["sampleTexture"].SetValue(GoopTexture.Value);
            effect.Parameters["stretch"].SetValue(1f);

            // Draw larger outline trail
            TrailLayer = 0;
            Trail.Render(effect, -Main.screenPosition);

            // Draw lighter inner trail
            TrailLayer = 1;
            effect.Parameters["stretch"].SetValue(0.9f);
            Trail.Render(effect, -Main.screenPosition);

            // Final brightest and shortest layer
            TrailLayer = 2;
            effect.Parameters["stretch"].SetValue(0.5f);
            Trail.Render(effect, -Main.screenPosition);
        }
        #endregion
    }

    #region Particles
    class SlapHandParticle : Particle
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "SlapHand";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        public int Frame = 0;
        public float Opacity;
        public Color Highlight;
        public bool Flipped;

        public SlapHandParticle(Vector2 position, Color color, Color highlight, float? rotation = null, float opacity = 1, float scale = 1, int? lifeTime = null)
        {
            Position = position;
            Color = color;
            Highlight = highlight;
            Rotation = rotation is null ? Main.rand.NextFloat(-(float)Math.PI / 8f, (float)Math.PI / 8f) : rotation.Value;
            Opacity = opacity;
            Scale = scale;
            Lifetime = lifeTime is null ? Main.rand.Next(10, 15) : lifeTime.Value;

            Flipped = Main.rand.NextBool();
        }

        public override void Update()
        {
            Frame = (int)(LifetimeCompletion * 3);
            Lighting.AddLight(Position, Color.ToVector3() * 0.8f * (float)Math.Pow(1 - LifetimeCompletion, 0.5f));
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D dustTexture = ParticleTexture;

            Rectangle dustFrame = dustTexture.Frame(2, 3, 0, Frame);
            Vector2 position = Position - basePosition;

            float drawScale = Scale;
            float drawRotation = Rotation;
            drawScale += 1f * MathF.Pow(Utils.GetLerpValue(8, 0, Time, true), 6f);
            drawRotation += 0.5f * MathF.Pow(Utils.GetLerpValue(7, 0, Time, true), 5f) * (Flipped ? -1 : 1);

            SpriteEffects drawEffect = Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Draw main layer
            spriteBatch.Draw(dustTexture, position, dustFrame, Color * Opacity, drawRotation, dustFrame.Size() / 2f, drawScale, drawEffect, 0);

            // Draw highlight infront main layer
            spriteBatch.Draw(dustTexture, position, dustFrame with { X = 50 }, Highlight * Opacity, drawRotation, dustFrame.Size() / 2f, drawScale, drawEffect, 0);


            return;

            Texture2D lensFlare = AssetDirectory.CommonTextures.BloomStreak.Value;

            // Draw lensflare
            int flareLifetime = Lifetime / 2;
            if (Time <= flareLifetime)
            {
                float flareCompletion = Time / (float)flareLifetime;

                Vector2 flareScale = new Vector2(1 - flareCompletion, 1) * 1.3f;
                flareScale *= 1 - (float)Math.Pow((flareCompletion - 0.7f) / 0.875f, 2);
                for (int i = 0; i < 2; i++)
                {
                    Color flareColor = i == 0 ? Color : Highlight * MathF.Pow(1 - flareCompletion, 0.3f);
                    flareScale *= (1 - i * 0.44f);

                    spriteBatch.Draw(lensFlare, position, null, flareColor with { A = 0 }, Rotation - MathHelper.PiOver4, lensFlare.Size() / 2f, flareScale * Scale, SpriteEffects.None, 0);
                    spriteBatch.Draw(lensFlare, position, null, flareColor with { A = 0 }, Rotation + MathHelper.PiOver4, lensFlare.Size() / 2f, flareScale * Scale, SpriteEffects.None, 0);
                }
            }
        }
    }

    public class GelKingCrownParticle : Particle
    {
        public override string Texture => AssetDirectory.EarlyGameMisc + "GelKingCrown";

        public override bool SetLifetime => true;

        public GelKingCrownParticle(Vector2 position, Vector2 velocity, int lifetime = 60)
        {
            Position = position;
            Scale = 1;
            Color = Color.White;
            Velocity = velocity;
            Rotation = velocity.ToRotation();
            Lifetime = lifetime;
        }

        public override void Update()
        {
            //Bounce back and spin
            Rotation += Velocity.X * 0.1f;
            Velocity.Y += 0.3f;
            Velocity.X *= 0.98f;

            float opacity = Math.Clamp((1 - LifetimeCompletion) * 2, 0, 1);
            Color = Color.White.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates())) * opacity;
        }
    }

    #endregion
}