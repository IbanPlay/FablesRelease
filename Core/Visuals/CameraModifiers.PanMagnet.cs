using Terraria.Graphics.CameraModifiers;
using static CalamityFables.Helpers.FablesUtils;

namespace CalamityFables.Core
{
    public class CameraPanMagnet : ICameraModifier
    {
        public string UniqueIdentity => "Iban's Camera Magnet";
        public bool Finished => false; //The effect never ends

        public int[] cutsceneImmunity = new int[Main.maxPlayers]; 

        public Vector2 magnetPosition = default;
        public bool inUse = false;
        public float currentHighestPriority = 0f;
        public EasingFunction easing;
        public float easingDegree;

        private float panProgress;
        public float PanProgress {
            get => panProgress;
            set => panProgress = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Use this when setting the camera position for a cutscene with immunity and you need the immunity to be properly synced in multiplayer
        /// </summary>
        /// <returns>
        /// If the local player is within the range ot the cutscene
        /// </returns>
        public bool SetMagnetPositionAndImmunityForEveryone(Vector2 magnetPosition, Vector2 cutsceneCenter, float cutsceneRange, float priority = 1f, EasingFunction _easing = null, float _easingDegree = 1f)
        {
            bool localPlayerInRange = false;

            //In singleplayer, simply set the magnet to the player, along the immunity. We do the same in mp if there's no immunity
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Main.LocalPlayer.WithinRange(cutsceneCenter, cutsceneRange))
                {
                    SetMagnetPosition(magnetPosition, priority, _easing, _easingDegree, true);
                    localPlayerInRange = true;
                }
            }
            //In multiplayer, we have to look over every player to give em immunity if within range
            else
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (!Main.player[i].active || !Main.player[i].WithinRange(cutsceneCenter, cutsceneRange))
                        continue;

                    //Set the magnet for the player in question
                    if (!Main.dedServ && i == Main.myPlayer)
                    {
                        SetMagnetPosition(magnetPosition, priority, _easing, _easingDegree, true);
                        localPlayerInRange = true;
                    }
                    //Otherwise set the player to have immunity
                    else
                        cutsceneImmunity[i] = 2;
                }
            }

            return localPlayerInRange;
        }

        /// <summary>
        /// Sets the position of the camera magnet, and potentially gives immunity to the player <br/>
        /// In multiplayer, please use <see cref="SetMagnetPositionAndImmunityForEveryone(Vector2, Vector2, float, float, EasingFunction, float)"/> to keep the immunity synced
        /// </summary>
        /// <param name="magnetPosition"></param>
        /// <param name="priority"></param>
        /// <param name="_easing"></param>
        /// <param name="_easingDegree"></param>
        /// <param name="grantImmunity"></param>
        public void SetMagnetPosition(Vector2 magnetPosition, float priority = 1f, EasingFunction _easing = null, float _easingDegree = 1f, bool grantImmunity = true)
        {
            if (grantImmunity)
                cutsceneImmunity[Main.myPlayer] = 2;

            if (currentHighestPriority < priority)
            {
                this.magnetPosition = magnetPosition;
                currentHighestPriority = priority;
                inUse = true;

                if (_easing != null)
                {
                    easing = _easing;
                    easingDegree = _easingDegree;
                }

                else
                {
                    easing = SineInOutEasing;
                    easingDegree = 1f;
                }
            }
        }

        public void Update(ref CameraInfo cameraInfo)
        {
            if (magnetPosition == default || panProgress == 0)
                return;

            if (easing == null)
                easing = SineInOutEasing;

            Vector2 halfScreen = new Vector2(-Main.screenWidth / 2f, -Main.screenHeight / 2f);
            cameraInfo.CameraPosition = Vector2.Lerp(cameraInfo.OriginalCameraCenter + halfScreen, magnetPosition + halfScreen, easing(PanProgress, easingDegree));
        }

        public void Reset()
        {
            currentHighestPriority = 0f;
            magnetPosition = default;
            PanProgress = 0;
            easing = SineInOutEasing;
            easingDegree = 1;
        }
    }
}