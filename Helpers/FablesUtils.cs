using CalamityFables.Content.Items.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Cooldowns;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using static System.Net.Mime.MediaTypeNames;
using static Terraria.GameContent.FontAssets;
using static Terraria.Player;
using Terraria.GameContent.Creative;

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        //TODO split this eventually.
        public static int GetFrameStartMana(this Player player) => player.GetModPlayer<FablesPlayer>().FrameStartMana;
        public static float FrameStartMinionSlots(this Player player) => player.GetModPlayer<FablesPlayer>().FrameStartMinionSlots;

        public static void DisableProjectileDye()
        {
            if (Main.CurrentDrawnEntityShader != -1 && Main.CurrentDrawnEntityShader != 0)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            }
        }

        public static void ReEnableProjectileDye()
        {
            if (Main.CurrentDrawnEntityShader != -1 && Main.CurrentDrawnEntityShader != 0)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            }
        }

        public static void LogILEpicFail(string name, string reason)
        {
            CalamityFables.Instance.Logger.Warn($"IL edit \"{name}\" failed! {reason}");
            SoundEngine.PlaySound(SoundID.DoorClosed with { PlayOnlyIfFocused = false , Type = SoundType.Sound});
            SoundEngine.PlaySound(SoundID.Thunder with { PlayOnlyIfFocused = false, Type = SoundType.Sound });
        }

        /// <summary>
        /// Returns the damage multiplier enemies have, both from the world difficulty and the difficulty slider from journey mode
        /// </summary>
        public static float EnemyDamageMultiplier
        {
            get
            {
                float damageMult = Main.GameModeInfo.EnemyDamageMultiplier;
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    var power = CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>();
                    if (power.GetIsUnlocked())
                        damageMult = power.StrengthMultiplierToGiveNPCs;
                }
                return damageMult;
            }
        }

        public static T GetOrDefault<T>(this TagCompound tag, string key) => tag.TryGet<T>(key, out T result) ? result : default;

        public static float DistanceTo(this Point point1, Point point2) => (float)Math.Sqrt(Math.Pow(Math.Abs(point1.X - point2.X), 2f) + Math.Pow(Math.Abs(point1.Y - point2.Y), 2f));

        public static float AngleLerpDirectional(float from, float to, float progress, bool clockwise)
        {
            from = from.Modulo(MathHelper.TwoPi);
            to = to.Modulo(MathHelper.TwoPi);


            if (clockwise)
            {
                // Clockwise
                if (from < to)
                    to -= MathHelper.TwoPi;
            }
            else
            {
                // Counter-clockwise
                if (from > to)
                    to += MathHelper.TwoPi;
            }

            return from + (to - from) * progress;
        }

        public static float AngleTowardsDirectional(this float currentAngle, float targetAngle, float maxChange, bool clockwise)
        {
            currentAngle = currentAngle.Modulo(MathHelper.TwoPi);
            targetAngle = targetAngle.Modulo(MathHelper.TwoPi);

            if (clockwise)
            {
                // Clockwise
                if (currentAngle < targetAngle)
                    targetAngle -= MathHelper.TwoPi;
            }
            else
            {
                // Counter-clockwise
                if (currentAngle > targetAngle)
                    targetAngle += MathHelper.TwoPi;
            }

            return currentAngle + MathHelper.Clamp(targetAngle - currentAngle, 0f - maxChange, maxChange);
        }

        /// <summary>
        /// Turns any angle to be between -pi and pi
        /// </summary>
        /// <returns></returns>
        public static float NormalizeAngle(this float angle) => (angle + MathHelper.Pi).Modulo(MathHelper.TwoPi) - MathHelper.Pi;

        public static Vector2 Normalized(this Vector2 vector) => Vector2.Normalize(vector);

        /// <summary>
        /// Gives the angle in radians between two other angles
        /// This function exists for vectors but somehow is missing for floats
        /// </summary>
        /// <param name="angle">Your source angle</param>
        /// <param name="otherAngle">The target angle</param>
        /// <returns></returns>
        public static float AngleBetween(this float angle, float otherAngle) => ((otherAngle - angle) + MathHelper.Pi).Modulo(MathHelper.TwoPi) - MathHelper.Pi;

        /// <summary>
        /// Calculates the shortest distance between a point and a line that passes through 2 specified points
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <returns></returns>
        public static float ShortestDistanceToLine(this Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 lineVector = lineEnd - lineStart;
            Vector2 perpendicular = lineVector.RotatedBy(MathHelper.PiOver2);
            Vector2 pointToOrigin = point - lineStart;

            return (float)Math.Abs((pointToOrigin.X * perpendicular.X + pointToOrigin.Y * perpendicular.Y)) / perpendicular.Length();
        }

        /// <summary>
        /// Gets the closest point on a line from a point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        /// <returns></returns>
        public static Vector2 ClosestPointOnLine2(this Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 perpendicular = (lineEnd - lineStart).RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
            float distanceToLine = point.ShortestDistanceToLine(lineStart, lineEnd);
            float lineSide = Math.Sign((point.X - lineStart.X) * (-lineEnd.Y + lineStart.Y) + (point.Y - lineStart.Y) * (lineEnd.X - lineStart.X));

            return point + distanceToLine * lineSide * perpendicular;
        }

        /// <summary>
        /// Gives the *real* modulo of a divided by a divisor.
        /// This method is necessary because the % operator in c# keeps the sign of the dividend, making it Fake as Fuck.
        /// </summary>
        /// <param name="dividend"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static float Modulo(this float dividend, float divisor)
        {
            return (dividend % divisor + divisor) % divisor;
        }

        public static long Modulo(this long dividend, long divisor)
        {
            return (dividend % divisor + divisor) % divisor;
        }

        public static int Modulo(this int dividend, int divisor)
        {
            return (dividend % divisor + divisor) % divisor;
        }

        /// <summary>
        /// Just like Math.Sign() but only returns 1 or -1. 0 is counted as 1
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int NonZeroSign(this float value) => Math.Sign(value) >= 0 ? 1 : -1;

        #region Easings

        /// <summary>
        /// Gets a value from 0 to 1 and returns an eased value.
        /// </summary>
        /// <param name="amount">How far along the easing are we</param>
        /// <returns></returns>
        public delegate float EasingFunction(float amount, float degree);

        public static float ConstantEasing(float amount, float degree = 1f) => 1f;
        public static float LinearEasing(float amount, float degree = 1f) => amount;
        //Sines
        public static float SineInEasing(float amount, float degree = 1f) => 1f - (float)Math.Cos(amount * MathHelper.Pi / 2f);
        public static float SineOutEasing(float amount, float degree = 1f) => (float)Math.Sin(amount * MathHelper.Pi / 2f);
        public static float SineInOutEasing(float amount, float degree = 1f) => -((float)Math.Cos(amount * MathHelper.Pi) - 1) / 2f;
        public static float SineBumpEasing(float amount, float degree = 1f) => (float)Math.Sin(amount * MathHelper.Pi);
        //Polynomials
        public static float PolyInEasing(float amount, float degree = 2f) => (float)Math.Pow(amount, degree);
        public static float PolyOutEasing(float amount, float degree = 2f) => 1f - (float)Math.Pow(1f - amount, degree);
        public static float PolyInOutEasing(float amount, float degree = 2f) => amount < 0.5f ? (float)Math.Pow(2, degree - 1) * (float)Math.Pow(amount, degree) : 1f - (float)Math.Pow(-2 * amount + 2, degree) / 2f;
        //Exponential
        public static float ExpInEasing(float amount, float degree = 1f) => amount == 0f ? 0f : (float)Math.Pow(2, 10f * amount - 10f);
        public static float ExpOutEasing(float amount, float degree = 1f) => amount == 1f ? 1f : 1f - (float)Math.Pow(2, -10f * amount);
        public static float ExpInOutEasing(float amount, float degree = 1f) => amount == 0f ? 0f : amount == 1f ? 1f : amount < 0.5f ? (float)Math.Pow(2, 20f * amount - 10f) / 2f : (2f - (float)Math.Pow(2, -20f * amount - 10f)) / 2f;
        //circular
        public static float CircInEasing(float amount, float degree = 1f) => (1f - (float)Math.Sqrt(1 - Math.Pow(amount, 2f)));
        public static float CircOutEasing(float amount, float degree = 1f) => (float)Math.Sqrt(1 - Math.Pow(amount - 1f, 2f));
        public static float CircInOutEasing(float amount, float degree = 1f) => amount < 0.5 ? (1f - (float)Math.Sqrt(1 - Math.Pow(2 * amount, 2f))) / 2f : ((float)Math.Sqrt(Math.Max(1 - Math.Pow(-2f * amount - 2f, 2f), 0)) + 1f) / 2f;


        public static float EaseInOutBack(float amount, float degree)
        {
            float c1 = 1.70158f;
            float c2 = c1 * 1.525f;

            return amount < 0.5
              ? (float)(Math.Pow(2 * amount, 2) * ((c2 + 1) * 2 * amount - c2)) / 2f
              : (float)(Math.Pow(2 * amount - 2, 2) * ((c2 + 1) * (amount * 2 - 2) + c2) + 2) / 2f;
        }


        /// <summary>
        /// This represents a part of a piecewise function.
        /// </summary>
        public struct CurveSegment
        {
            /// <summary>
            /// This is the type of easing used in the segment
            /// </summary>
            public EasingFunction easing;
            /// <summary>
            /// This indicates when the segment starts on the animation
            /// </summary>
            public float startingX;
            /// <summary>
            /// This indicates what the starting height of the segment is
            /// </summary>
            public float startingHeight;
            /// <summary>
            /// This represents the elevation shift that will happen during the segment. Set this to 0 to turn the segment into a flat line.
            /// Usually this elevation shift is fully applied at the end of a segment, but the sinebump easing type makes it be reached at the apex of its curve.
            /// </summary>
            public float elevationShift;
            /// <summary>
            /// This is the degree of the polynomial, if the easing mode chosen is a polynomial one
            /// </summary>
            public float degree;
            public CurveSegment(EasingFunction MODE, float startX, float startHeight, float elevationShift, float degree = 1)
            {
                easing = MODE;
                startingX = startX;
                startingHeight = startHeight;
                this.elevationShift = elevationShift;
                this.degree = degree;
            }
        }

        /// <summary>
        /// This gives you the height of a custom piecewise function for any given X value, so you may create your own complex animation curves easily.
        /// The X value is automatically clamped between 0 and 1, but the height of the function may go beyond the 0 - 1 range
        /// </summary>
        /// <param name="progress">How far along the curve you are. Automatically clamped between 0 and 1</param>
        /// <param name="segments">An array of curve segments making up the full animation curve</param>
        /// <returns></returns>
        public static float PiecewiseAnimation(float progress, params CurveSegment[] segments)
        {
            if (segments.Length == 0)
                return 0f;

            if (segments[0].startingX != 0) //If for whatever reason you try to not play by the rules, get fucked
                segments[0].startingX = 0;

            progress = MathHelper.Clamp(progress, 0f, 1f); //Clamp the progress
            float ratio = 0f;

            for (int i = 0; i <= segments.Length - 1; i++)
            {
                CurveSegment segment = segments[i];
                float startPoint = segment.startingX;
                float endPoint = 1f;

                if (progress < segment.startingX) //Too early. This should never get reached, since by the time you'd have gotten there you'd have found the appropriate segment and broken out of the for loop
                    continue;

                if (i < segments.Length - 1)
                {
                    if (segments[i + 1].startingX <= progress) //Too late
                        continue;
                    endPoint = segments[i + 1].startingX;
                }

                float segmentLenght = endPoint - startPoint;
                float segmentProgress = (progress - segment.startingX) / segmentLenght; //How far along the specific segment
                ratio = segment.startingHeight;

                //Failsafe because somehow it can fail? what
                if (segment.easing != null)
                    ratio += segment.easing(segmentProgress, segment.degree) * segment.elevationShift;

                else
                    ratio += LinearEasing(segmentProgress, segment.degree) * segment.elevationShift;

                break;
            }
            return ratio;
        }
        #endregion

        /// <summary>
        /// Returns a color lerp that supports multiple colors.
        /// </summary>
        /// <param name="increment">The 0-1 incremental value used when interpolating.</param>
        /// <param name="colors">The various colors to interpolate across.</param>
        /// <returns></returns>
        public static Color MulticolorLerp(float increment, params Color[] colors)
        {
            increment %= 0.999f;
            int currentColorIndex = (int)(increment * colors.Length);
            Color currentColor = colors[currentColorIndex];
            Color nextColor = colors[(currentColorIndex + 1) % colors.Length];
            return Color.Lerp(currentColor, nextColor, increment * colors.Length % 1f);
        }

        /// <summary>
        /// Draws an item in the inventory with a new texture to replace a previous one.
        /// Useful in situations where for example, a different sprite is used for the "real" inventory sprite so it may appear when the player is using it.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="newTexture"></param>
        /// <param name="originalSize"></param>
        /// <param name="position"></param>
        /// <param name="drawColor"></param>
        /// <param name="origin"></param>
        /// <param name="scale"></param>
        public static void DrawNewInventorySprite(this SpriteBatch spriteBatch, Texture2D newTexture, Vector2 originalSize, Vector2 position, Color drawColor, Vector2 origin, float scale, Vector2? offset = null)
        {
            Vector2 extraOffset;
            if (offset == null)
                extraOffset = Vector2.Zero;
            else
                extraOffset = offset.GetValueOrDefault();

            float largestDimensionOriginal = Math.Max(originalSize.X, originalSize.Y);
            float largestDimensionNew = Math.Max(newTexture.Width, newTexture.Height);

            //Scale the sprite so it will account for the dimension of the new sprite if it is larger than the old one (As in, we need to scale down the scale or else it will be too large)
            float scaleRatio = Math.Min(largestDimensionOriginal / largestDimensionNew, 1);

            //Offset the jellyfish sprite properly, since the fishing rod is larger than the jellyfish (Jellyfish width : 28px, Fishing rod width : 42)
            Vector2 positionOffset = Vector2.Zero;

            if (originalSize.X > newTexture.Width)
                positionOffset.X = (originalSize.X - newTexture.Width) / 2f;

            positionOffset *= scale;

            spriteBatch.Draw(newTexture, position + positionOffset + extraOffset, null, drawColor, 0f, origin, scale * scaleRatio, 0, 0);
        }

        public static float AngleAtPoint(Vector2 point)
        {
            return (float)Math.Atan2(point.Y, point.X);
        }

        public static float RadiusAtEllipsePoint(float horizontalSemiAxis, float verticalSemiAxis, Vector2 point)
        {
            float angle = AngleAtPoint(point);
            return (horizontalSemiAxis * verticalSemiAxis) / (float)Math.Sqrt(Math.Pow(horizontalSemiAxis, 2) * Math.Pow(Math.Sin(angle), 2) + Math.Pow(verticalSemiAxis, 2) * Math.Pow(Math.Cos(angle), 2));
        }
        public static float RadiusAtEllipseAngle(float horizontalSemiAxis, float verticalSemiAxis, float angle)
        {
            return (horizontalSemiAxis * verticalSemiAxis) / (float)Math.Sqrt(Math.Pow(horizontalSemiAxis, 2) * Math.Pow(Math.Sin(angle), 2) + Math.Pow(verticalSemiAxis, 2) * Math.Pow(Math.Cos(angle), 2));
        }


        public delegate void ChromaAberrationDelegate(Vector2 offset, Color colorMult);
        //Thanks spirit <3
        /// <summary>
        /// Draws a chromatic abberation effect.
        /// </summary>
        /// <param name="direction">The direction of the abberation</param>
        /// <param name="strength">The strenght of the abberation</param>
        /// <param name="action">The draw call itself.</param>
        public static void DrawChromaticAberration(Vector2 direction, float strength, ChromaAberrationDelegate drawCall)
        {
            for (int i = -1; i <= 1; i++)
            {
                Color aberrationColor = Color.White;
                switch (i)
                {
                    case -1:
                        aberrationColor = new Color(255, 0, 0, 0);
                        break;
                    case 0:
                        aberrationColor = new Color(0, 255, 0, 0);
                        break;
                    case 1:
                        aberrationColor = new Color(0, 0, 255, 0);
                        break;
                }

                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * i;
                offset *= strength;

                drawCall.Invoke(offset, aberrationColor);
            }
        }

        public static Vector2 GetArcVel(Vector2 startingPos, Vector2 targetPos, float gravity, float? minArcHeight = null, float? maxArcHeight = null, float? maxXvel = null, float? heightAboveTarget = null)
        {
            return GetArcVel(startingPos, targetPos, gravity, out _, minArcHeight, maxArcHeight, maxXvel, heightAboveTarget);
        }

        public static Vector2 GetArcVel(Vector2 startingPos, Vector2 targetPos, float gravity, out float maxHeight, float? minArcHeight = null, float? maxArcHeight = null, float? maxXvel = null, float? heightAboveTarget = null)
        {
            Vector2 DistanceToTravel = targetPos - startingPos;
            maxHeight = DistanceToTravel.Y - (heightAboveTarget ?? 0);

            if (minArcHeight != null)
                maxHeight = Math.Min(maxHeight, -(float)minArcHeight);

            if (maxArcHeight != null)
                maxHeight = Math.Max(maxHeight, -(float)maxArcHeight);

            float TravelTime;
            float neededYvel;

            if (maxHeight <= 0)
            {
                neededYvel = -(float)Math.Sqrt(-2 * gravity * maxHeight);
                TravelTime = (float)Math.Sqrt(-2 * maxHeight / gravity) + (float)Math.Sqrt(2 * Math.Max(DistanceToTravel.Y - maxHeight, 0) / gravity); //time up, then time down
            }
            else
            {
                neededYvel = 0;
                TravelTime = (-neededYvel + (float)Math.Sqrt(Math.Pow(neededYvel, 2) - (4 * -DistanceToTravel.Y * gravity / 2))) / (gravity); //time down
            }

            if (maxXvel != null)
                return new Vector2(MathHelper.Clamp(DistanceToTravel.X / TravelTime, -(float)maxXvel, (float)maxXvel), neededYvel);

            return new Vector2(DistanceToTravel.X / TravelTime, neededYvel);
        }

        public static float ArcLenght(float angle, float radius) => angle * radius;
        public static float ArcAngle(float lenght, float radius) => lenght / radius;

        /// <summary>
        /// Moves a position in steps of 16 pixels ahead, stopping if there are tiles in the way
        /// Useful to shoot a projectile a bit ahead without the issue that arises from shooting straight agaisnt a wakk
        /// </summary>
        /// <param name="position">The default position of the projectile</param>
        /// <param name="direction">The direction in which to advance</param>
        /// <param name="distance">How far ahead should the projectile be displaced</param>
        public static void ShiftShootPositionAhead(ref Vector2 position, Vector2 direction, int distance)
        {
            while (distance > 0)
            {
                int step = Math.Min(distance, 16);
                if (Collision.CanHitLine(position, 0, 0, position + direction * step, 0, 0))
                    position += direction * step;
                else
                    break;
                distance -= step;
            }
        }

        public static bool AABBvCircle(Rectangle rectangle, Vector2 center, float radius)
        {
            float nearestX = Math.Max(rectangle.X, Math.Min(center.X, rectangle.X + rectangle.Size().X));
            float nearestY = Math.Max(rectangle.Y, Math.Min(center.Y, rectangle.Y + rectangle.Size().Y));
            return (new Vector2(center.X - nearestX, center.Y - nearestY)).Length() < radius;
        }

        public static bool CounterClockwisePoints(Vector2 point1, Vector2 point2, Vector2 point3)
        {
            return (point3.Y - point1.Y) * (point2.X - point1.X) > (point2.Y - point1.Y) * (point3.X - point1.X);
        }

        public static bool LinesIntersect(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            return CounterClockwisePoints(line1Start, line2Start, line2End) != CounterClockwisePoints(line1End, line2Start, line2End) && CounterClockwisePoints(line1Start, line1End, line2Start) != CounterClockwisePoints(line1Start, line1End, line2End);
        }

        /// <summary>
        /// Draws text with an eight-way one-pixel offset border.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="baseDrawPosition"></param>
        /// <param name="main"></param>
        /// <param name="border"></param>
        /// <param name="scale"></param>
        public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float scale = 1f)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 drawPosition = baseDrawPosition + new Vector2(x, y);
                    if (x == 0 && y == 0)
                        continue;

                    DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, drawPosition, border, 0f, default, scale, SpriteEffects.None, 0f);
                }
            }
            DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, baseDrawPosition, main, 0f, default, scale, SpriteEffects.None, 0f);
        }
        public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float rotation, float scale = 1f)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 drawPosition = baseDrawPosition + new Vector2(x, y).RotatedBy(rotation);
                    if (x == 0 && y == 0)
                        continue;

                    DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, drawPosition, border, rotation, default, scale, SpriteEffects.None, 0f);
                }
            }
            DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, baseDrawPosition, main, rotation, default, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Determines the angular distance between two vectors based on dot product comparisons. This method ensures underlying normalization is performed safely.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        public static float AngleBetween(this Vector2 v1, Vector2 v2) => (float)Math.Acos(Math.Clamp(Vector2.Dot(v1.SafeNormalize(Vector2.Zero), v2.SafeNormalize(Vector2.Zero)), -1f, 1f));

        #region Euler
        /// <summary>
        /// Updates a position using dampened spring physics
        /// </summary>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <param name="goal"></param>
        /// <param name="frequency">The frequency at which the oscillation happens, and the speed at which the curve responds.
        /// Basically, scales the curve horizontally, without changing its vertical shape</param>
        /// <param name="damping">How fast the spring comes to settle down</param>
        /// <param name="responsiveness">Controls the initial response of the system. When = 0, the system takes a while to adapt. When positive, the response will be more immediate, and if superior to 1, it will overshoot the target. If inferior to 0, the system will start with an initial windup</param>
        public static void SemiImplicitEuler(ref Vector2 position, ref Vector2 velocity, Vector2 goal, Vector2 oldGoal, float frequency, float damping, float responsiveness)
        {
            //Get constraints
            float k1 = damping / (MathHelper.Pi * frequency);
            float k2 = 1 / (float)Math.Pow(MathHelper.TwoPi * frequency, 2f);
            float k3 = responsiveness * damping / (MathHelper.TwoPi * frequency);

            //DOESNT WORK!! k2 is super low and so it goes ballistic :(
            SemiImplicitEulerComputed(ref position, ref velocity, goal, oldGoal, k1, k2, k3);
        }

        public static void SemiImplicitEulerComputed(ref Vector2 position, ref Vector2 velocity, Vector2 goal, Vector2 oldGoal, float k1, float k2, float k3)
        {
            Vector2 goalVelocity = goal - oldGoal;

            //Update the position by adding the velocity to it
            position = position + velocity;

            Vector2 acceleration = (goal + k3 * goalVelocity - position - k1 * velocity) / k2;

            //Update the velocity by adding the acceleration to it
            velocity = velocity + acceleration;
        }
        #endregion

        /// <summary>
        /// Directly retrieves the best pickaxe power of the player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static int GetBestPickPower(this Player player)
        {
            int highestPickPower = 35; //35% if you have no pickaxes.
            for (int item = 0; item < Main.InventorySlotsTotal; item++)
            {
                if (player.inventory[item].pick <= 0)
                    continue;
                if (player.inventory[item].pick > highestPickPower)
                {
                    highestPickPower = player.inventory[item].pick;
                }
            }

            return highestPickPower;
        }

        public static bool InventoryHas(this Player player, params int[] items)
        {
            return player.inventory.Any(item => items.Contains(item.type));
        }

        public static Vector3 Vec3(this Vector2 vector) => new Vector3(vector.X, vector.Y, 0);

        public static Vector2 Vec2(this Vector3 vector) => new Vector2(vector.X, vector.Y);

        public static Vector3 ScreenCoord(this Vector3 vector) => new Vector3(-1 + vector.X / Main.screenWidth * 2, (-1 + vector.Y / Main.screenHeight * 2f) * -1, 0);

        public static T[] FastUnion<T>(this T[] front, T[] back)
        {
            T[] combined = new T[front.Length + back.Length];

            Array.Copy(front, combined, front.Length);
            Array.Copy(back, 0, combined, front.Length, back.Length);

            return combined;
        }

        #region Movement and Controls
        public static bool ControlsEnabled(this Player player, bool allowWoFTongue = false)
        {
            if (player.CCed) // Covers frozen (player.frozen), webs (player.webbed), and Medusa (player.stoned)
                return false;
            if (player.tongued && !allowWoFTongue)
                return false;
            return true;
        }

        public static bool StandingStill(this Player player, float velocity = 0.05f) => player.velocity.Length() < velocity;

        /// <summary>
        /// Checks if the player is ontop of solid ground. May also check for solid ground for X tiles in front of them
        /// </summary>
        /// <param name="player">The Player whose position is being checked</param>
        /// <param name="solidGroundAhead">How many tiles in front of the player to check</param>
        /// <param name="airExposureNeeded">How many tiles above every checked tile are checked for non-solid ground</param>
        public static bool CheckSolidGround(this Player player, int solidGroundAhead = 0, int airExposureNeeded = 0)
        {
            if (player.velocity.Y != 0) //Player gotta be standing still in any case
                return false;

            Tile checkedTile;
            bool ConditionMet = true;

            for (int i = 0; i <= solidGroundAhead; i++) //Check i tiles in front of the player
            {
                ConditionMet = Main.tile[(int)player.Center.X / 16 + player.direction * i, (int)(player.position.Y + (float)player.height - 1f) / 16 + 1].IsTileSolidGround();
                if (!ConditionMet)
                    return ConditionMet;

                for (int j = 1; j <= airExposureNeeded; j++) //Check j tiles ontop of each checked tiles for non-solid ground
                {
                    checkedTile = Main.tile[(int)player.Center.X / 16 + player.direction * i, (int)(player.position.Y + (float)player.height - 1f) / 16 + 1 - j];

                    ConditionMet = !(checkedTile != null && checkedTile.HasUnactuatedTile && Main.tileSolid[checkedTile.TileType]); //IsTileSolidGround minus the ground part, to avoid platforms and other half solid tiles messing it up
                    if (!ConditionMet)
                        return ConditionMet;
                }
            }
            return ConditionMet;
        }
        #endregion

        public static Item ActiveItem(this Player player) => Main.mouseItem.IsAir ? player.HeldItem : Main.mouseItem;


        public static void MinionAntiClump(this Projectile projectile, float pushForce = 0.05f)
        {
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];
                // Short circuits to make the loop as fast as possible
                if (!otherProj.active || otherProj.owner != projectile.owner || !otherProj.minion || k == projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == projectile.type;
                float taxicabDist = Math.Abs(projectile.position.X - otherProj.position.X) + Math.Abs(projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDist < projectile.width)
                {
                    if (projectile.position.X < otherProj.position.X)
                        projectile.velocity.X -= pushForce;
                    else
                        projectile.velocity.X += pushForce;

                    if (projectile.position.Y < otherProj.position.Y)
                        projectile.velocity.Y -= pushForce;
                    else
                        projectile.velocity.Y += pushForce;
                }
            }
        }
        /// <summary>
        /// Tells you if 3 points are in order aka point 2 is between point 1 and 3
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static bool ArePointsInOrder(float point1, float point2, float point3)
        {
            return (point1 < point2 && point2 < point3) || (point1 > point2 && point2 > point3);
        }

        public static void SimulateMortar(Vector2 arcVel, float gravity, Vector2 center, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                Dust.QuickDust(center, Color.Red);

                center += arcVel;
                arcVel += Vector2.UnitY * gravity;
            }
        }


        public static string GetMapChestName(string baseName, int x, int y)
        {
            // Bounds check.
            if (!WorldGen.InWorld(x, y, 2))
                return baseName;

            Tile tile = Main.tile[x, y];
            int left = x;
            int top = y;
            if (tile.TileFrameX % 36 != 0)
                left--;
            if (tile.TileFrameY != 0)
                top--;

            int chest = Chest.FindChest(left, top);

            // Valid chest index check.
            if (chest < 0)
                return baseName;

            string name = baseName;

            // Concatenate the chest's custom name if it has one.
            if (!string.IsNullOrEmpty(Main.chest[chest].name))
                name += $": {Main.chest[chest].name}";

            return name;
        }

        /// <summary>
        /// Vanilla code does not run for modded chests. Which is weird but thats a good thing for custom behavior. This utility method handles all the boilerplate for chests and can even handle closed and opened chests
        /// </summary>
        public static bool ChestRightClick(int i, int j, bool isLocked = false, SoundStyle? openSound = null, SoundStyle? closeSound = null)
        {
            Player player = Main.LocalPlayer;

            #region Cancel existing stuff
            Main.CancelClothesWindow(true);
            // If the player right clicked the chest while editing a sign, finish that up
            if (player.sign >= 0)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                player.sign = -1;
                Main.editSign = false;
                Main.npcChatText = "";
            }
            // If the player right clicked the chest while editing a chest, finish that up
            if (Main.editChest)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = "";
            }
            // If the player right clicked the chest after changing another chest's name, finish that up
            if (player.editedChestName)
            {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f, 0f, 0f, 0, 0, 0);
                player.editedChestName = false;
            }
            #endregion

            //In multiplayer we have to request the chest status before opening it, so the logic is simplfiied there
            if (Main.netMode == NetmodeID.MultiplayerClient && !isLocked)
            {
                // Right clicking the chest you currently have open closes it. This counts as interaction.
                if (i == player.chestX && j == player.chestY && player.chest >= 0)
                {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(closeSound ?? SoundID.MenuClose);
                }

                // Right clicking this chest opens it if it's not already open. This counts as interaction.
                else
                {
                    NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, i, (float)j, 0f, 0f, 0, 0, 0);
                    Main.stackSplit = 600;
                }
                return true;
            }

            else
            {
                if (isLocked)
                {
                    // If you right click the locked chest and you can unlock it, it unlocks itself but does not open. This counts as interaction.
                    if (Chest.Unlock(i, j))
                    {
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, (float)i, (float)j);
                        }
                        return true;
                    }
                }
                else
                {
                    player.piggyBankProjTracker.Clear();
                    player.voidLensChest.Clear();
                    int chest = Chest.FindChest(i, j);

                    if (chest >= 0)
                    {
                        Main.stackSplit = 600;

                        // If you right click the same chest you already have open, it closes. This counts as interaction.
                        if (chest == player.chest)
                        {
                            player.chest = -1;
                            SoundEngine.PlaySound(closeSound ?? SoundID.MenuClose);
                        }

                        // If you right click this chest when you have a different chest selected, that one closes and this one opens. This counts as interaction.
                        else
                        {
                            player.OpenChest(i, j, chest);
                            SoundEngine.PlaySound(player.chest < 0 ? (openSound ?? SoundID.MenuOpen) : SoundID.MenuTick); //Different sound if opening a new chest or switching chests
                        }

                        Recipe.FindRecipes();
                        return true;
                    }
                }
            }

            // This only occurs when the chest is locked and cannot be unlocked. You did not interact with the chest.
            return false;
        }


        public static void ChestMouseOver<T>(int i, int j, bool flipped = false) where T : ModItem
        {
            Player player = Main.LocalPlayer;
            int chest = Chest.FindChest(i, j);
            if (chest == -1)
                return;

            //Named chests display their name
            if (Main.chest[chest].name.Length > 0)
            {
                player.cursorItemIconText = Main.chest[chest].name;
                player.cursorItemIconID = -1;
            }

            //Otherwise they display an item icon
            else
            {
                player.cursorItemIconID = ModContent.ItemType<T>();
                player.cursorItemIconReversed = flipped;
                player.cursorItemIconText = "";
            }

            player.cursorItemIconEnabled = true;
            player.noThrow = 2;
        }

        public static void ChestMouseFar<T>(int i, int j) where T : ModItem
        {
            ChestMouseOver<T>(i, j);
            Player player = Main.LocalPlayer;

            //If we hover far and its not a text with text, we hide the icon
            if (player.cursorItemIconText == "")
            {
                player.cursorItemIconEnabled = false;
                player.cursorItemIconID = 0;
            }
        }

        public static bool IsChestFull(Chest chest, out float fillPercent)
        {
            int filledSlots = 0;
            for (int j = 0; j < 40; j++)
            {
                if (chest.item[j] != null && chest.item[j].type > 0 && chest.item[j].stack > 0)
                    filledSlots++;
            }

            fillPercent = filledSlots / 40f;
            return filledSlots == 40;
        }

        public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
        {
            for (var i = array.GetLowerBound(1); i <= array.GetUpperBound(1); i++)
            {
                yield return array[row, i];
            }
        }

        public static Vector2 SafeDirectionTo(this Entity entity, Vector2 target)
        {
            if (entity.Center == target)
                return Vector2.Zero;

            return entity.DirectionTo(target);
        }

        public static Vector2 SafeDirectionFrom(this Entity entity, Vector2 target) => -entity.SafeDirectionTo(target);


        public static float GetBrightness(this Color color)
        {
            Vector3 colorVec3 = color.ToVector3();
            return (colorVec3.X + colorVec3.Y + colorVec3.Z) / 3f;
        }

        public static Vector2 SafeDirectionFrom(this Vector2 Origin, Vector2 Target)
        {
            return (Origin - Target).SafeNormalize(Vector2.Zero);
        }
        public static Vector2 SafeDirectionTo(this Vector2 Origin, Vector2 Target)
        {
            return (Target - Origin).SafeNormalize(Vector2.Zero);
        }

        public static float HalfDiagonalLenght(this Entity entity) => (float)Math.Sqrt(Math.Pow(entity.width / 2, 2) + Math.Pow(entity.height / 2, 2));

        public static bool InRangeWithDiagonal(this Entity entity, Vector2 from, float reach) => entity.Distance(from) - entity.HalfDiagonalLenght() <= reach;

        public static void SetValue(this EffectParameter effectParameter, Color color) => effectParameter.SetValue(color.ToVector3());

        public static Point16 ToPoint16(this Point point) => new Point16(point);

        #region Polygon upscaling
        public delegate List<Vector2> PolygonUpscalingAlgorithm(List<Vector2> vertices, float sizeUp);

        public static List<Vector2> UpscalePolygonFromCenter(List<Vector2> vertices, float sizeUp)
        {
            List<Vector2> upscaledVertices = new List<Vector2>();

            Vector2 highestCoordinates = new Vector2(float.MinValue);
            Vector2 lowestCoordinates = new Vector2(float.MaxValue);

            foreach (Vector2 vertex in vertices)
            {
                highestCoordinates.X = Math.Max(vertex.X, highestCoordinates.X);
                highestCoordinates.Y = Math.Max(vertex.Y, highestCoordinates.Y);
                lowestCoordinates.X = Math.Min(vertex.X, lowestCoordinates.X);
                lowestCoordinates.Y = Math.Min(vertex.Y, lowestCoordinates.Y);
            }

            Vector2 center = (highestCoordinates + lowestCoordinates) / 2f;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 fromCenter = (vertices[i] - center);
                upscaledVertices.Add(center + fromCenter.SafeNormalize(Vector2.One) * (fromCenter.Length() + sizeUp));
            }
            return upscaledVertices;
        }

        public static List<Vector2> UpscalePolygonByCombiningPerpendiculars(List<Vector2> vertices, float sizeUp)
        {
            List<Vector2> upscaledVertices = new List<Vector2>();

            Vector2 previousPerpendicular = (vertices[vertices.Count - 1] - vertices[0]).RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 nextPerpendicular = (vertices[i] - vertices[(i + 1) % vertices.Count]).RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                upscaledVertices.Add(vertices[i] + (nextPerpendicular + previousPerpendicular) * sizeUp);
                previousPerpendicular = nextPerpendicular;
            }

            return upscaledVertices;
        }

        public static T Next<T>(this UnifiedRandom random, IEnumerable<T> collection)
        {
            return collection.ElementAt(random.Next(collection.Count()));
        }

        public static List<Vector2> UpscalePolygonByCombininParallels(List<Vector2> vertices, float sizeUp)
        {
            List<Vector2> upscaledVertices = new List<Vector2>();


            Vector2 previousParallel = (vertices[vertices.Count - 1] - vertices[0]).SafeNormalize(Vector2.Zero);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 nextParallel = (vertices[i] - vertices[(i + 1) % vertices.Count]).SafeNormalize(Vector2.Zero);
                upscaledVertices.Add(vertices[i] + (nextParallel + previousParallel) * sizeUp);
                previousParallel = nextParallel;
            }

            return upscaledVertices;
        }
        #endregion

        public static Rectangle BetterFrame(this Texture2D tex, int horizontalFrames = 1, int verticalFrames = 1, int frameX = 0, int frameY = 0)
        {
            int offsetX = horizontalFrames > 1 ? -2 : 0;
            int offsetY = verticalFrames > 1 ? -2 : 0;
            return tex.Frame(horizontalFrames, verticalFrames, frameX, frameY, offsetX, offsetY);
        }

        public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, bool> match)
        {
            foreach (var key in dict.Keys.ToArray()
                    .Where(key => match(key)))
                dict.Remove(key);
        }

        public static void RemoveAll<K, V>(this IDictionary<K, V> dict, Func<K, V, bool> match)
        {
            foreach (var key in dict.Keys.ToArray()
                    .Where(key => match(key, dict[key])))
                dict.Remove(key);

        }

        public static void DrawTextInBox(SpriteBatch spriteBatch, string textToDisplay, Color? boxColor = null)
        {
            if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
            {
                Vector2 boxSize = MouseText.Value.MeasureString(textToDisplay);
                //Get a "regexed" size which matches the text properly.
                //Indeed, there is some scuffery in the code that makes it so that chat tags still get accounted as extra size, so we have to use 2 different values
                //One for the displacement, which is the improper size vanilla uses, and another for the actual visual size, which is the one the textbox will use
                int numLines = Regex.Matches(textToDisplay, "\n").Count + 1; //It's inconsistent. Adding one by default makes some textboxes too large, not adding one can make some too small


                string heightCalculator = string.Concat(Enumerable.Repeat("mis nuevos los gatos \n", numLines));
                if (heightCalculator == "")
                    heightCalculator = "Ahj";


                Vector2 regexedBoxSize = new Vector2(ChatManager.GetStringSize(MouseText.Value, textToDisplay, Vector2.One).X, ChatManager.GetStringSize(MouseText.Value, heightCalculator, Vector2.One).Y);


                Vector2 textboxStart = new Vector2(Main.mouseX, Main.mouseY) + Vector2.One * 14;
                if (Main.ThickMouse)
                    textboxStart += Vector2.One * 6;

                if (!Main.mouseItem.IsAir)
                    textboxStart.X += 34;

                if (textboxStart.X + boxSize.X + 4f > (float)Main.screenWidth)
                    textboxStart.X = Main.screenWidth - boxSize.X - 4f;

                if (textboxStart.Y + regexedBoxSize.Y + 4f > (float)Main.screenHeight)
                    textboxStart.Y = Main.screenHeight - regexedBoxSize.Y - 4f;

                //It'd be great to be able to add a background to it but i don't think i know how to get the position of the text for that.
                //Also the "get string size" thing breaks with colored lines so :(
                Utils.DrawInvBG(spriteBatch, new Rectangle((int)textboxStart.X - 10, (int)textboxStart.Y - 10, (int)regexedBoxSize.X + 20, (int)regexedBoxSize.Y + 12), boxColor.HasValue ? boxColor.Value : new Color(25, 20, 55) * 0.925f);
            }

            //Add the hover text.
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(textToDisplay);
        }

        public static bool IsContactMelee(this Projectile proj, bool allowWhips = false)
        {
            //Same conditions as electrification from zapping jellyfishes
            if (proj.aiStyle == ProjAIStyleID.Spear ||
                proj.aiStyle == ProjAIStyleID.ShortSword ||
                proj.aiStyle == ProjAIStyleID.SleepyOctopod ||
                proj.aiStyle == ProjAIStyleID.HeldProjectile ||
                proj.aiStyle == ProjAIStyleID.NorthPoleSpear ||
                ProjectileID.Sets.AllowsContactDamageFromJellyfish[proj.type] ||
                (allowWhips && ProjectileID.Sets.IsAWhip[proj.type]))
                return true;

            //If the projectile doesnt use a custom AI style and isnt a spear, its definitely not a spear
            if (proj.aiStyle > 0)
                return false;

            //If doesnt go through tiles & doesnt penetrate infinitely or doesnt check for clear line of sight, not a "spear" or "held melee"
            if (proj.tileCollide != false || proj.penetrate > -1 || !proj.ownerHitCheck)
                return false;
            //Likely melee
            if (!proj.DamageType.CountsAsClass(DamageClass.Melee))
                return false;
            //Needs to be held projectile
            if (Main.player[proj.owner].heldProj == proj.whoAmI)
                return true;

            return false;
        }

        public static void GenericManipulatePlayerVariablesForHoldouts(Projectile holdout, int dummyItemTime = 2, bool keepProjectileAlive = true, bool doItemRotation = true)
        {
            Player owner = Main.player[holdout.owner];
            owner.UpdateBasicHoldoutVariables(holdout, dummyItemTime, keepProjectileAlive, doItemRotation);
        }


        /// <summary>
        /// Sets a dummy item time, sets the player's held projectile, if necessary keeps the projectile's timeleft to 2
        /// </summary>
        /// <param name="player"></param>
        /// <param name="holdout"></param>
        /// <param name="dummyItemTime"></param>
        /// <param name="keepProjectileAlive"></param>
        /// <param name="doItemRotation"></param>
        public static void UpdateBasicHoldoutVariables(this Player player, Projectile holdout, int dummyItemTime = 2, bool keepProjectileAlive = true, bool doItemRotation = true)
        {
            if (keepProjectileAlive)
                holdout.timeLeft = 2; //Makes sure the holdout doesn't die

            player.heldProj = holdout.whoAmI;

            if (dummyItemTime > 0)
                player.SetDummyItemTime(dummyItemTime); //Add a delay so the player can't button mash

            if (doItemRotation)
            {
                player.itemRotation = holdout.DirectionFrom(player.MountedCenter).ToRotation();
                if (holdout.Center.X < player.MountedCenter.X)
                {
                    player.itemRotation += (float)Math.PI;
                }
                player.itemRotation = MathHelper.WrapAngle(player.itemRotation);
            }
        }

        public static void GiveSupercritHits(this Projectile proj, int supercritHits = -1)
        {
            proj.GetGlobalProjectile<FablesProjectile>().supercritHits = supercritHits;
        }

        public static Vector2 GetCollisionPoint(Vector2 line1point1, Vector2 line1point2, Vector2 line2point1, Vector2 line2point2)
        {
            float A1 = line1point2.Y - line1point1.Y;
            float B1 = line1point1.X - line1point2.X;
            float C1 = A1 * line1point1.X + B1 * line1point1.Y;

            float A2 = line2point2.Y - line2point1.Y;
            float B2 = line2point1.X - line2point2.X;
            float C2 = A2 * line2point1.X + B2 * line2point1.Y;

            float delta = A1 * B2 - A2 * B1;

            //Parallel
            if (delta == 0)
                return Vector2.Zero;

            return new Vector2(B2 * C1 - B1 * C2, A1 * C2 - A2 * C1) / delta;
        }

        public static bool IsPartOfSegment(this Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            //Small tolerance for float errors
            return Math.Abs((point.Distance(segmentStart) + point.Distance(segmentEnd)) - segmentStart.Distance(segmentEnd)) < 0.04f;
        }

        public static Vector2 GetCollisionPoint(this Rectangle rect, Vector2 lineOrigin, Vector2 lineDirection)
        {
            Vector2[] corners = new Vector2[] { rect.TopLeft(), rect.TopRight(), rect.BottomRight(), rect.BottomLeft() };
            Vector2 bestCollisionPoint = Vector2.Zero;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 collisionPoint = GetCollisionPoint(lineOrigin, lineOrigin + lineDirection, corners[i], corners[(i + 1) % 4]);
                float distanceToPoint = collisionPoint.Distance(lineOrigin);
                if (collisionPoint != Vector2.Zero && distanceToPoint < bestDistance && collisionPoint.IsPartOfSegment(corners[i], corners[(i + 1) % 4]))
                {
                    bestCollisionPoint = collisionPoint;
                    bestDistance = distanceToPoint;
                }
            }

            return bestCollisionPoint;
        }

        public static Vector2 ClampMagnitude(this Vector2 vector, float maxMagnitude, float minMagnitude = 0f) => vector.SafeNormalize(Vector2.Zero) * Math.Clamp(vector.Length(), minMagnitude, maxMagnitude);

        public static List<float> ArcLenghtParametrize(this List<Vector2> points, out float totalLenght)
        {
            List<float> parameters = new List<float>();
            parameters.Add(0);
            for (int i = 1; i < points.Count; i++)
                parameters.Add(parameters[i - 1] + points[i].Distance(points[i - 1]));

            totalLenght = parameters[parameters.Count - 1];
            return parameters;
        }

        public static void GetTopLeft(this Tile tile, ref int i, ref int j, out int tileWidth, out int tileHeight)
        {
            TileObjectData data = TileObjectData.GetTileData(tile.TileType, 0);
            tileWidth = data.Width;
            tileHeight = data.Height;

            i -= (tile.TileFrameX % data.CoordinateFullWidth) / (data.CoordinateWidth + data.CoordinatePadding);
            int heightY = tile.TileFrameY % data.CoordinateFullHeight; //Get the frame Y but for a single style variant

            for (int l = 0; l < data.CoordinateHeights.Length; l++)
            {
                int currentCoordinateHeight = data.CoordinateHeights[l] + data.CoordinatePadding;
                if (heightY >= currentCoordinateHeight)
                {
                    j--;
                    heightY -= currentCoordinateHeight;
                }
            }
        }

        public static Vector2 ClampInRect(this Vector2 vector, Rectangle rect) => new Vector2(Math.Clamp(vector.X, rect.Left, rect.Right), Math.Clamp(vector.Y, rect.Top, rect.Bottom));

        /// <summary>
        /// Finds the dot product between two vectors.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static float DotProduct(this Vector2 vector1, Vector2 vector2, bool normalize = false)
        {
            if (normalize)
            {
                vector1.Normalize();
                vector2.Normalize();
            }
            return vector1.X * vector2.X + vector1.Y * vector2.Y;
        }

        public static Rectangle RectangleFromVectors(Vector2 topLeft, Vector2 bottomRight) => new Rectangle((int)Math.Min(topLeft.X, bottomRight.X), (int)Math.Min(topLeft.Y, bottomRight.Y), (int)Math.Abs(topLeft.X - bottomRight.X), (int)Math.Abs(topLeft.Y - bottomRight.Y));

        public static Vector2 InverseKinematic(Vector2 start, Vector2 end, float A, float B, bool flip)
        {
            float C = Vector2.Distance(start, end);
            float angle = (float)Math.Acos(Math.Clamp((C * C + A * A - B * B) / (2f * C * A), -1f, 1));
            if (flip)
                angle *= -1;
            return start + (angle + start.AngleTo(end)).ToRotationVector2() * A;
        }

        public static Point Divide(this Point value, int divisor)
        {
            value.X /= divisor;
            value.Y /= divisor;
            return value;
        }

        public static Color ColorFromHex(int hex)
        {
            byte[] bytes = BitConverter.GetBytes(hex);
            return new Color(bytes[2], bytes[1], bytes[0], 255);
        }

        public static Vector3 Vector3FromHex(int hex)
        {
            return ColorFromHex(hex).ToVector3();
        }


        public static bool IsPotionLike(this Item item, bool needsToBeConsumable = false) => (!needsToBeConsumable || item.consumable) && (item.potion || item.healMana > 0 || item.buffType > 0) && !item.CountsAsClass(DamageClass.Summon) && item.mountType == -1;

        public static bool HasInfinities(this Vector2 vector) => float.IsInfinity(vector.X) || float.IsInfinity(vector.Y);

        public static void CustomTeleport(this Player player, Vector2 position, int style)
        {
            //if (Main.netMode == NetmodeID.SinglePlayer)
            player.Teleport(position, style);
            //else
            if (Main.netMode == NetmodeID.MultiplayerClient)
                new RequestServerTeleportationPacket(player, position, style);
        }

        //Credits to AbsoluteAquarian for this one!
        /// <summary>
		/// Defines a local variable
		/// </summary>
		/// <param name="il">The context</param>
		/// <returns>The index of the local variable in the locals table</returns>
		public static int MakeLocalVariable<T>(this ILContext il)
        {
            var def = new VariableDefinition(il.Import(typeof(T)));
            il.Body.Variables.Add(def);
            return def.Index;
        }

        public static float SnapToCardinalDirection(this float rotation)
        {
            float rotationBetween = Math.Abs(rotation.AngleBetween(0));

            if (rotationBetween <= MathHelper.PiOver4)
                return 0;
            if (rotationBetween > MathHelper.PiOver2 + MathHelper.PiOver4)
                return MathHelper.Pi;

            rotationBetween = Math.Abs(rotation.AngleBetween(MathHelper.PiOver2));
            if (rotationBetween <= MathHelper.PiOver4)
                return MathHelper.PiOver2;

            return -MathHelper.PiOver2;
        }


        private static readonly MethodInfo quickMinecartSnapMethod = typeof(Player).GetMethod("QuickMinecartSnap", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool QuickMinecartSnapPublic(this Player player)
        {
            bool result = (bool)quickMinecartSnapMethod?.Invoke(player, null);
            return result;
        }


        //This sucks
        public static Vector4 ProjectileTileCollisionMimicry(Vector2 position, Vector2 velocity, int collisionWidth, int collisionHeight, out bool collided, bool fallThrough = true)
        {
            Vector2 lastVelocity = velocity;
            collided = false;

            int minimumStep = Math.Min(collisionWidth, collisionHeight);
            if (minimumStep < 3)
                minimumStep = 3;

            if (minimumStep > 16)
                minimumStep = 16;

            if (velocity.Length() > (float)minimumStep)
            {
                Vector2 tileCollisionVelocity = Collision.TileCollision(position, velocity, collisionWidth, collisionHeight, fallThrough, fallThrough);
                float velocityLength = velocity.Length();
                float maxStepDistance = minimumStep;
                Vector2 velocityDirection = velocity.SafeNormalize(Vector2.Zero);
                if (tileCollisionVelocity.Y == 0f)
                    velocityDirection.Y = 0f;

                Vector2 zero = Vector2.Zero;
                int steps = 0;
                while (velocityLength > 0f)
                {
                    steps++;
                    if (steps > 300)
                        break;

                    float moveStep = velocityLength;
                    if (moveStep > maxStepDistance)
                        moveStep = maxStepDistance;

                    velocityLength -= moveStep;
                    Vector2 stepVelocity = velocityDirection * moveStep;
                    Vector2 vector12 = Collision.TileCollision(position, stepVelocity, collisionWidth, collisionHeight, fallThrough, fallThrough);
                    position += vector12;
                    velocity = vector12;
                    Vector4 slopeCollisionResults = Collision.SlopeCollision(position, velocity, collisionWidth, collisionHeight, 0f, fall: true);

                    if (position.X != slopeCollisionResults.X)
                        collided = true;

                    if (position.Y != slopeCollisionResults.Y)
                        collided = true;

                    if (velocity.X != slopeCollisionResults.Z)
                        collided = true;

                    if (velocity.Y != slopeCollisionResults.W)
                        collided = true;

                    position.X = slopeCollisionResults.X;
                    position.Y = slopeCollisionResults.Y;
                    velocity.X = slopeCollisionResults.Z;
                    velocity.Y = slopeCollisionResults.W;

                    vector12 = velocity;
                    zero += vector12;
                }

                velocity = zero;
                if (Math.Abs(velocity.X - lastVelocity.X) < 0.0001f)
                    velocity.X = lastVelocity.X;
                if (Math.Abs(velocity.Y - lastVelocity.Y) < 0.0001f)
                    velocity.Y = lastVelocity.Y;

                Vector4 slopeCollisionResult = Collision.SlopeCollision(position, velocity, collisionWidth, collisionHeight, 0f, fall: true);

                if (position.X != slopeCollisionResult.X)
                    collided = true;

                if (position.Y != slopeCollisionResult.Y)
                    collided = true;

                if (velocity.X != slopeCollisionResult.Z)
                    collided = true;

                if (velocity.Y != slopeCollisionResult.W)
                    collided = true;

                position.X = slopeCollisionResult.X;
                position.Y = slopeCollisionResult.Y;
                velocity.X = slopeCollisionResult.Z;
                velocity.Y = slopeCollisionResult.W;
            }
            else
            {
                velocity = Collision.TileCollision(position, velocity, collisionWidth, collisionHeight, fallThrough, fallThrough);
                Vector4 slopeCollisionResults = Collision.SlopeCollision(position, velocity, collisionWidth, collisionHeight, 0f, fall: true);

                if (position.X != slopeCollisionResults.X)
                    collided = true;
                if (position.Y != slopeCollisionResults.Y)
                    collided = true;
                if (velocity.X != slopeCollisionResults.Z)
                    collided = true;
                if (velocity.Y != slopeCollisionResults.W)
                    collided = true;

                position.X = slopeCollisionResults.X;
                position.Y = slopeCollisionResults.Y;
                velocity.X = slopeCollisionResults.Z;
                velocity.Y = slopeCollisionResults.W;
            }

            return new Vector4(position.X, position.Y, velocity.X, velocity.Y);
        }

        /// <summary>
        /// Based on <see cref="NPC.SimpleStrikeNPC(int, int, bool, float, DamageClass, bool, float, bool)"/>, but takes the HitModifiers as a parameter. <br/>
        /// Should be used in conjunction with <see cref="NPC.GetIncomingStrikeModifiers(DamageClass, int, bool)"/> to generate the HitModifier, which can be further modified. <br/>
        /// Used to add armor penetration on strikes, for example.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="hitModifiers"></param>
        /// <param name="damage"></param>
        /// <param name="crit"></param>
        /// <param name="knockBack"></param>
        /// <param name="damageType"></param>
        /// <param name="damageVariation"></param>
        /// <param name="luck"></param>
        /// <param name="noPlayerInteraction"></param>
        /// <returns></returns>
        public static int ModifiableStrikeNPC(this NPC npc, NPC.HitModifiers hitModifiers, int damage, bool crit = false, float knockBack = 0f, DamageClass damageType = null, bool damageVariation = false, float luck = 0, bool noPlayerInteraction = false)
        {
            NPC.HitInfo hit = hitModifiers.ToHitInfo(damage, crit, knockBack, damageVariation, luck);
            int damageDone = npc.StrikeNPC(hit, fromNet: false, noPlayerInteraction);
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendStrikeNPC(npc, hit);

            return damageDone;
        }
    }
}
