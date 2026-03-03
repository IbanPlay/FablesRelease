using static CalamityFables.Helpers.FablesUtils;


namespace CalamityFables.Debug
{
    public abstract class DecipheredNPCAiNPC : ModNPC
    {
        public Player target => Main.player[NPC.target];

        public override bool IsLoadingEnabled(Mod mod) => false;

        public void HoverAI()
        {
            bool forcedToFleeDown = false;

            bool wantsToRunAway = NPC.type == NPCID.Poltergeist && !Main.pumpkinMoon;
            if (NPC.type == NPCID.Reaper && !Main.eclipse)
                wantsToRunAway = true;
            if (NPC.type == NPCID.Drippler && Main.dayTime)
                wantsToRunAway = true;

            if (NPC.justHit)
                NPC.ai[2] = 0f;

            //Move in a direction if fleeing?
            if (wantsToRunAway)
            {
                if (NPC.velocity.X == 0f)
                {
                    NPC.velocity.X = (float)Main.rand.Next(-1, 2) * 1.5f;
                    NPC.netUpdate = true;
                }
            }

            else if (NPC.ai[2] >= 0f)
            {
                int scanWidth = 16;
                bool sameishXrange = false;
                bool sameishYrange = false;
                if (NPC.position.X > NPC.ai[0] - (float)scanWidth && NPC.position.X < NPC.ai[0] + (float)scanWidth)
                    sameishXrange = true;
                else if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    sameishXrange = true;

                scanWidth += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)scanWidth && NPC.position.Y < NPC.ai[1] + (float)scanWidth)
                    sameishYrange = true;

                if (sameishXrange && sameishYrange)
                {
                    NPC.ai[2] += 1f;

                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X *= -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }

                NPC.TargetClosest();
            }

            //change direction towards the player
            else
            {
                NPC.ai[2] += 1f;
                NPC.direction = (target.Center.X - NPC.Center.X).NonZeroSign();
            }


            #region Floor check
            int npcFrontTileX = (int)(NPC.Center.X / 16f) + NPC.direction * 2;
            int npcCenterTileY = (int)(NPC.Center.Y / 16f);
            bool noGroundToHoverOn = true;
            bool rightAboveGround = false;
            int hoverHeight = 3;

            if (NPC.Bottom.Y > target.Top.Y)
            {
                for (int y = npcCenterTileY; y < npcCenterTileY + hoverHeight; y++)
                {
                    if ((Main.tile[npcFrontTileX, y].HasUnactuatedTile && Main.tileSolid[Main.tile[npcFrontTileX, y].TileType]) || Main.tile[npcFrontTileX, y].LiquidAmount > 0)
                    {
                        if (y <= npcCenterTileY + 1)
                            rightAboveGround = true;

                        noGroundToHoverOn = false;
                        break;
                    }
                }
            }

            if (forcedToFleeDown)
            {
                rightAboveGround = false;
                noGroundToHoverOn = true;
            }
            #endregion

            #region Up and Down hovering
            //Fall down at various speeds if no ground to hover from
            if (noGroundToHoverOn)
            {
                if (NPC.type == NPCID.Pixie || NPC.type == NPCID.IceElemental)
                {
                    NPC.velocity.Y += 0.2f;
                    if (NPC.velocity.Y > 2f)
                        NPC.velocity.Y = 2f;
                }
                else if (NPC.type == NPCID.Drippler)
                {
                    NPC.velocity.Y += 0.03f;
                    if (NPC.velocity.Y > 0.75f)
                        NPC.velocity.Y = 0.75f;
                }
                else
                {
                    NPC.velocity.Y += 0.1f;

                    //Ghosts fall down faster
                    if (NPC.type == NPCID.Ghost && wantsToRunAway)
                    {
                        NPC.velocity.Y -= 0.05f;
                        if (NPC.velocity.Y > 6f)
                            NPC.velocity.Y = 6f;
                    }

                    //Just fall down
                    else if (NPC.velocity.Y > 3f)
                    {
                        NPC.velocity.Y = 3f;
                    }
                }
            }

