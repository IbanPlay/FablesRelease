using System.IO;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace CalamityFables.Content.Boss.SeaKnightMiniboss
{
    public class NautilusPedestal : ModTile
    {
        public override string Texture => AssetDirectory.BurntDesert + Name;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileID.Sets.PreventsSandfall[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);

            //TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(TETrainingDummy.Hook_AfterPlacement, -1, 0, processedCoordinates: false);

            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.RandomStyleRange = 0;

            TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);

            DustType = DustID.Sand;
            LocalizedText name = CreateMapEntryName();
            name.SetDefault("");
            AddMapEntry(new Color(141, 111, 85));
            base.SetStaticDefaults();
        }

        public override bool CanExplode(int i, int j) => false;
        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
    }

    public class TESirNautilusSpawner : ModTileEntity
    {
        private static Dictionary<int, Rectangle> playerPositions = new Dictionary<int, Rectangle>();
        private static bool registeredPlayerPositions;

        public Vector2 WorldPosition => Position.ToVector2() * 16;
        public int npc = -1;
        public static bool anySpawnersInWorld = false;

        //Dummies use the same thing to only register player hitboxes once
        public override void Load()
        {
            TileEntity._UpdateStart += ClearBoxes;
        }

        public static void ClearBoxes()
        {
            playerPositions.Clear();
            registeredPlayerPositions = false;
        }

        public override bool IsTileValidForEntity(int x, int y)
        {
            if (!Main.tile[x, y].HasTile || Main.tile[x, y].TileType != TileType<NautilusPedestal>())
                return false;
            return true;
        }

        public override void Update()
        {
            anySpawnersInWorld = true;

            if (Main.npc.Any(n => n.type == NPCType<SirNautilus>() && n.active))
            {
                Deactivate();
                return;
            }

            if (npc != -1)
            {
                if (!Main.npc[npc].active || Main.npc[npc].type != NPCType<SirNautilusPassive>())
                {
                    Deactivate();
                }
                return;
            }

            NPC potentialPassiveNautie = Main.npc.FirstOrDefault(n => n.type == NPCType<SirNautilusPassive>() && n.active);
            if (potentialPassiveNautie != default)
            {
                npc = potentialPassiveNautie.whoAmI;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
                return;
            }

            FillPlayerHitboxes();
            bool nearbyPlayer = false;
            int areaSize = 2400;
            Rectangle nearbyArea = new Rectangle((int)WorldPosition.X - areaSize / 2, (int)WorldPosition.Y - areaSize / 2, areaSize, areaSize);

            foreach (KeyValuePair<int, Rectangle> item in playerPositions)
            {
                if (item.Value.Intersects(nearbyArea))
                {
                    nearbyPlayer = true;
                    break;
                }
            }
            if (nearbyPlayer)
                Activate();
        }

        private static void FillPlayerHitboxes()
        {
            if (registeredPlayerPositions)
                return;

            for (int i = 0; i < 255; i++)
            {
                if (Main.player[i].active)
                    playerPositions[i] = Main.player[i].getRect();
            }

            registeredPlayerPositions = true;
        }

        public void Activate()
        {
            int baseAnimation = 2; //Sleeping
            if (SirNautilusDialogue.DefeatedNautilus)
                baseAnimation = 9; //Ukuleling

            int nautie = NPC.NewNPC(new EntitySource_TileEntity(this), (int)WorldPosition.X, (int)WorldPosition.Y + 16, NPCType<SirNautilusPassive>(), ai3: baseAnimation);
            Main.npc[nautie].netUpdate = true;
            npc = nautie;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }

        public void Deactivate()
        {
            if (npc != -1 && Main.npc[npc].type == NPCType<SirNautilusPassive>())
                Main.npc[npc].active = false;

            npc = -1;
            if (Main.netMode != NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }

        #region Saving and syncing
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((short)npc);
        }


        public override void NetReceive(BinaryReader reader)
        {
            npc = reader.ReadInt16();
        }

        #endregion
    }
}
