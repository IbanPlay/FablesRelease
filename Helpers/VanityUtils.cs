using CalamityFables.Cooldowns;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;
using Terraria.Utilities;
using static Terraria.GameContent.FontAssets;
using static Terraria.Player;

namespace CalamityFables.Helpers
{
    //Use this for utilies that have to do with player vanity
    public static partial class FablesUtils
    {
        public static bool OnScreen(Rectangle rect) => rect.Intersects(new Rectangle(0, 0, Main.screenWidth, Main.screenHeight));

        public static void SetHeadRotation(this Player player, float rotation) => player.GetModPlayer<FablesPlayer>().cachedHeadRotation = rotation;

        public static Vector2 GetHeadRotationOrigin(this Player player)
        {
            Vector2 headVect = player.legFrame.Width * 0.5f * Vector2.UnitX + player.legFrame.Height * 0.4f * Vector2.UnitY * player.gravDir;
            Vector2 headPosition = player.headPosition;
            Vector2 basePosition = player.position;
            Vector2 coreOffset = new Vector2(-(player.bodyFrame.Width / 2) + (player.width / 2), player.height - player.bodyFrame.Height + 4f);

            if (player.gravDir == -1)
            {
                coreOffset.Y = player.bodyFrame.Height + 4f;
                headPosition.Y *= -1;
            }

            return basePosition + coreOffset + headPosition + headVect;
        }

        public static Vector2 GfxOffY(this NPC npc) => Vector2.UnitY * npc.gfxOffY;


        /// <summary>
        /// Gets an arm stretch amount from a number ranging from 0 to 1
        /// </summary>
        public static CompositeArmStretchAmount ToStretchAmount(this float percent)
        {
            if (percent < 0.25f)
                return CompositeArmStretchAmount.None;
            if (percent < 0.5f)
                return CompositeArmStretchAmount.Quarter;
            if (percent < 0.75f)
                return CompositeArmStretchAmount.ThreeQuarters;

            return CompositeArmStretchAmount.Full;
        }

        /// <summary>
        /// The exact same thing as Player.GetFrontHandPosition() except it properly accounts for gravity swaps instead of requiring the coders to do it manually afterwards.
        /// Additionally, it simply takes in the arm data instead of asking for the rotation and stretch separately.
        /// </summary>
        public static Vector2 GetFrontHandPositionImproved(this Player player, CompositeArmData arm)
        {
            Vector2 position = player.GetFrontHandPosition(arm.stretch, arm.rotation * player.gravDir).Floor();

            if (player.gravDir == -1f)
            {
                position.Y = player.position.Y + (float)player.height + (player.position.Y - position.Y);
            }

            return position;
        }

        /// <summary>
        /// The exact same thing as Player.GetBackHandPosition() except it properly accounts for gravity swaps instead of requiring the coders to do it manually afterwards.
        /// Additionally, it simply takes in the arm data instead of asking for the rotation and stretch separately.
        /// </summary>
        public static Vector2 GetBackHandPositionImproved(this Player player, CompositeArmData arm)
        {
            Vector2 position = player.GetBackHandPosition(arm.stretch, arm.rotation * player.gravDir).Floor();

            if (player.gravDir == -1f)
            {
                position.Y = player.position.Y + (float)player.height + (player.position.Y - position.Y);
            }

            return position;
        }

