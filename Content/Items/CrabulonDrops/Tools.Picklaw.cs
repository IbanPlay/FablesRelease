using Terraria.DataStructures;
using Terraria.Graphics.Renderers;
using CalamityFables.Particles;

namespace CalamityFables.Content.Items.CrabulonDrops
{
    public class MoldyPicklaw : ModItem
    {
        public static readonly SoundStyle ConversionSound = new(SoundDirectory.CrabulonDrops + "MoldMyceliumSpread", 3);

        public override string Texture => AssetDirectory.CrabulonDrops + Name;
        public override void Load()
        {
            FablesGeneralSystemHooks.OnMineTile += ConvertTilesIntoMycelium;
        }

        public static int MyceliumTileType;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Moldy Picklaw");
            Tooltip.SetDefault("Converts soft tiles and common stones into fragile mycelium\n" +
                "Mycelium is frail and will break apart multiple tiles at once");
        }

        public override void SetDefaults()
        {
            //Higher than the evil ore pickaxes
            Item.pick = 75;
            Item.damage = 13;
            Item.knockBack = 2f;

            Item.DamageType = DamageClass.Melee;
            Item.width = 44;
            Item.height = 44;

            Item.useTime = 14; //Mining speed
            Item.useAnimation = 25; //Swing speed
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.attackSpeedOnlyAffectsWeaponAnimation = true;
        }

        public struct ConversionEffectPosition
        {
            public Point position;
            public float fuel;
            public ConversionEffectPosition(Point point, float fuel)
            {
                this.position = point;
                this.fuel = fuel;
            }
        }

        public static List<ConversionEffectPosition> conversionEffectPositions = new List<ConversionEffectPosition>();

