using CalamityFables.Content.Tiles.BurntDesert;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;

namespace CalamityFables.Core
{
    public interface IExtraPlaceable
    {
        /// <summary>
        /// The base anchor of the placeable, used as a main indexer
        /// </summary>
        public Point Anchor { get; set; }

        public IEnumerable<Point> AlternateAnchors => null; 

        /// <summary>
        /// Called when the placeable is broken. use this to create gores or whatever else
        /// </summary>
        public void OnRemove() { }

        /// <summary>
        /// Called when the placeable is placed
        /// </summary>
        public void OnPlace() { }


        /// <summary>
        /// Return true if the object should persist, and false to get it removed
        /// </summary>
        public bool Update();

        /// <summary>
        /// Serialize the placeable into a tag compound. Reconstructed from said tag compound by using <see cref="ExtraPlaceableManager{T}.deserializationDelegate"/>
        /// </summary>
        /// <returns></returns>
        public TagCompound Serialize();

        /// <summary>
        /// Serializes the data through the bianrywriter
        /// </summary>
        public void NetSerialize(BinaryWriter writer);

        /// <summary>
        /// Draws the placeable. Used if <see cref="ExtraPlaceableManager{T}.drawsMesh"/> is set to false
        /// </summary>
        public void Draw() { }

        /// <summary>
        /// Draws the placeable by constructing a mesh. Used if <see cref="ExtraPlaceableManager{T}.drawsMesh"/> is set to true
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        public void BuildMesh(int index, ref VertexPositionColorTexture[] vertices, ref short[] indices) { }
    }

    public class ExtraPlaceableManager<T> where T : IExtraPlaceable
    {
        public Dictionary<Point, T> PlaceablesByPosition;
        public Dictionary<Point, T> PlaceableAlternateAnchors;

        /// <summary>
        /// Name that will be used as the tagcompound's name when saving/loading
        /// </summary>
        public string managerName;
        /// <summary>
        /// Name that will be used when saving each individual placeable object
        /// </summary>
        public string managedObjectName;

        /// <summary>
        /// Padding around each placeable to know if it should get drawn or not
        /// </summary>
        public Vector2 displayPadding;

        public delegate T PlaceableDeserializationDelegate(TagCompound tag);
        public PlaceableDeserializationDelegate deserializationDelegate;
        public delegate T PlaceableNetReceiveDelegate(System.IO.BinaryReader reader);
        public PlaceableNetReceiveDelegate netReceiveDelegate;


        public delegate void MeshDrawDelegate();
        public MeshDrawDelegate preConstructMesh;
        public MeshDrawDelegate postConstructMesh;

        /// <summary>
        /// Are the placeables drawn one at a time or as a single batched mesh?
        /// </summary>
        public bool drawsMesh;

        public bool restartSpritebatch;

        public ExtraPlaceableManager(string managerName, string managedObjectName, PlaceableDeserializationDelegate deserializationDelegate, PlaceableNetReceiveDelegate netReceiveDelegate, Vector2 padding, bool drawMesh = false, bool restartSpritebatch = true)
        {
            this.managerName = managerName;
            this.managedObjectName = managedObjectName;
            this.deserializationDelegate = deserializationDelegate;
            this.netReceiveDelegate = netReceiveDelegate;
            this.displayPadding = padding;
            this.drawsMesh = drawMesh;
            this.restartSpritebatch = restartSpritebatch;

            PlaceablesByPosition = new Dictionary<Point, T>();
            PlaceableAlternateAnchors = new Dictionary<Point, T>();
            FablesGeneralSystemHooks.PostUpdateEverythingEvent += PostUpdateEverything;
            FablesGeneralSystemHooks.SaveWorldDataEvent += SaveData;
            FablesGeneralSystemHooks.LoadWorldDataEvent += LoadData;
            FablesGeneralSystemHooks.NetSendEvent += SendData;
            FablesGeneralSystemHooks.NetReceiveEvent += ReceiveData;
        }

