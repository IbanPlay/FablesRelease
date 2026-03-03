namespace CalamityFables.Content.Boss.MushroomCrabBoss
{
    public class SporeHeart : ModItem
    {
        public override string Texture => AssetDirectory.Crabulon + Name;

        public override void Load()
        {
            Terraria.On_Player.PickupItem += PickupSporeHeart;
        }

        private Item PickupSporeHeart(Terraria.On_Player.orig_PickupItem orig, Player self, int playerIndex, int worldItemArrayIndex, Item itemToPickUp)
        {
            if (itemToPickUp.type == Type)
            {
                SoundEngine.PlaySound(SoundID.Grab, self.Center);

                if (self.HasBuff<CrabulonDOT>())
                {
                    self.ClearBuff(ModContent.BuffType<CrabulonDOT>());
                    self.Heal(Crabulon.SporeHeart_SporedHealing);
                }
                else
                    self.Heal(Crabulon.SporeHeart_UnsporedHealing);

                itemToPickUp = new Item();
                Main.item[worldItemArrayIndex] = itemToPickUp;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, worldItemArrayIndex);
                return itemToPickUp;
            }

            else
                return orig(self, playerIndex, worldItemArrayIndex, itemToPickUp);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Heart");
            ItemID.Sets.IgnoresEncumberingStone[Type] = true;
            ItemID.Sets.IsAPickup[Type] = true;
            ItemID.Sets.ItemSpawnDecaySpeed[Type] = 1;
            Item.ResearchUnlockCount = 0;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
        }

        public override bool ItemSpace(Player player) => true;


        public override void GrabRange(Player player, ref int grabRange)
        {
            if (player.lifeMagnet)
                grabRange += Item.lifeGrabRange / 2; //Life magnet only half as effective
        }

        public override bool GrabStyle(Player player)
        {
            //This is just Player.PullItem_Pickup which is private fsr
            float distanceToPlayer = Item.Distance(player.Center);
            distanceToPlayer = 15 / distanceToPlayer;
            Item.velocity = (Item.velocity * 4f + (player.Center - Item.Center) * distanceToPlayer) / 5f;
            Lighting.AddLight(Item.position, CommonColors.MushroomDeepBlue.ToVector3() * 4f);
            return true;
        }

        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            Lighting.AddLight(Item.position, CommonColors.MushroomDeepBlue.ToVector3() * 4f);
            if (Item.timeSinceItemSpawned > 5 * 60)
            {
                Item.active = false;

                for (int i = 0; i < 5; i++)
                {
                    Vector2 gushDirection = Main.rand.NextVector2CircularEdge(1, 1);
                    Vector2 dustPosition = Item.Center + gushDirection * Main.rand.NextFloat(0f, 2.6f);
                    Vector2 velocity = gushDirection.RotatedByRandom(MathHelper.PiOver4) * Main.rand.NextFloat(0f, 3f);
                    Dust d = Dust.NewDustPerfect(dustPosition, DustID.GlowingMushroom, velocity);
                    d.noLightEmittence = true;
                }
            }
        }
    }
}
