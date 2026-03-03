namespace CalamityFables.Content.Debug
{
    /*
    public class KinematicsCreature : ModProjectile
    {
        public bool stepDone = false;
        public Vector2 offsetFromPlayer;
        public Vector2 newOffsetFromPlayer;

        public ref float Ticks => ref Projectile.ai[1];
        public ref float Frame => ref Projectile.localAI[0];

        public List<CCDKinematicJoint> Limbs;
        public List<Vector2> IdealStepPositions;
        public List<Vector2> PreviousIdealStepPositions;

        public Vector2 movementTarget;

        public ref Player Owner => ref Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Powerful Lizard");
        }

        public override string Texture => AssetDirectory.Invisible;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.damage = 0;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9999;
            Projectile.aiStyle = -1;
            Projectile.netImportant = true;
        }



        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.controlHook)
            {
                Projectile.active = false;
                return;
            }

            Projectile.timeLeft = 2;

            if (Main.mouseRight)
                movementTarget = Main.MouseWorld;

            movementTarget = Owner.Center - Vector2.UnitY * 100f;

            Vector2 velocity = Projectile.SafeDirectionTo(movementTarget);
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, velocity.X * 9f, 0.06f);



            if (Projectile.Distance(movementTarget) < 3f)
                Projectile.velocity = Vector2.Zero;
            GroundMovement();

            Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.X * 0.02f, 0.05f);

            Projectile.scale = 0.7f;

            if (Limbs == null)
            {
                InitializeLimbs();
            }
            Vector2[] limbEndEffectors = new Vector2[4];
        }

        public bool GroundMovement()
        {
            int npcFrontTileX = (int)(Projectile.Center.X / 16f) + Projectile.velocity.X.NonZeroSign();
            int npcCenterTileY = (int)(Projectile.Center.Y / 16f);

            bool canHover = false;
            int groundDistance = 0;

            //Check beneath the mortar to check if theres any solid ground beneath
            for (int y = 0; y < 10; y++)
            {
                Vector2 positionOffset = Vector2.UnitY.RotatedBy(0) * y * 18;
                Point tile = (Projectile.Center + positionOffset).ToTileCoordinates();

                if ((Main.tile[tile].HasUnactuatedTile && Main.tileSolid[Main.tile[tile].TileType]))
                {
                    canHover = true;
                    groundDistance = y;
                    break;
                }
            }

            if (!canHover)
                Projectile.velocity.Y += 0.3f;

            else if (Projectile.velocity.Y > 0f)
                Projectile.velocity.Y -= 0.5f;

            else if (groundDistance < 5)
                Projectile.velocity.Y -= 0.3f;

            else if (groundDistance < 7)
                Projectile.velocity.Y -= 0.2f;

            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y, -8f, 6f);

            return canHover;
        }

        public Vector2 LimbAttachmentPoint(int index)
        {
            Vector2 offset;
            switch (index)
            {
                case 0:
                    offset = new Vector2(-110f, 10f);
                    break;
                case 1:
                    offset = new Vector2(-70f, 30f);
                    break;
                case 2:
                    offset = new Vector2(70f, 30f);
                    break;
                case 3:
                default:
                    offset = new Vector2(110f, 10f);
                    break;
            }

            offset = offset.RotatedBy(Projectile.rotation) * Projectile.scale;
            return Projectile.Center + offset;
        }

        public Vector2 GetIdealStepPosition(int index, float limbLenght)
        {
            Vector2 limbPosition = Limbs[index].Position;
            Vector2 normal = Vector2.UnitY.RotatedBy(Projectile.rotation);
            Vector2 offset = Vector2.UnitX * Math.Sign(Projectile.velocity.X) * 16f * 4f;

            if (index < 2)
                normal = normal.RotatedBy(MathHelper.PiOver4);
            else
                normal = normal.RotatedBy(-MathHelper.PiOver4);

            int maxLenghtInTime = (int)(limbLenght / 16 * 1.8f);


            for (float angle = 0f; angle < MathHelper.PiOver4 * 1.3f; angle += 0.02f)
            {
                for (int i = 0; i < maxLenghtInTime; i++)
                {
                    int angleSign = index < 2 ? -1 : 1;
                    Point position = (offset + limbPosition + normal.RotatedBy(angleSign * angle) * 8f * i).ToTileCoordinates();
                    if (Main.tile[position].IsTileSolidGround())
                        return position.ToWorldCoordinates() - Vector2.UnitY * 6f;
                }
            }
            return limbPosition + normal * limbLenght * 0.65f;
        }

        public void InitializeLimbs()
        {
            Limbs = new List<CCDKinematicJoint>();
            for (int i = 0; i < 4; i++)
            {

                CCDKinematicJoint joint = new CCDKinematicJoint(LimbAttachmentPoint(i));
                Limbs.Add(joint);

                for (int j = 0; j < 2; j++)
                {
                    CCDKinematicsConstraint constraint = null;
                    float segmentLenght = 10f;
                    switch (j)
                    {
                        case 0:
                            segmentLenght = 125f * Projectile.scale;
                            break;
                        case 1:
                            segmentLenght = 250f * Projectile.scale;
                            constraint = new CCDKinematicsConstraint(-MathHelper.Pi, -0.05f);
                            break;
                    }

                    if (constraint != null && i > 1)
                        constraint.flipConstraintAngles = true;

                    Limbs[i].ExtendInDirection(Vector2.UnitY * segmentLenght, constraint);
                }
            }

            IdealStepPositions = new List<Vector2>();
            for (int i = 0; i < 4; i++)
            {
                IdealStepPositions.Add(GetIdealStepPosition(i, Limbs[i].GetLimbLenght()));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Limbs == null)
                return false;

            Texture2D limbStartTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "CrabulonLimbStart").Value;
            Texture2D limbEndTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "CrabulonLimbEnd").Value;
            Texture2D bodyTex = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + "CrabulonBody").Value;

            Main.EntitySpriteDraw(bodyTex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, bodyTex.Size() / 2, Projectile.scale, 0, 0);

            int j = 0;
            foreach (CCDKinematicJoint limb in Limbs)
            {
                List<CCDKinematicJoint> joints = limb.GetSubLimb();
                for (int i = 1; i < joints.Count; i++)
                {
                    Vector2 jointVector = joints[i].SegmentVector;
                    float rotation = jointVector.ToRotation();

                    Texture2D tex = i == 1 ? limbStartTex : limbEndTex;
                    Vector2 origin = i == 1 ? new Vector2(22, 18) : new Vector2(16, 44);
                    SpriteEffects flip = SpriteEffects.None;
                    if (j < 2)
                    {
                        flip = SpriteEffects.FlipVertically;
                        origin.Y = tex.Height - origin.Y;
                    }

                    Main.EntitySpriteDraw(tex, joints[i - 1].Position - Main.screenPosition, null, lightColor, rotation, origin, Projectile.scale, flip, 0);
                }
                j++;
            }

            return false;
        }
    }
    */
}

