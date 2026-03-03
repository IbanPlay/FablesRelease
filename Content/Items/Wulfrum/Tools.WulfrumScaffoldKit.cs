using System.IO;
using Terraria.ModLoader.IO;

namespace CalamityFables.Content.Items.Wulfrum
{
    [ReplacingCalamity("WulfrumScaffoldKit")]
    public class WulfrumScaffoldKit : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumItems + Name;

        public int storedScrap = 0;
        public static int TilesPerScrap = 40;
        public static int TileTime = 6 * 60;
        public static int TileReach = 40;
        public static int PlacedTileType => ModContent.TileType<WulfrumTemporaryPipes>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Scaffold Kit");
            Tooltip.SetDefault("Places down temporary metal scaffolding. Uses up one wulfrum metal scrap for " + TilesPerScrap.ToString() + " tiles built\n" +
            "Scaffold needs to be adjacent to a solid tile to be placed down\n" +
            "[c/83B87E:'For when you need something built fast and don't need it to last.']"
            );
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 42;
            Item.useTime = Item.useAnimation = 25;
            Item.autoReuse = false;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.HiddenAnimation;
            Item.UseSound = null;
            Item.noMelee = true;
            Item.channel = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 50);
            storedScrap = 0;
            Item.shoot = ModContent.ProjectileType<WulfrumScaffoldKitHoldout>();
            TileTime = 6 * 60;
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
        }

        public override bool CanUseItem(Player player)
        {
            return (storedScrap > 0 || player.HasItem(ModContent.ItemType<WulfrumMetalScrap>())) && !player.noBuilding
                && !Main.projectile.Any(n => n.active && n.owner == player.whoAmI && n.type == Item.shoot);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float barScale = 1f;

            var barBG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarBack").Value;
            var barFG = ModContent.Request<Texture2D>(AssetDirectory.UI + "GenericBarFront").Value;

            Vector2 drawPos = position + Vector2.UnitY * (frame.Height - 2) * scale + Vector2.UnitX * (frame.Width - barBG.Width * barScale) * scale * 0.5f;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(storedScrap / (float)TilesPerScrap * barFG.Width), barFG.Height);
            Color colorBG = Color.RoyalBlue;
            Color colorFG = Color.Lerp(Color.Teal, Color.YellowGreen, storedScrap / (float)TilesPerScrap);

            spriteBatch.Draw(barBG, drawPos, null, colorBG, 0f, origin, scale * barScale, 0f, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, colorFG * 0.8f, 0f, origin, scale * barScale, 0f, 0f);
        }


        #region saving the durability
        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);
            if (clone is WulfrumScaffoldKit a && item.ModItem is WulfrumScaffoldKit a2)
            {
                a.storedScrap = a2.storedScrap;
            }
            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["storedScrap"] = storedScrap;
        }

        public override void LoadData(TagCompound tag)
        {
            storedScrap = tag.GetInt("storedScrap");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(storedScrap);
        }

        public override void NetReceive(BinaryReader reader)
        {
            storedScrap = reader.ReadInt32();
        }
        #endregion

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<WulfrumMetalScrap>(6).
                AddIngredient<EnergyCore>(1).
                AddTile(TileID.Anvils).
                Register();
        }
    }


    public class WulfrumScaffoldKitHoldout : ModProjectile
    {
        public override void Load()
        {
            PipeCleanupManager = new WulfrumPipeManager();
        }

        public Player Owner => Main.player[Projectile.owner];
        public WulfrumScaffoldKit Kit => Owner.HeldItem.ModItem as WulfrumScaffoldKit;
        public bool CanOwnerGoOn => Kit.storedScrap > 0 || Owner.HasItem(ModContent.ItemType<WulfrumMetalScrap>());

        public bool CanSelectTile(Point tilePos)
        {
            //You only need to check when starting a scaffold
            if (SelectedTiles.Count > 0)
                return CanSelectMoreTiles(tilePos);


            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (Main.tile[tilePos.X + i, tilePos.Y + j].IsTileFull())
                        return true;
                }
            }

            return false;
        }
        public bool CanSelectMoreTiles(Point tilePos)
        {
            for (int i = -2; i < 3; i++)
            {
                for (int j = -2; j < 3; j++)
                {
                    if (Math.Abs(i) == 2 && Math.Abs(j) == 2)
                        continue;

                    if (Main.tile[tilePos.X + i, tilePos.Y + j].TileType == WulfrumScaffoldKit.PlacedTileType || SelectedTiles.ContainsKey(new Point(tilePos.X + i, tilePos.Y + j)))
                        return true;
                }
            }

            return false;
        }

        public static TemporaryTileManager PipeCleanupManager;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Scaffold Kit");
        }

        public static int tileGlowTime = 10;

        public Dictionary<Point, int> SelectedTiles = new Dictionary<Point, int>(); //Might need some cloning stuff for mp? idk, probably not
        public override string Texture => AssetDirectory.Invisible;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Owner.channel && CanOwnerGoOn)
            {
                //Initialize the position
                if (Projectile.timeLeft > 2)
                    Projectile.position = Owner.MouseWorld();

                Owner.itemTime = 2;
                Owner.itemAnimation = 2;

                Projectile.position = Projectile.position.MoveTowards(Owner.MouseWorld(), 16);
                //Projectile.position = raytracePosition();

                if ((Projectile.position - Owner.Center).Length() > WulfrumScaffoldKit.TileReach * 16)
                    Projectile.position = Owner.Center + Vector2.Normalize(Projectile.position - Owner.Center) * WulfrumScaffoldKit.TileReach * 16;

                if (Owner.whoAmI == Main.myPlayer)
                {
                    Point tilePos = Projectile.position.ToTileCoordinates();
                    Tile hoveredTile = Main.tile[tilePos];

                    if ((!hoveredTile.HasTile || Main.tileCut[hoveredTile.TileType] || TileID.Sets.BreakableWhenPlacing[hoveredTile.TileType]) && !SelectedTiles.ContainsKey(tilePos) && CanSelectTile(tilePos))
                    {
                        SelectedTiles.Add(tilePos, tileGlowTime);

                        if (Kit.storedScrap > 0)
                            Kit.storedScrap--;

                        else
                        {
                            Owner.ConsumeItem(ModContent.ItemType<WulfrumMetalScrap>());
                            Kit.storedScrap = WulfrumScaffoldKit.TilesPerScrap - 1;
                            SoundEngine.PlaySound(SoundID.Item65);
                            if (Main.netMode != NetmodeID.Server)
                            {
                                Gore shard = Gore.NewGoreDirect(Owner.GetSource_ItemUse(Owner.HeldItem), Owner.Center, Main.rand.NextVector2Circular(4f, 4f), Mod.Find<ModGore>("WulfrumPinger2").Type, Main.rand.NextFloat(0.5f, 1f));
                                shard.timeLeft = 10;
                                shard.alpha = 100 - Main.rand.Next(0, 60);
                            }
                        }
                    }

                    foreach (Point position in SelectedTiles.Keys)
                    {
                        if (SelectedTiles[position] > 0)
                            SelectedTiles[position]--;
                    }
                }

                Projectile.timeLeft = 2;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Owner.whoAmI)
                return false;

            Texture2D sprite = TextureAssets.Projectile[Type].Value;

            Effect tileEffect = Scene["WulfrumScaffoldSelection"].GetShader().Shader;

            tileEffect.Parameters["mainOpacity"].SetValue(1f);
            tileEffect.Parameters["tileEdgeBlendStrenght"].SetValue(2f);
            tileEffect.Parameters["placementGlowColor"].SetValue(Color.GreenYellow.ToVector4());
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

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, tileEffect, Main.GameViewMatrix.TransformationMatrix);

            foreach (Point pos in SelectedTiles.Keys)
            {
                tileEffect.Parameters["blinkTime"].SetValue(SelectedTiles[pos] / (float)tileGlowTime);
                tileEffect.Parameters["cardinalConnections"].SetValue(new bool[] { Connected(pos, 0, -1), Connected(pos, -1, 0), Connected(pos, 1, 0), Connected(pos, 0, 1) });
                tileEffect.Parameters["tilePosition"].SetValue(pos.ToVector2() * 16f);

                Main.spriteBatch.Draw(sprite, pos.ToWorldCoordinates() - Main.screenPosition, null, Color.White, 0, new Vector2(sprite.Width / 2f, sprite.Height / 2f), 8f, 0, 0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public bool Connected(Point pos, int displaceX, int displaceY) => SelectedTiles.ContainsKey(new Point(pos.X + displaceX, pos.Y + displaceY));


        public override void OnKill(int timeLeft)
        {
            if (SelectedTiles.Keys.Count > 0)
                SoundEngine.PlaySound(SoundID.Item101 with { Volume = SoundID.Item101.Volume * 0.6f }, Owner.Center);

            if (Main.myPlayer == Owner.whoAmI)
            {
                //Sync packet runs locally so it's fine
                Module packet = new WulfrumScaffoldKitSyncPacket((byte)Main.myPlayer, SelectedTiles);
                packet.Send();
            }
        }
    }

    [Serializable]
    public class WulfrumScaffoldKitSyncPacket : Module
    {
        public readonly byte whoAmI;
        public Point[] selectedPositions;

        public WulfrumScaffoldKitSyncPacket(byte whoAmI, Dictionary<Point, int> selectedTiles)
        {
            this.whoAmI = whoAmI;
            selectedPositions = new Point[selectedTiles.Keys.Count];

            int i = 0;
            foreach (Point pos in selectedTiles.Keys)
            {
                selectedPositions[i] = pos;
                i++;
            }
        }

        protected override void Receive()
        {
            if (WulfrumScaffoldKitHoldout.PipeCleanupManager == null)
                WulfrumScaffoldKitHoldout.PipeCleanupManager = new WulfrumPipeManager();

            for (int i = 0; i < selectedPositions.Length; i++)
            {
                Point pos = selectedPositions[i];
                Tile t = Main.tile[pos];
                if (t.HasTile && (Main.tileCut[t.TileType] || TileID.Sets.BreakableWhenPlacing[t.TileType]))
                    WorldGen.KillTile(pos.X, pos.Y);
                TempTilesManagerSystem.AddTemporaryTile(pos, WulfrumScaffoldKitHoldout.PipeCleanupManager);
                WorldGen.PlaceTile(pos.X, pos.Y, WulfrumScaffoldKit.PlacedTileType);
            }

            if (Main.netMode == NetmodeID.Server)
                Send(-1, whoAmI, false);
        }
    }

    public class WulfrumPipeManager : TemporaryTileManager
    {
        public override int[] ManagedTypes => new int[] { WulfrumScaffoldKit.PlacedTileType };

        public override TemporaryTile Setup(Point pos)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 dustpos = pos.ToWorldCoordinates();
                Dust.NewDustPerfect(dustpos, 83, Main.rand.NextVector2Circular(3f, 3f), Scale: Main.rand.NextFloat(0.4f, 0.7f));
            }

            TemporaryTile tile = new TemporaryTile(pos, this, WulfrumScaffoldKit.TileTime);
            return tile;
        }

        public override void UpdateEffect(TemporaryTile tile)
        {
            if (tile.timeleft < WulfrumScaffoldKit.TileTime * 0.1f && Main.rand.NextBool(10))
            {
                Vector2 dustpos = tile.position.ToWorldCoordinates();
                Dust.NewDustPerfect(dustpos, 226, Main.rand.NextVector2Circular(4f, 4f), Scale: Main.rand.NextFloat(0.4f, 1f));
            }
        }
    }

    public class WulfrumTemporaryPipes : ModTile, ISpecialTempTileDraw
    {
        public static int PlaceTimeMax = 10;
        //public float PlaceProgress(Point pos) => Math.Clamp((PlaceTimeStart - Main.GameUpdateCount) / (pos.GetTileRNG() * 5f + 5f), 0, 1);
        //public float PlaceProgress(Point pos) => Math.Clamp((PlaceTimeStart - Main.GameUpdateCount) / (pos.GetTileRNG() * 5f + 5f), 0, 1);

        public static Vector2 DisplaceStart(Point pos) => new Vector2(0, -(pos.GetSmoothTileRNG() * 10f + 7f));
        public static float RotationStart(Point pos) => -MathHelper.PiOver4 * 0.8f + MathHelper.PiOver4 * 1.6f * pos.GetSmoothTileRNG(1);

        public override string Texture => AssetDirectory.WulfrumTiles + Name;

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            HitSound = SoundID.Item52;
            DustType = 83;
            AddMapEntry(new Color(128, 90, 77));
            Main.tileLighted[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.00f;
            g = 0.6f;
            b = 0.3f;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = Main.rand.NextBool(3) ? 3 : Main.rand.NextBool(3) ? 1 : 2;
        }

        public override void PlaceInWorld(int i, int j, Item item)
        {
            SoundEngine.PlaySound(SoundID.Item52 with { Volume = SoundID.Item52.Volume * 0.75f, Pitch = SoundID.Item52.Pitch - 0.5f }, new Vector2(i * 16, j * 16));
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            return false;
        }

        public void CoolDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Point pos = new Point(i, j);
            Tile tile = Main.tile[pos];

            float timeToGo = pos.GetSmoothTileRNG(2) * 22;
            float animProgress = (float)Math.Pow(MathHelper.Clamp((TempTilesManagerSystem.GetTemporaryTileTime(pos) - (WulfrumScaffoldKit.TileTime - timeToGo)) / timeToGo, 0, 1), 2);

            Vector2 position = pos.ToWorldCoordinates() + DisplaceStart(pos) * animProgress - Main.screenPosition;
            Rectangle frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);

            Color tileColor = Lighting.GetColor(pos) * (1 - animProgress);

            Main.spriteBatch.Draw(TextureAssets.Tile[Type].Value, position, frame, tileColor, RotationStart(pos) * animProgress, frame.Size() / 2f, 1f, 0, 0);
        }
    }
}
