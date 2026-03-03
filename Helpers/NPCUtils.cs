using CalamityFables.Cooldowns;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Graphics;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;
using Terraria.Utilities;
using static Terraria.GameContent.FontAssets;
using static Terraria.Player;

namespace CalamityFables.Helpers
{
    public static partial class FablesUtils
    {
        //TODO split this eventually.
        public static void AutoloadCommonBossDrops(string internalName, string displayName, string texturePath, out int maskType, out int trophyType, out int relicType, out int bagType, out AutoloadedBossBag bag, bool prehardmode, bool maskHidesHead = false)
        {
            maskType = BossMaskLoader.LoadBossMask(internalName, displayName, texturePath, maskHidesHead);
            relicType = BossRelicLoader.LoadBossRelic(internalName, displayName, texturePath);
            trophyType = BossTrophyLoader.LoadBossTrophy(internalName, displayName, texturePath);
            bagType = BossBagLoader.LoadBossBag(internalName, displayName, texturePath, prehardmode, out bag);
        }

        public static void AutoloadCommonBossDrops(string internalName, string displayName, string texturePath, out int maskType, out int trophyType, out int relicType, bool maskHidesHead = false)
        {
            maskType = BossMaskLoader.LoadBossMask(internalName, displayName, texturePath, maskHidesHead);
            relicType = BossRelicLoader.LoadBossRelic(internalName, displayName, texturePath);
            trophyType = BossTrophyLoader.LoadBossTrophy(internalName, displayName, texturePath);
        }

        public static void Resize(this NPC npc, int newWidth, int newHeight)
        {
            npc.position = npc.Center;
            npc.width = newWidth;
            npc.height = newHeight;
            npc.Center = npc.position;
        }

        public static void RemoveBuff(this NPC npc, int buffType)
        {
            if (buffType < 0 || buffType >= BuffLoader.BuffCount || !BuffID.Sets.CanBeRemovedByNetMessage[buffType])
                return;

            int buffIndex = npc.FindBuffIndex(buffType);
            if (buffIndex != -1)
            {
                npc.DelBuff(buffIndex);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.RequestNPCBuffRemoval, -1, -1, null, npc.whoAmI, buffType);
            }
        }


        private static Color TintColor(Color color, float tintR, float tintG, float tintB, float tintPercent)
        {
            color.R = (byte)MathHelper.Lerp(color.R, color.R * tintR, tintPercent);
            color.G = (byte)MathHelper.Lerp(color.G, color.G * tintG, tintPercent);
            color.B = (byte)MathHelper.Lerp(color.B, color.B * tintB, tintPercent);
            return color;
        }
        private static Color FloorColor(Color color, byte minR, byte minG, byte minB)
        {
            if (color.R < minR)
                color.R = minR;
            if (color.G < minG)
                color.G = minG;
            if (color.B < minB)
                color.B = minB;

            return color;
        }


        public static Color TintFromBuffAesthetic(this NPC npc, Color npcColor, float ichorOpacity = 0.4f, float dotOpacity = 0.2f)
        {
            if (!npc.canDisplayBuffs)
                return npcColor;

            float tintR = 1f;
            float tintG = 1f;
            float tintB = 1f;

            if (npc.poisoned)
            {
                tintR *= 0.65f;
                tintB *= 0.75f;
            }

            if (npc.venom)
            {
                tintG *= 0.45f;
                tintR *= 0.75f;
            }

            if (npc.midas)
            {
                tintB *= 0.3f;
                tintR *= 0.85f;
            }

            if (npc.betsysCurse)
            {
                tintR *= 0.8f;
                tintG *= 0.6f;
            }

            if (npc.oiled)
            {
                tintR *= 0.4f;
                tintG *= 0.4f;
                tintB *= 0.4f;
            }

            if (npc.stinky)
            {
                tintR *= 0.7f;
                tintB *= 0.55f;
            }

            if (npc.drippingSlime)
            {
                tintR *= 0.8f;
                tintG *= 0.8f;
            }

            if (npc.drippingSparkleSlime)
            {
                tintB *= 0.85f;
                tintG *= 0.75f;
            }

            npcColor = TintColor(npcColor, tintR, tintG, tintB, dotOpacity);

            if (npc.ichor)
            {
                npcColor = TintColor(npcColor, 1f, 1f, 0f, ichorOpacity);
                npcColor = FloorColor(npcColor, 100, 84, 20);
            }

            if (npc.CanApplyHunterPotionEffects() && npc.lifeMax > 1)
            {
                byte highlightR;
                byte highlightG;
                byte highlightB;

                //Critters or friendly NPCs
                if (npc.friendly || npc.catchItem > 0 || (npc.damage == 0 && npc.lifeMax == 5))
                {
                    highlightR = 50;
                    highlightG = byte.MaxValue;
                    highlightB = 50;
                }
                else
                {
                    highlightR = byte.MaxValue;
                    highlightG = 50;
                    highlightB = 50;
                }

                npcColor = FloorColor(npcColor, highlightR, highlightG, highlightB);
            }

            return npcColor;
        }


