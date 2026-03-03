using CalamityFables.Content.Items.Wulfrum;
using CalamityFables.Content.NPCs.Wulfrum;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;
using Terraria.ModLoader.IO;
using Terraria.GameContent.Drawing;

namespace CalamityFables.Content.Tiles.Wulfrum
{
    [ReplacingCalamity("WulfrumLureItem")]
    public class WulfrumLureItem : ModItem
    {
        public static int SignalTime = 50 * 60;
        public static int SpawnIntervals = 10 * 60;
        public static int MaxEnemiesPerWave = 5;

        public override string Texture => AssetDirectory.WulfrumTiles + Name;


#if !DEBUG
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SignalTime / 60f);
#endif

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            DisplayName.SetDefault("Wulfrum Lure");
            Tooltip.SetDefault("Emit a wulfrum signal that lures Wulfrum automatons out, lasting for {0} seconds\n" +
                "Can only be triggered once per day");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileType<WulfrumLure>());
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(0, 0, 10);
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1).
            AddIngredient(ItemType<EnergyCore>(), 3).
            AddIngredient(ItemType<WulfrumMetalScrap>(), 7).
            AddTile(TileID.Anvils).
            Register();
        }
    }


    public class WulfrumLure : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.WulfrumTiles + Name;
        public static readonly SoundStyle UseFailSound = new SoundStyle(SoundDirectory.Wulfrum + "WulfrumLureFail");

        public override void Load()
        {
            FablesGeneralSystemHooks.SaveWorldDataEvent += SaveWulfrumLureUseage;
            FablesGeneralSystemHooks.LoadWorldDataEvent += LoadWulfrumLureUseage;
            FablesGeneralSystemHooks.NetSendEvent += SendWulfrumLureUseage;
            FablesGeneralSystemHooks.NetReceiveEvent += RecieveWulfrumLureUseage;


            //Set to false when the day starts and when the world gets cleared
            FablesGeneralSystemHooks.OnDayStart += ResetWulfrumLureUseage;
            FablesGeneralSystemHooks.ClearWorldEvent += ResetWulfrumLureUseage;
            FablesGeneralSystemHooks.PreUpdateProjectilesEvent += ResetForceGlowDraw;
        }

        #region Once a day stuff
        public static bool UsedWulfrumLureToday = false;
        public static float GaClankAnim = 0f;


        public static bool ForceEnabledDrawing;
        private void ResetForceGlowDraw()
        {
            ForceEnabledDrawing = false;
            GaClankAnim -= 1 / 60f;
            if (GaClankAnim < 0f)
                GaClankAnim = 0f;
        }

        private void ResetWulfrumLureUseage() => UsedWulfrumLureToday = false;
        private void LoadWulfrumLureUseage(TagCompound tag)
        {
            UsedWulfrumLureToday = tag.GetOrDefault<bool>("WulfrumLureUsed");
        }

        private void SaveWulfrumLureUseage(TagCompound tag)
        {
            tag["WulfrumLureUsed"] = UsedWulfrumLureToday;
        }


        private void SendWulfrumLureUseage(System.IO.BinaryWriter writer)
        {
            writer.Write(UsedWulfrumLureToday);
        }
        private void RecieveWulfrumLureUseage(System.IO.BinaryReader reader)
        {
            UsedWulfrumLureToday = reader.ReadBoolean();
        }
        #endregion

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.addTile(Type);
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("Wulfrum Lure");
            AddMapEntry(new Color(194, 255, 67), name);
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = 83;
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            int left = i - tile.TileFrameX / 18;
            int top = j - tile.TileFrameY / 18;

            //Doesnt work
            if (UsedWulfrumLureToday)
            {
                GaClankAnim = 1f;
                SoundEngine.PlaySound(UseFailSound);
                return true;
            }

            if (Main.projectile.Any(p => p.active && p.type == ProjectileType<WulfrumLureSignal>() && (p.Center - Main.LocalPlayer.Center).Length() < 2000))
                return true;

            Vector2 lurePosition = new Vector2(left + 1, top + 1).ToWorldCoordinates(0, 0);
            lurePosition += new Vector2(0f, -24f);

            SoundEngine.PlaySound(WulfrumTreasurePinger.ScanBeepSound, lurePosition);
            Projectile.NewProjectile(new EntitySource_WorldEvent(), lurePosition, Vector2.Zero, ProjectileType<WulfrumLureSignal>(), 0, 0f, Main.myPlayer);

            new SyncWulfrumLureUseToday().Send();
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconID = ItemType<WulfrumLureItem>();
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (drawData.tileFrameX == 0 && drawData.tileFrameY == 0)
            {
                ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.Background, true);
            }
            ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles, true);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Point p = new Point(i, j);
            Color color = Lighting.GetColor(p);
            Tile t = Main.tile[p];
            if (t.IsTileFullbright)
                color = Color.White;

            bool disabled = UsedWulfrumLureToday && !ForceEnabledDrawing;

                Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            if (layer == TileDrawLayer.Background)
            {
                Vector2 drawPos = p.ToWorldCoordinates() - Main.screenPosition;
                Rectangle frame = new Rectangle(0, 38, 34, 34);
                float rotation = Main.GlobalTimeWrappedHourly * 1.5f;
                if (disabled)
                    rotation = (float)Math.Pow(GaClankAnim, 5f) * 0.6f;
                spriteBatch.Draw(texture, drawPos + Vector2.One * 8, frame, color, rotation, new Vector2(17, 17), 1f, 0, 0f);
            }
            else
            {
                if (!TileDrawing.IsVisible(t))
                    return;

                Vector2 drawPos = p.ToWorldCoordinates(0, 0) - Main.screenPosition;
                Rectangle frame = new Rectangle(t.TileFrameX, t.TileFrameY, 16, 16);

                //Dark lights
                if (UsedWulfrumLureToday && !ForceEnabledDrawing)
                    frame.X += 36 * 2;
                //Glowing
                else
                {
                    frame.X += 36;
                    color = Color.White;
                }

                spriteBatch.Draw(texture, drawPos, frame, color,0, Vector2.Zero, 1f, 0, 0f);
            }
        }
    }

    public class WulfrumLureSignal : ModProjectile
    {
        public override string Texture => AssetDirectory.Invisible;
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Signal");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = WulfrumLureItem.SignalTime;
        }

        public override void AI()
        {
            Time++;
            WulfrumLure.ForceEnabledDrawing = true;

            if (Time % WulfrumLureItem.SpawnIntervals == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player player = Main.LocalPlayer;
                if (Main.netMode == NetmodeID.Server)
                {
                    float closestPlayerDistance = float.MaxValue;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (!Main.player[i].active || Main.player[i].dead)
                            continue;

                        float newDistance = (Main.player[i].Center - Projectile.Center).Length();
                        if (newDistance < closestPlayerDistance)
                        {
                            closestPlayerDistance = newDistance;
                            player = Main.player[i];
                        }
                    }
                }

                if ((player.Center - Projectile.Center).Length() > 3500)
                    return;

                WulfrumNexus.SpawnFormation(Projectile.Center, WulfrumNexus.GetFormation(Main.rand.Next(3, 6)), Projectile.GetSource_FromThis(), (Projectile.Center.X - player.Center.X).NonZeroSign(), false);
            }

            if (Time % 2 == 0 && FablesUtils.IntoMorseCode("perimeter breached", Time / WulfrumLureItem.SignalTime))
            {
                float dustCount = MathHelper.TwoPi * 300 / 8f;
                for (int i = 0; i < dustCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / dustCount;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, 229);
                    dust.position = Projectile.Center + angle.ToRotationVector2() * 300;
                    dust.scale = 0.7f;
                    dust.noGravity = true;
                    dust.velocity = Projectile.velocity;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(WulfrumTreasurePinger.RechargeBeepSound, Projectile.Center);
        }
    }

    [Serializable]
    public class SyncWulfrumLureUseToday : Module
    {
        byte whoAmI;
        public SyncWulfrumLureUseToday() { whoAmI = (byte)Main.myPlayer; }

        protected override void Receive()
        {
            WulfrumLure.UsedWulfrumLureToday = true;

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }
}
