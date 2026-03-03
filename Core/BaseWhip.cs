using Terraria.Enums;

namespace CalamityFables.Core //Credits to blockaroz for adapting this out of the vaniller code
{
    public abstract class BaseWhip : ModProjectile
    {
        protected int xFrames = 1;
        protected int yFrames = 5;
        protected int xFrame = 0;

        protected string WhipName;
        protected int Segments;
        protected float Range;
        protected float AnimationLength;
        protected Color? StringColor;
        protected int HandleOffset;
        protected SoundStyle CrackSound;

        #region Fields
        protected Player Owner => Main.player[Projectile.owner];

        public ref float Age => ref Projectile.ai[0];

        public float AnimProgress => Age / AnimationLength;

        public float MiddleOfArc => AnimationLength / 1.5f;

        public bool HitboxActive => Utils.GetLerpValue(0.1f, 0.7f, AnimProgress, true) * Utils.GetLerpValue(0.9f, 0.7f, AnimProgress, true) > 0.5f;

        public List<Vector2> PointsForCollision => Projectile.WhipPointsForCollision;

        public Vector2 EndPoint => PointsForCollision[^1];

        public List<Vector2> PointsForDrawing;
        #endregion

        public BaseWhip(string name, int segments = 20, float rangeMultiplier = 1f, Color? stringColor = null, int handleOffset = 2)
        {
            WhipName = name;
            Segments = Math.Max(3, segments);
            Range = rangeMultiplier;
            StringColor = stringColor;
            HandleOffset = handleOffset;
        }

        public override void Load()
        {
            Terraria.On_Projectile.FillWhipControlPoints += HijackWhipControlPoints;
            Terraria.On_Projectile.GetWhipSettings += HijackWhipSettings;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault(WhipName);
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();
            CrackSound = SoundID.Item153;
            SafeSetDefaults();
        }

        /// <summary>
        /// Can be used to implement defaults values without overriding <see cref="SetDefaults"/>.
        /// </summary>
        public virtual void SafeSetDefaults() { }

        #region Hijack Vanilla Code
        private void HijackWhipControlPoints(Terraria.On_Projectile.orig_FillWhipControlPoints orig, Projectile proj, List<Vector2> controlPoints)
        {
            orig(proj, controlPoints);
            if (proj.ModProjectile is BaseWhip projo)
                projo.CustomizeWhipControlPoints(controlPoints);
        }
        
        /// <summary>
        /// Can be used to modify the position of each point on the whip.
        /// </summary>
        /// <param name="controlPoints"></param>
        public virtual void CustomizeWhipControlPoints(List<Vector2> controlPoints) { }

        private void HijackWhipSettings(Terraria.On_Projectile.orig_GetWhipSettings orig, Projectile proj, out float timeToFlyOut, out int segments, out float rangeMultiplier)
        {
            orig(proj, out timeToFlyOut, out segments, out rangeMultiplier);
            if (proj.ModProjectile is BaseWhip projo)
                projo.CustomizeWhipSettings(ref segments, ref rangeMultiplier);
        }

        /// <summary>
        /// Can be used to dynamically modify the number of segments the whip has or it's range multiplier.
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="rangeMultiplier"></param>
        public virtual void CustomizeWhipSettings(ref int segments, ref float rangeMultiplier)
        {
            segments = Segments;
            rangeMultiplier = Range;
        }
        #endregion

        public override bool PreAI()
        {
            AnimationLength = Owner.itemAnimationMax * Projectile.MaxUpdates;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.ai[0]++;
            Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * (Projectile.ai[0] - 1f);
            Projectile.spriteDirection = (!(Vector2.Dot(Projectile.velocity, Vector2.UnitX) < 0f)) ? 1 : -1;

            if (Age >= AnimationLength || Owner.itemAnimation == 0)
            {
                Projectile.Kill();
                return false;
            }

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemAnimation = Owner.itemAnimationMax - (int)(Projectile.ai[0] / Projectile.MaxUpdates);
            Owner.itemTime = Owner.itemAnimation;

            if (Age == (int)(AnimationLength / 2f))
            {
                PointsForCollision.Clear();
                Projectile.FillWhipControlPoints(Projectile, PointsForCollision);
                SoundEngine.PlaySound(CrackSound, PointsForCollision[^1]);
            }

            if (HitboxActive)
            {
                PointsForCollision.Clear();
                Projectile.FillWhipControlPoints(Projectile, PointsForCollision);
            }

            ArcAI();
            return false;
        }

