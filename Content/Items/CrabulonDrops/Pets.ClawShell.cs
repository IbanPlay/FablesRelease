using CalamityFables.Particles;
using Terraria.WorldBuilding;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class ClawShell : ModItem
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Claw Shell");
            Tooltip.SetDefault("Summons a haunted mudcrab shell\n" +
                "'Stuffed with fungal goo!'");
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.WispinaBottle);

            Item.shoot = ModContent.ProjectileType<CrabSpirit>();
            Item.buffType = ModContent.BuffType<CrabSpiritBuff>();

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

    public class CrabSpiritBuff : ModBuff
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.lightPet[Type] = true;
            DisplayName.SetDefault("Mudcrab Spirit");
            Description.SetDefault("Only supereffective moves will hit this shell");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            bool _ = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref _, ModContent.ProjectileType<CrabSpirit>());
        }
    }

    public class CrabSpirit : ModProjectile, IDrawPixelated
    {
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public DrawhookLayer layer
        {
            get
            {
                if (Owner.isLockedToATile)
                    return DrawhookLayer.BehindTiles;
                return DrawhookLayer.AboveNPCs;
            }
        }

        public Asset<Texture2D> BloomTexture;

        public VerletNet rags; 
        public Asset<Texture2D> RagsEndPoints;
        public Asset<Texture2D> RagRopes;
        public ref float RagsSine => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crab Spirit");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.LightPet[Type] = true;

            ProjectileID.Sets.CharacterPreviewAnimations[Type] =
                ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type], 5);
        }

        public override bool? CanCutTiles() => false;
        public override bool? CanDamage() => false;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Wisp);
            Projectile.aiStyle = -1;
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.light = 0f;
        }

        public override void AI()
        {
            if (!Owner.dead && Owner.HasBuff<CrabSpiritBuff>())
                Projectile.timeLeft = 2;

            float distanceToPlayer = Projectile.Distance(Owner.Center);
            //Warp
            if (distanceToPlayer > 2000)
            {
                Projectile.Center = Owner.Center;
                rags = null;
                distanceToPlayer = 0f;
            }

            //TP the rags
            if (rags != null && !rags.extremities[0].position.WithinRange(Owner.Center, 350f))
            {
                rags = null;
            }

            Vector2 desiredPosition = Owner.Center + Vector2.UnitY * Owner.gfxOffY;

            if (Owner.controlLeft)
                desiredPosition.X -= 100f;
            if (Owner.controlRight)
                desiredPosition.X += 100f;

            if (Owner.controlDown)
                desiredPosition.Y += 100f;
            else
            {
                if (Owner.controlUp)
                    desiredPosition.Y -= 160f;
                else
                    desiredPosition.Y -= 64f;
            }
            desiredPosition.Y += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 5f;

            desiredPosition = (desiredPosition - Owner.Center).ClampMagnitude(100f) + Owner.Center;

            float distanceToDesiredPosition = Projectile.Distance(desiredPosition);

            Vector2 goalVelocity = (desiredPosition - Projectile.Center) * (0.03f + 0.075f * Utils.GetLerpValue(20f, 90f, distanceToDesiredPosition, true));

            float approachAcceleration = 0.1f + MathF.Pow(Utils.GetLerpValue(70, 0, distanceToPlayer, true), 2f) * 0.3f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
            Projectile.velocity *= 0.98f;

            Projectile.rotation = Projectile.velocity.X * 0.1f;


            //Just striaght up magnet to the ideal position if we're close enough
            if (Projectile.WithinRange(desiredPosition, 1f))
            {
                Projectile.Center = desiredPosition;
                Projectile.velocity = Vector2.Zero;
            }

            //Animation
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame >= 4)
                Projectile.frame = 0;

            if (rags == null && !Main.dedServ)
                InitializeRags();
            if (rags != null)
                SimulateRags();

            Lighting.AddLight(Projectile.Center, 0.25f, 0.25f, 0.8f);
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTex = TextureAssets.Projectile[Type].Value;
            BloomTexture ??= ModContent.Request<Texture2D>(Texture + "Bloom");

            Rectangle frame = projectileTex.Frame(1, 4, 0, Projectile.frame, 0, -2);

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation;
            Vector2 squish = Vector2.One * Projectile.scale;

            Main.EntitySpriteDraw(projectileTex, drawPosition, frame, lightColor, drawRotation, frame.Size() * 0.5f, squish, 0, 0);
            Main.EntitySpriteDraw(BloomTexture.Value, drawPosition, frame, lightColor with { A = 0 }, drawRotation, frame.Size() * 0.5f, squish, 0, 0);


            return false;
        }

        public void InitializeRags()
        {
            rags = new VerletNet();

            VerletPoint start = new VerletPoint(Owner.Center - Vector2.UnitY * 12f * Owner.gravDir, true);
            VerletPoint end = new VerletPoint(Owner.Center - Vector2.UnitX * Owner.direction * 36 - Vector2.UnitY * 12f * Owner.gravDir);
            rags.AddChain(start, end, 14, RagWidth, RagColor, 2f);

            start = new VerletPoint(Owner.Center - Vector2.UnitX * Owner.direction * 6, true);
            end = new VerletPoint(Owner.Center - Vector2.UnitX * Owner.direction * (72 + 6));
            rags.AddChain(start, end, 14, RagWidth, RagColor, 2f);

            start = new VerletPoint(Owner.Center + Vector2.UnitY * 4f * Owner.gravDir, true);
            end = new VerletPoint(Owner.Center + Vector2.UnitY * 4f * Owner.gravDir - Vector2.UnitX * Owner.direction * 52);
            rags.AddChain(start, end, 14, RagWidth, RagColor, 2f);
        }

        public void SimulateRags()
        {
            rags.extremities[0].position = Owner.Center - Vector2.UnitY * 12f * Owner.gravDir;
            rags.extremities[2].position = Owner.Center + Vector2.UnitX * Owner.direction * 6;
            rags.extremities[4].position = Owner.Center + Vector2.UnitY * 4f * Owner.gravDir;

            bool needsFlip = (Owner.Center.X - rags.extremities[3].position.X).NonZeroSign() != Owner.direction;
            bool falling = Owner.velocity.Y > 5;

            int indexAlongTrail = 0;
            int ragIndex = 0;

            foreach (VerletPoint point in rags.points)
            {
                float progressAlongTrail = indexAlongTrail / 14f;

                //A directional push towards the player's back. More or less strong depending on how fast the player is moving
                Vector2 customGravity = -Vector2.UnitX * Owner.direction * ((needsFlip ? 1.4f : 0.3f));

                switch (ragIndex)
                {
                    case 0:
                        customGravity.Y -= 0.2f;
                        customGravity.Y += 1.5f * (float)Math.Sin(indexAlongTrail * 5.6f + RagsSine * 0.08f) * (float)Math.Sin(progressAlongTrail * 2.4f);

                        break;

                    case 1:
                        customGravity.Y += 0.9f * (float)Math.Sin(progressAlongTrail * 9.6f - RagsSine * 0.10f) * (0.2f + 0.8f * (float)Math.Sin(progressAlongTrail * 2.4f));
                        break;

                    case 2:
                        customGravity.Y += (float)Math.Sin(progressAlongTrail * MathHelper.Pi * 1.2f) * 0.3f + 0.1f * progressAlongTrail;
                        customGravity.Y += 0.4f * (float)Math.Sin(progressAlongTrail * 5.6f - RagsSine * 0.12f) ;
                        break;
                }

                //Make the trail sine laterally
                if (falling)
                {
                    Vector2 fallingGravity = new Vector2(0f, customGravity.Y * 0.3f + Owner.velocity.Y * 0.1f);

                    fallingGravity.X = (float)Math.Sin(indexAlongTrail * 0.8f + RagsSine * 0.03f + indexAlongTrail * 1.4f) * 5f * MathF.Sin(indexAlongTrail / 9f * MathHelper.Pi);

                    customGravity = Vector2.Lerp(customGravity, fallingGravity, Utils.GetLerpValue(5, 10, Owner.velocity.Y, true));
                }

                point.customGravity = customGravity;




                indexAlongTrail++;
                if (indexAlongTrail == 15)
                {
                    ragIndex++;
                    indexAlongTrail = 0;
                }
            }

            int iterations = 4 + (int)(Utils.GetLerpValue(5f, 10f, Math.Abs(Owner.velocity.X), true) * 10f);

            rags.Update(iterations, 0f, 0.7f);
            RagsSine++;
        }


        public void SetRagFrame(int i, ref Rectangle frame)
        {
            if (i == 0)
                frame = new Rectangle(0, 0, 46, 4);

            else if (i == 1)
                frame = new Rectangle(0, 6, 64, 4);
            else
                frame = new Rectangle(0, 12, 62, 4);
        }


        public float RagWidth(float progress) => 2f;
        public Color RagColor(float progress) => Color.White;


        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            if (rags == null)
                return;

            RagRopes ??= ModContent.Request<Texture2D>(Texture + "Tendrils");
            RagsEndPoints ??= ModContent.Request<Texture2D>(Texture + "TendrilsEnd");

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

            rags.RenderFramed(RagRopes.Value, -Main.screenPosition, SetRagFrame);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

            Vector2 drawPos = (rags.points[28] - Main.screenPosition) / 2f;
            Rectangle frame = RagsEndPoints.Frame(1, 3, 0, 2, 0, -2);
            Main.spriteBatch.Draw(RagsEndPoints.Value, drawPos, frame, Color.White, 0f, frame.Size() * 0.5f, 0.5f, 0, 0);

            for (int i = 1; i < 6; i+=2)
            {
                drawPos = (rags.extremities[i] - Main.screenPosition) / 2f;
                frame = RagsEndPoints.Frame(1, 3, 0, (i - 1) / 2, 0, -2);
                Main.spriteBatch.Draw(RagsEndPoints.Value, drawPos, frame, Color.White, 0f, frame.Size() * 0.5f, 0.5f, 0, 0);
            }

        }
    }
}