        private void ConvertTilesIntoMycelium(Player player, Item pickaxe, int x, int y, Tile tile, int originalTileType)
        {
            if (pickaxe.type != Type)
                return;
            //Only starts chains from convertable tiles
            if (!ValidTileForMyceliumConversion(originalTileType))
                return;

            Point blockMinedPos = new Point(x, y);
            Vector2 worldPosition = blockMinedPos.ToWorldCoordinates();
            Vector2 mineDirection = (worldPosition - player.Center).SafeNormalize(Vector2.UnitY);

            Vector2 bottomRight = Vector2.One.Normalized();
            Vector2 bottomLeft = new Vector2(-1f, 1f).Normalized();
            float bottomRightDot = Vector2.Dot(mineDirection, bottomRight);
            float bottomLeftDot = Vector2.Dot(mineDirection, bottomLeft);

            //Get the direction unit from the two dot products
            Vector2 directionUnit;
            Vector2 perpendicularUnit; //Counter clockwise perpendicular
            float angle;

            if (bottomRightDot <= 0 && bottomLeftDot <= 0) //Top
            {
                directionUnit = -Vector2.UnitY;
                perpendicularUnit = -Vector2.UnitX;
                angle = -MathHelper.PiOver2;
            }
            else if (bottomRightDot >= 0 && bottomLeftDot >= 0) //Bottom
            { 
                directionUnit = Vector2.UnitY;
                perpendicularUnit = Vector2.UnitX;
                angle = MathHelper.PiOver2;
            }
            else if (bottomRightDot <= 0 && bottomLeftDot >= 0) //Left
            { 
                directionUnit = -Vector2.UnitX;
                perpendicularUnit = Vector2.UnitY;
                angle = MathHelper.Pi;
            }
            else                                                //Right
            { 
                directionUnit = Vector2.UnitX; 
                perpendicularUnit = -Vector2.UnitY;
                angle = 0;
            }

            //Convert directions to points & make a duplicated conversion point
            Point conversionPosition = new Point(blockMinedPos.X, blockMinedPos.Y);
            Point directionPoint = directionUnit.ToPoint();
            Point perpendicularPoint = perpendicularUnit.ToPoint();

            conversionEffectPositions?.Clear();

            ushort topSideSproutFlags = 0;
            ushort botSideSproutFlags = 0;


            //Spawn 3 initial blocks in a > pattern
            bool aheadSpawned = MyceliumConversion(conversionPosition + directionPoint, 1f);
            if (MyceliumConversion(conversionPosition + perpendicularPoint, 1f))
                topSideSproutFlags += 1;
            if (MyceliumConversion(conversionPosition - perpendicularPoint, 1f))
                botSideSproutFlags += 1;

            //Keep going only if the block ahead could get converted
            if (aheadSpawned)
            {
                conversionPosition += directionPoint;
                //Spawn 3 more blocks in a > pattern
                aheadSpawned = MyceliumConversion(conversionPosition + directionPoint, 2f);
                if(MyceliumConversion(conversionPosition + perpendicularPoint, 2f))
                    topSideSproutFlags += 2;
                if (MyceliumConversion(conversionPosition - perpendicularPoint, 2f))
                    botSideSproutFlags += 2;

                

                //Spawn front tetromino
                if (aheadSpawned)
                    MyceliumShape_FrontTetromino(conversionPosition + directionPoint + directionPoint, angle);
                
                //Side tetrominos if we converted any adjacent parts of the initial shape
                if (aheadSpawned || topSideSproutFlags >= 2)
                    MyceliumShape_SideTetromino(conversionPosition + directionPoint + perpendicularPoint, directionPoint, perpendicularPoint);
                if (aheadSpawned || botSideSproutFlags >= 2)
                    MyceliumShape_SideTetromino(conversionPosition + directionPoint - perpendicularPoint, directionPoint, perpendicularPoint, true);

                //Side frills
                MyceliumShape_Frills(topSideSproutFlags, blockMinedPos, directionPoint, perpendicularPoint);
                MyceliumShape_Frills(botSideSproutFlags, blockMinedPos, directionPoint, perpendicularPoint, true);

                SoundEngine.PlaySound(ConversionSound, worldPosition);
            }



            float highestFuel = 1f;
            int minX = Main.maxTilesX;
            int maxX = 0;
            int minY = Main.maxTilesY;
            int maxY = 0;


            foreach (ConversionEffectPosition effect in conversionEffectPositions)
            {
                minX = Math.Min(effect.position.X, minX);
                minY = Math.Min(effect.position.Y, minY);
                maxX = Math.Max(effect.position.X, maxX);
                maxY = Math.Max(effect.position.Y, maxY);

                highestFuel = Math.Max(highestFuel, effect.fuel);
            }

            if (conversionEffectPositions.Count > 0)
            {
                NetMessage.SendTileSquare(Main.myPlayer, minX, minY, maxX - minX, maxY - minY);
            }

            //We have to batch the conversion effects and add all the particles at the end so that the tile framing is properly set
            foreach (ConversionEffectPosition effect in conversionEffectPositions)
            {
                ParticleHandler.SpawnParticle(new MyceliumTileConversion(effect.position, 1f - (effect.fuel - 1) / highestFuel));
            }
        }

        public void MyceliumShape_Frills(ushort flags, Point originalPosition, Point directionPoint, Point perpendicularPoint, bool flip = false)
        {
            //If any of the side parts of the > formation have been converted in the first place
            if (flags > 0)
            {
                if (flip)
                    perpendicularPoint = Point.Zero - perpendicularPoint;

                //If both have sprouted (3) pick a random one. If only 1 has sprouted, pick it
                if ((flags == 3 && Main.rand.NextBool()) || flags == 2)
                    MyceliumConversion(originalPosition + directionPoint + perpendicularPoint + perpendicularPoint, 4f);
                else
                    MyceliumConversion(originalPosition + perpendicularPoint + perpendicularPoint, 3f);
            }
        }

