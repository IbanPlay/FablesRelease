using CalamityFables.Content.Boss.SeaKnightMiniboss;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace CalamityFables.Content.Items.SirNautilusDrops
{
    public class GhostlySeasaddle : ModItem
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public static Asset<Texture2D> GhostOverlay;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghostly Seasaddle");
            Tooltip.SetDefault("Summons a rideable Velocicampus mount"); //Sygraptor? Velocicampus?
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item79;
            Item.noMelee = true;
            Item.mountType = ModContent.MountType<GhostlySeasaddleMount>();
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            GhostOverlay = GhostOverlay ?? ModContent.Request<Texture2D>(Texture + "GhostOverlay");
            Texture2D frontTexutre = GhostOverlay.Value;
            Color overlayColor = FablesUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly, Color.White, Color.DodgerBlue, Color.MediumTurquoise, Color.Aqua);
            overlayColor.A = 0;
            overlayColor *= 0.5f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.3f);

            spriteBatch.Draw(frontTexutre, position, frame, overlayColor, 0, origin, scale, 0, 0);
        }
    }


    public class GhostlySeasaddleBuff : ModBuff
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
            DisplayName.SetDefault("Velocicampus Mount");
            Description.SetDefault("...Is this one a dwarf or was Nautilus riding on a giant specimen???");
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<GhostlySeasaddleMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }

    public class GhostlySeasaddleMount : ModMount
    {
        public override string Texture => AssetDirectory.SirNautilusDrops + Name;

        internal const int xFrames = 4;
        internal const int yFrames = 10;

        public virtual SoundStyle StepSound => SirNautilus.SignathionStep with { Pitch = 0.1f };
        public virtual SoundStyle LandSound => SirNautilus.SignathionHeavyStomp with { Pitch = 0.1f, Volume = 0.7f };
        public virtual SoundStyle JumpSound => SirNautilus.SignathionWaterRifle with { Pitch = 0.2f };

        protected class SeasaddleMountSpecificData
        {
            public Vector2 oldVelocity;
            public int timeInTheAir;

            public SeasaddleMountSpecificData()
            {
                oldVelocity = Vector2.Zero;
                timeInTheAir = 0;
            }
        }

        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<GhostlySeasaddleBuff>(); // The ID number of the buff assigned to the mount.

            // Movement
            MountData.acceleration = 0.1f; // The rate at which the mount speeds up.
            MountData.blockExtraJumps = false; // Determines whether or not you can use a double jump (like cloud in a bottle) while in the mount.
            MountData.constantJump = false; // Allows you to hold the jump button down.
            MountData.fallDamage = 0.5f; // Fall damage multiplier.
            MountData.runSpeed = 5f;
            MountData.dashSpeed = 8f; // The speed the mount moves when in the state of dashing.
            MountData.fatigueMax = 0;
            MountData.jumpHeight = 7; // How high the mount can jump.
            MountData.jumpSpeed = 7f;

            // Frame data and player offsets
            MountData.totalFrames = xFrames * yFrames; // Amount of animation frames for the mount
            MountData.heightBoost = 40; // Height between the mount and the ground

            MountData.playerYOffsets = Enumerable.Repeat(36, MountData.totalFrames).ToArray(); // Fills an array with values for less repeating code
            MountData.playerYOffsets[13] = MountData.playerYOffsets[14] = MountData.playerYOffsets[18] = MountData.playerYOffsets[19] -= 2; //Walk anim
            MountData.playerYOffsets[20] = MountData.playerYOffsets[21] = MountData.playerYOffsets[24] = MountData.playerYOffsets[25] -= 2; //Run anim
            MountData.playerYOffsets[30] = MountData.playerYOffsets[31] = MountData.playerYOffsets[32] += 4; //Jump anim
            MountData.playerYOffsets[33] = MountData.playerYOffsets[34] = MountData.playerYOffsets[35] += 4; //Fall anim

            MountData.idleFrameStart = 200;

            MountData.xOffset = 0;
            MountData.yOffset = 0;
            MountData.playerHeadOffset = 42;
            MountData.bodyFrame = 0;
        }

        public void DustEffects(Player player)
        {
            for (int i = 0; i < 32; i++)
            {
                Dust cust = Dust.NewDustPerfect(player.Bottom + Main.rand.NextFloat(-40f, 40f) * Vector2.UnitX, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.8f);

                cust.noGravity = true;
                cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f) + Main.rand.NextVector2Circular(1f, 1f);
                cust.rotation = Main.rand.NextFloat(1f, 1.5f);

                cust.customData = Color.Teal.ToVector3();
            }

            for (int i = 0; i < 12; i++)
            {
                Dust cust = Dust.NewDustPerfect(player.Bottom + Main.rand.NextFloat(-40f, 40f) * Vector2.UnitX, DustID.TintableDustLighted, Vector2.Zero, 100, Color.DeepSkyBlue, Main.rand.NextFloat(0.7f, 1f));

                cust.noGravity = true;
                cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 9f) + Main.rand.NextVector2Circular(1f, 1f);
                cust.rotation = Main.rand.NextFloat(0f, 1.56f);
            }
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            SoundEngine.PlaySound(SirNautilus.SignathionAppearSizzle with { Volume = 0.06f }, player.Center);
            DustEffects(player);
            player.mount._mountSpecificData = new SeasaddleMountSpecificData();
            skipDust = true;
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            SoundEngine.PlaySound(SirNautilus.SignathionDisappearSizzle with { Pitch = 0.3f, Volume = 0.05f }, player.Center);
            DustEffects(player);
            skipDust = true;
        }

        public override void UpdateEffects(Player player)
        {
            //Mighty wind immunity
            player.buffImmune[BuffID.WindPushed] = true;
        }

        public override bool UpdateFrame(Player mountedPlayer, int state, Vector2 velocity)
        {
            int previousAnimation = (int)(mountedPlayer.mount._frame / yFrames);
            int previousFrame = mountedPlayer.mount._frame % yFrames;
            int newAnimation;

            if (!mountedPlayer.compositeFrontArm.enabled && mountedPlayer.ItemTimeIsZero)
                mountedPlayer.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.PiOver2 * -0.4f * mountedPlayer.direction);

            if (velocity.Y == 0)
            {
                int animationDirection = Math.Sign(velocity.X) * mountedPlayer.direction;

                //Standing still
                if (velocity.X == 0)
                {
                    newAnimation = 0;
                    mountedPlayer.mount._frame = 0;
                    mountedPlayer.mount._frameCounter = 0;
                }
                else if (Math.Abs(velocity.X) < MountData.runSpeed / mountedPlayer.moveSpeed)
                {
                    newAnimation = 1;

                    float animationSpeed = 0.1f + Utils.GetLerpValue(MountData.dashSpeed, 0f, Math.Abs(velocity.X), true) * 0.02f;
                    mountedPlayer.mount._frameCounter += 1 / (60f * animationSpeed) * animationDirection;
                    mountedPlayer.mount._frameCounter = FablesUtils.Modulo(mountedPlayer.mount._frameCounter, 10);
                    mountedPlayer.mount._frame = (int)mountedPlayer.mount._frameCounter;
                }
                else
                {
                    newAnimation = 2;

                    mountedPlayer.mount._frameCounter += 1 / (60f * 0.09f) * animationDirection;
                    mountedPlayer.mount._frameCounter = FablesUtils.Modulo(mountedPlayer.mount._frameCounter, 8);
                    mountedPlayer.mount._frame = (int)mountedPlayer.mount._frameCounter;
                }

                mountedPlayer.fullRotation = 0;
            }

            //Falling jumping anims
            else
            {
                if (previousAnimation != 3)
                {
                    mountedPlayer.mount._frameCounter = 0;
                    mountedPlayer.mount._frame = 0;
                }

                newAnimation = 3;

                //Animation loop (anim speed scales with fall speed
                float animSpeed = 0.09f + Utils.GetLerpValue(4f, 0f, Math.Abs(velocity.Y), true) * 0.03f;
                mountedPlayer.mount._frameCounter += 1 / (60f * animSpeed);
                mountedPlayer.mount._frameCounter = mountedPlayer.mount._frameCounter % 3;

                mountedPlayer.mount._frame = (int)mountedPlayer.mount._frameCounter;
                if (velocity.Y > 0)
                    mountedPlayer.mount._frame += 3;

                //Rotation, inspired by aurora saddle
                int direction = Math.Sign(velocity.X);
                mountedPlayer.fullRotation = velocity.Y * 0.01f * direction * MountData.jumpHeight / 14f;
                mountedPlayer.fullRotationOrigin = (mountedPlayer.Hitbox.Size() + new Vector2(0, 42)) / 2;

                ((SeasaddleMountSpecificData)mountedPlayer.mount._mountSpecificData).timeInTheAir++;
            }


            DoTheSounds(mountedPlayer, velocity, newAnimation, previousAnimation, mountedPlayer.mount._frame, previousFrame);

            ((SeasaddleMountSpecificData)mountedPlayer.mount._mountSpecificData).oldVelocity = velocity;
            //We have to set this at the end, so that it doesnt mess with the sounds
            if (velocity.Y == 0)
                ((SeasaddleMountSpecificData)mountedPlayer.mount._mountSpecificData).timeInTheAir = 0;

            mountedPlayer.mount._frame += newAnimation * yFrames;
            return false;
        }

        public void DoTheSounds(Player mountedPlayer, Vector2 velocity, int xFrame, int previousXFrame, int yFrame, int previousYFrame)
        {
            SeasaddleMountSpecificData mountData = mountedPlayer.mount._mountSpecificData as SeasaddleMountSpecificData;

            //Landing
            if (mountData.oldVelocity.Y > 0 && velocity.Y == 0 && mountData.timeInTheAir > 20)
            {
                float volume = Utils.GetLerpValue(60, 130, mountData.timeInTheAir, true) * 0.5f + 0.5f;
                SoundEngine.PlaySound(LandSound with { Volume = LandSound.Volume * volume * 0.3f }, mountedPlayer.Center);


                for (int i = 0; i < 22; i++)
                {
                    Dust cust = Dust.NewDustPerfect(mountedPlayer.Bottom + Main.rand.NextFloat(-40f, 40f) * Vector2.UnitX, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.8f);

                    cust.noGravity = true;
                    cust.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.5f);
                    cust.velocity += cust.position.DirectionFrom(mountedPlayer.Bottom + Vector2.UnitY * 4f) * Main.rand.NextFloat(1f, 3f);
                    cust.rotation = Main.rand.NextFloat(1f, 1.5f);

                    cust.customData = Color.Teal.ToVector3();
                }
            }

            //Jumping
            if (previousXFrame != 3 && xFrame == 3 && velocity.Y < 0)
            {
                SoundEngine.PlaySound(JumpSound with { Volume = 0.3f}, mountedPlayer.Center);

                for (int i = 0; i < 22; i++)
                {
                    Dust cust = Dust.NewDustPerfect(mountedPlayer.Bottom + Main.rand.NextFloat(-40f, 40f) * Vector2.UnitX, ModContent.DustType<SpectralWaterDustNoisy>(), Vector2.Zero, 100, default, 1.8f);

                    cust.noGravity = true;
                    cust.velocity = -velocity * Main.rand.NextFloat(0.1f, 0.44f);
                    cust.velocity.X *= 0.3f;
                    cust.rotation = Main.rand.NextFloat(1f, 1.5f);

                    cust.customData = Color.Teal.ToVector3();
                }
            }

            //Footstep sounds only happen if we were part of the same X anim, and changing Y frames
            if (previousXFrame == xFrame && previousYFrame != yFrame)
            {
                //Walking
                if (xFrame == 1 && yFrame % 5 == 3)
                    SoundEngine.PlaySound(StepSound with { Volume = StepSound.Volume * 0.2f }, mountedPlayer.Bottom);

                //Running
                if (xFrame == 2 && yFrame % 4 == 0)
                    SoundEngine.PlaySound(StepSound with { Volume = StepSound.Volume * 0.4f }, mountedPlayer.Bottom);
            }

            //If going from moving to idle, do a final footsep
            if (previousXFrame != 3 && previousXFrame != 0 && xFrame == 0)
                SoundEngine.PlaySound(StepSound with { Volume = StepSound.Volume * 0.3f }, mountedPlayer.Bottom);
        }

        public static void AddDrawDataWithMountShader(List<DrawData> playerDrawData, DrawData data, Player drawPlayer)
        {
            if (!drawPlayer.miscDyes[3].active || drawPlayer.miscDyes[3] == null)
            {
                playerDrawData.Add(data);
                return;
            }

            data.shader = GameShaders.Armor.GetShaderIdFromItemId(drawPlayer.miscDyes[3].type);
            playerDrawData.Add(data);
        }

        public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer, ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition, ref Rectangle frame, ref Color drawColor, ref Color glowColor, ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin, ref float drawScale, float shadow)
        {
            // Draw is called for each mount texture we provide, so we check drawType to avoid duplicate draws.
            Texture2D mountTexture;
            if (drawType == 0)
                mountTexture = ModContent.Request<Texture2D>(Texture).Value;
            else
                mountTexture = ModContent.Request<Texture2D>(Texture + "Front").Value;

            int frameWidth = mountTexture.Width / xFrames;
            int frameHeight = mountTexture.Height / yFrames;

            int yFrame = drawPlayer.mount._frame % yFrames;
            int xFrame = (int)(drawPlayer.mount._frame / yFrames);

            int direction = spriteEffects == SpriteEffects.FlipHorizontally ? -1 : 1;
            Rectangle mountFrame = new Rectangle(xFrame * frameWidth, yFrame * frameHeight, frameWidth - 2, frameHeight - 2);
            Vector2 origin = mountFrame.Size() / 2f;
            switch (xFrame)
            {
                case 0:
                    origin.X += 4 * direction;
                    break;
                case 1:
                    origin.X += 2 * direction;
                    break;
                case 2:
                    origin.X += 14 * direction;
                    break;
                case 3:
                    origin.X += 2 * direction;
                    break;
            }

            Vector2 drawPos = drawPosition;
            AddDrawDataWithMountShader(playerDrawData, new DrawData(
                mountTexture,
                drawPos,
                mountFrame,
                drawColor,
                drawPlayer.fullRotation,
                origin,
                drawScale,
                1 - spriteEffects, 0),
                drawPlayer);

            // by returning true, the regular drawing will still happen.
            return false;
        }
    }
}
