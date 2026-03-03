using MonoMod.Cil;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    public interface IMovingSurface
    {
        /// <summary>
        /// Is the platform tangible? <br/>
        /// Used to make conditionally solid moving platforms
        /// </summary>
        public bool Solid => true;
        /// <summary>
        /// Should the player phase through regular top surfaces (platforms and such) while riding the platform?
        /// </summary>
        public bool DisableTopSurfaces => true;

        public Vector2 LastPosition { get; }

        public List<Player> RidingPlayers { get; }
    }

    public class MovingPlatformSystem : ModSystem
    {
        public static List<NPC> allPlatforms = new List<NPC>();
        public static bool platformTouched;
        public static bool ignoreTopSurfaces;

        public override void Load()
        {
            IL_Player.Update += HijackCollisionLogic;
            IL_Player.DryCollision += HijackCollisionLogic_Again;
            FablesNPC.PostAIEvent += RegisterMovingPlatforms;
        }



        public override void PreUpdateNPCs() => allPlatforms.Clear();

        //Cache all the platforms
        private void RegisterMovingPlatforms(NPC npc)
        {
            if (npc.ModNPC == null || npc.ModNPC is not IMovingSurface platform || !platform.Solid)
                return;
            allPlatforms.Add(npc);
        }

        private void HijackCollisionLogic_Again(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Player>("SlopeDownMovement"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Entity>("velocity"),
                i => i.MatchStloc(out int _)))
            {
                FablesUtils.LogILEpicFail("Add moving platform collision (drycollision)", "Could not locate Player.SlopeDownMovement");
                return;
            }

            ILLabel labelToSkipStepDown = null;

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(0),
                i => i.MatchLdflda<Entity>("velocity"),
                i => i.MatchLdfld<Vector2>("Y"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("gravity"),
                i => i.MatchBneUn(out labelToSkipStepDown)))
            {
                FablesUtils.LogILEpicFail("Add moving platform collision (drycollision)", "Could not locate the start of the pre-stepdown check");
                return;
            }

            cursor.EmitDelegate(IgnoreStepDown);
            cursor.Emit(Brtrue, labelToSkipStepDown);
        }

        private void HijackCollisionLogic(ILContext il)
        {
            //This runs the platform collision logic before the first StepDown is called, which fucks up the platform
            //After running the platform collision, if the player was on a platform, we insert into a check to make StepDown not get called if the player touches a platform
            //After this, we also edit the fallthrough variable to include the result of wether or not the player was touching the platform & if that platform ignores other platforms

            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<Player>("SlopeDownMovement")))
            {
                FablesUtils.LogILEpicFail("Add moving platform collision", "Could not locate Player.SlopeDownMovement");
                return;
            }

            //Update the platform collision after slopedownmovement, and right before the dreaded StepDown call
            cursor.Emit(Ldarg_0);
            cursor.EmitDelegate(UpdatePlatformCollision);

            ILLabel flag26Label = null;

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("mount"),
                i => i.MatchCallvirt<Mount>("get_Type"),
                i => i.MatchLdcI4(MountID.UFO),
                i => i.MatchBeq(out flag26Label))
                )
            {
                FablesUtils.LogILEpicFail("Add moving platform collision", "Could not locate the start of the flag26 check");
                return;
            }

            //Branch and make it so that stepDown isnt called when on a platform
            cursor.EmitDelegate(IgnoreStepDown);
            cursor.Emit(Brtrue, flag26Label);

            int fallThroughVariableIndex = 0;

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Entity>("velocity"),
                i => i.MatchStloc(out int _), //Set velocity variable to player velocity

                i => i.MatchLdarg(0),
                i => i.MatchLdcI4(0),
                i => i.MatchStfld<Player>("slideDir"), //Set slide dir

                i => i.MatchLdcI4(0),
                i => i.MatchStloc(out int _), //Set ignore platforms

                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("controlDown"),
                i => i.MatchStloc(out fallThroughVariableIndex) //Set fallthrough
                ))
            {
                FablesUtils.LogILEpicFail("Add moving platform collision", "Could not locate the part where fallthrough & ignoreplatforms get called");
                return;
            }

            //OR the "fallthrough" variable if we are meant to ignore top surfaces
            cursor.EmitLdloc(fallThroughVariableIndex);
            cursor.EmitDelegate(IgnoreTopSurfaces);
            cursor.Emit(Or);
            cursor.EmitStloc(fallThroughVariableIndex);
        }

        public void UpdatePlatformCollision(Player player)
        {
            platformTouched = false;
            ignoreTopSurfaces = false;

            FablesPlayer mp = player.Fables();
            NPC lastPlatform = mp.riddenPlatform; //Cache the last platform for optimization
            mp.DetachFromPlatform();

            bool fallThrough = player.controlDown;
            if ((player.gravDir == -1f) | (player.mount.Active && (player.mount.Cart || player.mount.Type == 12 || player.mount.Type == 7 || player.mount.Type == 8 || player.mount.Type == 23 || player.mount.Type == 44 || player.mount.Type == 48)))
                fallThrough = true;
            if (player.grapCount > 0 || player.justJumped || player.velocity.Y < 0 || fallThrough)
                return;

            //At the feet of the player
            Rectangle playerFeetRect = new Rectangle((int)player.position.X, (int)player.position.Y + player.height, player.width, 2);
            //Check gets longer with velocity so that you can't accelerate straight through the platform
            if (player.velocity.Y > 0)
                playerFeetRect.Height += (int)player.velocity.Y;

            NPC intersectedPlatform = null;

            //Prioritize the last platform ridden when checking, since its likely it will be the one we are gonna keep riding lol
            if (lastPlatform != null && lastPlatform.active)
            {
                Rectangle NPCRect = new Rectangle((int)lastPlatform.position.X, (int)lastPlatform.position.Y, lastPlatform.width, 8);

                float lastHeight = (lastPlatform.ModNPC as IMovingSurface).LastPosition.Y;
                //Platform going up
                if (lastHeight > lastPlatform.position.Y)
                    NPCRect.Height += (int)(lastHeight - lastPlatform.position.Y);

                bool intersection = playerFeetRect.Intersects(NPCRect);
                if (intersection)
                    intersectedPlatform = lastPlatform;
            }
            if (intersectedPlatform == null)
            {
                foreach (NPC NPC in allPlatforms)
                {
                    //Skip the last one since we already checked it
                    if (NPC == lastPlatform)
                        continue;

                    Rectangle NPCRect = new Rectangle((int)NPC.position.X, (int)NPC.position.Y, NPC.width, 8);

                    float lastHeight = (NPC.ModNPC as IMovingSurface).LastPosition.Y;
                    //Platform going up
                    if (lastHeight > NPC.position.Y)
                        NPCRect.Height += (int)(lastHeight - NPC.position.Y);

                    bool intersection = playerFeetRect.Intersects(NPCRect);
                    if (intersection)
                        intersectedPlatform = NPC;
                }
            }

            if (intersectedPlatform != null)
            {
                platformTouched = true;
                ignoreTopSurfaces = (intersectedPlatform.ModNPC as IMovingSurface).DisableTopSurfaces;
                mp.AttachToPlatform(intersectedPlatform);

                player.gfxOffY = intersectedPlatform.gfxOffY;
                player.velocity.Y = 0;
                player.jump = 0;
                player.fallStart = (int)(player.position.Y / 16f);
                player.position.Y = intersectedPlatform.position.Y - player.height + 4;
                player.position += intersectedPlatform.velocity;
                return;
            }
        }

        public bool IgnoreStepDown() => platformTouched;
        public bool IgnoreTopSurfaces() => ignoreTopSurfaces;

        //Taken from slr with modifications from yours truly. Ty guys
        public static void MultiplayerCollisionFailsafe(NPC platform, float previousHeight)
        {
            float yDistTraveled = platform.position.Y - previousHeight;

            if (platform.velocity != Vector2.Zero && platform.velocity.Y < -1f && yDistTraveled < platform.velocity.Y * 1.5 && yDistTraveled > platform.velocity.Y * 6)
            {
                //this loop outside of the normal moving platform loop in the IL edit is mainly for multiplayer with some potential for extreme lag situations on fast platforms
                //what is happening is that when terraria skips frames (or lags in mp) they add the NPC velocity multiplied by the skipped frames up to 5x a normal frame until caught up, but only run the ai once
                //so we can end up with frames where the platform skips 5x its normal velocity likely clipping through Players since the platform is thin.
                //to solve this, the collision code takes into account the previous platform position accessed by this AI for the hitbox to cover the whole travel from previous fully processed frame.
                //only handling big upwards y movements since the horizontal skips don't seem as jarring to the user since platforms tend to be wide, and vertical down skips aren't jarring since Player drops onto platform anyway instead of clipping through.
                
                //What the guy above said
                foreach (Player player in Main.player)
                {
                    if (!player.active ||
                        player.dead ||
                        player.justJumped ||
                        player.velocity.Y < 0 ||
                        player.GoingDownWithGrapple ||
                        player.controlDown ||
                        player.gravDir == -1f)
                        continue;

                    //Taken from vanilla collision logic
                    if (player.mount.Active && (player.mount.Cart || player.mount.Type == 12 || player.mount.Type == 7 || player.mount.Type == 8 || player.mount.Type == 23 || player.mount.Type == 44 || player.mount.Type == 48))
                        continue;

                    Rectangle playerRect = new Rectangle((int)player.position.X, (int)player.position.Y + player.height, player.width, 1);
                    Rectangle platformRect = new Rectangle((int)platform.position.X, (int)platform.position.Y, platform.width, 8 + (int)(Math.Max(0, player.velocity.Y) + Math.Abs(yDistTraveled)));

                    if (playerRect.Intersects(platformRect) && player.position.Y <= platform.position.Y)
                    {
                        player.velocity.Y = 0;
                        player.position.Y = platform.position.Y - player.height + 4;
                        player.position += platform.velocity;
                    }
                }
            }

        }
    }
}
