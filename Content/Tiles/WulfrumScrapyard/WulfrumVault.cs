using CalamityFables.Content.NPCs.Wulfrum;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using CalamityFables.Content.Tiles.WulfrumScrapyard;
using Microsoft.Xna.Framework.Input;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.Localization;
using Terraria.ObjectData;

namespace CalamityFables.Content.Tiles.WulfrumScrapyard
{
    public class WulfrumVault : ModTile, IModdedContainer
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public static readonly SoundStyle UnlockSound = new(SoundDirectory.Wulfrum + "WulfrumVaultUnlock");

        public override void SetStaticDefaults()
        {
            Main.tileSpelunker[Type] = true;
            Main.tileContainer[Type] = true;
            Main.tileShine2[Type] = true;
            Main.tileShine[Type] = 1200;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileOreFinderPriority[Type] = 500;
            TileID.Sets.BasicChest[Type] = true;
            FablesSets.CustomContainer[Type] = true;
            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);

            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;

            TileObjectData.newTile.Width = 5;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.Origin = new Point16(2, 2);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 18 };

            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, processedCoordinates: true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(PostPlaceHook, -1, 0, processedCoordinates: false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[5] {
                127, //Ice rod ice blocks
                138, //Boulders
                484, //Rolling cacti
                664,
                665
            };

            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);

            RegisterItemDrop(ModContent.ItemType<WulfrumVaultItem>());
            AddMapEntry(CommonColors.WulfrumMetalLight, Language.GetOrRegister("Mods.CalamityFables.Tiles.WulfrumVault.MapEntry"));
            AddMapEntry(CommonColors.WulfrumMetalLight, Language.GetOrRegister("Mods.CalamityFables.Tiles.WulfrumVaultLocked.MapEntry"));
            WulfrumNexusSpawner.VaultType = Type;

            DustType = DustID.Tungsten;
            TileID.Sets.DisableSmartCursor[Type] = true;
            AdjTiles = new int[] { TileID.Containers };
        }

        public override ushort GetMapOption(int i, int j)
        {
            GetRowOrigin(ref i, ref j);
            return (ushort)(IsLockedChest(i, j) ? 1 : 0);
        }

        public override LocalizedText DefaultContainerName(int frameX, int frameY)
        {
            LocalizedText text = this.GetLocalization("ContainerName", () => "Wulfrum Vault");
            return text;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = 1;

        //Top left corner
        public static void GetRowOrigin(ref int i, ref int j)
        {
            Tile tile = Main.tile[i, j];
            i -= (tile.TileFrameX % 90) / 18;
            j -= tile.TileFrameY / 18;
        }

        public bool IsChestThere(Tile t) => t.TileFrameY == 0 && (t.TileFrameX % 90) == 0;

        #region Hover stuff
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => settings.player.InInteractionRange(i, j, TileReachCheckSettings.Simple);
        public override void MouseOver(int i, int j) => Hovering(i, j);
        public override void MouseOverFar(int i, int j) => Hovering(i, j, true);
        public static void Hovering(int i, int j, bool far = false)
        {
            GetRowOrigin(ref i, ref j);
            if (!far)
                FablesUtils.ChestMouseOver<WulfrumVaultItem>(i, j);
            else
                FablesUtils.ChestMouseFar<WulfrumVaultItem>(i, j);
        }
        #endregion

        #region Chest whatchamacallit
        public override bool RightClick(int i, int j)
        {
            GetRowOrigin(ref i, ref j);
            return FablesUtils.ChestRightClick(i, j, IsLockedChest(i, j));
        }

        public override bool IsLockedChest(int i, int j) => Main.tile[i, j].TileFrameX >= 90;

        public override bool UnlockChest(int i, int j, ref short frameXAdjustment, ref int dustType, ref bool manual)
        {
            bool canUnlock = true;
            //Can't unlock if theres still a tile entity overlapping the chest
            if (TileEntity.ByPosition.ContainsKey(new Point16(i, j)) && TileEntity.ByPosition[new Point16(i, j )] is WulfrumNexusSpawner)
                canUnlock = false;

            if (!canUnlock)
                return false;

            manual = true;
            SoundEngine.PlaySound(UnlockSound, new Vector2(i, j) * 16f);
            for (int x = i; x < i + 5; x++)
            {
                for (int y = j; y < j + 2; y++)
                {
                    Tile tileSafely2 = Framing.GetTileSafely(x, y);
                    if (tileSafely2.TileType == Type)
                    {
                        tileSafely2.TileFrameX -= 90;
                        
                        //Failsafe
                        if (tileSafely2.TileFrameX < 0)
                            tileSafely2.TileFrameX += 90; 
                    }
                }
            }

            //Voom
            ParticleHandler.SpawnParticle(new Particles.CircularPulseShine(new Vector2(i + 1.5f, j + 1.5f) * 16f, CommonColors.WulfrumBlue, 1f));
            ParticleHandler.SpawnParticle(new Particles.CircularPulseShine(new Vector2(i + 3.5f, j + 1.5f) * 16f, CommonColors.WulfrumBlue, 1f));

            dustType = DustType;
            return true;
        }

        public void SyncFrameOnUnlock(int x, int y)
        {
            NetMessage.SendTileSquare(-1, x - 1, y - 1, 7, 5);
        }

        public int StorageWidth => 5;
        public int StorageHeight => 3;

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => ModdedContainers.PreventAutodestructionTileFrame(Type, i, j, ref resetFrame, ref noBreak);

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            GetRowOrigin(ref i, ref j);
            return Chest.CanDestroyChest(i, j);
        }

        public bool TryDestroying(int x, int y)
        {
            GetRowOrigin(ref x, ref y);
            //CalamityFables.Instance.Logger.Debug("Attempting to destroy modded chest");
            return !Chest.DestroyChest(x, y);
        }

        public static int PostPlaceHook(int x, int y, int type = 21, int style = 0, int direction = 1, int alternate = 0)
        {
            GetRowOrigin(ref x, ref y);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Chest.CreateChest(x, y);
            }
            else
            {
                var packet = new SyncGenericContainerModule(Main.myPlayer, true, x, y, direction, type, ModContent.ItemType<WulfrumVaultItem>());
                packet.Send(-1, -1, false);
            }

            return 1;
        }

        public Module GetDestructionPacket(int x, int y)
        {
            GetRowOrigin(ref x, ref y);
            return new SyncGenericContainerModule(Main.myPlayer, false, x, y);
        }
        #endregion

        //Animate it opening
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            GetRowOrigin(ref i, ref j);
            int chestIndex = Chest.FindChest(i, j);
            if (chestIndex == -1)
                return;

            Chest chest = Main.chest[chestIndex];
            frameYOffset += 54 * chest.frame;
        }
    }

    public class WulfrumVaultItem : ModItem
    {
        public override string Texture => AssetDirectory.WulfrumScrapyard + Name;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wulfrum Vault");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 22;
            Item.maxStack = 99;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = 10;
            Item.rare = ItemRarityID.White;
            Item.createTile = ModContent.TileType<WulfrumVault>();
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<DullPlatingItem>(20).
                AddRecipeGroup("IronBar", 2).
                AddTile<BunkerWorkshop>().
                Register();
        }
    }
}