        /// <summary>
        /// Properly sets the player's held item rotation and position by doing the annoying math for you, since vanilla decided to be wholly inconsistent about it!
        /// This all assumes the player is facing right. All the flip stuff is automatically handled in here.
        /// 
        /// Do note it has to use the UseStyle = Shoot to work.
        /// </summary>
        /// <param name="player">The player for which we set the hold style</param>
        /// <param name="desiredRotation">The desired rotation of the item</param>
        /// <param name="desiredPosition">The desired position of the item</param>
        /// <param name="spriteSize">The size of the item sprite (used in calculations)</param>
        /// <param name="rotationOriginFromCenter">The offset from the center of the sprite of the rotation origin</param>
        /// <param name="noSandstorm">Should the swirly effect from the sandstorm jump be disabled</param>
        /// <param name="flipAngle">Should the angle get flipped with the player, or should it be rotated by 180 degrees</param>
        /// <param name="stepDisplace">Should the item get displaced with the player's height during the walk anim? </param>
        public static void CleanHoldStyle(Player player, float desiredRotation, Vector2 desiredPosition, Vector2 spriteSize, Vector2? rotationOriginFromCenter = null, bool noSandstorm = false, bool flipAngle = false, bool stepDisplace = true)
        {
            if (noSandstorm)
                player.sandStorm = false;

            //Since Vector2.Zero isn't a compile-time constant, we can't use it directly as the default parameter
            if (rotationOriginFromCenter == null)
                rotationOriginFromCenter = Vector2.Zero;

            Vector2 origin = rotationOriginFromCenter.Value;
            //Flip the origin's X position, since the sprite will be flipped if the player faces left.
            origin.X *= player.direction;
            //Additionally, flip the origin's Y position in case the player is in reverse gravity.
            origin.Y *= player.gravDir;

            player.itemRotation = desiredRotation;


            if (flipAngle)
                player.itemRotation *= player.direction;
            else if (player.direction < 0)
                player.itemRotation += MathHelper.Pi;


            //This anchors the item to rotate around the center of its sprite.
            //By default we are already aligned with the vertical center, but to align the horizontal center, we need to counteract the offset of 10 pixels
            //that vanilla uses for the origin, and then counteract it further with the offset of half the sprite width

            Vector2 consistentCenterAnchor = player.itemRotation.ToRotationVector2() * -( spriteSize.X * 0.5f + 10f) * player.direction;

            //And now, we can just shift the anchor to have the origin offset we want
            Vector2 consistentAnchor = consistentCenterAnchor - origin.RotatedBy(player.itemRotation);

            
            Vector2 finalPosition = desiredPosition + consistentAnchor;
            //From DrawPlayer_27_HeldItem, if the item usestyle is 5 (shoot), the draw position is incremented vertically by half the sprite size. Correct for that
            if (player.HeldItem.useStyle == ItemUseStyleID.Shoot)
                finalPosition.Y -= spriteSize.Y * 0.5f;


            //Account for the players extra height when stepping
            if (stepDisplace)
            {
                int frame = player.bodyFrame.Y / player.bodyFrame.Height;
                if ((frame > 6 && frame < 10) || (frame > 13 && frame < 17))
                {
                    finalPosition -= Vector2.UnitY * 2f;
                }
            }

            player.itemLocation = finalPosition;
        }

        /// <summary>
        /// Hides all visual accs the player is wearing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="hideHeadAccs"></param>
        /// <param name="hideBodyAccs"></param>
        /// <param name="hideLegAccs"></param>
        /// <param name="hideShield"></param>
        public static void HideAccessories(this Player player, bool hideHeadAccs = true, bool hideBodyAccs = true, bool hideLegAccs = true, bool hideShield = true)
        {
            if (hideHeadAccs)
                player.face = -1;

            if (hideBodyAccs)
            {
                player.handon = -1;
                player.handoff = -1;

                player.back = -1;
                player.front = -1;
                player.neck = -1;
            }

            if (hideLegAccs)
            {
                player.shoe = -1;
                player.waist = -1;
            }

            if (hideShield)
                player.shield = -1;
        }

        /// <summary>
        /// Changes the drawset to make everything the same precised dye
        /// </summary>
        public static void OverrideAllDyes(ref PlayerDrawSet drawSet, int dyeID)
        {
            drawSet.cHead = dyeID;
            drawSet.cBody = dyeID;
            drawSet.cLegs = dyeID;
            drawSet.cHandOn = dyeID;
            drawSet.cHandOff = dyeID;
            drawSet.cBack = dyeID;
            drawSet.cFront = dyeID;
            drawSet.cShoe = dyeID;
            drawSet.cFlameWaker = dyeID;
            drawSet.cWaist = dyeID;
            drawSet.cShield = dyeID;
            drawSet.cNeck = dyeID;
            drawSet.cFace = dyeID;
            drawSet.cWings = dyeID;
            drawSet.cUnicornHorn = dyeID;
            drawSet.cBeard = dyeID;
            drawSet.cBackpack = dyeID;
            drawSet.cTail = dyeID;
            drawSet.cFaceHead = dyeID;
            drawSet.cFaceFlower = dyeID;
        }


