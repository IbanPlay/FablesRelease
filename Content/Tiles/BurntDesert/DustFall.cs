using CalamityFables.Content.Boss.SeaKnightMiniboss;
using CalamityFables.Content.Tiles.BurntDesert;
using CalamityFables.Content.Tiles.Wulfrum.Furniture;
using Microsoft.Xna.Framework.Graphics;
using Stubble.Core.Classes;
using System;
using System.IO;
using System.Transactions;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace CalamityFables.Content.Tiles.BurntDesert
{
    /// <summary>
    /// A sort of tile entity that's handled by <see cref="OpalescentPrismManager"/>, and can be interacted with through the invisible hitbox tiles that are <see cref="OpalescentPrismTile"/>
    /// </summary>
    public class DustFall
    {
        public Point anchor;
        public bool ghostly;
        public float opacityFade;
        public bool naturallySpawned = false;
        public const float COLUMN_HEIGHT = 170f;

        public DustFall(Point anchor)
        {
            this.anchor = anchor;
            DustFallManager.DustfallsByPosition[anchor] = this;
        }

        public void Update()
        {
            //If the anchor is broken
            if ((Main.netMode != NetmodeID.MultiplayerClient || (Main.sectionManager != null && Main.sectionManager.TileLoaded(anchor.X, anchor.Y))) &&
                !DustFallManager.PositionValidForDustFall(anchor, ghostly ? -1 : 1, true))
            {
                DustFallManager.RemoveDustFall(this);
                return;
            }

            bool shouldShow = true;

            //Fade in and out the ghostly dust falls when nautilus goes solo
            if (naturallySpawned && ghostly)
                shouldShow = SirNautilus.SignathionVisualInfluence <= 0;
            if (naturallySpawned && !ghostly)
                shouldShow = SirNautilus.SignathionVisualInfluence > 0;

            if (shouldShow && opacityFade < 1f)
            {
                opacityFade += 0.04f;
                if (opacityFade > 1f)
                    opacityFade = 1f;
            }
            if (!shouldShow && opacityFade > 0f)
            {
                opacityFade -= 0.02f;
                if (opacityFade > 0f)
                    opacityFade = 0f;
            }

            //Only do visual updates when close
            //if (Main.LocalPlayer.DistanceSQ(anchor.ToWorldCoordinates()) < 3000 * 3000)
            //{

            //}
        }

        /// <summary>
        /// Called when the dustfall is broken
        /// </summary>
        public void OnBreak()
        {

        }

        public void DrawGlow(int index, ref VertexPositionColorTexture[] vertices, ref short[] indices)
        {
            //use the fade value so it doesnt abruptly appear or cut off
            Color streakColorLeft = Color.Red * opacityFade;
            Color streakColorRight = Color.Red * opacityFade;

            streakColorLeft.G = 0;
            streakColorRight.G = 255;

            streakColorLeft.B = (byte)(ghostly ? 255 : 0);
            streakColorRight.B = (byte)(ghostly ? 255 : 0);

            Vector3 topLeft = anchor.ToWorldCoordinates(0, 0).Vec3() + Vector3.UnitY * 2;
            Vector3 botLeft = topLeft + Vector3.UnitY * COLUMN_HEIGHT;
            if (ghostly)
            {
                topLeft.Y += 16f;
                botLeft = topLeft - Vector3.UnitY * COLUMN_HEIGHT;
            }

            float width = 24f;
            topLeft.X -= (width - 16) / 2f;
            botLeft.X -= (width - 16) / 2f;

            float uvLeft = topLeft.X / 16f;
            float uvRight = (topLeft.X + width) / 16f;

            vertices[index * 4] = new VertexPositionColorTexture(topLeft, streakColorLeft, new Vector2(uvLeft, 0f));
            vertices[index * 4 + 1] = new VertexPositionColorTexture(topLeft + Vector3.UnitX * width, streakColorRight, new Vector2(uvRight, 0f));
            vertices[index * 4 + 2] = new VertexPositionColorTexture(botLeft + Vector3.UnitX * width, streakColorRight, new Vector2(uvRight, 1f));
            vertices[index * 4 + 3] = new VertexPositionColorTexture(botLeft, streakColorLeft, new Vector2(uvLeft, 1f));

            indices[index * 6] = (short)(index * 4);
            indices[index * 6 + 1] = (short)(index * 4 + 1);
            indices[index * 6 + 2] = (short)(index * 4 + 2);
            indices[index * 6 + 3] = (short)(index * 4);
            indices[index * 6 + 4] = (short)(index * 4 + 2);
            indices[index * 6 + 5] = (short)(index * 4 + 3);
        }


        #region Saving and loading
        public static TagCompound Serialize(DustFall dustFall)
        {
            return new TagCompound
            {
                ["anchor"] = dustFall.anchor,
                ["ghostly"] = dustFall.ghostly,
                ["natural"] = dustFall.naturallySpawned
            };
        }

        public static DustFall Deserialize(TagCompound tag)
        {
            DustFall dustFall = new DustFall(tag.Get<Point>("anchor"));
            if (tag.TryGet<bool>("ghostly", out bool ghostly))
                dustFall.ghostly = ghostly;
            if (tag.TryGet<bool>("natural", out bool natural))
                dustFall.naturallySpawned = natural;

            return dustFall;
        }

        public void NetSend(BinaryWriter writer)
        {
            writer.Write(anchor.X);
            writer.Write(anchor.Y);
            writer.Write(ghostly);
            writer.Write(naturallySpawned);
        }

        public static void NetReceive(BinaryReader reader)
        {
            DustFall dustFall = new DustFall(new Point(reader.ReadInt32(), reader.ReadInt32()));
            dustFall.ghostly = reader.ReadBoolean();
            dustFall.naturallySpawned = reader.ReadBoolean();
        }
        #endregion
    }
}