        public static float HalfDiagonalLenght(this NPC npc) => (float)Math.Sqrt(Math.Pow(npc.width / 2, 2) + Math.Pow(npc.height / 2, 2));


        public static void HideFromBestiary(this ModNPC n)
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(n.Type, value);
        }


        internal static bool PlayerActiveDelegate(Player p) => p.active;
        public static Player GetNearestPlayer(this Entity entity, Func<Player, bool> predicate = null)
        {
            if (predicate == null)
                predicate = PlayerActiveDelegate;

            return Main.player.Where(predicate).OrderBy(p => p.Distance(entity.Center)).FirstOrDefault();
        }

        public static int[] GetLocalNPCImmunity(this Projectile proj)
        {
            if (proj.type == 626 || proj.type == 627 || proj.type == 628)
            {
                Projectile projectile = proj.FindStardustDragonHeadOfOwner();
                if (projectile != null)
                    return projectile.localNPCImmunity;
            }

            return proj.localNPCImmunity;
        }

        private static readonly MethodInfo findStardustDragonHeadOfOwnerMethod = typeof(Projectile).GetMethod("FindStardustDragonHeadOfOwner", BindingFlags.NonPublic | BindingFlags.Instance);
        public static Projectile FindStardustDragonHeadOfOwner(this Projectile proj)
        {
            var result = findStardustDragonHeadOfOwnerMethod.Invoke(proj, null);
            if (result == null)
                return null;
            return (Projectile)result;
        }

        public static bool? VanillaCanBeHitByPlayer(this NPC npc, Player player)
        {
            if (npc.immune[player.whoAmI] != 0)
                return false;
            return null;
        }

        public static bool? VanillaCanBeHitByProjectile(this NPC npc, Projectile projectile)
        {
            int[] immunity = projectile.GetLocalNPCImmunity();
            bool notImmune = (!projectile.usesLocalNPCImmunity && !projectile.usesIDStaticNPCImmunity) ||
                        (projectile.usesLocalNPCImmunity && immunity[npc.whoAmI] == 0) ||
                        (projectile.usesIDStaticNPCImmunity && Projectile.IsNPCIndexImmuneToProjectileType(projectile.type, npc.whoAmI));

            if (!notImmune)
                return false;
            return null;
        }

        //TODO 1.4.4 : Update this haha.
        public static void VanillaSpoofProjectileHitIFrames(this NPC npc, Projectile projectile)
        {
            int[] immunity = projectile.GetLocalNPCImmunity();

            #region Fugly
            if (projectile.usesIDStaticNPCImmunity)
            {
                npc.immune[projectile.owner] = 0;
                Projectile.perIDStaticNPCImmunity[projectile.type][npc.whoAmI] = (uint)((int)Main.GameUpdateCount + projectile.idStaticNPCHitCooldown);
            }

            else if (projectile.type == 632)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 514)
            {
                npc.immune[projectile.owner] = 1;
            }
            else if (projectile.type == 595 || projectile.type == 735)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 927)
            {
                npc.immune[projectile.owner] = 4;
            }
            else if (projectile.type == 286)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 443)
            {
                npc.immune[projectile.owner] = 8;
            }
            else if (projectile.type >= 424 && projectile.type <= 426)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 634 || projectile.type == 635)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 659)
            {
                npc.immune[projectile.owner] = 5;
            }
            else if (projectile.type == 246)
            {
                npc.immune[projectile.owner] = 7;
            }
            else if (projectile.type == 249)
            {
                npc.immune[projectile.owner] = 7;
            }
            else if (projectile.type == 16)
            {
                npc.immune[projectile.owner] = 8;
            }
            else if (projectile.type == 409)
            {
                npc.immune[projectile.owner] = 6;
            }
            else if (projectile.type == 407)
            {
                npc.immune[projectile.owner] = 20;
            }
            else if (projectile.type == 311)
            {
                npc.immune[projectile.owner] = 7;
            }
            else if (projectile.type == 582 || projectile.type == 902)
            {
                npc.immune[projectile.owner] = 7;
            }
            else
            {
                if (projectile.type == 864)
                {
                    immunity[npc.whoAmI] = 10;
                    npc.immune[projectile.owner] = 0;
                }
                else if (projectile.type == 661 || projectile.type == 856)
                {
                    immunity[npc.whoAmI] = 8;
                    npc.immune[projectile.owner] = 0;
                }
                else if (projectile.type == 866)
                {
                    immunity[npc.whoAmI] = -1;
                    npc.immune[projectile.owner] = 0;
                }
                else if (projectile.usesLocalNPCImmunity && projectile.localNPCHitCooldown != -2)
                {
                    npc.immune[projectile.owner] = 0;
                    immunity[npc.whoAmI] = projectile.localNPCHitCooldown;
                }
                else if (projectile.penetrate != 1)
                {
                    npc.immune[projectile.owner] = 10;
                }
            }

            //OnHitByProjectile happens here

            if (projectile.type == 638 || projectile.type == 639 || projectile.type == 640)
            {
                immunity[npc.whoAmI] = -1;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 617)
            {
                immunity[npc.whoAmI] = 8;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 656)
            {
                immunity[npc.whoAmI] = 8;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 618)
            {
                immunity[npc.whoAmI] = 20;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 642)
            {
                immunity[npc.whoAmI] = 10;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 857)
            {
                immunity[npc.whoAmI] = 10;
                npc.immune[projectile.owner] = 0;
            }
            else if (projectile.type == 611 || projectile.type == 612)
            {
                immunity[npc.whoAmI] = 6;
                npc.immune[projectile.owner] = 4;
            }
            else if (projectile.type == 645)
            {
                immunity[npc.whoAmI] = -1;
                npc.immune[projectile.owner] = 0;
            }
            #endregion
        }

        public static void VanillaSpoofPlayerHitIFrames(this NPC npc, Player player)
        {
            npc.immune[player.whoAmI] = player.itemAnimation;
        }

        /// <summary>
        /// Detects nearby hostile NPCs from a given point
        /// </summary>
        /// <param name="origin">The position where we wish to check for nearby NPCs</param>
        /// <param name="maxDistanceToCheck">Maximum amount of pixels to check around the origin</param>
        /// <param name="ignoreTiles">Whether to ignore tiles when finding a target or not</param>
        /// <param name="bossPriority">Whether bosses should be prioritized in targetting or not</param>
        public static NPC ClosestNPCAt(this Vector2 origin, float maxDistanceToCheck, bool ignoreTiles = true, bool bossPriority = false)
        {
            NPC closestTarget = null;
            float distance = maxDistanceToCheck;
            if (bossPriority)
            {
                bool bossFound = false;
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    // If we've found a valid boss target, ignore ALL targets which aren't bosses.
                    if (bossFound && !(Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye))
                        continue;
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);

                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);

                        if (Vector2.Distance(origin, Main.npc[index].Center) < (distance + extraDistance) && canHit)
                        {
                            if (Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye)
                                bossFound = true;
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            else
            {
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);

                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);

                        if (Vector2.Distance(origin, Main.npc[index].Center) < (distance + extraDistance) && canHit)
                        {
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            return closestTarget;
        }

        /// <summary>
        /// Callback to make an attack deal damage with no positive scaling. Not compatible with crits unfortunately
        /// </summary>
        /// <param name="info"></param>
        /// <param name="unchangingDamage"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static NPC.HitInfo SetUnscaledDamage(this NPC.HitInfo info, int unchangingDamage, NPC.HitModifiers modifier)
        {
            info.SourceDamage = unchangingDamage;

            //We recalculating damage jjjj

            //WE can't get the crit override here so L
            if (modifier.SuperArmor)
                info.Damage = 1;
            else
            {
                float damage = unchangingDamage;

                float defense = modifier.Defense.ApplyTo(0);
                float armorPenetration = defense * Math.Clamp(modifier.ScalingArmorPenetration.Value, 0, 1) + modifier.ArmorPenetration.Value;
                defense = Math.Max(defense - armorPenetration, 0);

                float damageReduction = defense * modifier.DefenseEffectiveness.Value;
                damage = Math.Max(damage - damageReduction, 1);

                info.Damage = (int)damage;
            }

            return info;
        }
    }
}