        public static void GoScary(float screenshakeStrenght = 14f, int screenshakeLenght = 40, float chromaAbberationStrenght = 10f, int chromaAbberationDuration = 30, float vignetteOpacity = 1f, int vignetteDuration = 30, float desaturationStrenght = 0.7f, int desaturationLenght = 50)
        {
            if (vignetteDuration > 0 && vignetteOpacity > 0)
                VignetteFadeEffects.AddVignetteEffect(new VignettePunchModifier(vignetteDuration, vignetteOpacity, SineInEasing));
            if (chromaAbberationStrenght > 0 && chromaAbberationDuration > 0)
                ChromaticAbberationManager.Add(new ChromaShakeModifier(0f, chromaAbberationStrenght, chromaAbberationDuration));
            if (screenshakeLenght > 0 && screenshakeStrenght > 0)
                CameraManager.AddCameraEffect(new PunchCameraWithEasings(screenshakeStrenght, screenshakeLenght, PolyInOutEasing, 3.5f));
            if (desaturationLenght > 0 && desaturationStrenght != 0)
                ScreenDesaturation.SetDesaturation(desaturationLenght, desaturationStrenght);
        }

        public static bool IntoMorseCode(string originalText, float completion)
        {
            int spaceLenght = 13;
            int betweenLetterLenght = 7;
            int betweenBlipLenght = 4;
            int shortLenght = 3;
            int longLenght = 8;

            char[] TextKeys = { ' ', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r',
                's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
            string[] MorseKeys = { " ", ".-|", "-...|", "-.-. |", "-..|", ".|", "..-.|"
                    , "--.|", "....|", "..|", ".---|","-.-|",".-..|","--|",
                      "-.|","---|",".--.|","--.-|",".-.|","...|","-|","..-|",
                      "...-|",".--|","-..-|","-.--|","--..|",".----|",
                      "..---|","...--|","....-|",".....|","-....|","--...|",
                      "---..|","----.|","-----|" };

            string morseText = "";
            originalText = originalText.ToLower();

            //Construct a string of text that replaces all the stuff with morse.
            for (int i = 0; i < originalText.Length; i++)
            {
                for (int j = 0; j < 37; j++)
                {
                    if (TextKeys[j] == originalText[i])
                    {
                        morseText += MorseKeys[j];
                        break;
                    }
                }
            }

            List<bool> morseState = new List<bool>();

            for (int i = 0; i < morseText.Length; i++)
            {
                if (morseText[i] == " ".ToCharArray()[0])
                    morseState.AddRange(Enumerable.Repeat(false, spaceLenght));

                if (morseText[i] == "|".ToCharArray()[0])
                    morseState.AddRange(Enumerable.Repeat(false, betweenLetterLenght));

                if (morseText[i] == ".".ToCharArray()[0])
                    morseState.AddRange(Enumerable.Repeat(true, shortLenght));

                if (morseText[i] == "-".ToCharArray()[0])
                    morseState.AddRange(Enumerable.Repeat(true, longLenght));

                morseState.AddRange(Enumerable.Repeat(false, betweenBlipLenght));
            }

            return morseState[(int)((morseState.Count - 1) * completion)];
        }


        public delegate bool TileActionAttemptWithPlayer(Player player, int x, int y);
        public static void DoBootsEffect(this Player player, TileActionAttemptWithPlayer theEffectMethod)
        {
            if (player.miscCounter % 2 == 0 && player.velocity.Y == 0f && player.grappling[0] == -1 && player.velocity.X != 0f)
            {
                int x = (int)player.Center.X / 16;
                int y = (int)(player.position.Y + (float)player.height - 1f) / 16;
                theEffectMethod(player, x, y);
            }
        }

        /// <summary>
        /// Gets the top left of a player's draw frame
        /// Usually used afterwards by usign a vector like <see cref="PlayerDrawSet.headVect"/> to get the head origin, or by going to the center for the body origin
        /// </summary>
        public static Vector2 GetFrameOrigin(this PlayerDrawSet drawInfo)
        {
            return new Vector2(
            (int)(drawInfo.Position.X - Main.screenPosition.X - (drawInfo.drawPlayer.bodyFrame.Width / 2) + (float)(drawInfo.drawPlayer.width / 2)),
            (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - (float)drawInfo.drawPlayer.bodyFrame.Height + 4f));
        }

        /// <summary>
        /// Gets the alignment for the player's head
        /// </summary>
        /// <param name="drawInfo">The playerdrawset to get the head position from</param>
        /// <param name="addBob">Should we add a height offset when the player bobs along while walking</param>
        /// <param name="vanillaStyle">The position will be incorrect if done like that, but it will work for any extra layers the size of the player using drawInfo.headVect as an origin</param>
        /// <returns></returns>
        public static Vector2 HeadPosition(this PlayerDrawSet drawInfo, bool addBob = false, bool vanillaStyle = false)
        {
            //drawInfo.drawPlayer.GetHelmetDrawOffset() + 
            Vector2 drawPosition = GetFrameOrigin(drawInfo);

            if (vanillaStyle)
                drawPosition += drawInfo.drawPlayer.headPosition + drawInfo.headVect;
            else
            {
                //Aligns it to stay consistent between gravity flips
                if (drawInfo.drawPlayer.gravDir == -1)
                    drawPosition.Y = (int)drawInfo.Position.Y - Main.screenPosition.Y + (float)drawInfo.drawPlayer.bodyFrame.Height - 4f;

                //Headvect is the offset from the top left of the sprite that points towards the head rotation point
                Vector2 headOffset = drawInfo.drawPlayer.headPosition + drawInfo.headVect;

                //By default drawPlayer.headPosition is set to 6 if the player isn't dead and has reverse gravity. Cancel that
                if (!drawInfo.drawPlayer.dead && drawInfo.drawPlayer.gravDir == -1)
                    headOffset.Y -= 6;

                headOffset.Y *= drawInfo.drawPlayer.gravDir;
                drawPosition += headOffset;
            }

            if (addBob)
                drawPosition += Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height] * drawInfo.drawPlayer.gravDir;

            return drawPosition;
        }

