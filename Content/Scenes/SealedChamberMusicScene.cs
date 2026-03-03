using CalamityFables.Content.Boss.SeaKnightMiniboss;

namespace CalamityFables.Content.Scenes
{
    public class SealedChamberMusicScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Sounds/Music/SealedChamber");

        public override bool IsSceneEffectActive(Player player)
        {
            int npcType = ModContent.NPCType<SirNautilusPassive>();
            Rectangle chamberRect = PointOfInterestMarkerSystem.NautilusChamberWorldRectangle;

            if (PointOfInterestMarkerSystem.NautilusChamberPos != Vector2.Zero)
            {
                //If theres a chamber in the world and the player isn't even inside, don't play the theme no matter what.
                if (!player.Hitbox.Intersects(chamberRect))
                    return false;

                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC npc = Main.npc[j];
                    if (!npc.active || npc.type != npcType)
                        continue;

                    //Check if the nautilus is inside the chamber
                    if (npc.Hitbox.Intersects(chamberRect))
                        return true;
                }
                return false;
            }

            //IF theres no chamber in the world
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC npc = Main.npc[j];
                if (!npc.active || npc.type != npcType)
                    continue;

                Rectangle npcBox = new Rectangle((int)npc.Center.X - chamberRect.Width / 2, (int)npc.Center.Y - chamberRect.Height / 2, chamberRect.Width, chamberRect.Height + 100);
                if (player.Hitbox.Intersects(npcBox))
                    return true;
            }

            return false;
        }
    }
}