        /// <summary>
        /// Can be used to implement whip behavior.
        /// </summary>
        public virtual void ArcAI() { }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;

        public override void CutTiles()
        {
            Vector2 value = new Vector2(Projectile.width * Projectile.scale * 0.5f, 0f);
            for (int i = 0; i < PointsForCollision.Count; i++)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Utils.PlotTileLine(PointsForCollision[i] - value, PointsForCollision[i] + value, Projectile.height * Projectile.scale, DelegateMethods.CutTiles);
            }
        }

        //Unused recreation of Projectile.FillWhipControlPoints(). Kept for the better variable names
        /* 
		public virtual void SetPoints(List<Vector2> controlPoints)
		{
			float time = Projectile.ai[0] / flyTime;
			float timeModified = time * 1.5f;
			float segmentOffset = MathHelper.Pi * 10f * (1f - timeModified) * -Projectile.spriteDirection / segments;
			float tLerp = 0f;

			if (timeModified > 1f)
			{
				tLerp = (timeModified - 1f) / 0.5f;
				timeModified = MathHelper.Lerp(1f, 0f, tLerp);
			}

			//vanilla code
			Player player = Main.player[Projectile.owner];
			Item heldItem = player.HeldItem;
			float realRange = ContentSamples.ItemsByType[heldItem.type].useAnimation * 2 * time * player.whipRangeMultiplier;
			float num8 = Projectile.velocity.Length() * realRange * timeModified * rangeMult / segments;
			Vector2 playerArmPosition = Main.GetPlayerArmPosition(Projectile);
			Vector2 firstPos = playerArmPosition;
			float num10 = 0f - MathHelper.PiOver2;
			Vector2 midPos = firstPos;
			float num11 = 0f + MathHelper.PiOver2 + MathHelper.PiOver2 * Projectile.spriteDirection;
			Vector2 lastPos = firstPos;
			float num12 = 0f + MathHelper.PiOver2;
			controlPoints.Add(playerArmPosition);

			for (int i = 0; i < segments; i++)
			{
				float num14 = segmentOffset * (i / (float)segments);
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
        */

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            PointsForDrawing ??= [];
            PointsForDrawing.Clear();
            Projectile.FillWhipControlPoints(Projectile, PointsForDrawing);

            DrawBehindWhip(ref lightColor);

