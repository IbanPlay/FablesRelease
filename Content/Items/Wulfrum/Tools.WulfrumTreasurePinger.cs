using CalamityFables.Particles;
using System.IO;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumTreasurePinger")]
    public class WulfrumTreasurePinger : ModItem
    {
        public static readonly SoundStyle ScanBeepSound = new(SoundDirectory.Wulfrum + "WulfrumPing") { PitchVariance = 0.1f };
        public static readonly SoundStyle ScanBeepBreakSound = new(SoundDirectory.Wulfrum + "WulfrumPingBreak");
        public static readonly SoundStyle RechargeBeepSound = new(SoundDirectory.Wulfrum + "WulfrumPingReady") { PitchVariance = 0.1f };

        public int usesLeft = maxUses;
        public const int maxUses = 20;
        public static int breakTime = 90;
        public int timeBeforeBlast = 90;

        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Treasure Pinger");
            Tooltip.SetDefault("Helps you find metal that's hopefully more valuable than wulfrum\n" +
            "This contraption seems incredibly shoddy. [c/fc4903:It'll break sooner than later for sure]"
            );
            Item.ResearchUnlockCount = 1;

            if (Main.dedServ)
                return;
            for (int i = 1; i < 5; i++)
                ChildSafety.SafeGore[Mod.Find<ModGore>("WulfrumPinger" + i.ToString()).Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 42;
            Item.useTime = Item.useAnimation = 25;
            Item.autoReuse = false;
            Item.holdStyle = 16; //Custom hold style
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 50);
            usesLeft = maxUses;
            timeBeforeBlast = breakTime;
            Item.ChangePlayerDirectionOnShoot = false;
        }

        /*
		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
		{
			itemGroup = (ContentSamples.CreativeHelper.ItemGroup)CalamityResearchSorting.ToolsOther;
		}
        */

        public override void HoldItem(Player player)
        {
            player.SyncMousePosition();

            //If the explosion sequence has been initiated.
            if (usesLeft <= 0 && timeBeforeBlast < breakTime)
            {
                timeBeforeBlast--;
                player.itemTime = 2;
                player.itemAnimation = 2;

                float breakProgress = 1 - timeBeforeBlast / (float)breakTime;

                int smokeLikelyhood = (int)Math.Floor(1 + timeBeforeBlast / (float)breakTime * 4);
                if (Main.rand.NextBool(smokeLikelyhood))
                {
                    Vector2 smokePos = player.GetBackHandPosition(player.compositeBackArm.stretch, player.compositeBackArm.rotation).Floor();
                    Vector2 velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 0.7f) * Main.rand.NextFloat(6f, 9f + 6f * breakProgress);

                    Color smokeStart = Main.rand.NextBool() ? Color.GreenYellow : Color.DeepSkyBlue;
                    Color smokeEnd = Color.Lerp(new Color(110, 110, 110), new Color(60, 60, 60), breakProgress);

                    float smokeSize = Main.rand.NextFloat(1.4f, 2.2f) * (0.6f + 0.4f * breakProgress);

                    Particle smoke = new SmallSmokeParticle(smokePos, velocity, smokeStart, smokeEnd, smokeSize, 115 - Main.rand.Next(30));
                    ParticleHandler.SpawnParticle(smoke);
                }
            }

            if (timeBeforeBlast <= 0)
            {
                int scrapRefund = Main.rand.Next(0, 4);
                if (scrapRefund > 0)
                    player.QuickSpawnItem(player.GetSource_ItemUse(Item), ModContent.ItemType<WulfrumMetalScrap>(), scrapRefund);

                Item.TurnToAir();

                int smokeCount = Main.rand.Next(5, 10);
                int shrapnelCount = Main.rand.Next(3, 5);
                int sparkCount = Main.rand.Next(4, 8);

                Vector2 centerPosition = player.GetBackHandPosition(player.compositeBackArm.stretch, player.compositeBackArm.rotation).Floor();

                for (int i = 0; i < smokeCount; i++)
                {
                    Vector2 velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(12f, 16f);

                    Color smokeStart = Main.rand.NextBool() ? Color.GreenYellow : Color.Aqua;
                    Color smokeEnd = new Color(60, 60, 60);

                    float smokeSize = Main.rand.NextFloat(1.4f, 2.2f);

                    Particle smoke = new SmallSmokeParticle(centerPosition, velocity, smokeStart, smokeEnd, smokeSize, 135 - Main.rand.Next(30));
                    ParticleHandler.SpawnParticle(smoke);
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < shrapnelCount; i++)
                    {
                        Vector2 shrapnelVelocity = Main.rand.NextVector2Circular(9f, 9f);
                        float shrapnelScale = Main.rand.NextFloat(0.8f, 1f);

                        Gore.NewGore(player.GetSource_ItemUse(Item), centerPosition, shrapnelVelocity, Mod.Find<ModGore>("WulfrumPinger" + Main.rand.Next(1, 5).ToString()).Type, shrapnelScale);
                    }
                }

                for (int i = 0; i < sparkCount; i++)
                {
                    Dust.NewDustPerfect(centerPosition, 226, Main.rand.NextVector2Circular(18f, 18f), Scale: Main.rand.NextFloat(0.4f, 1f));
                }
            }
        }

        public override bool CanUseItem(Player player) => !(TilePingerSystem.tileEffects["WulfrumPingTileEffect"].Active);

        public override bool? UseItem(Player player)
        {
            if (TilePingerSystem.AddPing("WulfrumPingTileEffect", player.Center, player))
            {
                if (player.name != "John Wulfrum")
                    usesLeft--;

                if (usesLeft <= 0)
                {
                    if (player.whoAmI == Main.myPlayer)
                        SoundEngine.PlaySound(ScanBeepBreakSound);

                    //Start the breaking anim.
                    timeBeforeBlast--;
                }

                else if (player.whoAmI == Main.myPlayer)
                    SoundEngine.PlaySound(ScanBeepSound);
                else
                    SoundEngine.PlaySound(ScanBeepSound with { Volume = ScanBeepSound.Volume * 0.4f}, player.Center);

                return true;
            }

            return false;
        }

        #region drawing stuff
        public void SetItemInHand(Player player, Rectangle heldItemFrame)
        {
            //Make the player face where they're aiming.
            if (player.MouseWorld().X > player.Center.X)
            {
                player.ChangeDir(1);
            }
            else
            {
                player.ChangeDir(-1);
            }

            float itemRotation = player.compositeBackArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.GetBackHandPositionImproved(player.compositeBackArm).Floor();
            Vector2 itemSize = new Vector2(52, 42);
            Vector2 itemOrigin = new Vector2(-20, -13);

            if (usesLeft == 0)
            {
                itemPosition += Main.rand.NextVector2CircularEdge(4f, 4f) * (1 - timeBeforeBlast / (float)breakTime);
                //We have to lower the timebeforeblast here as well because the item that gets processed here is not actually the same item as the item held in the players hands.
                //Basically we are dealing with a clone of the item that the drawn player uses.
                //So no, don't worry, the item isn't breaking twice as fast.
                timeBeforeBlast--;
            }

            FablesUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
        }

        public void SetPlayerArms(Player player)
        {
            //Calculate the dirction in which the players arms should be pointing at.
            Vector2 playerToCursor = (player.MouseWorld() - player.Center).SafeNormalize(Vector2.UnitX);
            float armPointingDirection = (playerToCursor.ToRotation() + MathHelper.PiOver2).Modulo(MathHelper.TwoPi);

            //"crop" the rotation so the player only tilts the fishing rod slightly up and slightly down.
            if (armPointingDirection < MathHelper.Pi)
            {
                armPointingDirection = armPointingDirection / MathHelper.Pi * MathHelper.PiOver4 * 0.5f - MathHelper.PiOver4 * 0.3f;
            }

            //It gets a bit harder if its pointing left; ouch
            else
            {
                armPointingDirection -= MathHelper.Pi;

                armPointingDirection = armPointingDirection / MathHelper.Pi * MathHelper.PiOver4 * 0.5f - MathHelper.PiOver4 * 0.3f + MathHelper.Pi;
            }

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armPointingDirection * player.gravDir - MathHelper.PiOver2);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armPointingDirection * player.gravDir - MathHelper.PiOver2);
        }

        public override void HoldStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);
        public override void UseStyle(Player player, Rectangle heldItemFrame) => SetItemInHand(player, heldItemFrame);
        public override void HoldItemFrame(Player player) => SetPlayerArms(player);
        public override void UseItemFrame(Player player) => SetPlayerArms(player);

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (usesLeft == maxUses || usesLeft == 0)
                return;

            float barScale = 1.3f;

            var barBG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarBack").Value;
            var barFG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarFront").Value;

            Vector2 drawPos = position + Vector2.UnitY * (frame.Height - 2) * scale + Vector2.UnitX * (frame.Width - barBG.Width * barScale) * scale * 0.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(usesLeft / (float)maxUses * barFG.Width), barFG.Height);
            Color colorBG = Color.RoyalBlue;
            Color colorFG = Color.Lerp(Color.Teal, Color.YellowGreen, usesLeft / (float)maxUses);

            spriteBatch.Draw(barBG, drawPos, null, colorBG, 0f, origin, scale * barScale, 0f, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, colorFG * 0.8f, 0f, origin, scale * barScale, 0f, 0f);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (usesLeft > 0)
                return true;

            var tex = TextureAssets.Item[Type].Value;

            float blastProgress = (1 - timeBeforeBlast / (float)breakTime);
            position += Main.rand.NextVector2CircularEdge(4f, 4f) * blastProgress;
            drawColor = Color.Lerp(drawColor, Color.OrangeRed, blastProgress);

            spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale, 0f, 0f);
            return false;
        }
        #endregion

        #region saving the durability
        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);
            if (clone is WulfrumTreasurePinger a && item.ModItem is WulfrumTreasurePinger a2)
            {
                a.usesLeft = a2.usesLeft;
                a.timeBeforeBlast = a2.timeBeforeBlast;
            }
            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["usesLeft"] = usesLeft;
        }

        public override void LoadData(TagCompound tag)
        {
            usesLeft = tag.GetInt("usesLeft");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(usesLeft);
        }

        public override void NetReceive(BinaryReader reader)
        {
            usesLeft = reader.ReadInt32();
        }
        #endregion

        public override void AddRecipes()
        {
            //Intentionally craftable anywhere.
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(6).
                Register();
        }
    }

    public class WulfrumPingTileEffect : IPingedTileEffect
    {
        internal static Texture2D emptyFrame;
        const int MaxPingLife = 350;
        const int MaxPingTravelTime = 60;
        const float PingWaveThickness = 50f;

        const float MaxPingRadius = 1700f;
        public static Vector2 PingCenter = Vector2.Zero;
        public static int PingTimer = 0;
        public static float PingProgress => (MaxPingLife - PingTimer) / (float)MaxPingLife;

        public bool Active => PingTimer > 0;

        public BlendState BlendState => BlendState.Additive;

        public bool TryAddPing(Vector2 position, Player pinger)
        {
            //Only one ping at a time
            if (Active)
                return false;

            PingCenter = position;
            PingTimer = MaxPingLife;
            return true;
        }


        public Effect SetupEffect()
        {
            if (emptyFrame == null)
                emptyFrame = ModContent.Request<Texture2D>(AssetDirectory.Invisible).Value;

            Effect tileEffect = Scene["WulfrumTilePing"].GetShader().Shader;
            tileEffect.Parameters["pingCenter"].SetValue(PingCenter);
            tileEffect.Parameters["pingRadius"].SetValue(MaxPingRadius);
            tileEffect.Parameters["pingWaveThickness"].SetValue(PingWaveThickness);
            tileEffect.Parameters["pingProgress"].SetValue(PingProgress);
            tileEffect.Parameters["pingTravelTime"].SetValue(MaxPingTravelTime / (float)MaxPingLife);
            tileEffect.Parameters["pingFadePoint"].SetValue(0.9f);
            tileEffect.Parameters["edgeBlendStrength"].SetValue(1f);
            tileEffect.Parameters["edgeBlendOutLenght"].SetValue(6f);
            tileEffect.Parameters["tileEdgeBlendStrenght"].SetValue(2f);

            tileEffect.Parameters["waveColor"].SetValue(Color.GreenYellow.ToVector4());
            tileEffect.Parameters["baseTintColor"].SetValue(Color.DeepSkyBlue.ToVector4() * 0.5f);
            tileEffect.Parameters["scanlineColor"].SetValue(Color.YellowGreen.ToVector4() * 1f);
            tileEffect.Parameters["tileEdgeColor"].SetValue(Color.GreenYellow.ToVector3());
            tileEffect.Parameters["Resolution"].SetValue(8f);

            tileEffect.Parameters["time"].SetValue(Main.GameUpdateCount);
            Vector4[] scanLines = new Vector4[]
            {
                new Vector4(0f, 4f, 0.1f, 0.5f),
                new Vector4(1f, 4f, 0.1f, 0.5f),
                new Vector4(37f, 60f, 0.4f, 1f),
                new Vector4(2f, 6f, -0.2f, 0.3f),
                new Vector4(0f, 4f, 0.1f, 0.5f), //vertical start
                new Vector4(1f, 4f, 0.1f, 0.5f),
                new Vector4(2f, 6f, -0.2f, 0.3f)
            };

            tileEffect.Parameters["ScanLines"].SetValue(scanLines);
            tileEffect.Parameters["ScanLinesCount"].SetValue(scanLines.Length);
            tileEffect.Parameters["verticalScanLinesIndex"].SetValue(4);

            return tileEffect;
        }

        public void PerTileSetup(Point pos, ref Effect effect)
        {
            //Up, left, right, down.
            effect.Parameters["cardinalConnections"].SetValue(new bool[] { Connected(pos, 0, -1), Connected(pos, -1, 0), Connected(pos, 1, 0), Connected(pos, 0, 1) });
            effect.Parameters["tilePosition"].SetValue(pos.ToVector2() * 16f);
        }

        public static bool Connected(Point pos, int displaceX, int displaceY) => Main.IsTileSpelunkable(pos.X + displaceX, pos.Y + displaceY) && Main.tile[pos].TileType == Main.tile[pos.X + displaceX, pos.Y + displaceY].TileType;

        public bool ShouldRegisterTile(int x, int y)
        {
            return Main.IsTileSpelunkable(x, y);
        }

        public void DrawTile(Point pos)
        {
            Main.spriteBatch.Draw(emptyFrame, pos.ToWorldCoordinates() - Main.screenPosition, null, Color.White, 0, new Vector2(emptyFrame.Width / 2f, emptyFrame.Height / 2f), 8f, 0, 0);
        }

        public void EditDrawData(int i, int j, ref TileDrawInfo drawData)
        {
            float distanceFromCenter = (new Point(i, j).ToWorldCoordinates() - PingCenter).Length();
            float currentExpansion = MathHelper.Clamp(PingProgress * MaxPingLife / (float)MaxPingTravelTime, 0f, 1f) * MaxPingRadius;

            if (distanceFromCenter - 8 > currentExpansion)
                return;

            float brightness = 1f;
            Tile tile = Framing.GetTileSafely(i, j);
            //Counteracts slopes and half tiles being too bright
            if (tile.Slope != SlopeType.Solid || tile.IsHalfBlock)
                brightness = 0.64f;

            //Fade on the edges
            if (distanceFromCenter + 8 > currentExpansion)
                brightness *= 1 - (distanceFromCenter - currentExpansion + 8f) / 16f;

            //Fade away with the effect
            brightness *= 1 - Math.Max(PingProgress - 0.9f, 0) / (0.1f);

            if (drawData.tileLight.R < 200 * brightness) drawData.tileLight.R = (byte)(200 * brightness);
            if (drawData.tileLight.G < 200 * brightness) drawData.tileLight.G = (byte)(200 * brightness);
            if (drawData.tileLight.B < 200 * brightness) drawData.tileLight.B = (byte)(200 * brightness);
        }

        public void UpdateEffect()
        {
            if (PingTimer > 0)
            {
                PingTimer--;

                //if the effect ended (and the player has a treasure pigner in their inventory, of course), play a recharge beep
                if (PingTimer == 0 && Main.LocalPlayer.InventoryHas(ModContent.ItemType<WulfrumTreasurePinger>()))
                    SoundEngine.PlaySound(WulfrumTreasurePinger.RechargeBeepSound);
            }
        }
    }
}
