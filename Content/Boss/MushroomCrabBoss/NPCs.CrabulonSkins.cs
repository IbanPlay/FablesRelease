namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public partial class Crabulon : ModNPC, ICustomDeathMessages, IDrawOverTileMask
    {
        public int[] chosenSkinIndices = new int[7];

        public const int BODY_VARIANTS = 3;
        public const int LEG_VARIANTS = 3;
        public const int VIOLIN_ARM_VARIANTS = 3;
        public const int ARM_VARIANTS = 3;

        #region Skin lists
        //Body
        public static readonly CrabulonBodySkin[] BodySkins = new CrabulonBodySkin[]
        {
#region Crabby skin
            new CrabulonBodySkin("Crabby/", (Crabulon crab, List<CrabulonProp> props) =>
            {
            //Hanging props
            props.Add(new CrabulonProp(crab, new Vector2(36, 30), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/HangingProp1", new Vector2(3, 0))
                .SetSidewaysData(new Vector2(27, 30))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(19, 36), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/HangingProp2", new Vector2(2, 0))
                .SetSidewaysData(new Vector2(4, 36))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(1, 46), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/HangingProp3", new Vector2(3, 0))
                .SetSidewaysData(new Vector2(-9, 46))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-34, 34), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/HangingProp5", new Vector2(5, 0))
                .SetSidewaysData(new Vector2(-31, 34))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-22, 32), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/HangingProp4", new Vector2(3, 0))
                .SetSidewaysData(new Vector2(-33, 32))
                .HideTopDown());

            //Top down only props
            props.Add(new CrabulonProp(crab, new Vector2(-26, -42), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/TopDownProp1", new Vector2(9, 17))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(-2, -51), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/TopDownProp2", new Vector2(7, 26))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(20, -46), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/TopDownProp3", new Vector2(3, 21))
                .TopDownOnly());

            //Outer props
            props.Add(new CrabulonProp(crab, new Vector2(-58, -37), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp1", new Vector2(15, 17))
                .SetSidewaysData(new Vector2(-53, -37))
                .SetTopDownData(new Vector2(-59, -7), true, new Vector2(16, 16)));
            props.Add(new CrabulonProp(crab, new Vector2(-51, -40), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp2", new Vector2(12, 22))
                .SetSidewaysData(new Vector2(-50, -40))
                .SetTopDownData(new Vector2(-52, -13), true, new Vector2(11, 20)));
            props.Add(new CrabulonProp(crab, new Vector2(-32, -42), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp3", new Vector2(7, 30))
                .SetSidewaysData(new Vector2(-35, -42))
                .SetTopDownData(new Vector2(-32, -11), true, new Vector2(7, 26)));
            props.Add(new CrabulonProp(crab, new Vector2(20, -41), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp4", new Vector2(5, 29))
                .SetSidewaysData(new Vector2(13, -41))
                .SetTopDownData(new Vector2(22, -17), true, new Vector2(7, 22)));
            props.Add(new CrabulonProp(crab, new Vector2(43, -43), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp5", new Vector2(2, 25))
                .SetSidewaysData(new Vector2(30, -43))
                .SetTopDownData(new Vector2(42, -10), true, new Vector2(1, 25)));
            props.Add(new CrabulonProp(crab, new Vector2(50, -38), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/OuterProp6", new Vector2(3, 18))
                .SetSidewaysData(new Vector2(39, -38))
                .SetTopDownData(new Vector2(51, -2), true, new Vector2(2, 17)));

            //Inner props
            props.Add(new CrabulonProp(crab, new Vector2(-55, -24), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/Prop1", new Vector2(18, 10))
                .SetSidewaysData(new Vector2(-64, -24), 0.65f)
                .SetTopDownData(new Vector2(-61, 14), true, new Vector2(18, 9)));
            props.Add(new CrabulonProp(crab, new Vector2(-35, -29), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/Prop2", new Vector2(12, 17))
                .SetSidewaysData(new Vector2(-52, -27), 0.65f)
                .SetTopDownData(new Vector2(-41, 7)));
            props.Add(new CrabulonProp(crab, new Vector2(21, -29), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/Prop3", new Vector2(1, 21))
                .SetSidewaysData(new Vector2(9, -29))
                .SetTopDownData(new Vector2(22, 2)));
            props.Add(new CrabulonProp(crab, new Vector2(39, -18), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Crabby/Prop4", new Vector2(2, 10))
                .SetSidewaysData(new Vector2(18, -18))
                .SetTopDownData(new Vector2(39, 19)));

            //Whiskers
            props.Add(new CrabulonProp(crab, new Vector2(-22, 7), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Crabby/Whisker", new Vector2(87, 11), false, true) { faceProp = true , glowmaskColor = Color.White * 0.6f}
                .SetSidewaysData(new Vector2(-48, 7), 0.8f)
                .SetTopDownData(new Vector2(-23, 53), true, new Vector2(72, 2)));
            props.Add(new CrabulonProp(crab, new Vector2(14, 9), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Crabby/Whisker", new Vector2(87, 11), true, true) { faceProp = true, glowmaskColor = Color.White * 0.6f }
                .SetSidewaysData(new Vector2(-28, 8), 0.8f)
                .SetTopDownData(new Vector2(17, 53), true, new Vector2(72, 2)));
            //Eyes
            props.Add(new CrabulonProp(crab, new Vector2(-21, 6), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Crabby/Eyestalk", new Vector2(20, 14), false, true, true) { faceProp = true }
            .SetSidewaysData(new Vector2(-46, 6))
            .SetTopDownData(new Vector2(-21, 55), true, new Vector2(20, 2)));
            props.Add(new CrabulonProp(crab, new Vector2(13, 8), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Crabby/Eyestalk", new Vector2(20, 14), true, true, true) { faceProp = true }
            .SetSidewaysData(new Vector2(-19, 7), customTexture:true, customTextureOrigin: new Vector2(1, 13))
            .SetTopDownData(new Vector2(13, 53), true, new Vector2(20, 2)));
            }),
            #endregion
#region Hairy
            new CrabulonBodySkin("Hairy/", (Crabulon crab, List<CrabulonProp> props) =>
            { 
                
            //Top down only props
            props.Add(new CrabulonProp(crab, new Vector2(-32, -43), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/TopDownProp1", new Vector2(1, 16))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(-4, -59), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/TopDownProp2", new Vector2(5, 14))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(52, -31), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/TopDownProp3", new Vector2(1, 14))
                .TopDownOnly());

            //"Hair" all around it
            props.Add(new CrabulonProp(crab, new Vector2(-73, -7), -Vector2.UnitX, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair1", new Vector2(12, 1))
                .SetSidewaysData(new Vector2(-69, -7), 1f, true, new Vector2(12, 1))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-76, -37), -Vector2.UnitX.RotatedBy(MathHelper.PiOver4), 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair2", new Vector2(13, 9))
                .SetSidewaysData(new Vector2(-68, -37), 1f, true, new Vector2(11, 9))
                .SetTopDownData(new Vector2(-75, 4), true, new Vector2(14, 7)));
            props.Add(new CrabulonProp(crab, new Vector2(-57, -38), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair3", new Vector2(12, 28))
                .SetSidewaysData(new Vector2(-47, -37), 1f, true, new Vector2(12, 29))
                .SetTopDownData(new Vector2(-56, -10), true, new Vector2(13, 23)));
            props.Add(new CrabulonProp(crab, new Vector2(-46, -43), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair4", new Vector2(7, 9))
                .SetSidewaysData(new Vector2(-38, -42), 1f, true, new Vector2(5, 10))
                .SetTopDownData(new Vector2(-50, -21), true, new Vector2(7, 14)));
            props.Add(new CrabulonProp(crab, new Vector2(44, -44), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair5", new Vector2(3, 16))
                .SetSidewaysData(new Vector2(52, -42), 1f, true, new Vector2(1, 18))
                .SetTopDownData(new Vector2(44, -14), true, new Vector2(3, 15)));
            props.Add(new CrabulonProp(crab, new Vector2(76, -37), Vector2.UnitX.RotatedBy(-MathHelper.PiOver4), 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair6", new Vector2(1, 7))
                .SetSidewaysData(new Vector2(70, -35), 1f, true, new Vector2(1, 9))
                .SetTopDownData(new Vector2(77, -9), true, new Vector2(2, 8)));
            props.Add(new CrabulonProp(crab, new Vector2(77, -17), Vector2.UnitX, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Hairy/Hair7", new Vector2(0, 1))
                .SetSidewaysData(new Vector2(71, -17), 1f, true, new Vector2(0, 1))
                .HideTopDown());

            //"Teeth" around the central crack
            props.Add(new CrabulonProp(crab, new Vector2(-35, -45), Vector2.UnitX.RotatedBy(-0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.2f, "Hairy/Tooth1", new Vector2(14, 15))
                .SetSidewaysData(new Vector2(-22, -52), 1f, true, new Vector2(3, 8))
                .SetTopDownData(new Vector2(-34, -30), true, new Vector2(1, 9)));
            props.Add(new CrabulonProp(crab, new Vector2(-30, -35), Vector2.UnitX.RotatedBy(-0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.6f, "Hairy/Tooth2", new Vector2(1, 13))
                .SetSidewaysData(new Vector2(-39, -35), 1f, true, new Vector2(2, 13))
                .SetTopDownData(new Vector2(-36, 2), true, new Vector2(1, 7)));
            props.Add(new CrabulonProp(crab, new Vector2(-37, -18), Vector2.UnitX.RotatedBy(-0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.6f, "Hairy/Tooth3", new Vector2(2, 12))
                .SetSidewaysData(new Vector2(-48, -19), 1f, true, new Vector2(1, 11))
                .SetTopDownData(new Vector2(-40, 20), true, new Vector2(1, 7)));
            props.Add(new CrabulonProp(crab, new Vector2(28, -45), -Vector2.UnitX.RotatedBy(0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.6f, "Hairy/Tooth4", new Vector2(13, 17))
                .SetSidewaysData(new Vector2(36, -45), 1f, true, new Vector2(15, 17))
                .SetTopDownData(new Vector2(34, -20), true, new Vector2(20, 11)));
            props.Add(new CrabulonProp(crab, new Vector2(28, -41), -Vector2.UnitX.RotatedBy(0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.6f, "Hairy/Tooth5", new Vector2(19, 9))
                .SetSidewaysData(new Vector2(30, -41), 1f, true, new Vector2(19, 9))
                .SetTopDownData(new Vector2(32, -8), true, new Vector2(23, 7)));
            props.Add(new CrabulonProp(crab, new Vector2(19, -18), -Vector2.UnitX.RotatedBy(0.5f), 0.03f, 0f, MathHelper.PiOver2 * 0.6f, "Hairy/Tooth6", new Vector2(12, 12))
                .SetSidewaysData(new Vector2(12, -15), 1f, true, new Vector2(13, 15))
                .SetTopDownData(new Vector2(24, 18), true, new Vector2(17, 7)));

            //"Whiskers" (original crab limbs)
            props.Add(new CrabulonProp(crab, new Vector2(-21, 9), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/Whisker1", new Vector2(18, 5)) { faceProp = true}
            .SetSidewaysData(new Vector2(-41, 9), 1f, true, new Vector2(16, 5))
            .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(13, 11),   Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/Whisker2", new Vector2(0, 5)) { faceProp = true}
            .SetSidewaysData(new Vector2(-9, 11), 1f, true, new Vector2(0, 5))
            .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-16, 12),   Vector2.UnitY, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/Whisker3", new Vector2(9, 2)) { faceProp = true}
            .SetSidewaysData(new Vector2(-34, 12), 1f, true, new Vector2(9, 2))
            .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(6, 14),   Vector2.UnitY, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/Whisker4", new Vector2(3, 2)) { faceProp = true}
            .SetSidewaysData(new Vector2(-14, 14))
            .HideTopDown());

            //Eyes
            props.Add(new CrabulonProp(crab, new Vector2(-16, 3), -Vector2.UnitX.RotatedBy(MathHelper.PiOver4), 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/EyestalkL", new Vector2(15, 15), false, true, true) { faceProp = true }
            .SetSidewaysData(new Vector2(-36, 3))
            .SetTopDownData(new Vector2(-16, 56), true, new Vector2(15, 3)));
            props.Add(new CrabulonProp(crab, new Vector2(8, 5), Vector2.UnitX.RotatedBy(-MathHelper.PiOver4), 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Hairy/EyestalkR", new Vector2(1, 15), false, true, true) { faceProp = true }
            .SetSidewaysData(new Vector2(-14, 5), customTexture:true, customTextureOrigin: new Vector2(1, 15))
            .SetTopDownData(new Vector2(8, 58), true, new Vector2(1, 3)));
            }),
            #endregion
#region Shroomy
            new CrabulonBodySkin("Shroomy/", (Crabulon crab, List<CrabulonProp> props) =>
            {
            //lil hanging roots
            props.Add(new CrabulonProp(crab, new Vector2(-54, 24), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Root1", new Vector2(5, 0))
                .SetSidewaysData(new Vector2(-50, 24))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-30, 34), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Root2", new Vector2(5, 0))
                .SetSidewaysData(new Vector2(-36, 34))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(10, 44), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Root3", new Vector2(7, 0))
                .SetSidewaysData(new Vector2(-2, 44))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(38, 26), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Root4", new Vector2(1, 0))
                .SetSidewaysData(new Vector2(30, 26))
                .HideTopDown());
            props.Add(new CrabulonProp(crab, new Vector2(-53, 18), Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Root5", new Vector2(0, 0))
                .SetSidewaysData(new Vector2(45, 18))
                .HideTopDown());
                
            //Top down only props
            props.Add(new CrabulonProp(crab, new Vector2(-33, -53), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/TopDownProp1", new Vector2(2, 40))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(16, -43), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/TopDownProp2", new Vector2(17, 66))
                .TopDownOnly());
            props.Add(new CrabulonProp(crab, new Vector2(43, -35), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/TopDownProp3", new Vector2(0, 46))
                .TopDownOnly());

            //Follicles around it
            props.Add(new CrabulonProp(crab, new Vector2(-88, -17), -Vector2.UnitX, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle1", new Vector2(15, 9))
                .SetSidewaysData(new Vector2(-80, -17), 0.8f)
                .SetTopDownData(new Vector2(-86, 12)));
            props.Add(new CrabulonProp(crab, new Vector2(-84, -19), -Vector2.UnitY.RotatedBy(0.6f), 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle2", new Vector2(15, 25))
                .SetSidewaysData(new Vector2(-76, -19), 0.8f)
                .SetTopDownData(new Vector2(-82, 10)));
            props.Add(new CrabulonProp(crab, new Vector2(-65, -38), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle3", new Vector2(16, 54))
                .SetSidewaysData(new Vector2(-59, -44), 1f, true, new Vector2(10, 48))
                .SetTopDownData(new Vector2(-65, -3)));
            props.Add(new CrabulonProp(crab, new Vector2(-54, -48), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle4", new Vector2(1, 26))
                .SetSidewaysData(new Vector2(-42, -48), 0.8f)
                .SetTopDownData(new Vector2(-54, -9)));
            props.Add(new CrabulonProp(crab, new Vector2(-26, -47), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle5", new Vector2(13, 41))
                .SetSidewaysData(new Vector2(-14, -50), 1f, true, new Vector2(13, 38))
                .SetTopDownData(new Vector2(-22, -13), true, new Vector2(17, 34)));
            props.Add(new CrabulonProp(crab, new Vector2(8, -56), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle6", new Vector2(7, 18))
                .SetSidewaysData(new Vector2(22, -64), 1f, true,  new Vector2(7, 10))
                .SetTopDownData(new Vector2(8, -7)));
            props.Add(new CrabulonProp(crab, new Vector2(49, -53), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle7", new Vector2(10, 29))
                .SetSidewaysData(new Vector2(44, -52), 1f, true, new Vector2(7, 30))
                .SetTopDownData(new Vector2(49, -13),      true, new Vector2(8, 20)));
            props.Add(new CrabulonProp(crab, new Vector2(62, -43), -Vector2.UnitY.RotatedBy(-0.6f), 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle8", new Vector2(1, 19))
                .SetSidewaysData(new Vector2(58, -43), 0.8f)
                .SetTopDownData(new Vector2(62, -10)));
            props.Add(new CrabulonProp(crab, new Vector2(-58, -43), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle9", new Vector2(7, 25))
                .SetSidewaysData(new Vector2(-48, -48), 1f, true, new Vector2(7, 20))
                .SetTopDownData(new Vector2(-58, -2), true, new Vector2(7, 20)));
            props.Add(new CrabulonProp(crab, new Vector2(58, -44), -Vector2.UnitY, 0.03f, 0f, MathHelper.PiOver2 * 0.8f, "Shroomy/Follicle10", new Vector2(1, 46))
                .SetSidewaysData(new Vector2(48, -46), 0.8f)
                .SetTopDownData(new Vector2(56, -7)));

            //Whiskers
            props.Add(new CrabulonProp(crab, new Vector2(-20, 10), -Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Shroomy/WhiskerL", new Vector2(43, 2), false, true) { glowmaskColor = Color.White * 0.6f, faceProp = true}
            .SetSidewaysData(new Vector2(-38, 8), 1f, true, new Vector2(35, 2))
            .SetTopDownData(new Vector2(-22, 55),     true, new Vector2(41, 4)));
            props.Add(new CrabulonProp(crab, new Vector2(11, 9),    Vector2.UnitX, 0.1f, 0.3f, MathHelper.PiOver4 * 0.4f, "Shroomy/WhiskerR", new Vector2(2, 3),  false, true) { glowmaskColor = Color.White * 0.6f, faceProp = true}
            .SetSidewaysData(new Vector2(-8, 9),  1f, true, new Vector2(3, 3))
            .SetTopDownData(new Vector2(10, 53),      true, new Vector2(3, 2)));
            
            //Eyes
            props.Add(new CrabulonProp(crab, new Vector2(-23, 8), -Vector2.UnitY.RotatedBy(0.3f), 0.1f, 0.3f, MathHelper.PiOver4 * 0.2f, "Shroomy/EyestalkL", new Vector2(8, 16), false, true, true) { faceProp = true}
            .SetSidewaysData(new Vector2(-37, 7), 1f, true, new Vector2(10, 15))
            .SetTopDownData(new Vector2(-24, 54), true, new Vector2(7, 1)));
            props.Add(new CrabulonProp(crab, new Vector2(9, 6),  -Vector2.UnitY.RotatedBy(0.3f), 0.1f, 0.3f, MathHelper.PiOver4 * 0.2f, "Shroomy/EyestalkR", new Vector2(2, 16), false, true, true) { faceProp = true}
            .SetSidewaysData(new Vector2(-10, 5), 1f, true, new Vector2(1, 15))
            .SetTopDownData(new Vector2(10, 56), true, new Vector2(3, 1))); 
            }),
#endregion
        };

        //Legs
        public static readonly CrabulonLegSkin[] LegSkins = new CrabulonLegSkin[]
        {
            new CrabulonLegSkin("Crabby/",  new Vector2(13, 12), new Vector2(8, 28)), //Crabby
            new CrabulonLegSkin("Hairy/",   new Vector2(13, 12), new Vector2(8, 32)), //Hairy
            new CrabulonLegSkin("Shroomy/", new Vector2(13, 12), new Vector2(10, 30)), //Shroomy
        };

        //Arms
        public static readonly CrabulonArmSkin[] ViolinArmSkins = new CrabulonArmSkin[]
        {
            new CrabulonArmSkin("Crabby/Violin",  new Vector2(54, 12), -2.42f, new Vector2(26, 26), -0.05f, new Vector2(42, 24), new Vector2(4, 10)),
            new CrabulonArmSkin("Hairy/Violin",   new Vector2(54, 12), -2.42f, new Vector2(20, 22), -0.05f, new Vector2(42, 24), new Vector2(4, 10)),
            new CrabulonArmSkin("Shroomy/Violin", new Vector2(54, 12), -2.42f, new Vector2(20, 24), -0.05f, new Vector2(42, 24), new Vector2(4, 10)),
        };
        public static readonly CrabulonArmSkin[] ArmSkins = new CrabulonArmSkin[]
        {
            new CrabulonArmSkin("Crabby/",  new Vector2(8, 8), -1.02f, new Vector2(59, 5), 2.63f, Vector2.Zero, Vector2.Zero),
            new CrabulonArmSkin("Hairy/",   new Vector2(8, 8), -1.02f, new Vector2(55, 6), 2.63f, Vector2.Zero, Vector2.Zero),
            new CrabulonArmSkin("Shroomy/", new Vector2(8, 8), -1.02f, new Vector2(56, 6), 2.63f, Vector2.Zero, Vector2.Zero),
        };
        #endregion

        #region Legs
        public class CrabulonLegSkin
        {
            public readonly float spriteSizeMultiplier;
            public readonly string texturePath;
            public readonly Vector2 forelegOrigin;
            public readonly Vector2 legOrigin;

            public CrabulonLegSkin(string texturePath, Vector2 forelegOrigin, Vector2 legOrigin, float spriteSizeMultiplier = 1f)
            {
                this.texturePath = texturePath;
                this.forelegOrigin = forelegOrigin;
                this.legOrigin = legOrigin;
                this.spriteSizeMultiplier = spriteSizeMultiplier;
            }
        }
        #endregion

        #region Arm
        public class CrabulonArmSkin
        {
            public readonly float spriteSizeMultiplier;
            public readonly string texturePath;

            public readonly float forearmRotationOffset;
            public readonly float armRotationOffset;

            public readonly Vector2 forearmOrigin;
            public readonly Vector2 armOrigin;

            public readonly Vector2 clawOffset;
            public readonly Vector2 clawOrigin;

            public CrabulonArmSkin(string texturePath, Vector2 forearmOrigin, float forearmRotationOffset, Vector2 armOrigin, float armRotationOffset, Vector2 clawOffset, Vector2 clawOrigin, float spriteSizeMultiplier = 1f)
            {
                this.texturePath = texturePath;
                this.forearmOrigin = forearmOrigin;
                this.forearmRotationOffset = forearmRotationOffset;
                this.armOrigin = armOrigin;
                this.armRotationOffset = armRotationOffset;
                this.spriteSizeMultiplier = spriteSizeMultiplier;
                this.clawOffset = clawOffset;
                this.clawOrigin = clawOrigin;
            }
        }
        #endregion

        #region Body
        public class CrabulonBodySkin
        {
            public readonly float spriteSizeMultiplier;
            public readonly string texturePath;
            public readonly Action<Crabulon, List<CrabulonProp>> Decorator;

            public CrabulonBodySkin(string texturePath, Action<Crabulon, List<CrabulonProp>> decorator, float sizeMultiplier = 1f)
            {
                this.texturePath = texturePath;
                this.spriteSizeMultiplier = sizeMultiplier;
                this.Decorator = decorator;
            }
        }

        public void ApplySkin(CrabulonBodySkin skin)
        {
            spriteSizeMultiplier = skin.spriteSizeMultiplier;

            ForwardsSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "Body");
            TopDownSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "BodyTopDown");
            SidewaysSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "BodySide");

            FissureSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "Fissure");
            FissureSidewaysSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "FissureSide");
            FissureTopDownSprite = ModContent.Request<Texture2D>(AssetDirectory.Crabulon + skin.texturePath + "FissureTopDown");

            Props = new List<CrabulonProp>();
            skin.Decorator(this, Props);
        }
        #endregion
    }
}
