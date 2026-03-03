using CalamityFables.Content.NPCs.Desert;
using Terraria.DataStructures;
using Terraria.Localization;

namespace CalamityFables.Content.Boss.DesertWormBoss
{
    public class AntlionChunk : ModProjectile, ICustomDeathMessages
    {
        public override string Texture => AssetDirectory.DesertScourge + "AntlionChunks";
        public int Variant => (int)Projectile.ai[0];
        public virtual int VariantCount => 8;

        internal PrimitiveTrail TrailDrawer;
        private List<Vector2> cache;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Antlion Chunk");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {

            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.velocity.Y += 0.2f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16;

            if (!Main.dedServ)
            {
                DoDust();
                ManageCache();
                ManageTrail();
            }
        }

        public virtual void DoDust()
        {
            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextBool() ? 256 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.velocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 2f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            if (ChildSafety.Disabled && Main.rand.NextBool(20))
            {
                int goreType = Main.rand.Next(1094, 1104);
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Projectile.velocity * 0.4f, goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 200);
                gore.scale *= 0.5f;
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
                    cache.Add(Projectile.Center + Projectile.velocity);
                }
            }

            cache.Add(Projectile.Center + Projectile.velocity);

            while (cache.Count > 16)
                cache.RemoveAt(0);
        }

        public virtual void ManageTrail()
        {
            TrailDrawer = TrailDrawer ?? new PrimitiveTrail(30, WidthFunction, ColorFunction);
            TrailDrawer.SetPositionsSmart(cache, Projectile.Center + Projectile.velocity, FablesUtils.RigidPointRetreivalFunction);
            TrailDrawer.NextPosition = Projectile.Center + Projectile.velocity * 1.05f;
        }

        public virtual float WidthFunction(float completion)
        {
            return 11f * (float)Math.Pow(completion, 0.6f) * (0.85f + 0.15f * Utils.GetLerpValue(12f, 5f, Projectile.velocity.Length(), true));
        }

        public virtual Color ColorFunction(float completion)
        {
            return Color.Lerp(Color.Olive, Color.DarkGoldenrod, (float)Math.Pow(completion, 3f)) * completion;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            if (ChildSafety.Disabled)
            {
                for (int i = 0; i < 2; i++)
                {
                    int goreType = Main.rand.Next(1094, 1104);
                    Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 4f, goreType);
                    gore.timeLeft = 2;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Dust dusty = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextBool() ? 256 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(1.2f, 2.5f);
                dusty.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 4f;
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            SoundEngine.PlaySound(DesertScourge.PreyBelchDebrisImpactSound, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect effect = Scene["GlorpTrail"].GetShader().Shader;
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 4.2f + Projectile.whoAmI * 0.1f);
            effect.Parameters["repeats"].SetValue(3f);
            effect.Parameters["voronoi"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "Voronoi").Value);
            effect.Parameters["noiseOverlay"].SetValue(ModContent.Request<Texture2D>(AssetDirectory.Noise + "PrettyManifoldNoise").Value);
            TrailDrawer?.Render(effect, -Main.screenPosition);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, VariantCount, 0, Variant, 0, -2);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);
            return false;
        }

        public bool CustomDeathMessage(Player player, ref PlayerDeathReason customDeath)
        {
            if (!Main.rand.NextBool(2))
                return false;

            customDeath.CustomReason = Language.GetText("Mods.CalamityFables.Extras.DeathMessages.DesertScourgeAntlionSpit").ToNetworkText(player.name);
            return true;
        }
    }

    public class StormlionChunk : AntlionChunk
    {
        public override string Texture => AssetDirectory.DesertScourge + "StormlionChunks";
        public override int VariantCount => 4;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Stormlion Chunk");
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }


        public override void ManageTrail()
        {
            base.ManageTrail();
        }

        public override float WidthFunction(float completion)
        {
            return 16f * (float)Math.Pow(completion, 0.6f);
        }

        public override Color ColorFunction(float completion)
        {
            Color baseColor = base.ColorFunction(completion);
            byte baseAlpha = baseColor.A;
            baseColor = Color.Lerp(baseColor, Color.MediumTurquoise, 0.12f + 0.4f * (float)Math.Pow(completion, 4f));

            return baseColor with { A = baseAlpha };
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && !player.ghost && player.Top.Y <= Projectile.Center.Y && player.Distance(Projectile.Center) < 500)
                {
                    fallThrough = false;
                    break;
                }
            }
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override void DoDust()
        {
            if (Main.rand.NextBool(7))
            {
                Dust dusty = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextBool() ? 256 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(0.8f, 1.5f);
                dusty.velocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 2f);
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dusty = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 226, 0, 0, 100);
                dusty.position += Main.rand.NextVector2Circular(10f, 10f);
                dusty.scale = Main.rand.NextFloat(0.4f, 1f);
                dusty.velocity = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 2f);
                dusty.noLight = true;
                dusty.noGravity = true;
            }


            if (ChildSafety.Disabled && Main.rand.NextBool(22))
            {
                int goreType = Mod.Find<ModGore>("DigestedStormlionGore" + (Main.rand.Next(5) + 1).ToString()).Type;
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Projectile.velocity * 0.4f, goreType);
                gore.timeLeft = 2;
                gore.alpha = Main.rand.Next(80, 200);
                gore.scale *= 0.5f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DesertScourgeElectroblast>(), DesertScourge.PreyBelch_StormlionExplosionDamage / 2, 10, Main.myPlayer, ai2: Stormlion.DEATH_BLAST_RADIUS);
            }

            if (ChildSafety.Disabled)
            {
                for (int i = 0; i < 2; i++)
                {
                    int goreType = Mod.Find<ModGore>("DigestedStormlionGore" + (Main.rand.Next(5) + 1).ToString()).Type;
                    Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 4f, goreType);
                    gore.timeLeft = 2;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Dust dusty = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Main.rand.NextBool() ? 256 : 4, 0, 0, 100);
                dusty.scale = Main.rand.NextFloat(1.2f, 2.5f);
                dusty.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 4f;
                dusty.noLight = true;
                dusty.noGravity = !Main.rand.NextBool(6);

                if (dusty.type == 4)
                    dusty.color = new Color(80, 170, 40, 120);
            }

            SoundEngine.PlaySound(DesertScourge.PreyBelchStormDebrisImpactSound, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            Rectangle frame = texture.Frame(1, VariantCount, 0, Variant, 0, -2);
            Color color = Color.Lerp(Color.Turquoise, Color.White, 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + 0.1f * Projectile.whoAmI));

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color, Projectile.rotation, frame.Size() / 2f, Projectile.scale, 0, 0);

            return base.PreDraw(ref lightColor);
        }
    }
}