        public void MyceliumShape_FrontTetromino(Point startPosition, float angle)
        {
            //Full shape of the tetronimo because it doesnt variate, just gets cropped shorter or longer
            (Vector2 pos, int dist)[] fullShape = new[] { 
                (Vector2.Zero, 0), (Vector2.UnitX, 1), (Vector2.UnitX * 2, 2), //Base shape of a 3 tiles long stick
                (new Vector2(2, 1), 3), (new Vector2(3, 1), 4),                //Adds a sideways bifurcation at the end of the stick that's 2 long
                (new Vector2(2, -1), 3), (new Vector2(3, -1), 4),              //Adds a second sideways bifurcation that's also 2 long
                (new Vector2(4, -1), 5) };                                     //Makes the second bifurcation 1 longer

            int shapeLenght = Math.Min(3 + 2 * Main.rand.Next(3), 8);

            //Flip vertically at random
            bool flipped = Main.rand.NextBool();

            //Flip and rotate the positions on the tetronimo
            for (int i = 0; i < shapeLenght; i++)
            {
                if (flipped)
                    fullShape[i].pos.Y = -fullShape[i].pos.Y;

                Vector2 rotatedVector = fullShape[i].pos;

                if (angle == MathHelper.Pi) //Left rotation
                    rotatedVector.X *= -1;
                else if (angle == MathHelper.PiOver2) //Bottom rotation
                {
                    float placeholder = rotatedVector.X;
                    rotatedVector.X = rotatedVector.Y;
                    rotatedVector.Y = placeholder;
                }
                else if (angle == -MathHelper.PiOver2) //Top rotation
                {
                    float placeholder = rotatedVector.X;
                    rotatedVector.X = -rotatedVector.Y;
                    rotatedVector.Y = -placeholder;
                }

                bool placed = MyceliumConversion(startPosition + rotatedVector.ToPoint(), 3 + fullShape[i].dist);

                //If theres an interruption
                if (!placed)
                {
                    //If we were either just placing the base stalk / the second bifurcation, end it there
                    if (i <= 2 || i >= 5)
                        break;
                    //If we ran into a problem on the first bifurcation, skip to the second bifurcation
                    i = 4;
                }
            }
        }

        public void MyceliumShape_SideTetromino(Point startPosition, Point directionPoint, Point perpendicularPoint, bool flip = false)
        {
            //Flip the perpendicular
            if (flip)
                perpendicularPoint = Point.Zero - perpendicularPoint;

            
            int lenght = Main.rand.Next(3, 5); //3 or 4 tiles long
            int directionAlternance = Main.rand.Next(2); //1 or 2 (either straight ahead or perpendicular)

            Point currentPosition = startPosition;
            for (int i = 0; i < lenght; i++)
            {
                bool placed = MyceliumConversion(currentPosition, 3 + i);

                //Snake the current position in a zig zag
                currentPosition += directionAlternance % 2 == 0 ? directionPoint : perpendicularPoint;
                directionAlternance++;

                //if we started by pointing perpendicularly and are therefore pointing perpendicularly again for the final block, randomly keep potinging ahead instead
                if (directionAlternance == 1 && i == 2 && Main.rand.NextBool())
                    directionAlternance++;

                //Cut it short if we couldnt place the root there
                if (!placed)
                    break;
            }
        }

        public bool MyceliumConversion(Point position, float fuel)
        {
            Tile tile = Main.tile[position];
            //Returns true even if the tile wasn't converted if it's a mycelium tile. This helps prevent one mycelium root from interrupting another
            if (!tile.HasTile || !ValidTileForMyceliumConversion(tile.TileType))
                return tile.TileType == MyceliumTileType;


            WorldGen.KillTile(position.X, position.Y, false, false, true);
            if (!Main.tile[position].HasTile)
            {
                WorldGen.PlaceTile(position.X, position.Y, MyceliumTileType, true, false);
                conversionEffectPositions.Add(new ConversionEffectPosition(position, fuel));
                return true;
            }
            else
                return false;
        }

