using CalamityFables.Content.Items.BurntDesert;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    public class ScourgeSpineDecor : ModTileEntity, IDrawBehindTiles
    {
        public string TexturePath => AssetDirectory.BurntDesert + Name;

        public static Asset<Texture2D> Texture;
        public List<Vector2> ControlPoints = new();

        public bool PlayerPlaced = false;

        //Cached stuff
        public Rectangle? bounds;
        public int segmentCount = -1;
        public Vector2[] spineDrawPositions;
        public int[] spineRandomVariants;
        public float spineLength;
        public int randomSeed;
        public bool drawFlipped;
        public byte salty;

        public bool isPlacementPreview = false;
        public Color placementPreviewDrawColor;

        public BezierCurve spineCurve => new BezierCurve(ControlPoints.ToArray());

        public bool hasHead;
        public bool hasTail;

        public Vector2 WorldPosition => Position.ToVector2() * 16;

        public Vector2 SpineNormal {
            get {
                Vector2 worldStart = WorldPosition;
                Vector2 worldEnd = ControlPoints[^1];

                int towardsTheSky = worldEnd.X - worldStart.X > 0 ? -1 : 1;
                Vector2 tangent = towardsTheSky == -1 ? worldStart.DirectionTo(worldEnd) : worldEnd.DirectionTo(worldStart);

                tangent = new Vector2(-tangent.Y, tangent.X);

                return tangent;

            }
        }

        #region Drawing
        public void UpdateBounds()
        {
            if (ControlPoints.Count == 0)
                return;

            Vector2 topLeft = Position.ToWorldCoordinates();
            Vector2 bottomRight = Position.ToWorldCoordinates();

            for (int i = 0; i < ControlPoints.Count; i++)
            {
                Vector2 positionToCheck = ControlPoints[i];

                topLeft.X = Math.Min(topLeft.X, positionToCheck.X);
                topLeft.Y = Math.Min(topLeft.Y, positionToCheck.Y);

                bottomRight.X = Math.Max(bottomRight.X, positionToCheck.X);
                bottomRight.Y = Math.Max(bottomRight.Y, positionToCheck.Y);
            }

            topLeft -= Vector2.One * 48;
            bottomRight += Vector2.One * 48;

            if (hasHead)
            {
                topLeft -= Vector2.One * 160f;
                bottomRight += Vector2.One * 160f;
            }
            else if (hasTail)
            {
                topLeft -= Vector2.One * 60f;
                bottomRight += Vector2.One * 60f;
            }

            bounds = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
        }

        public void SetControlPoints(Point endPoint, int curvatureOffset)
        {
            Vector2 worldStart = Position.ToWorldCoordinates();
            Vector2 worldEnd = endPoint.ToWorldCoordinates();
            int towardsTheSky = worldEnd.X - worldStart.X > 0 ? -1 : 1;

            Vector2 tangent = towardsTheSky == 1 ? worldStart.DirectionTo(worldEnd) : worldEnd.DirectionTo(worldStart);
            Vector2 spineNormal = new Vector2(-tangent.Y, tangent.X);
            float dist = worldStart.Distance(worldEnd);
            float spineVerticalSquish = 1 - curvatureOffset.ModulusPositive(7) * 0.05f; //How squished can the spine get alongside its normal

            ControlPoints.Clear();
            ControlPoints.Add(worldStart);

            if (!hasHead && !hasTail)
            {
                //Approximate a semicircle with bezier
                dist *= spineVerticalSquish;
                float inwardBezierDist = dist * 0.3f * (Player.FlexibleWandCycleOffset.ModulusPositive(7) / 6f) * towardsTheSky;
                ControlPoints.Add(worldStart + spineNormal * dist * 0.666f + tangent * inwardBezierDist);
                ControlPoints.Add(worldEnd + spineNormal * dist * 0.666f - tangent * inwardBezierDist);
               
            }
            else
            {
                float spineNormalOutwards = 0.666f * dist;
                spineNormalOutwards *= Utils.GetLerpValue(0, 4, Math.Abs(endPoint.X - (float)Position.X), true) * 0.8f + 0.2f;

                //Approximate a semicircle with bezier
                if (Player.FlexibleWandCycleOffset.ModulusPositive(10) < 5)
                {
                    spineNormalOutwards *= (1 - Player.FlexibleWandCycleOffset.ModulusPositive(5) / 4f) * 0.7f + 0.3f;
                    ControlPoints.Add(worldStart + spineNormal * spineNormalOutwards + tangent * dist * towardsTheSky * 0.04f);
                    ControlPoints.Add(worldEnd + spineNormal * spineNormalOutwards - tangent * dist * towardsTheSky * 0.04f);
                }
                else
                {
                    //Approximate a double semicircle
                    float bendPercent = Player.FlexibleWandCycleOffset.ModulusPositive(5) / 4f;

                    Vector2 middle = Vector2.Lerp(worldStart, worldEnd, 0.5f + bendPercent * 0.4f);
                    float segment1dist = worldStart.Distance(middle);
                    float segment2dist = middle.Distance(worldEnd);
                    spineNormalOutwards = segment1dist * 0.6666f;

                    ControlPoints.Add(worldStart + spineNormal * spineNormalOutwards - tangent * towardsTheSky * segment1dist * 0.05f);
                    ControlPoints.Add(middle + spineNormal * spineNormalOutwards + tangent * towardsTheSky * segment1dist * 0.05f);

                    ControlPoints.Add(middle);

                    spineNormalOutwards = segment2dist * 0.6666f;
                    ControlPoints.Add(middle - spineNormal * spineNormalOutwards - tangent * towardsTheSky * segment2dist * 0.05f);
                    ControlPoints.Add(worldEnd - spineNormal * spineNormalOutwards + tangent * towardsTheSky * segment2dist * 0.05f);
                }
            }

            ControlPoints.Add(worldEnd);
        }

        public void CalculateSegmentPositions()
        {
            int lenghtCalculationPrecision = 30;
            BezierCurve curve = spineCurve;

            //Get an approximation of the curve's total lenght by sampling multiple points and then calculating the distance between them
            Vector2[] calculatePositions = curve.GetPoints(lenghtCalculationPrecision).ToArray();

            spineLength = 0f;

            for (int i = 1; i < lenghtCalculationPrecision; i++)
                spineLength += (calculatePositions[i] - calculatePositions[i - 1]).Length();

            //Calculate how many segments we can fit in
            segmentCount = (int)Math.Round(spineLength / 28f);

            //Get as many positions as we need
            spineDrawPositions = curve.GetEvenlySpacedPoints(segmentCount).ToArray();

            spineRandomVariants = new int[segmentCount];
            UnifiedRandom spineRandom = new UnifiedRandom(randomSeed);

            drawFlipped = spineRandom.NextBool();
            //Get random values for segment variants
            for (int i = 0; i < segmentCount; i++)
                spineRandomVariants[i] = spineRandom.Next(4);
        }

        public bool IsOnScreen()
        {
            if (!bounds.HasValue)
                UpdateBounds();
            if (!bounds.HasValue)
                return false;

            Rectangle rect = bounds.Value;
            Rectangle drawRect = new Rectangle(rect.X - (int)Main.screenPosition.X, rect.Y - (int)Main.screenPosition.Y, rect.Width, rect.Height);
            return FablesUtils.OnScreen(drawRect);
        }

        public void DrawBehindTiles(SpriteBatch spriteBatch)
        {
            if (Texture == null)
                Texture = ModContent.Request<Texture2D>(TexturePath);
            Texture2D tex = Texture.Value;

            if (spineLength < 0 || spineDrawPositions == null)
                CalculateSegmentPositions();

            if (segmentCount <= 1)
                return;

            int segmentsToDraw = (hasHead || hasTail) ? segmentCount : segmentCount - 1;

            if (!hasTail)
            {
                for (int i = 0; i < segmentsToDraw; i++)
                    DrawSegment(tex, spriteBatch, i, segmentsToDraw);
            }
            else
                for (int i = segmentsToDraw - 1; i > 0; i--)
                    DrawSegment(tex, spriteBatch, i, segmentsToDraw);
        }

        private void DrawSegment(Texture2D tex, SpriteBatch spriteBatch, int i, int segmentsToDraw)
        {
            int segmentVariant = spineRandomVariants[i];
            int spineprogress = ((i % 3) + 1);
            //Special frame for the last segment before the head
            if (hasHead && i == segmentsToDraw - 2)
                spineprogress = 0;

            Rectangle frame = new Rectangle(76 * Math.Max(0, segmentVariant - 1), 58 * spineprogress, 74, 58);

            Vector2 position = spineDrawPositions[i];
            float rotation;

            if (i == segmentCount - 1)
                rotation = (spineDrawPositions[i] - spineDrawPositions[i - 1]).ToRotation() + MathHelper.PiOver2;
            else
                rotation = (spineDrawPositions[i + 1] - spineDrawPositions[i]).ToRotation() + MathHelper.PiOver2;

            Color drawColor;
            if (!isPlacementPreview)
                drawColor = Lighting.GetColor((int)position.X / 16, (int)position.Y / 16); //Lighting of the position of the chain segment
            else
                drawColor = placementPreviewDrawColor;

            Vector2 origin = new Vector2(37, 36);

            if (hasHead && i == segmentCount - 1)
            {
                if (segmentCount > 1)
                    position = spineDrawPositions[i - 1] + (rotation - MathHelper.PiOver2).ToRotationVector2() * 30f;
                frame = new Rectangle(228, 124 * segmentVariant, 118, 122);
                origin = new Vector2(58, 102);
            }
            if (hasTail)
            {
                //Flip rotation
                rotation = (spineDrawPositions[i] - spineDrawPositions[i - 1]).ToRotation() + MathHelper.PiOver2;
                rotation += MathHelper.Pi;
                if (i == segmentCount - 1)
                    frame.Y = 232;
            }

            if (salty >= 1)
                frame.X += tex.Width / 3;
            if (salty >= 2)
                frame.X += tex.Width / 3;

            spriteBatch.Draw(tex, position - Main.screenPosition, frame, drawColor, rotation, origin, 1f, drawFlipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }
        #endregion

        #region Breaking behavior
        public override void Update()
        {
            CheckIfBroken();
        }

        public override bool IsTileValidForEntity(int x, int y) => true;

        public void CheckIfBroken()
        {
            Tile tileEnd = Main.tile[ControlPoints[^1].ToTileCoordinates()];
            Tile tileStart = Main.tile[Position.ToPoint()];

            if (tileStart.HasTile && (tileEnd.HasTile || hasHead || hasTail))
                return;

            Kill(Position.X, Position.Y);
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.Server)
                new KillTileEntityPacket(Position).Send(runLocally: false);

            if (spineDrawPositions == null)
                CalculateSegmentPositions();

            if (!Main.dedServ && spineDrawPositions != null && spineDrawPositions.Length > 1)
            {
                SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt);

                for (int i = segmentCount - 1; i >= 0; i--)
                {
                    if (Main.rand.NextBool(4))
                        continue;

                    int goreCount = Main.rand.Next(2);
                    for (int j = 0; j <= goreCount; j++)
                    {
                        Vector2 goreSpeed = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-1f, -0.4f));
                        int goreID = Gore.NewGore(new EntitySource_TileEntity(this), spineDrawPositions[i], goreSpeed, Mod.Find<ModGore>("ScourgeSpineGore" + Main.rand.Next(1, 8).ToString()).Type);

                        float goreRotation;
                        if (i == 0)
                            goreRotation = (spineDrawPositions[i + 1] - spineDrawPositions[i]).ToRotation();
                        else
                            goreRotation = (spineDrawPositions[i] - spineDrawPositions[i - 1]).ToRotation();

                        Main.gore[goreID].rotation = goreRotation;
                        Main.gore[goreID].velocity.X *= 0.8f;
                    }
                }
            }

            if (hasHead && Main.netMode != NetmodeID.MultiplayerClient && spineDrawPositions.Length > 1)
            {
                float mauriceRotation = (spineDrawPositions[segmentCount - 1] - spineDrawPositions[segmentCount - 2]).ToRotation() + MathHelper.PiOver2;
                Vector2 adjustedOrigin = spineDrawPositions[segmentCount - 2] + (mauriceRotation - MathHelper.PiOver2).ToRotationVector2() * 30f;

                int headVariant = spineRandomVariants[segmentCount - 1];
                int flipped = drawFlipped ? 1 : 0;
                Projectile proj = Projectile.NewProjectileDirect(new EntitySource_TileEntity(this), adjustedOrigin, new Vector2(0, 0.5f), ModContent.ProjectileType<MauricingScourgeHead>(), 300, 0, Main.myPlayer, headVariant, flipped, mauriceRotation);
                if (PlayerPlaced && proj.ModProjectile is MauricingScourgeHead fallingHead)
                {
                    fallingHead.DropItem = !PlayerPlaced;
                }
            
            }



            if (Main.netMode != NetmodeID.MultiplayerClient && PlayerPlaced && spineDrawPositions.Length > 1)
            {
                int itemType = hasHead ? ModContent.ItemType<ScourgeSpineHeadPlacer>() : ModContent.ItemType<ScourgeSpinePlacer>();
                Vector2 position = hasHead ? spineDrawPositions[^1] : spineDrawPositions[Main.rand.Next(spineDrawPositions.Length)];

                Item.NewItem(new EntitySource_TileEntity(this), position, itemType);
            }

            base.OnKill();
        }
        #endregion

        #region Saving and syncing
        public override void SaveData(TagCompound tag)
        {
            tag["MiddleSegments"] = ControlPoints;
            tag["HasHead"] = hasHead;
            tag["HasTail"] = hasTail;
            tag["Random"] = randomSeed;
            tag["PlayerPlaced"] = PlayerPlaced;
            tag["Salty"] = salty;
        }

        public override void LoadData(TagCompound tag)
        {
            try
            {
                ControlPoints = tag.Get<List<Vector2>>("MiddleSegments");
                hasHead = tag.GetBool("HasHead");
                hasTail = tag.GetBool("HasTail");
                randomSeed = tag.GetInt("Random");
                PlayerPlaced = tag.GetBool("PlayerPlaced");
                if (!tag.TryGet("Salty", out salty))
                    salty = 0;
            }

            catch (Exception e)
            {
                CalamityFables.Instance.Logger.Debug("Scourge spine failed to load: " + e);
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(hasHead);
            writer.Write(hasTail);
            writer.Write(randomSeed);
            writer.Write(PlayerPlaced);
            writer.Write(salty);
            writer.Write(ControlPoints.Count);
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                writer.WriteVector2(ControlPoints[i]);
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            hasHead = reader.ReadBoolean();
            hasTail = reader.ReadBoolean();
            randomSeed = reader.ReadInt32();
            PlayerPlaced = reader.ReadBoolean();
            salty = reader.ReadByte();
            int controlPointCount = reader.ReadInt32();
            ControlPoints = new List<Vector2>();
            for (int i = 0; i < controlPointCount; i++)
            {
                ControlPoints.Add(reader.ReadVector2());
            }
        }
        #endregion
    }

    public class MauricingScourgeHead : ModProjectile
    {
        public override string Texture => AssetDirectory.BurntDesert + "ScourgeSpineDecor";
        public int Variant => (int)Projectile.ai[0];
        public bool Flipped => (int)Projectile.ai[1] == 1;

        public bool DropItem = true;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Maurice");

            if (Main.dedServ)
                return;
            //We do it here because theres no better spot to do it bleh
            for (int i = 1; i < 8; i++)
            {
                ChildSafety.SafeGore[Mod.Find<ModGore>("ScourgeSpineGore" + i.ToString()).Type] = true;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = 250;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.hide = true;
            Projectile.penetrate = -1;
        }

        public bool setRotation = false;

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.Y > 0)
            {
                if (Main.LocalPlayer.Distance(Projectile.Center) < 1000)
                    CameraManager.Quake = 2 + 6 * Utils.GetLerpValue(Projectile.velocity.Y, 0, 3, true);

                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = SoundID.DD2_MonkStaffGroundImpact.Volume * Utils.GetLerpValue(oldVelocity.Y, 0, 3, true) }, Projectile.Center);
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            return false;
        }

        public override void AI()
        {
            if (!setRotation)
            {
                setRotation = true;
                Projectile.rotation = Projectile.ai[2];
            }

            if (Projectile.tileCollide)
            {
                Projectile.velocity.Y += 0.65f;
                Projectile.rotation -= 0.014f * Math.Sign(Projectile.rotation.ToRotationVector2().X);
            }

        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= Utils.GetLerpValue(Projectile.velocity.Y, 0, 5, true);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (damageDone > 50)
                CombatText.NewText(Projectile.Hitbox, Color.OrangeRed, "+MAURICED", true, true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Lighting.GetColor((Projectile.Center - Vector2.UnitY * 16f).ToTileCoordinates());
            float opacity = Projectile.timeLeft > 60 ? 1 : Projectile.timeLeft / 60f;

            Texture2D tex = TextureAssets.Projectile[Type].Value;

            Rectangle frame = new Rectangle(228, 124 * Variant, 116, 122);
            Vector2 origin = new Vector2(58, 102);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor * opacity, Projectile.rotation, origin, Projectile.scale, Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!DropItem)
                return;
            if (Main.netMode == NetmodeID.MultiplayerClient || Main.rand.NextBool(3))
                return;
            Rectangle itemDrop = new Rectangle((int)Projectile.position.X + (int)(Projectile.Size.X / 2), (int)Projectile.position.Y + (int)(Projectile.Size.Y / 2), 1, 1);
            Item.NewItem(Projectile.GetSource_DropAsItem(), itemDrop, ModContent.ItemType<ScourgeSpineHeadPlacer>());
        }
    }
}