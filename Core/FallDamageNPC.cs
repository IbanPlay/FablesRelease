namespace CalamityFables.Core
{
    public class FallDamageNPC : GlobalNPC
    {
        /// <summary>
        /// How much "gravity damage" is this npc slated to recieve once it hits the ground.
        /// </summary>
        private int potentialEnergyDamage = 0;

        /// <summary>
        /// How much "gravity damage" is this npc slated to recieve once it hits the ground.
        /// </summary>
        private int potentialMinDamage = 0;

        /// <summary>
        /// How far must the npc fall for it to recieve full fall damage
        /// </summary>
        private float fallDistanceForFullDamage = 0;

        /// <summary>
        /// How far must the npc fall for it to recieve any fall damage
        /// </summary>
        private float fallDistanceForMinDamage = 0;

        private float fallStart;

        /// <summary>
        /// This keeps track of the last recorded velocity, so that we can dispel the potential fall damage if the enemy slows their fall down by any means
        /// </summary>
        private Vector2 OldVelocity = Vector2.Zero;
        /// <summary>
        /// This keeps track of the last recorded velocity, because terraria is dumb and we need two of them
        /// </summary>
        private Vector2 OlderVelocity = Vector2.Zero;


        public int PotentialEnergyDamage {
            get {
                return potentialEnergyDamage;
            }
            private set {
                //You can't stack gravity damage. Higher "gravity" simply replaces the gravity damage.
                potentialEnergyDamage = value == 0 ? 0 : Math.Max(potentialEnergyDamage, value);
            }
        }

        public int PotentialMinDamage {
            get {
                return potentialMinDamage;
            }
            private set {
                potentialMinDamage = value == 0 ? 0 : Math.Max(potentialMinDamage, value);
            }
        }


        public bool FallDamageSusceptible => PotentialEnergyDamage > 0;

        public int RecievedDamage(NPC npc)
        {
            if (npc.Center.Y - fallDistanceForMinDamage <= fallStart)
                return 0;

            float fallDistance = npc.Center.Y - fallStart;
            float fallCompletion = Utils.GetLerpValue(fallDistanceForMinDamage, fallDistanceForFullDamage, fallDistance, true);

            return (int)fallCompletion * PotentialEnergyDamage;
        }


        public override bool InstancePerEntity => true;

        public override GlobalNPC Clone(NPC npc, NPC npcClone)
        {
            FallDamageNPC myClone = (FallDamageNPC)base.Clone(npc, npcClone);

            myClone.potentialEnergyDamage = potentialEnergyDamage;
            myClone.potentialMinDamage = potentialMinDamage;
            myClone.OldVelocity = OldVelocity;
            myClone.OlderVelocity = OlderVelocity;
            myClone.fallDistanceForFullDamage = fallDistanceForFullDamage;
            myClone.fallStart = fallStart;

            return myClone;
        }

        public override void SetDefaults(NPC npc)
        {
            PotentialEnergyDamage = 0;
            potentialMinDamage = 0;
            fallDistanceForFullDamage = 0f;
            OldVelocity = Vector2.Zero;
            OlderVelocity = Vector2.Zero;
        }

        /// <summary>
        /// Sets up a NPC to recieve fall damage when they hit the ground.
        /// </summary>
        /// <param name="npc">The npc to apply fall damage to</param>
        /// <param name="potentialDamage">The maximum fall damage taken by the npc once they hit the ground</param>
        /// <param name="terminalVelocityForFullDamage">The downwards velocity necessary to recieve the full fall damage. By default 10, the npc max fall speed</param>
        public void ApplyFallDamage(NPC npc, int potentialDamage, int minDamage, float maxFallDistance = 160, float minFallDistance = 16f)
        {
            //NPCs that don't collide with tiles simply don't get fall damage, lol. Same goes for the no gravity ones
            if (npc.noTileCollide || npc.noGravity)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                new SyncNPCFallDamagePacket(npc.whoAmI, potentialDamage, minDamage, maxFallDistance, minFallDistance).Send(-1, -1, false);
                return;
            }    

            npc.collideY = false;
            PotentialEnergyDamage = potentialDamage;
            PotentialMinDamage = minDamage;
            OldVelocity = npc.velocity;
            OlderVelocity = npc.velocity;
            fallDistanceForFullDamage = maxFallDistance;
            fallDistanceForMinDamage = minFallDistance;
            fallStart = npc.Center.Y;
        }

        public override void PostAI(NPC npc)
        {
            if (FallDamageSusceptible)
            {
                if (npc.noTileCollide || npc.noGravity || OldVelocity.Y == 0 || npc.knockBackResist == 0)
                {
                    PotentialEnergyDamage = 0;
                    return;
                }

                if (npc.velocity.Y < 0)
                    fallStart = npc.Center.Y;

                float newVerticalVelocity = npc.velocity.Y;
                float oldVerticalVelocity = OldVelocity.Y;
                float olderVerticalVelocity = OlderVelocity.Y;

                /*
                //If the thing flies back up, cancel the fall dmg (Aka if you're going up despite going down beforehand
                if (newVerticalVelocity < 0 && oldVerticalVelocity >= 0)
                    PotentialEnergyDamage = 0;

                //Same goes for if it slows its fall. 
                //We use 3 variables cuz there is a frame inbetween the fall and the landing where the velocity is still set to something.
                if (newVerticalVelocity > 0 && olderVerticalVelocity > 0 && oldVerticalVelocity < olderVerticalVelocity)
                    PotentialEnergyDamage = 0;
                */

                //If the npc hit a tile/Came to a stop after falling
                if (npc.collideY || (newVerticalVelocity == 0 && oldVerticalVelocity > 0))
                {
                    float fallDistance = npc.Center.Y - fallStart;
                    float fallCompletion = Utils.GetLerpValue(fallDistanceForMinDamage, fallDistanceForFullDamage, fallDistance, true);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        new SyncThudLandingSFX(npc.Center, 0.5f + fallCompletion).Send();
                        int damage = RecievedDamage(npc);
                        if (damage > 0)
                            npc.SimpleStrikeNPC(Math.Max(PotentialMinDamage, damage), 0, false, 0f);
                    }

                    PotentialEnergyDamage = 0;
                    PotentialMinDamage = 0;
                    return;
                }

                OlderVelocity = OldVelocity;
                OldVelocity = npc.velocity;
            }
        }

        [Serializable]
        public class SyncNPCFallDamagePacket : Module
        {
            private readonly int npc;
            private int potentialDamage;
            private int minDamage;
            private float maxFallDistance;
            private float minFallDistance;

            public SyncNPCFallDamagePacket(int npc, int potentialDamage, int minDamage, float maxFallDistance = 160, float minFallDistance = 16f)
            {
                this.npc = npc;
                this.potentialDamage = potentialDamage;
                this.minDamage = minDamage;
                this.maxFallDistance = maxFallDistance;
                this.minFallDistance = minFallDistance;
            }

            protected override void Receive()
            {
                Main.npc[npc].GetGlobalNPC<FallDamageNPC>().ApplyFallDamage(Main.npc[npc], potentialDamage, minDamage, maxFallDistance, minFallDistance);
            }
        }

        [Serializable]
        public class SyncThudLandingSFX : SyncSoundPacket
        {
            public SyncThudLandingSFX(Vector2 pos, float vol = 1) : base(pos, vol) { }
            public override SoundStyle SyncedSound => SoundID.DD2_MonkStaffGroundImpact; 
        }
    }
}
