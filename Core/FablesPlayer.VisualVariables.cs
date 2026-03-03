using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using static Mono.Cecil.Cil.OpCodes;

namespace CalamityFables.Core
{
    //Tracks variables used for visual effects like a dampened string for the player's velocity and a scarf esque verlet net
    public partial class FablesPlayer : ModPlayer
    {
        public Vector2 lastVelocity;
        public int lastDirection;

        public DampedVelocityTracker springyVelocityTracker = new DampedVelocityTracker(0.26f, 0.05f, 2f);
        public DampedVelocityTracker hairLikeVelocityTracker = new DampedVelocityTracker(0.46f, 0.04f, 1.3f);

        public VerletNet scarfSimulation;
        public float scarfTimer = 0;
        public float scarfTimerSmooth = 0;

        public void UpdateVisualVariables()
        {
            Vector2 playerAcceleration = Player.velocity - lastVelocity;
            springyVelocityTracker.Update(Player.velocity, playerAcceleration);
            hairLikeVelocityTracker.Update(Player.velocity, playerAcceleration);

            if (scarfSimulation is null)
            {
                scarfSimulation = new VerletNet();
                scarfSimulation.AddChain(new VerletPoint(Player.Center, true), new VerletPoint(Player.Center - Vector2.UnitX * Player.direction * 120f), 10);
            }    
            if (scarfSimulation != null)
            {
                int index = 0;

                //Teleported, reset
                if (!scarfSimulation.points[0].position.WithinRange(Player.Center, 400f))
                {
                    scarfSimulation = new VerletNet();
                    scarfSimulation.AddChain(new VerletPoint(Player.Center, true), new VerletPoint(Player.Center - Vector2.UnitX * Player.direction * 120f), 10);
                }

                scarfSimulation.points[0].position = Player.Center;


                bool needsFlip = (Player.Center.X - scarfSimulation.points[^1].position.X).NonZeroSign() != Player.direction;
                bool falling = Player.velocity.Y > 5;

                foreach (VerletPoint point in scarfSimulation.points)
                {
                    //A directional push towards the player's back. More or less strong depending on how fast the player is moving
                    Vector2 customGravity = -Vector2.UnitX * Player.direction * ((needsFlip ? 1.4f : 0.5f) + Utils.GetLerpValue(2f, 9f, Math.Abs(hairLikeVelocityTracker.value.X), true));

                    //Baseline gravity
                    customGravity.Y -= 0.02f;

                    //Alternating sinewave that gets stronger towards the end of the scarf
                    customGravity.Y += 0.4f * (float)Math.Sin(index * 0.6f + scarfTimer * 0.03f) * 0.5f * (1f + (float)Math.Pow(index / (float)(scarfSimulation.points.Count - 1), 2.2f));

                    //Make the trail sine laterally
                    if (falling)
                    {
                        Vector2 fallingGravity = new Vector2(0f, customGravity.Y * 0.3f + Player.velocity.Y * 0.1f);

                        fallingGravity.X = (float)Math.Sin(index * 0.8f + scarfTimerSmooth * 0.03f) * 5f * MathF.Sin(index / (float)(scarfSimulation.points.Count - 1) * MathHelper.Pi);

                        customGravity = Vector2.Lerp(customGravity, fallingGravity, Utils.GetLerpValue(5, 10, Player.velocity.Y, true));
                    }

                    point.customGravity = customGravity;

                    index++;
                }

                scarfSimulation.Update(3, 0f, 0.7f);
                scarfTimer++;
                scarfTimerSmooth++;

                if (scarfTimer > 150)
                    scarfTimer = 0;           
            }

            lastVelocity = Player.velocity;
            lastDirection = Player.direction;

            if (spinEffects.Count > 0)
            {
                if (!Player.mount.Active)
                {
                    Player.fullRotationOrigin = Player.Size * 0.5f;
                    float rotation = 0;
                    foreach (ISpinEffect spin in spinEffects)
                        spin.Update(ref rotation, Player.direction);

                    Player.fullRotation = rotation;

                    spinEffects.RemoveAll(i => i.Finished);
                    if (spinEffects.Count == 0)
                        Player.fullRotation = 0;
                }
                //Reset if mounting while spinning
                else
                {
                    spinEffects.Clear();
                    Player.fullRotation = 0;
                }
            }
        }