            //Fly up
            else
            {
                //Float back up
                if (NPC.type == NPCID.Pixie || NPC.type == NPCID.IceElemental)
                {
                    if ((NPC.directionY < 0 && NPC.velocity.Y > 0f) || rightAboveGround)
                        NPC.velocity.Y -= 0.2f;
                }

                else if (NPC.type == NPCID.Drippler)
                {
                    if ((NPC.directionY < 0 && NPC.velocity.Y > 0f) || rightAboveGround)
                        NPC.velocity.Y -= 0.075f;

                    if (NPC.velocity.Y < -0.75f)
                        NPC.velocity.Y = -0.75f;
                }

                //If doing down float up
                else if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y -= 0.1f;
                }

                //Not too up
                if (NPC.velocity.Y < -4f)
                    NPC.velocity.Y = -4f;
            }
            #endregion

            #region Bouncing on surfaces
            if (NPC.collideX)
            {
                //BOunce on the walls
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;

                //Min bounce strenght of 1
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                    NPC.velocity.X = 1f;

                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                    NPC.velocity.X = -1f;
            }
            if (NPC.collideY)
            {
                //BOunce on the floor and ceiling
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;

                //Min bounce strenthg of 1
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                    NPC.velocity.Y = 1f;

                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                    NPC.velocity.Y = -1f;
            }
            #endregion

            #region horizontal speed
            float maxSpeed = 2f;
            if (NPC.direction == -1 && NPC.velocity.X > -maxSpeed)
            {
                //Accelerate sideways
                NPC.velocity.X -= 0.1f;
                if (NPC.velocity.X > maxSpeed) //Even more if going faster than its max horizontal speed but in the opposite way
                    NPC.velocity.X -= 0.1f;

                else if (NPC.velocity.X > 0f) //Even more if its facing the opposite direction
                    NPC.velocity.X += 0.05f;

                //CLamp velocity
                if (NPC.velocity.X < -maxSpeed)
                    NPC.velocity.X = -maxSpeed;
            }

            else if (NPC.direction == 1 && NPC.velocity.X < maxSpeed)
            {
                //Accelerate sideways
                NPC.velocity.X += 0.1f;
                if (NPC.velocity.X < -maxSpeed) //Even more if going faster than its max horizontal speed but in the opposite way
                    NPC.velocity.X += 0.1f;

                else if (NPC.velocity.X < 0f) //Even more if its facing the opposite direction
                    NPC.velocity.X -= 0.05f;

                //CLamp velocity
                if (NPC.velocity.X > maxSpeed)
                    NPC.velocity.X = maxSpeed;
            }
            #endregion

            #region Y movement
            maxSpeed = ((NPC.type != NPCID.Drippler) ? 1.5f : 1f);
            if (NPC.directionY == -1 && NPC.velocity.Y > -maxSpeed)
            {
                NPC.velocity.Y -= 0.04f;
                if (NPC.velocity.Y > maxSpeed)
                    NPC.velocity.Y -= 0.05f;

                else if (NPC.velocity.Y > 0f)
                    NPC.velocity.Y += 0.03f;

                if (NPC.velocity.Y < -maxSpeed)
                    NPC.velocity.Y = -maxSpeed;
            }
            else if (NPC.directionY == 1 && NPC.velocity.Y < maxSpeed)
            {
                NPC.velocity.Y += 0.04f;
                if (NPC.velocity.Y < -maxSpeed)
                    NPC.velocity.Y += 0.05f;

                else if (NPC.velocity.Y < 0f)
                    NPC.velocity.Y -= 0.03f;

                if (NPC.velocity.Y > maxSpeed)
                    NPC.velocity.Y = maxSpeed;
            }
            #endregion

        }
    }
}