        #region Placing
        /// <summary>
        /// Basic placement check that makes sure that we don't have anything overlapping on the position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool CanPlaceNewObject(T newObject)
        {
            if (!WorldGen.InWorld(newObject.Anchor.X, newObject.Anchor.Y))
                return false;

            //Only placeable on tiles that don't already contain a placeable
            if (PlaceablesByPosition.ContainsKey(newObject.Anchor) || PlaceableAlternateAnchors.ContainsKey(newObject.Anchor))
                return false;

            if (newObject.AlternateAnchors != null)
            {
                foreach (Point altAnchor in newObject.AlternateAnchors)
                    if (PlaceablesByPosition.ContainsKey(altAnchor) || PlaceableAlternateAnchors.ContainsKey(altAnchor))
                        return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to place a new object, returns true if failed
        /// </summary>
        public bool TryPlaceNewObject(T newObject)
        {
            if (!CanPlaceNewObject(newObject))
                return false;

            PlaceablesByPosition.Add(newObject.Anchor, newObject);

            if (newObject.AlternateAnchors != null)
            {
                foreach (Point altAnchor in newObject.AlternateAnchors)
                    PlaceableAlternateAnchors.Add(altAnchor, newObject);
            }

            newObject.OnPlace();
            return true;
        }
        #endregion

        public void RemovePlaceable(T placeable)
        {
            if (!WorldGen.generatingWorld)
                placeable.OnRemove();

            PlaceablesByPosition.Remove(placeable.Anchor);

            if (placeable.AlternateAnchors != null)
            {
                foreach (Point altAnchor in placeable.AlternateAnchors)
                    PlaceableAlternateAnchors.Remove(altAnchor);
            }
        }

        /// <summary>
        /// Gets the object associated with the coordinates
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public T GetExtraPlaceable(int i, int j)
        {
            if (PlaceablesByPosition.TryGetValue(new Point(i, j), out var placeable))
                return placeable;

            if (PlaceableAlternateAnchors.TryGetValue(new Point(i, j), out placeable))
                return placeable;

            return default(T);
        }

        #region Update and draw hooks
        public void PostUpdateEverything()
        {
            //Update all placeables
            foreach (var item in PlaceablesByPosition)
            {
                //...Failsafe..?
                item.Value.Anchor = item.Key;
                if (!item.Value.Update() && Main.netMode != NetmodeID.MultiplayerClient)
                    RemovePlaceable(item.Value);
            }
        }

        public VertexPositionColorTexture[] vertices;
        public short[] indices;

        public void DrawExtraPlaceables()
        {
            if (PlaceablesByPosition.Count == 0)
                return;
            List<T> visiblePlaceables = new();
            foreach (var item in PlaceablesByPosition)
            {
                T placeable = item.Value;
                Rectangle drawRect = new Rectangle(placeable.Anchor.X * 16 - (int)(displayPadding.X) - (int)Main.screenPosition.X, (int)placeable.Anchor.Y * 16 - (int)(displayPadding.Y) - (int)Main.screenPosition.Y, (int)(displayPadding.X * 2), (int)(displayPadding.Y * 2));
                if (FablesUtils.OnScreen(drawRect))
                    visiblePlaceables.Add(placeable);
            }

            if (visiblePlaceables.Count == 0)
                return;

            if (!drawsMesh)
            {
                if (restartSpritebatch)
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                foreach (T placeable in visiblePlaceables)
                    placeable.Draw();

                if (restartSpritebatch)
                    Main.spriteBatch.End();
            }
            else
            {
                preConstructMesh();
                int index = 0;
                foreach (T placeable in visiblePlaceables)
                {
                    placeable.BuildMesh(index, ref vertices, ref indices);
                    index++;
                }
                postConstructMesh();
            }
        }
        #endregion

        #region Saving and loading data
        public void SaveData(TagCompound tag)
        {
            TagCompound managerTag = new TagCompound();
            tag[managerName + "Count"] = PlaceablesByPosition.Count;

            int p = 0;
            foreach (var item in PlaceablesByPosition)
            {
                managerTag.Add(managedObjectName + p,  item.Value.Serialize());
                p++;
            }
            tag[managerName] = managerTag;
        }

        public void LoadData(TagCompound tag)
        {
            PlaceablesByPosition.Clear();
            PlaceableAlternateAnchors.Clear();

            if (tag.TryGet<int>(managerName + "Count", out int count) && tag.TryGet(managerName, out TagCompound placeablesTag))
            {
                for (int p = 0; p < count; p++)
                {
                    T newObject = deserializationDelegate(placeablesTag.GetCompound(managedObjectName + p));
                    if (newObject == null)
                        continue;

                    PlaceablesByPosition.Add(newObject.Anchor, newObject);
                    if (newObject.AlternateAnchors != null)
                    {
                        foreach (Point altAnchor in newObject.AlternateAnchors)
                            PlaceableAlternateAnchors.Add(altAnchor, newObject);
                    }
                }
            }
        }

        private void SendData(System.IO.BinaryWriter writer)
        {
            writer.Write(PlaceablesByPosition.Count);
            foreach (var item in PlaceablesByPosition)
                item.Value.NetSerialize(writer);
        }

        private void ReceiveData(BinaryReader reader)
        {
            //PlaceablesByPosition.Clear();
            //PlaceableAlternateAnchors.Clear();

            Dictionary<Point, T> receivedPlaceables = new();

            int itemCount = reader.ReadInt32();
            for (int i = 0; i < itemCount; i++)
            {
                T newObject = netReceiveDelegate(reader);
                receivedPlaceables.Add(newObject.Anchor, newObject);
            }

            //Remove all the ones that we have that the server doesnt (removed, but without the remove packet...? Weird
            foreach (var existingPlaceable in PlaceablesByPosition)
            {
                if (!receivedPlaceables.ContainsKey(existingPlaceable.Value.Anchor))
                {
                    T duplicatePlaceable = existingPlaceable.Value;
                    PlaceablesByPosition.Remove(duplicatePlaceable.Anchor);
                    if (duplicatePlaceable.AlternateAnchors != null)
                    {
                        foreach (Point altAnchor in duplicatePlaceable.AlternateAnchors)
                            PlaceableAlternateAnchors.Remove(altAnchor);
                    }
                }
            }

            //Remove all the ones that didnt change
            foreach (var syncedPlaceable in receivedPlaceables)
            {
                //Assumped to be the same if it shares a position
                if (PlaceablesByPosition.ContainsKey(syncedPlaceable.Key))
                {
                    if (syncedPlaceable.Value.AlternateAnchors != null)
                    {
                        //Same anchor and alternate anchors, this is a duplicate. Byeee
                        if (syncedPlaceable.Value.AlternateAnchors == PlaceablesByPosition[syncedPlaceable.Key].AlternateAnchors)
                            receivedPlaceables.Remove(syncedPlaceable.Key);
                        //if not the same anchors, we gotta reset that one!
                        else
                        {
                            T duplicatePlaceable = PlaceablesByPosition[syncedPlaceable.Key];
                            PlaceablesByPosition.Remove(duplicatePlaceable.Anchor);
                            if (duplicatePlaceable.AlternateAnchors != null)
                            {
                                foreach (Point altAnchor in duplicatePlaceable.AlternateAnchors)
                                    PlaceableAlternateAnchors.Remove(altAnchor);
                            }
                        }
                    }
                    else
                    {
                        receivedPlaceables.Remove(syncedPlaceable.Key);
                    }
                }
            }

            //Now the only ones left are ones that are to be added
            foreach (var syncedPlaceable in receivedPlaceables)
            {
                PlaceablesByPosition.Add(syncedPlaceable.Value.Anchor, syncedPlaceable.Value);
                if (syncedPlaceable.Value.AlternateAnchors != null)
                {
                    foreach (Point altAnchor in syncedPlaceable.Value.AlternateAnchors)
                        PlaceableAlternateAnchors.Add(altAnchor, syncedPlaceable.Value);
                }
            }
        }

        #endregion
    }
}