        public List<ISpinEffect> spinEffects = new List<ISpinEffect>();

        public void AddSpinEffect(ISpinEffect effect, bool clearLowerPriority = false)
        {
            if (Player.mount.Active)
                return;

            spinEffects.Add(effect);
            if (clearLowerPriority)
                spinEffects.RemoveAll(i => i.Priority < effect.Priority);
            spinEffects.OrderBy(i => i.Priority);
        }
    }

    public interface ISpinEffect
    {
        public bool Finished { get; set; }
        public void Update(ref float rotation, int direction);

        public float Priority { get; set; }
    }

    public class BasicPirouetteEffect : ISpinEffect
    {
        public bool Finished { get; set; }
        public float Priority { get; set; }
   
        public int time;
        public float totalTime;
        public float spinCount;
        public float spinPower;

        public BasicPirouetteEffect(int duration, float spinCount = 1f, float spinPower = 0.3f)
        {
            this.time = 0;
            this.totalTime = duration;
            this.spinCount = spinCount;
            this.spinPower = spinPower;
        }

        public void Update(ref float rotation, int direction)
        {
            rotation -= MathHelper.TwoPi * spinCount * (1 - MathF.Pow(time / totalTime, spinPower)) * direction;
            time++;
            if (time >= totalTime)
                Finished = true;
        }
    }

    public class DampedVelocityTracker
    {
        public Vector2 value;
        public Vector2 velocity;
        public Vector2? lastTarget = null;

        public float damping;
        public float frequency;
        public float reaction;

        public float k1;
        public float k2;
        public float k3;
        public float maxValues;

        /// <summary>
        /// An utility class that calculates a value accelerating towards a goal in a damped / springlike fashion
        /// </summary>
        /// <param name="damping">How fast the spring comes to settle down<br/>
        /// If the value is zero, the system will never stop oscillating and the value is undamped<br/>
        /// If the value is between zero and one, the system will end up settling down<br/>
        /// If the value is higher than one, the system will not oscillate and immediately settle to the target value</param>
        /// <param name="frequency">The frequency at which the oscillation happens, and the speed at which the curve responds.
        /// Basically, scales the curve horizontally, without changing its vertical shape</param>
        /// <param name="reaction">Controls the initial response of the system.<br/>When = 0, the system takes a while to adapt.<br/> When positive, the response will be more immediate, and if superior to 1, it will overshoot the target.<br/> If inferior to 0, the system will start with an initial windup</param>
        /// <param name="maxValues">The maximum value of the tracker</param>
        public DampedVelocityTracker(float damping, float frequency, float reaction, float maxValues = 80)
        {
            this.damping = damping;
            this.frequency = frequency;
            this.reaction = reaction;
            this.maxValues = maxValues;

            RecalculateFactors();
        }

        //Separated for debug purposes
        public void RecalculateFactors()
        {
            k1 = damping / (MathHelper.Pi * frequency);
            k2 = 1 / MathF.Pow(MathHelper.TwoPi * frequency, 2f);
            k3 = (reaction * damping) / (MathHelper.TwoPi * frequency);
        }

        /// <summary>
        /// Updates the simulation
        /// </summary>
        /// <param name="target">This is the target value our spring wants to reach</param>
        /// <param name="targetAcceleration">This is the difference between the last position of our target and the current one<br/>
        /// Leave empty if you want to use the last recorded difference in targets </param>
        public void Update(Vector2 target, Vector2? targetAcceleration = null)
        {
            if (value.HasNaNs() || value.HasInfinities())
                value = target;
            if (velocity.HasNaNs() || value.HasInfinities())
                velocity = Vector2.Zero;

            if (!targetAcceleration.HasValue)
            {
                if (lastTarget == null)
                    targetAcceleration = Vector2.Zero;
                else
                    targetAcceleration = target - lastTarget.Value;
            }

            value += velocity;
            velocity += (target + k3 * targetAcceleration.Value - value - k1 * velocity) / k2;

            value.X = Math.Clamp(value.X, -maxValues, maxValues);
            value.Y = Math.Clamp(value.Y, -maxValues, maxValues);
            velocity.X = Math.Clamp(velocity.X, -maxValues, maxValues);
            velocity.Y = Math.Clamp(velocity.Y, -maxValues, maxValues);

            lastTarget = target;
        }
    }
}

