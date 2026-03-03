using CalamityFables.Particles;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    [ReplacingCalamity("DecapoditaSprout")]
    public class Hookstaff : ModItem
    {
        public override string Texture => AssetDirectory.Crabulon + "HookstaffHeld";

        internal static Asset<Texture2D> RealSprite;

        public static readonly SoundStyle DigSound = new SoundStyle(SoundDirectory.Crabulon + "HookstaffPlunge");
        public static readonly SoundStyle RipSound = new SoundStyle(SoundDirectory.Crabulon + "HookstaffRip");


        public static Player summoningPlayer;
        public static Vector2 summoningPosition;
        public static int spawnTimer;


        public override void Load()
        {
            FablesPlayer.PostUpdateMiscEffectsEvent += CreateUseVisualsAndSoundsAndSlowdowns;
            FablesGeneralSystemHooks.ClearWorldEvent += ResetSpawnTimer;
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += UpdateCrabulonSpawn;
        }

        #region Spawning stuff
        private void UpdateCrabulonSpawn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (spawnTimer > 0)
            {
                if (summoningPlayer == null || summoningPlayer.dead || !summoningPlayer.active)
                {
                    summoningPlayer = null;
                    spawnTimer = 0;
                    new ResetCrabulonSpawnTimerPacket().Send(-1, -1, false);
                    return;
                }

                spawnTimer++;

                if (spawnTimer > 60 * 2)
                {
                    spawnTimer = 0;
                    new ResetCrabulonSpawnTimerPacket().Send(-1, -1, false);

                    Crabulon crabulonInstance = ModContent.GetInstance<Crabulon>();
                    int crabulonWidth = crabulonInstance.CollisionBoxWidth;
                    int crabulonHeight = crabulonInstance.CollisionBoxHeight + 30;
                    Vector2 size = new Vector2(crabulonWidth, crabulonHeight);
                    Vector2 originOffset = -size * 0.5f;

                    Vector2 bestPosition = summoningPosition;
                    float bestScore = FablesUtils.TileFillPercent(summoningPosition + originOffset, crabulonWidth, crabulonHeight);

                    //Dust.QuickBox(bestPosition + originOffset, bestPosition - originOffset, 10, Color.Red, null);

                    //Randomly look around for a best spawning position if the one we have atm doesnt fit
                    float minimumTreshold = 2f;
                    while (bestScore < Math.Min(0.8f, minimumTreshold))
                    {
                        float leniency = Utils.GetLerpValue(2f, 0.5f, minimumTreshold, true);

                        Vector2 randomPosition = summoningPosition + Main.rand.NextVector2Circular(400f + leniency * 400f, 100f + leniency * 300f);
                        float randomPosScore = ScoreCrabulonSpawnPosition(ref randomPosition, originOffset, size, Math.Min(0.8f, minimumTreshold));
                        //Dust.QuickBox(randomPosition + originOffset, randomPosition - originOffset, 10, Color.Red, null);

                        if (randomPosScore > bestScore)
                        {
                            bestScore = randomPosScore;
                            bestPosition = randomPosition;
                        }

                        //Slowly become more acceptant of less than filled spots
                        minimumTreshold -= 1 / 200f;
                        if (minimumTreshold < 0.5f)
                        {
                            bestPosition = summoningPosition - Vector2.UnitY * 180f;
                            break;
                        }
                    }

                    int crab = NPC.NewNPC(NPC.GetBossSpawnSource(summoningPlayer.whoAmI), (int)bestPosition.X, (int)bestPosition.Y, ModContent.NPCType<Crabulon>());
                    //Manually sync crabulon's spawn or else it will skip the first ai tick
                    if (crab < 200 && Main.dedServ)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, crab);
                }
            }
        }

        public static float ScoreCrabulonSpawnPosition(ref Vector2 position, Vector2 originOffset, Vector2 size, float scoreToBeat = 1f)
        {
            float posScore = FablesUtils.TileFillPercent(position + originOffset, size.X, size.Y);

            if (posScore > scoreToBeat)
            {
                int riseIterationsMax = 20;
                int freeAirClearnce = (int)(size.Y / 2f);

                while (Collision.SolidCollision(position + originOffset - Vector2.UnitY * freeAirClearnce, (int)size.X, freeAirClearnce))
                {
                    position.Y -= 16;

                    //Give up if too far in the ground
                    riseIterationsMax--;
                    if (riseIterationsMax <= 0)
                    {
                        posScore = 0;
                        break;
                    }    
                }
            }

            return posScore;
        }

        private void ResetSpawnTimer() => spawnTimer = 0;
        #endregion

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Hookstaff");
            Tooltip.SetDefault("Used to poke and prod at the mycelium within the mushroom biome\n" +
                "Can only be used when standing on mushroom grass\n" +
                "Might provoke some subterranean creatures...");
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 35;
            Item.useTurn = true;
            Item.autoReuse = false;
            Item.useAnimation = 85;
            Item.useTime = 85;
            Item.rare = ItemRarityID.Quest;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = DigSound;
            Item.autoReuse = true;
            Item.value = Item.sellPrice(0, 0, 10);
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI && player.ZoneGlowshroom && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight) && !NPC.AnyNPCs(ModContent.NPCType<Crabulon>()) && spawnTimer == 0)
            {
                spawnTimer = 1;

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    summoningPlayer = player;
                    summoningPosition = new Vector2(player.Center.X, player.Center.Y + 150);
                }
                else
                    new HookstaffUsePacket().Send(-1, -1, false);
            }
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            Tile floorTile = Main.tile[(player.Bottom + new Vector2(8f * player.direction, 8f)).ToTileCoordinates()];
            if (!floorTile.HasUnactuatedTile || floorTile.TileType != TileID.MushroomGrass)
                return false;
            return true;
        }


        private void CreateUseVisualsAndSoundsAndSlowdowns(Player player)
        {
            if (player.itemTime == 0 || player.itemTime == player.itemTimeMax || player.HeldItem == null || player.HeldItem.type != Type)
                return;

            Point floorTilePos = (player.Bottom + new Vector2(8f * player.direction, 12f)).ToTileCoordinates();
            Tile floorTile = Main.tile[floorTilePos];
            if (!floorTile.HasUnactuatedTile || floorTile.TileType != TileID.MushroomGrass)
                return;

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            bool bigTug = animProgress >= 0.75f;
            bool smallTug = animProgress >= 0.55f && animProgress < 0.65f;

            float lastAnimProgress = 1 - (player.itemTime + 1) / (float)player.itemTimeMax;
            bool bigTugStart = bigTug && lastAnimProgress < 0.75f;
            bool smallTugStart = smallTug && lastAnimProgress < 0.55f;

            if (!bigTug)
            {
                player.controlJump = false;
                player.GetModPlayer<FablesPlayer>().MoveSpeedModifier *= 0.1f;
                if (Math.Abs(player.velocity.X) > player.maxRunSpeed)
                    player.velocity.X *= 0.9f;
            }

            //Going down into the floor
            if (animProgress < 0.2f)
                WorldGen.KillTile_MakeTileDust(floorTilePos.X, floorTilePos.Y, floorTile);

            //Going up
            else if (bigTug || smallTug)
            {
                int iterations = 1;
                int fiberChance = 2;

                if (smallTug)
                    fiberChance = 6;

                if (smallTugStart)
                {
                    SoundEngine.PlaySound(SoundID.LiquidsHoneyLava with { Type = SoundType.Sound, Volume = 0.4f, Pitch = -0.2f }, player.Bottom);
                    if (Main.myPlayer == player.whoAmI)
                        CameraManager.Quake += 2.5f;
                }

                if (bigTugStart)
                {
                    iterations = 8;

                    SoundEngine.PlaySound(RipSound, player.Bottom);

                    if (Main.myPlayer == player.whoAmI)
                        CameraManager.AddCameraEffect(new SnappyDirectionalCameraTug(-Vector2.UnitY.RotatedBy(player.direction * 0.15f) * 13f, 20, 0.6f, 0.15f,
                            FablesUtils.PolyOutEasing, 2f, FablesUtils.PolyOutEasing, 2.5f));
                }

                for (int i = 0; i < iterations; i++)
                {
                    if (Main.rand.NextBool(fiberChance))
                    {
                        Particle fiber = new MyceliumStrandParticle(player.Bottom + Vector2.UnitX * player.direction * Main.rand.NextFloat(5f, 19f));
                        ParticleHandler.SpawnParticle(fiber);
                        if (smallTug)
                        {
                            fiber.Velocity *= 0.8f;
                            fiber.Scale *= 0.7f;
                        }
                        else if (bigTugStart)
                        {
                            fiber.Velocity *= 1.3f;
                        }
                    }

                    if (Main.rand.NextBool(5) && bigTug)
                    {
                        Vector2 sporePos = player.Bottom + Vector2.UnitX * 14f * player.direction + Main.rand.NextVector2Circular(8f, 8f);
                        Vector2 sporeVel = (player.Bottom + Vector2.UnitY * 30f).DirectionTo(sporePos) * Main.rand.NextFloat(0.1f, 1.2f);
                        SporeGas particle = new SporeGas(sporePos, sporeVel, player.Center, 200f, Main.rand.NextFloat(0.9f, 2.5f), 0.01f);
                        particle.dustSpawnRate = 0f;
                        particle.counter = 77;
                        ParticleHandler.SpawnParticle(particle);
                    }

                    if (Main.rand.NextBool(4))
                    {
                        Vector2 dustPos = player.Bottom + Vector2.UnitX * Main.rand.NextFloat(6f, 26f) * player.direction;
                        Dust d = Dust.NewDustPerfect(dustPos, DustID.MushroomSpray, -Vector2.UnitY.RotatedByRandom(0.5f).RotatedBy(player.direction * 0.7f) * Main.rand.NextFloat(0.1f, 1.2f));
                        d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                    }
                }
            }
        }

        public float MotionCycle(Player player, float offset)
        {
            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float yOffset;

            if (animProgress < 0.2f)
                yOffset = -0.3f + 1.5f * FablesUtils.PolyInOutEasing((animProgress) / 0.2f, 2.5f);
            else if (animProgress < 0.75f)
            {
                yOffset = 1.2f;
                yOffset += (float)Math.Sin((animProgress - 0.2f) / 0.55f * MathHelper.Pi) * 0.2f;

                if (animProgress > 0.45f)
                    yOffset -= FablesUtils.PolyInEasing((float)Math.Sin((animProgress - 0.45f) / 0.4f * MathHelper.Pi), 3f) * 0.7f;
            }
            else
            {
                yOffset = 1f - 2.1f * FablesUtils.PolyOutEasing((animProgress - 0.75f) / 0.25f, 4f);
            }

            return yOffset;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            //Default
            float yOffset = MotionCycle(player, 0f) * 10f;

            Vector2 itemPosition = player.MountedCenter + new Vector2(11f * player.direction, (yOffset + 4f) * player.gravDir);

            if (yOffset > 0)
                itemPosition.X -= player.direction * yOffset * 0.2f;

            float rotationCycle = MotionCycle(player, MathHelper.PiOver2);
            float itemRotation = rotationCycle * 0.05f;
            if (rotationCycle < 0)
                itemRotation = rotationCycle * 0.1f;

            Vector2 itemSize = new Vector2(14, 62);
            Vector2 itemOrigin = new Vector2(0, 0);

            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin, true, true);
        }

        public override void UseItemFrame(Player player)
        {
            Vector2 direction = Vector2.UnitX * player.direction;
            float rotation = MotionCycle(player, MathHelper.PiOver2) * 0.43f;

            direction = direction.RotatedBy(rotation * player.direction);

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, direction.ToRotation() - MathHelper.PiOver2);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (RealSprite == null)
                RealSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + Name);
            Texture2D properSprite = RealSprite.Value;
            spriteBatch.DrawNewInventorySprite(properSprite, new Vector2(14f, 62f), position, drawColor, origin, scale, new Vector2(-1f, -0f));
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (RealSprite == null)
                RealSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + Name);
            Texture2D properSprite = RealSprite.Value;
            spriteBatch.Draw(properSprite, Item.position - Main.screenPosition, null, lightColor, rotation, properSprite.Size() / 2f, scale, 0, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Hook).
                AddRecipeGroup(RecipeGroupID.IronBar, 5).
                AddIngredient(ItemID.GlowingMushroom, 30).
                AddTile(TileID.Anvils).
                Register();
        }
    }

    #region Tiles
    public class HookstaffDisplayIcon : ModItem
    {
        public override string Texture => AssetDirectory.Crabulon + "Hookstaff";

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 0;
            ItemID.Sets.Deprecated[Type] = true;
            ItemID.Sets.ItemsThatShouldNotBeInInventory[Type] = true;
        }
    }

    public class HookstaffTile : ModTile
    {
        public override string Texture => AssetDirectory.Crabulon + "HookstaffTile";

        public static Asset<Texture2D> GlowSprite;
        public override void Load()
        {
            if (!Main.dedServ)
                GlowSprite = ModContent.Request<Texture2D>(Texture + "Glow");
        }

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(ModContent.ItemType<Hookstaff>());
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileLighted[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = 6;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(3, 1);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Hookstaff");
            AddMapEntry(new Color(90, 167, 209), name);
            TileID.Sets.FramesOnKillWall[Type] = true;
            DustType = DustID.Tin;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.2f;
            g = 0.4f;
            b = 1.27f;

            float lightOscillation = 1f + 0.7f * (0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f);
            g *= lightOscillation;
            b *= lightOscillation;
            r *= lightOscillation;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer && Main.rand.NextBool(70) && Main.tile[i, j].TileFrameY == 0)
            {
                Dust d = Dust.NewDustPerfect(new Vector2(i + Main.rand.NextFloat(), j + Main.rand.NextFloat()) * 16f, DustID.MushroomTorch, -Vector2.UnitY, 0, Color.CornflowerBlue);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                d.velocity *= Main.rand.NextFloat();
                d.noLight = true;
                d.noLightEmittence = false;
            }

            if (closer && Main.rand.NextBool(20) && Main.tile[i, j].TileFrameY == 0)
            {
                Dust d = Dust.NewDustPerfect(new Vector2(i + Main.rand.NextFloat(), j + Main.rand.NextFloat(0.6f, 1f)) * 16f, DustID.MushroomSpray, -Vector2.UnitY, 0);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.5f);
                d.velocity *= Main.rand.NextFloat();
            }
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<HookstaffDisplayIcon>();
        }

        public override bool RightClick(int i, int j)
        {
            WorldGen.KillTile(i, j, false, false, false);
            if (!Main.tile[i, j].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);

            return true;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Texture2D tex = GlowSprite.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + zero;
            Tile t = Main.tile[i, j];

            Vector2 center = new Vector2(i - t.TileFrameX / 18 + 3f, j - t.TileFrameY / 18 + 1f) * 16f;
            float opacity = Utils.GetLerpValue(700f, 100f, Main.LocalPlayer.Distance(center), true) * (0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly));
            Color baseColor = Color.Lerp(Color.Orange, Color.DodgerBlue, (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f);

            Color outlineColor = (baseColor * opacity * 0.7f) with { A = 0 };

            spriteBatch.Draw(tex, drawOffset, new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16), outlineColor, 0f, Vector2.Zero, 1f, 0, 0);
            return true;
        }
    }

    public class HookstaffBuriedTile : ModTile
    {
        public override string Texture => AssetDirectory.Crabulon + "HookstaffBuriedTile";

        public static Asset<Texture2D> GlowSprite;
        public override void Load()
        {
            if (!Main.dedServ)
                GlowSprite = ModContent.Request<Texture2D>(Texture + "Outline");
        }

        public override void SetStaticDefaults()
        {
            RegisterItemDrop(ModContent.ItemType<Hookstaff>());
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileWaterDeath[Type] = false;
            Main.tileLighted[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Width = 2;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.Origin = new Point16(1, 3);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 18 };
            TileObjectData.newTile.FlattenAnchors = true;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.WaterDeath = false;

            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Hookstaff");
            AddMapEntry(new Color(90, 167, 209), name);
            DustType = DustID.Tin;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.2f;
            g = 0.4f;
            b = 1.27f;

            float lightOscillation = 1.3f + 0.4f * (0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f);
            g *= lightOscillation;
            b *= lightOscillation;
            r *= lightOscillation;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            Tile t = Main.tile[i, j];
            float sideways = 1 + t.TileFrameY / (4f * 18f);

            if (closer && Main.rand.NextBool(70) && Main.tile[i, j].TileFrameX == 0)
            {
                Dust d = Dust.NewDustPerfect(new Vector2(i + Main.rand.NextFloat(sideways), j + Main.rand.NextFloat()) * 16f, DustID.MushroomTorch, -Vector2.UnitY, 0, Color.CornflowerBlue);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.2f);
                d.velocity *= Main.rand.NextFloat();
                d.noLight = true;
                d.noLightEmittence = false;
            }

            if (closer && Main.rand.NextBool(20) && Main.tile[i, j].TileFrameX == 0)
            {
                Dust d = Dust.NewDustPerfect(new Vector2(i + Main.rand.NextFloat(sideways), j + Main.rand.NextFloat()) * 16f, DustID.MushroomSpray, -Vector2.UnitY, 0);
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.8f, 1.5f);
                d.velocity *= Main.rand.NextFloat();
            }
        }


        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<HookstaffDisplayIcon>();
        }
        public override bool RightClick(int i, int j)
        {
            WorldGen.KillTile(i, j, false, false, false);
            if (!Main.tile[i, j].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, (float)i, (float)j, 0f, 0, 0, 0);

            return true;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Texture2D tex = GlowSprite.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawOffset = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y + 2) + zero;
            Tile t = Main.tile[i, j];

            Vector2 center = new Vector2(i - t.TileFrameX / 18 + 1f, j - t.TileFrameY / 18 + 2f) * 16f;
            float opacity = Utils.GetLerpValue(700f, 100f, Main.LocalPlayer.Distance(center), true) * (0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly));
            Color baseColor = Color.Lerp(Color.Orange, Color.DodgerBlue, (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f);

            Color outlineColor = (baseColor * opacity * 0.7f) with { A = 0 };

            int height = 16 + (t.TileFrameY / 18 == 3 ? 2 : 0);
            spriteBatch.Draw(tex, drawOffset, new Rectangle(t.TileFrameX, t.TileFrameY, 16, height), outlineColor, 0f, Vector2.Zero, 1f, 0, 0);
            return true;
        }
    }
    #endregion


    public class MyceliumStrandParticle : Particle
    {
        public override bool SetLifetime => true;
        public override string Texture => AssetDirectory.Crabulon + "MyceliumStrandParticle";
        public static Asset<Texture2D> BloomAsset;

        public override bool UseCustomDraw => true;
        public override int FrameVariants => 7;

        public float opacity = 1f;

        public MyceliumStrandParticle(Vector2 position)
        {
            Position = position;
            Scale = Main.rand.NextFloat(0.6f, 0.9f);
            Color = Color.White;
            Velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(1.4f, 3.2f);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Lifetime = Main.rand.Next(40, 60);
            Variant = Main.rand.Next(FrameVariants);
        }

        public override void Update()
        {
            Rotation += Velocity.X.NonZeroSign() * 0.02f;

            if (LifetimeCompletion > 0.2f)
                Scale *= 0.99f;


            if (LifetimeCompletion > 0.26f && LifetimeCompletion < 0.8f)
            {
                Velocity *= 0.96f;
            }

            if (LifetimeCompletion > 0.7f)
            {
                opacity = 1 - (LifetimeCompletion - 0.7f) / 0.3f;
            }

            Velocity.Y += 0.01f + LifetimeCompletion * 0.01f;
            Velocity.X += 0.02f + LifetimeCompletion * 0.03f;
        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D baseTex = ParticleTexture;
            BloomAsset ??= ModContent.Request<Texture2D>(Texture + "Bloom");
            Texture2D bloomTex = BloomAsset.Value;
            Rectangle frame = baseTex.Frame(1, 7, 0, Variant);
            Color lightColor = Lighting.GetColor(Position.ToTileCoordinates());

            spriteBatch.Draw(baseTex, Position - basePosition, frame, lightColor.MultiplyRGBA(Color.White) * opacity, Rotation, frame.Size() * 0.5f, Scale, 0, 0);

            Color bloomColor = lightColor.MultiplyRGBA(Color.White with { A = 0 });
            spriteBatch.Draw(bloomTex, Position - basePosition, frame, bloomColor * opacity * 0.5f, Rotation, frame.Size() * 0.5f, Scale, 0, 0);
        }
    }

    [Serializable]
    public class HookstaffUsePacket : Module
    {
        public byte summoningPlayer;
        public Vector2 summoningPosition;

        public HookstaffUsePacket()
        {
            summoningPlayer = (byte)Main.myPlayer;
            summoningPosition = new Vector2(Main.LocalPlayer.Center.X, Main.LocalPlayer.Center.Y + 150);
        }

        protected override void Receive()
        {
            if (Hookstaff.summoningPlayer != null && Hookstaff.spawnTimer > 0)
                return;

            Player player = Main.player[summoningPlayer];
            Hookstaff.summoningPlayer = player;
            Hookstaff.summoningPosition = new Vector2(player.Center.X, player.Center.Y + 150);
            Hookstaff.spawnTimer = 1;
        }
    }

    [Serializable]
    public class ResetCrabulonSpawnTimerPacket : Module
    {
        //Allow player to usei t again
        protected override void Receive()
        {
            Hookstaff.spawnTimer = 0;
        }
    }
}