        public static bool ValidTileForMyceliumConversion(int type)
        {
            //Can't ruin the dirtiest block
            if (type == TileID.DirtiestBlock)
                return false;

            if (TileID.Sets.CanBeDugByShovel[type])
                return true;
            if (TileID.Sets.Conversion.Stone[type])
                return true;
            if (TileID.Sets.Conversion.Ice[type])
                return true;
            if (TileID.Sets.Conversion.HardenedSand[type])
                return true;
            if (TileID.Sets.Conversion.Sandstone[type])
                return true;
            return false;
        }


    }

    public class MyceliumMold : ModTile, ICustomLayerTile
    {
        public override string Texture => AssetDirectory.CrabulonDrops + Name;

        public static Asset<Texture2D> RootTexture;

        public override void Load()
        {
            FablesGeneralSystemHooks.PostSetupContentEvent += SetTileMerge;

            if (Main.dedServ)
                return;

            RootTexture = ModContent.Request<Texture2D>(Texture + "Roots");
        }

        public override void SetStaticDefaults()
        {
            if (Type == ModContent.TileType<MyceliumMold>())
                MoldyPicklaw.MyceliumTileType = Type;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileMergeDirt[Type] = true;

            Main.tileBrick[Type] = true;
            FablesUtils.MergeWithGeneral(Type);
            FablesUtils.MergeWithDesert(Type);
            FablesUtils.MergeWithSnow(Type);
            TileID.Sets.ChecksForMerge[Type] = true;

            MineResist = 0.3f;
            HitSound = SoundID.Item177;
            DustType = DustID.Scorpion;
            AddMapEntry(new Color(204, 209, 216));
        }

        public void SetTileMerge()
        {
            FablesUtils.SetMerge(Type, ModContent.TileType<MyceliumMoldEcho>());
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile myTile = Main.tile[i, j];
            if (myTile.Slope != SlopeType.Solid || myTile.IsHalfBlock)
                return;
            ExtraTileRenderLayers.AddSpecialDrawingPoint(i, j, TileDrawLayer.BehindTiles);
        }

        public void DrawSpecialLayer(int i, int j, TileDrawLayer layer, SpriteBatch spriteBatch)
        {
            Vector2 center = new Vector2(i * 16, j * 16) + Vector2.One * 8 - Main.screenPosition;
            Color lightColor = Lighting.GetColor(i, j);

            Tile t = Main.tile[i, j];
            if (t.IsActuated)
            {
                lightColor = (lightColor * 0.4f) with { A = lightColor.A };
            }

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (t.TileColor != PaintID.None)
            {
                Texture2D paintedTex = Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(Type, 0, t.TileColor);
                texture = paintedTex ?? texture;
            }

            for (int x = 0; x < 4; x++)
            {
                Point adjacentCoordinates = new Point(i, j) + FablesUtils.DirectAdjacentTileDirections[x];
                if (Main.tile[adjacentCoordinates].IsTileSolid())
                    continue;
                DrawRootOnSide(spriteBatch, i, j, center, x * MathHelper.PiOver2, texture, lightColor);
            }
        }

        public void DrawRootOnSide(SpriteBatch spriteBatch, int i, int j, Vector2 center, float rotation, Texture2D texture, Color lightColor)
        {
            int random = (i * j % 17 + (int)(rotation * 27)) * Main.tile[i, j].TileFrameNumber;
            random <<= 2;
            random = (int)(random * 317329.23f % 12);

            if (random < 2)
                return;

            int variant = ((int)((Math.Pow(j, 4) % 39) * 3 - i * j % 17 * 22 + rotation * i * 20.42f)).ModulusPositive(8);
            Vector2 unitVectorX = Vector2.UnitX.RotatedBy(rotation);
            Vector2 unitVectorY = Vector2.UnitY.RotatedBy(rotation);

            Rectangle frame = new Rectangle(variant * 16, 270, 16, 16);

            Vector2 offset = -unitVectorY * (float)(2f - Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i * 0.5f)) + unitVectorX * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + i * j) * 1f;
            //Vector2 offset = -unitVectorY * 2f;

            Vector2 position = center + offset;
            rotation += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + (i * j) % MathHelper.TwoPi + 0.5f) * 0.16f;

            Vector2 origin = new Vector2(frame.Width / 2, frame.Height);
            spriteBatch.Draw(texture, position, frame, lightColor, rotation, origin, 1, 0, 0);

            frame.Y += 16;
            spriteBatch.Draw(texture, position, frame, Color.White * CommonColors.WulfrumLightMultiplier, rotation, origin, 1, 0, 0);
        }

        public static int BreakChainLenght = 0;

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail || effectOnly || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Tile myTile = Main.tile[i, j];

            //noItem being false means that its regular mining and therefore 
            if (!noItem)
                BreakChainLenght = 0;

            BreakChainLenght++;

            for (int m = 0; m < 4; m++)
            {
                int cachedChainlenght = BreakChainLenght;

                int shatterChance = Math.Max(1, BreakChainLenght / 2);
                Point tileShatter = new Point(i, j) + FablesUtils.DirectAdjacentTileDirections[m];

                Tile adjacentTile = Main.tile[tileShatter];
                if (adjacentTile.HasTile && adjacentTile.TileType == Type && WorldGen.genRand.NextBool(shatterChance))
                {
                    myTile.HasTile = false;
                    WorldGen.KillTile(tileShatter.X, tileShatter.Y, fail: false, effectOnly: false, noItem: true);
                    if (Main.dedServ)
                        NetMessage.TrySendData(MessageID.TileManipulation, -1, -1, null, 20, tileShatter.X, tileShatter.Y);
                }

                //Restore the chain lenght to whatever the cache was before
                BreakChainLenght = cachedChainlenght;
            }
        }
    }

    public class MyceliumTileConversion : Particle
    {
        public override string Texture => AssetDirectory.CrabulonDrops + "MyceliumMoldPlacement";
        public override bool UseCustomDraw => true;
        public override bool SetLifetime => true;

        Point tilePosition;
        short xFrameOffset = 0;
        short yFrameOffset = 0;

        float fuel;

        public MyceliumTileConversion(Point tilePosition, float fuel)
        {
            //This particle uses specific shit to the client so we just cut it off here to avoid errors
            if (Main.dedServ)
                return;

            Position = tilePosition.ToWorldCoordinates(0, 0);
            Velocity = Vector2.Zero;
            this.tilePosition = tilePosition;

            Tile tile = Main.tile[tilePosition];

            xFrameOffset = tile.TileFrameX;
            yFrameOffset = tile.TileFrameY;

            Main.instance.TilesRenderer.GetTileDrawData(tilePosition.X, tilePosition.Y, tile, (ushort)tile.TileType, ref xFrameOffset, ref yFrameOffset, out _, out _, out _, out _, out int frameXExtra, out int frameYExtra, out _, out _, out _, out _);
            xFrameOffset += (short)frameXExtra;
            yFrameOffset += (short)frameYExtra;

            Lifetime = (int)(50 - fuel * 35);
            FrontLayer = false;
        }

        public override void Update()
        {
            if (Lifetime - Time == 10)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 center = Position + Vector2.One * 8f;
                    Rectangle aroundTile = FablesUtils.RectangleFromVectors(center - Vector2.One * 9f, center + Vector2.One * 9f);
                    Vector2 position = (center + Main.rand.NextVector2CircularEdge(20f, 20f)).ClampInRect(aroundTile);


                    Dust d = Dust.NewDustPerfect(position, DustID.FoodPiece, Vector2.Zero, 0, Color.CornflowerBlue with { A = 0 }, Main.rand.NextFloat(1f, 2f));
                    d.velocity = Vector2.Zero;
                    d.noGravity = true;
                }
            }

        }

        public override void CustomDraw(SpriteBatch spriteBatch, Vector2 basePosition)
        {
            Texture2D texture = ParticleTexture;
            Rectangle frame = new Rectangle(xFrameOffset, yFrameOffset, 16, 16);
            Color color = Lighting.GetColor(tilePosition);
            color = Color.Lerp(color, Color.RoyalBlue, 0.6f);
            color *= (1 - LifetimeCompletion);
            color.A = 0;

            spriteBatch.Draw(texture, Position - basePosition, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            spriteBatch.Draw(texture, Position - basePosition, frame, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
        }
    }
}