        /// <summary>
        /// Gets the alignment for the player's body (Aka the center of the players frame)
        /// </summary>
        public static Vector2 BodyPosition(this PlayerDrawSet drawInfo)
        {
            Vector2 drawPosition = GetFrameOrigin(drawInfo);
            drawPosition += drawInfo.drawPlayer.bodyPosition + drawInfo.drawPlayer.bodyFrame.Size() / 2f;
            return drawPosition;
        }

        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's sprite effects
        /// </summary>
        public static void AdjustOffsetOrigin(this PlayerDrawSet drawInfo, Rectangle frame, ref Vector2 offset, ref Vector2 origin)
        {
            if ((drawInfo.playerEffect & SpriteEffects.FlipVertically) != 0)
            {
                offset.Y *= -1;
                origin.Y = frame.Height - origin.Y;
            }

            if ((drawInfo.playerEffect & SpriteEffects.FlipHorizontally) != 0)
            {
                offset.X *= -1;
                origin.X = frame.Width - origin.X;
            }
        }

        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's sprite effects
        /// </summary>
        public static void AdjustOffsetOrigin(this PlayerDrawSet drawInfo, Texture2D texture, ref Vector2 offset, ref Vector2 origin) => AdjustOffsetOrigin(drawInfo, new Rectangle(0, 0, texture.Width, texture.Height), ref offset, ref origin);

        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's sprite effects
        /// </summary>
        public static void AdjustOffsetOrigin(this PlayerDrawSet drawInfo, Asset<Texture2D> texture, ref Vector2 offset, ref Vector2 origin) => AdjustOffsetOrigin(drawInfo, new Rectangle(0, 0, texture.Width(), texture.Height()), ref offset, ref origin);



        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's item effects
        /// </summary>
        public static void AdjustItemOffsetOrigin(this PlayerDrawSet drawInfo, Rectangle frame, ref Vector2 offset, ref Vector2 origin)
        {
            if ((drawInfo.itemEffect & SpriteEffects.FlipVertically) != 0)
            {
                offset.Y *= -1;
                origin.Y = frame.Height - origin.Y;
            }

            if ((drawInfo.itemEffect & SpriteEffects.FlipHorizontally) != 0)
            {
                offset.X *= -1;
                origin.X = frame.Width - origin.X;
            }
        }
        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's sprite effects
        /// </summary>
        public static void AdjustItemOffsetOrigin(this PlayerDrawSet drawInfo, Texture2D texture, ref Vector2 offset, ref Vector2 origin) => AdjustOffsetOrigin(drawInfo, new Rectangle(0, 0, texture.Width, texture.Height), ref offset, ref origin);

        /// <summary>
        /// Adjusts the offset and origin based on the drawInfo's sprite effects
        /// </summary>
        public static void AdjustItemOffsetOrigin(this PlayerDrawSet drawInfo, Asset<Texture2D> texture, ref Vector2 offset, ref Vector2 origin) => AdjustOffsetOrigin(drawInfo, new Rectangle(0, 0, texture.Width(), texture.Height()), ref offset, ref origin);

    }
}