            if (StringColor is not null)
            {
                Vector2 stringPoint = PointsForDrawing[0];
                for (int i = 0; i < PointsForDrawing.Count - 2; i++)
                {
                    Vector2 nextPoint = PointsForDrawing[i + 1] - PointsForDrawing[i];
                    Color color = StringColor.Value.MultiplyRGBA(Projectile.GetAlpha(Lighting.GetColor(PointsForDrawing[i].ToTileCoordinates())));
                    var scale = new Vector2(1f, (nextPoint.Length() + 2f) / TextureAssets.FishingLine.Height());
                    Main.EntitySpriteDraw(TextureAssets.FishingLine.Value, stringPoint - Main.screenPosition, null, color, nextPoint.ToRotation() - MathHelper.PiOver2, new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 2f), scale, SpriteEffects.None, 0);
                    stringPoint += nextPoint;
                }
            }

            //whip
            Asset<Texture2D> texture = TextureAssets.Projectile[Type];
            DrawWhipSegments(texture.Value, PointsForDrawing, ref lightColor);
            return false;
        }

        /// <summary>
        /// Similar to <see cref="ModProjectile.PreDraw(ref Color)"/>, can be used to draw things behind the whip.
        /// </summary>
        /// <param name="lightColor"></param>
        public virtual void DrawBehindWhip(ref Color lightColor) { }

        /// <summary>
        /// Draws all of the whip segments. <br/>
        /// Can be overridden for custom segment drawing, or each segment subset can be changed individually.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="points"></param>
        /// <param name="lightColor"></param>
        public virtual void DrawWhipSegments(Texture2D texture, List<Vector2> points, ref Color lightColor)
        {
            SpriteEffects effect = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            DrawMidSegments(texture, points, effect);
            DrawFirstSegment(texture, points[0], points[1], effect);
            DrawLastSegment(texture, points[points.Count - 2], points[points.Count - 1], effect);
        }

        /// <summary>
        /// Draws the first segment, or handle, of the whip.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="nextPosition"></param>
        /// <param name="effects"></param>
        public virtual void DrawFirstSegment(Texture2D texture, Vector2 position, Vector2 nextPosition, SpriteEffects effects)
        {
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, FirstSegmentFrame);
            Color color = Projectile.GetAlpha(Lighting.GetColor(position.ToTileCoordinates()));
            Vector2 origin = whipFrame.Size() / 2;
            origin.Y += HandleOffset;
            float rotation = (nextPosition - position).ToRotation() - MathHelper.PiOver2;

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, whipFrame, color, rotation, origin, Projectile.scale, effects, 0);
        }

        /// <summary>
        /// Draws the chain segments of the whip.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="points"></param>
        /// <param name="effects"></param>
        public virtual void DrawMidSegments(Texture2D texture, List<Vector2> points, SpriteEffects effects)
        {
            for(int i = 1; i < points.Count - 2; i++)
                if (ShouldDrawSegment(i))
                    DrawMidSegment(texture, i, points[i], points[i + 1], effects);
        }

        /// <summary>
        /// Draws an individual chain segment of the whip. The specific segment is denoted by segmentID.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="segmentID"></param>
        /// <param name="position"></param>
        /// <param name="nextPosition"></param>
        /// <param name="effects"></param>
        public virtual void DrawMidSegment(Texture2D texture, int segmentID, Vector2 position, Vector2 nextPosition, SpriteEffects effects)
        {
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, MidSegmentFrame(segmentID));
            Color color = Projectile.GetAlpha(Lighting.GetColor(position.ToTileCoordinates()));
            Vector2 origin = whipFrame.Size() / 2;
            Vector2 difference = nextPosition - position;
            float rotation = difference.ToRotation() - MathHelper.PiOver2;
            Vector2 scale = SegmentScale(segmentID, difference, whipFrame);

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, whipFrame, color, rotation, origin, scale * Projectile.scale, effects, 0);
        }

        /// <summary>
        /// Draws the final segment of the whip
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="position"></param>
        /// <param name="nextPosition"></param>
        /// <param name="effects"></param>
        public virtual void DrawLastSegment(Texture2D texture, Vector2 position, Vector2 nextPosition, SpriteEffects effects)
        {
            Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, LastSegmentFrame);
            Color color = Projectile.GetAlpha(Lighting.GetColor(position.ToTileCoordinates()));
            Vector2 origin = whipFrame.Size() / 2;
            float rotation = (nextPosition - position).ToRotation() - MathHelper.PiOver2;

            Main.EntitySpriteDraw(texture, position - Main.screenPosition, whipFrame, color, rotation, origin, Projectile.scale, effects, 0);
        }

        public virtual int FirstSegmentFrame => 0;

        public virtual int MidSegmentFrame(int segment) => 1 + segment % 3;

        public virtual int LastSegmentFrame => yFrames - 1;

        public virtual bool ShouldDrawSegment(int segment) => segment % 2 == 0;

        public virtual Vector2 SegmentScale(int segment, Vector2 difference, Rectangle frame) => Vector2.One;
        #endregion
    }
}