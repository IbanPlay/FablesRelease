namespace CalamityFables.Core
{
    //The "Unique dust" of "Unique AIs"
    public class DustSimulation
    {
        /*
        public List<Dust> simulatedDust;

        public DustSimulation()
        {
            simulatedDust = new List<Dust>();
        }

        public void UpdateDusts()
        {
            bool shouldSandstormParticlesPersist = false;
            int dustCount = simulatedDust.Count;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = simulatedDust[i];
                if (i < Main.maxDustToDraw)
                {
                    if (!dust.active)
                        continue;

                    ModDust modDust = DustLoader.GetDust(dust.type);
                    if (modDust != null && !modDust.Update(dust))
                    {
                        continue;
                    }

                    if (dust.scale > 10f)
                        dust.active = false;

                    int num3 = dust.type;
                    if ((uint)(num3 - 299) <= 2u)
                    {
                        dust.scale *= 0.96f;
                        dust.velocity.Y -= 0.01f;
                    }


                    dust.position += dust.velocity;
                    if (dust.type == 258)
                    {
                        dust.noGravity = true;
                        dust.scale += 0.015f;
                    }

                    if (dust.type == 230)
                    {
                        dust.scale += 0.02f;
                    }

                    if (dust.type == 154 || dust.type == 218)
                    {
                        dust.rotation += dust.velocity.X * 0.3f;
                        dust.scale -= 0.03f;
                    }

                    if (dust.type == 172)
                    {
                        float num14 = dust.scale * 0.5f;
                        if (num14 > 1f)
                            num14 = 1f;

                        float num15 = num14;
                        float num16 = num14;
                        float num17 = num14;
                        num15 *= 0f;
                        num16 *= 0.25f;
                        num17 *= 1f;
                    }

                    if (dust.type == 182)
                    {
                        dust.rotation += 1f;
                        if (!dust.noLight)
                        {
                            float num18 = dust.scale * 0.25f;
                            if (num18 > 1f)
                                num18 = 1f;

                            float num19 = num18;
                            float num20 = num18;
                            float num21 = num18;
                            num19 *= 1f;
                            num20 *= 0.2f;
                            num21 *= 0.1f;
                        }
                    }

                    if (dust.type == 261)
                    {
                        if (!dust.noLight && !dust.noLightEmittence)
                        {
                            float num22 = dust.scale * 0.3f;
                            if (num22 > 1f)
                                num22 = 1f;
                        }

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }

                        dust.velocity *= new Vector2(0.97f, 0.99f);
                        dust.scale -= 0.0025f;
                        if (dust.customData != null && dust.customData is Player)
                        {
                            Player player4 = (Player)dust.customData;
                            dust.position += player4.position - player4.oldPosition;
                        }
                    }

                    if (dust.type == 254)
                    {
                        float num23 = dust.scale * 0.35f;
                        if (num23 > 1f)
                            num23 = 1f;

                        float num24 = num23;
                        float num25 = num23;
                        float num26 = num23;
                        num24 *= 0.9f;
                        num25 *= 0.1f;
                        num26 *= 0.75f;
                    }

                    if (dust.type == 255)
                    {
                        float num27 = dust.scale * 0.25f;
                        if (num27 > 1f)
                            num27 = 1f;

                        float num28 = num27;
                        float num29 = num27;
                        float num30 = num27;
                        num28 *= 0.9f;
                        num29 *= 0.1f;
                        num30 *= 0.75f;
                    }

                    if (dust.type == 211 && dust.noLight && Collision.SolidCollision(dust.position, 4, 4))
                        dust.active = false;

                    if (dust.type == 213 || dust.type == 260)
                    {
                        dust.rotation = 0f;
                        float num31 = dust.scale / 2.5f * 0.2f;
                        Vector3 vector = Vector3.Zero;
                        switch (dust.type)
                        {
                            case 213:
                                vector = new Vector3(255f, 217f, 48f);
                                break;
                            case 260:
                                vector = new Vector3(255f, 48f, 48f);
                                break;
                        }

                        vector /= 255f;
                        if (num31 > 1f)
                            num31 = 1f;

                        vector *= num31;
                    }

                    if (dust.type == 157)
                    {
                        float num32 = dust.scale * 0.2f;
                        float num33 = num32;
                        float num34 = num32;
                        float num35 = num32;
                        num33 *= 0.25f;
                        num34 *= 1f;
                        num35 *= 0.5f;
                    }

                    if (dust.type == 206)
                    {
                        dust.scale -= 0.1f;
                        float num36 = dust.scale * 0.4f;
                        float num37 = num36;
                        float num38 = num36;
                        float num39 = num36;
                        num37 *= 0.1f;
                        num38 *= 0.6f;
                        num39 *= 1f;
                    }

                    if (dust.type == 163)
                    {
                        float num40 = dust.scale * 0.25f;
                        float num41 = num40;
                        float num42 = num40;
                        float num43 = num40;
                        num41 *= 0.25f;
                        num42 *= 1f;
                        num43 *= 0.05f;
                    }

                    if (dust.type == 205)
                    {
                        float num44 = dust.scale * 0.25f;
                        float num45 = num44;
                        float num46 = num44;
                        float num47 = num44;
                        num45 *= 1f;
                        num46 *= 0.05f;
                        num47 *= 1f;
                    }

                    if (dust.type == 170)
                    {
                        float num48 = dust.scale * 0.5f;
                        float num49 = num48;
                        float num50 = num48;
                        float num51 = num48;
                        num49 *= 1f;
                        num50 *= 1f;
                        num51 *= 0.05f;
                    }

                    if (dust.type == 156)
                    {
                        float num52 = dust.scale * 0.6f;
                        _ = dust.type;
                        float num53 = num52;
                        float num54 = num52;
                        num53 *= 0.9f;
                        num54 *= 1f;
                    }

                    if (dust.type == 234)
                    {
                        float lightAmount = dust.scale * 0.6f;
                        _ = dust.type;
                    }

                    if (dust.type == 175)
                        dust.scale -= 0.05f;

                    if (dust.type == 174)
                    {
                        dust.scale -= 0.01f;
                        float num55 = dust.scale * 1f;
                        if (num55 > 0.6f)
                            num55 = 0.6f;
                    }

                    if (dust.type == 235)
                    {
                        Vector2 vector2 = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                        vector2.Normalize();
                        vector2 *= 15f;
                        dust.scale -= 0.01f;
                    }
                    else if (dust.type == 228 || dust.type == 279 || dust.type == 229 || dust.type == 6 || dust.type == 242 || dust.type == 135 || dust.type == 127 || dust.type == 187 || dust.type == 75 || dust.type == 169 || dust.type == 29 || (dust.type >= 59 && dust.type <= 65) || dust.type == 158 || dust.type == 293 || dust.type == 294 || dust.type == 295 || dust.type == 296 || dust.type == 297 || dust.type == 298 || dust.type == 302)
                    {
                        if (!dust.noGravity)
                            dust.velocity.Y += 0.05f;
                    }
                    else if (dust.type == 269)
                    {
                        if (!dust.noLight)
                        {
                            float num57 = dust.scale * 1.4f;
                            if (num57 > 1f)
                                num57 = 1f;
                        }

                        if (dust.customData != null && dust.customData is Vector2)
                        {
                            Vector2 vector4 = (Vector2)dust.customData - dust.position;
                            dust.velocity.X += 1f * (float)Math.Sign(vector4.X) * dust.scale;
                        }
                    }
                    else if (dust.type == 159)
                    {
                        float num58 = dust.scale * 1.3f;
                        if (num58 > 1f)
                            num58 = 1f;
                        if (dust.noGravity)
                        {
                            if (dust.scale < 0.7f)
                                dust.velocity *= 1.075f;
                            else if (Main.rand.Next(2) == 0)
                                dust.velocity *= -0.95f;
                            else
                                dust.velocity *= 1.05f;

                            dust.scale -= 0.03f;
                        }
                        else
                        {
                            dust.scale += 0.005f;
                            dust.velocity *= 0.9f;
                            dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.02f;
                            dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.02f;
                        }
                    }
                    else if (dust.type == 164)
                    {
                        float num60 = dust.scale;
                        if (num60 > 1f)
                            num60 = 1f;

                        if (dust.noGravity)
                        {
                            if (dust.scale < 0.7f)
                                dust.velocity *= 1.075f;
                            else if (Main.rand.Next(2) == 0)
                                dust.velocity *= -0.95f;
                            else
                                dust.velocity *= 1.05f;

                            dust.scale -= 0.03f;
                        }
                        else
                        {
                            dust.scale -= 0.005f;
                            dust.velocity *= 0.9f;
                            dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.02f;
                            dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.02f;
                        }
                    }
                    else if (dust.type == 173)
                    {
                        float num62 = dust.scale;
                        if (num62 > 1f)
                            num62 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.8f;
                            dust.velocity.X += (float)Main.rand.Next(-20, 21) * 0.01f;
                            dust.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.01f;
                            dust.scale -= 0.01f;
                        }
                        else
                        {
                            dust.scale -= 0.015f;
                            dust.velocity *= 0.8f;
                            dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.005f;
                            dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.005f;
                        }
                    }
                    else if (dust.type == 304)
                    {
                        dust.velocity.Y = (float)Math.Sin(dust.rotation) / 5f;
                        dust.rotation += 0.015f;
                        if (dust.scale < 1.15f)
                        {
                            dust.alpha = Math.Max(0, dust.alpha - 20);
                            dust.scale += 0.0015f;
                        }
                        else
                        {
                            dust.alpha += 6;
                            if (dust.alpha >= 255)
                                dust.active = false;
                        }
                    }
                    else if (dust.type == 184)
                    {
                        if (!dust.noGravity)
                        {
                            dust.velocity *= 0f;
                            dust.scale -= 0.01f;
                        }
                    }
                    else if (dust.type == 160 || dust.type == 162)
                    {
                        float num66 = dust.scale * 1.3f;
                        if (num66 > 1f)
                            num66 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.8f;
                            dust.velocity.X += (float)Main.rand.Next(-20, 21) * 0.04f;
                            dust.velocity.Y += (float)Main.rand.Next(-20, 21) * 0.04f;
                            dust.scale -= 0.1f;
                        }
                        else
                        {
                            dust.scale -= 0.1f;
                            dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.02f;
                            dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.02f;
                        }
                    }
                    else if (dust.type == 168)
                    {
                        float num68 = dust.scale * 0.8f;
                        if ((double)num68 > 0.55)
                            num68 = 0.55f;

                        dust.scale += 0.03f;
                        dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.02f;
                        dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.02f;
                        dust.velocity *= 0.99f;
                    }
                    else if (dust.type >= 139 && dust.type < 143)
                    {
                        dust.velocity.X *= 0.98f;
                        dust.velocity.Y *= 0.98f;
                        if (dust.velocity.Y < 1f)
                            dust.velocity.Y += 0.05f;

                        dust.scale += 0.009f;
                        dust.rotation -= dust.velocity.X * 0.4f;
                        if (dust.velocity.X > 0f)
                            dust.rotation += 0.005f;
                        else
                            dust.rotation -= 0.005f;
                    }
                    else if (dust.type == 14 || dust.type == 16 || dust.type == 31 || dust.type == 46 || dust.type == 124 || dust.type == 186 || dust.type == 188 || dust.type == 303)
                    {
                        dust.velocity.Y *= 0.98f;
                        dust.velocity.X *= 0.98f;
                        if (dust.type == 31)
                        {
                            if (dust.noGravity)
                            {
                                dust.velocity *= 1.02f;
                                dust.scale += 0.02f;
                                dust.alpha += 4;
                                if (dust.alpha > 255)
                                {
                                    dust.scale = 0.0001f;
                                    dust.alpha = 255;
                                }
                            }
                        }

                        if (dust.type == 303 && dust.noGravity)
                        {
                            dust.velocity *= 1.02f;
                            dust.scale += 0.03f;
                            if (dust.alpha < 90)
                                dust.alpha = 90;

                            dust.alpha += 4;
                            if (dust.alpha > 255)
                            {
                                dust.scale = 0.0001f;
                                dust.alpha = 255;
                            }
                        }
                    }
                    else if (dust.type == 32)
                    {
                        dust.scale -= 0.01f;
                        dust.velocity.X *= 0.96f;
                        if (!dust.noGravity)
                            dust.velocity.Y += 0.1f;
                    }
                    else if (dust.type >= 244 && dust.type <= 247)
                    {
                        dust.rotation += 0.1f * dust.scale;
                        Color color = Color.White;
                        byte num69 = (byte)((color.R + color.G + color.B) / 3);
                        float num70 = ((float)(int)num69 / 270f + 1f) / 2f;
                        float num71 = ((float)(int)num69 / 270f + 1f) / 2f;
                        float num72 = ((float)(int)num69 / 270f + 1f) / 2f;
                        num70 *= dust.scale * 0.9f;
                        num71 *= dust.scale * 0.9f;
                        num72 *= dust.scale * 0.9f;
                        if (dust.alpha < 255)
                        {
                            dust.scale += 0.09f;
                            if (dust.scale >= 1f)
                            {
                                dust.scale = 1f;
                                dust.alpha = 255;
                            }
                        }
                        else
                        {
                            if ((double)dust.scale < 0.8)
                                dust.scale -= 0.01f;

                            if ((double)dust.scale < 0.5)
                                dust.scale -= 0.01f;
                        }

                        float num73 = 1f;
                        if (dust.type == 244)
                        {
                            num70 *= 226f / 255f;
                            num71 *= 118f / 255f;
                            num72 *= 76f / 255f;
                            num73 = 0.9f;
                        }
                        else if (dust.type == 245)
                        {
                            num70 *= 131f / 255f;
                            num71 *= 172f / 255f;
                            num72 *= 173f / 255f;
                            num73 = 1f;
                        }
                        else if (dust.type == 246)
                        {
                            num70 *= 0.8f;
                            num71 *= 181f / 255f;
                            num72 *= 24f / 85f;
                            num73 = 1.1f;
                        }
                        else if (dust.type == 247)
                        {
                            num70 *= 0.6f;
                            num71 *= 172f / 255f;
                            num72 *= 37f / 51f;
                            num73 = 1.2f;
                        }

                        num70 *= num73;
                        num71 *= num73;
                        num72 *= num73;
                    }
                    else if (dust.type == 43)
                    {
                        dust.rotation += 0.1f * dust.scale;
                        Color color2 = Color.White;
                        float num74 = (float)(int)color2.R / 270f;
                        float num75 = (float)(int)color2.G / 270f;
                        float num76 = (float)(int)color2.B / 270f;
                        float num77 = (float)(int)dust.color.R / 255f;
                        float num78 = (float)(int)dust.color.G / 255f;
                        float num79 = (float)(int)dust.color.B / 255f;
                        num74 *= dust.scale * 1.07f * num77;
                        num75 *= dust.scale * 1.07f * num78;
                        num76 *= dust.scale * 1.07f * num79;
                        if (dust.alpha < 255)
                        {
                            dust.scale += 0.09f;
                            if (dust.scale >= 1f)
                            {
                                dust.scale = 1f;
                                dust.alpha = 255;
                            }
                        }
                        else
                        {
                            if ((double)dust.scale < 0.8)
                                dust.scale -= 0.01f;

                            if ((double)dust.scale < 0.5)
                                dust.scale -= 0.01f;
                        }

                        if ((double)num74 < 0.05 && (double)num75 < 0.05 && (double)num76 < 0.05)
                            dust.active = false;
                    }
                    else if (dust.type == 15 || dust.type == 57 || dust.type == 58 || dust.type == 274 || dust.type == 292)
                    {
                        dust.velocity.Y *= 0.98f;
                        dust.velocity.X *= 0.98f;
                        if (!dust.noLightEmittence)
                        {
                            float num80 = dust.scale;
                            if (dust.type != 15)
                                num80 = dust.scale * 0.8f;

                            if (dust.noLight)
                                dust.velocity *= 0.95f;

                            if (num80 > 1f)
                                num80 = 1f;
                        }
                    }
                    else if (dust.type == 204)
                    {
                        if (dust.fadeIn > dust.scale)
                            dust.scale += 0.02f;
                        else
                            dust.scale -= 0.02f;

                        dust.velocity *= 0.95f;
                    }
                    else if (dust.type == 110)
                    {
                        float num81 = dust.scale * 0.1f;
                        if (num81 > 1f)
                            num81 = 1f;
                    }
                    else if (dust.type == 111)
                    {
                        float num82 = dust.scale * 0.125f;
                        if (num82 > 1f)
                            num82 = 1f;
                    }
                    else if (dust.type == 112)
                    {
                        float num83 = dust.scale * 0.1f;
                        if (num83 > 1f)
                            num83 = 1f;
                    }
                    else if (dust.type == 113)
                    {
                        float num84 = dust.scale * 0.1f;
                        if (num84 > 1f)
                            num84 = 1f;
                    }
                    else if (dust.type == 114)
                    {
                        float num85 = dust.scale * 0.1f;
                        if (num85 > 1f)
                            num85 = 1f;
                    }
                    else if (dust.type == 66)
                    {
                        if (dust.velocity.X < 0f)
                            dust.rotation -= 1f;
                        else
                            dust.rotation += 1f;

                        dust.velocity.Y *= 0.98f;
                        dust.velocity.X *= 0.98f;
                        dust.scale += 0.02f;
                        float num86 = dust.scale;
                        if (dust.type != 15)
                            num86 = dust.scale * 0.8f;

                        if (num86 > 1f)
                            num86 = 1f;
                    }
                    else if (dust.type == 267)
                    {
                        if (dust.velocity.X < 0f)
                            dust.rotation -= 1f;
                        else
                            dust.rotation += 1f;

                        dust.velocity.Y *= 0.98f;
                        dust.velocity.X *= 0.98f;
                        dust.scale += 0.02f;
                        float num87 = dust.scale * 0.8f;
                        if (num87 > 1f)
                            num87 = 1f;

                        if (dust.noLight)
                            dust.noLight = false;
                    }
                    else if (dust.type == 20 || dust.type == 21 || dust.type == 231)
                    {
                        dust.scale += 0.005f;
                        dust.velocity.Y *= 0.94f;
                        dust.velocity.X *= 0.94f;
                        float num88 = dust.scale * 0.8f;
                        if (num88 > 1f)
                            num88 = 1f;
                    }
                    else if (dust.type == 27 || dust.type == 45)
                    {
                        if (dust.type == 27 && dust.fadeIn >= 100f)
                        {
                            if ((double)dust.scale >= 1.5)
                                dust.scale -= 0.01f;
                            else
                                dust.scale -= 0.05f;

                            if ((double)dust.scale <= 0.5)
                                dust.scale -= 0.05f;

                            if ((double)dust.scale <= 0.25)
                                dust.scale -= 0.05f;
                        }

                        dust.velocity *= 0.94f;
                        dust.scale += 0.002f;
                        float num89 = dust.scale;
                        if (dust.noLight)
                        {
                            num89 *= 0.1f;
                            dust.scale -= 0.06f;
                            if (dust.scale < 1f)
                                dust.scale -= 0.06f;
                        }

                        if (num89 > 1f)
                            num89 = 1f;
                    }
                    else if (dust.type == 55 || dust.type == 56 || dust.type == 73 || dust.type == 74)
                    {
                        dust.velocity *= 0.98f;
                        if (!dust.noLightEmittence)
                        {
                            float num90 = dust.scale * 0.8f;
                            if (dust.type == 55)
                            {
                                if (num90 > 1f)
                                    num90 = 1f;
                            }
                            else if (dust.type == 73)
                            {
                                if (num90 > 1f)
                                    num90 = 1f;
                            }
                            else if (dust.type == 74)
                            {
                                if (num90 > 1f)
                                    num90 = 1f;
                            }
                            else
                            {
                                num90 = dust.scale * 1.2f;
                                if (num90 > 1f)
                                    num90 = 1f;
                            }
                        }
                    }
                    else if (dust.type == 71 || dust.type == 72)
                    {
                        dust.velocity *= 0.98f;
                        float num91 = dust.scale;
                    }
                    else if (dust.type == 270)
                    {
                        dust.velocity *= 1.0050251f;
                        dust.scale += 0.01f;
                        dust.rotation = 0f;
                        dust.velocity.Y = (float)Math.Sin(dust.position.X * 0.0043982295f) * 2f;
                        dust.velocity.Y -= 3f;
                        dust.velocity.Y /= 20f;

                    }
                    else if (dust.type == 271)
                    {
                        dust.velocity *= 1.0050251f;
                        dust.scale += 0.003f;
                        dust.rotation = 0f;
                        dust.velocity.Y -= 4f;
                        dust.velocity.Y /= 6f;
                    }
                    else if (dust.type == 268)
                    {
                        dust.velocity *= 1.0050251f;
                        dust.scale += 0.01f;
                        if (!shouldSandstormParticlesPersist)
                            dust.scale -= 0.05f;

                        dust.rotation = 0f;
                        dust.velocity.Y = (float)Math.Sin(dust.position.X * 0.0043982295f) * 2f;
                        dust.velocity.Y += 3f;

                    }
                    else if (!dust.noGravity && dust.type != 41 && dust.type != 44)
                    {
                        if (dust.type == 107)
                            dust.velocity *= 0.9f;
                        else
                            dust.velocity.Y += 0.1f;
                    }

                    if (dust.type == 5 || (dust.type == 273 && dust.noGravity))
                        dust.scale -= 0.04f;

                    if (dust.type == 33 || dust.type == 52 || dust.type == 266 || dust.type == 98 || dust.type == 99 || dust.type == 100 || dust.type == 101 || dust.type == 102 || dust.type == 103 || dust.type == 104 || dust.type == 105 || dust.type == 123 || dust.type == 288)
                    {
                        if (dust.velocity.X == 0f)
                        {
                            dust.rotation += 0.5f;
                            dust.scale -= 0.01f;
                        }

                        dust.alpha += 2;
                        dust.scale -= 0.005f;
                        if (dust.alpha > 255)
                            dust.scale = 0f;

                        if (dust.velocity.Y > 4f)
                            dust.velocity.Y = 4f;

                        if (dust.noGravity)
                        {
                            if (dust.velocity.X < 0f)
                                dust.rotation -= 0.2f;
                            else
                                dust.rotation += 0.2f;

                            dust.scale += 0.03f;
                            dust.velocity.X *= 1.05f;
                            dust.velocity.Y += 0.15f;
                        }
                    }

                    if (dust.type == 35 && dust.noGravity)
                    {
                        dust.scale += 0.03f;
                        if (dust.scale < 1f)
                            dust.velocity.Y += 0.075f;

                        dust.velocity.X *= 1.08f;
                        if (dust.velocity.X > 0f)
                            dust.rotation += 0.01f;
                        else
                            dust.rotation -= 0.01f;

                        float num92 = dust.scale * 0.6f;
                        if (num92 > 1f)
                            num92 = 1f;
                    }
                    else if (dust.type == 152 && dust.noGravity)
                    {
                        dust.scale += 0.03f;
                        if (dust.scale < 1f)
                            dust.velocity.Y += 0.075f;

                        dust.velocity.X *= 1.08f;
                        if (dust.velocity.X > 0f)
                            dust.rotation += 0.01f;
                        else
                            dust.rotation -= 0.01f;
                    }
                    else if (dust.type == 67 || dust.type == 92)
                    {
                        float num93 = dust.scale;
                        if (num93 > 1f)
                            num93 = 1f;

                        if (dust.noLight)
                            num93 *= 0.1f;
                    }
                    else if (dust.type == 185)
                    {
                        float num94 = dust.scale;
                        if (num94 > 1f)
                            num94 = 1f;

                        if (dust.noLight)
                            num94 *= 0.1f;
                    }
                    else if (dust.type == 107)
                    {
                        float num95 = dust.scale * 0.5f;
                        if (num95 > 1f)
                            num95 = 1f;
                    }
                    else if (dust.type == 34 || dust.type == 35 || dust.type == 152)
                    {
                        dust.alpha += Main.rand.Next(2);
                        if (dust.alpha > 255)
                            dust.scale = 0f;

                        dust.velocity.Y = -0.5f;
                        if (dust.type == 34)
                        {
                            dust.scale += 0.005f;
                        }
                        else
                        {
                            dust.alpha++;
                            dust.scale -= 0.01f;
                            dust.velocity.Y = -0.2f;
                        }

                        dust.velocity.X += (float)Main.rand.Next(-10, 10) * 0.002f;
                        if ((double)dust.velocity.X < -0.25)
                            dust.velocity.X = -0.25f;

                        if ((double)dust.velocity.X > 0.25)
                            dust.velocity.X = 0.25f;


                        if (dust.type == 35)
                        {
                            float num96 = dust.scale * 0.3f + 0.4f;
                            if (num96 > 1f)
                                num96 = 1f;

                        }
                    }

                    if (dust.type == 68)
                    {
                        float num97 = dust.scale * 0.3f;
                        if (num97 > 1f)
                            num97 = 1f;

                    }

                    if (dust.type == 70)
                    {
                        float num98 = dust.scale * 0.3f;
                        if (num98 > 1f)
                            num98 = 1f;

                    }

                    if (dust.type == 41)
                    {
                        dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.01f;
                        dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.01f;
                        if ((double)dust.velocity.X > 0.75)
                            dust.velocity.X = 0.75f;

                        if ((double)dust.velocity.X < -0.75)
                            dust.velocity.X = -0.75f;

                        if ((double)dust.velocity.Y > 0.75)
                            dust.velocity.Y = 0.75f;

                        if ((double)dust.velocity.Y < -0.75)
                            dust.velocity.Y = -0.75f;

                        dust.scale += 0.007f;
                        float num99 = dust.scale * 0.7f;
                        if (num99 > 1f)
                            num99 = 1f;

                    }
                    else if (dust.type == 44)
                    {
                        dust.velocity.X += (float)Main.rand.Next(-10, 11) * 0.003f;
                        dust.velocity.Y += (float)Main.rand.Next(-10, 11) * 0.003f;
                        if ((double)dust.velocity.X > 0.35)
                            dust.velocity.X = 0.35f;

                        if ((double)dust.velocity.X < -0.35)
                            dust.velocity.X = -0.35f;

                        if ((double)dust.velocity.Y > 0.35)
                            dust.velocity.Y = 0.35f;

                        if ((double)dust.velocity.Y < -0.35)
                            dust.velocity.Y = -0.35f;

                        dust.scale += 0.0085f;
                        float num100 = dust.scale * 0.7f;
                        if (num100 > 1f)
                            num100 = 1f;

                    }
                    else if (dust.type != 304 && (modDust == null || !modDust.MidUpdate(dust)))
                    {
                        dust.velocity.X *= 0.99f;
                    }

                    if (dust.type != 79 && dust.type != 268 && dust.type != 304)
                        dust.rotation += dust.velocity.X * 0.5f;

                    if (dust.fadeIn > 0f && dust.fadeIn < 100f)
                    {
                        if (dust.type == 235)
                        {
                            dust.scale += 0.007f;
                            int num101 = (int)dust.fadeIn - 1;
                            if (num101 >= 0 && num101 <= 255)
                            {
                                Vector2 value3 = dust.position - Main.player[num101].Center;
                                float num102 = value3.Length();
                                num102 = 100f - num102;
                                if (num102 > 0f)
                                    dust.scale -= num102 * 0.0015f;

                                value3.Normalize();
                                float num103 = (1f - dust.scale) * 20f;
                                value3 *= 0f - num103;
                                dust.velocity = (dust.velocity * 4f + value3) / 5f;
                            }
                        }
                        else if (dust.type == 46)
                        {
                            dust.scale += 0.1f;
                        }
                        else if (dust.type == 213 || dust.type == 260)
                        {
                            dust.scale += 0.1f;
                        }
                        else
                        {
                            dust.scale += 0.03f;
                        }

                        if (dust.scale > dust.fadeIn)
                            dust.fadeIn = 0f;
                    }
                    else if (dust.type != 304)
                    {
                        if (dust.type == 213 || dust.type == 260)
                            dust.scale -= 0.2f;
                        else
                            dust.scale -= 0.01f;
                    }

                    if (dust.type >= 130 && dust.type <= 134)
                    {
                        float num104 = dust.scale;
                        if (num104 > 1f)
                            num104 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }
                        else if (dust.type == 131)
                        {
                            dust.velocity *= 0.98f;
                            dust.velocity.Y -= 0.1f;
                            dust.scale += 0.0025f;
                        }
                        else
                        {
                            dust.velocity *= 0.95f;
                            dust.scale -= 0.0025f;
                        }
                    }
                    else if (dust.type == 278)
                    {
                        float num105 = dust.scale;
                        if (num105 > 1f)
                            num105 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }
                        else
                        {
                            dust.velocity *= 0.95f;
                            dust.scale -= 0.0025f;
                        }
                    }
                    else if (dust.type >= 219 && dust.type <= 223)
                    {
                        float num106 = dust.scale;
                        if (num106 > 1f)
                            num106 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }

                        dust.velocity *= new Vector2(0.97f, 0.99f);
                        dust.scale -= 0.0025f;
                    }
                    else if (dust.type == 226)
                    {
                        float num107 = dust.scale;
                        if (num107 > 1f)
                            num107 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }

                        dust.velocity *= new Vector2(0.97f, 0.99f);
                        dust.scale -= 0.01f;
                    }
                    else if (dust.type == 272)
                    {
                        float num108 = dust.scale;
                        if (num108 > 1f)
                            num108 = 1f;

                        if (dust.noGravity)
                        {
                            dust.velocity *= 0.93f;
                            if (dust.fadeIn == 0f)
                                dust.scale += 0.0025f;
                        }

                        dust.velocity *= new Vector2(0.97f, 0.99f);

                        dust.scale -= 0.01f;
                    }
                    else if (dust.type != 304 && dust.noGravity)
                    {
                        dust.velocity *= 0.92f;
                        if (dust.fadeIn == 0f)
                            dust.scale -= 0.04f;
                    }

                    if (dust.position.Y > Main.screenPosition.Y + (float)Main.screenHeight)
                        dust.active = false;

                    if (dust.scale < 0.1f)
                        dust.active = false;
                }
                else
                {
                    dust.active = false;
                }
            }

            simulatedDust.RemoveAll(d => !d.active);
        }

        public void DrawDusts(SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)screenPosition.X - 1000, (int)screenPosition.Y - 1050, Main.screenWidth + 2000, Main.screenHeight + 2100);
            Microsoft.Xna.Framework.Rectangle rectangle2 = rectangle;
            int dustCount = simulatedDust.Count;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = simulatedDust[i];
                if (!dust.active)
                    continue;

                if ((dust.type >= 130 && dust.type <= 134) || (dust.type >= 219 && dust.type <= 223) || dust.type == 226 || dust.type == 278)
                    rectangle = rectangle2;

                if (new Microsoft.Xna.Framework.Rectangle((int)dust.position.X, (int)dust.position.Y, 4, 4).Intersects(rectangle))
                {
                    float scale = dust.GetVisualScale();

                    if (dust.type >= 130 && dust.type <= 134)
                    {
                        float num = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y);
                        num *= 0.3f;
                        num *= 10f;
                        if (num > 10f)
                            num = 10f;

                        for (int j = 0; (float)j < num; j++)
                        {
                            Vector2 velocity = dust.velocity;
                            Vector2 value = dust.position - velocity * j;
                            float scale2 = dust.scale * (1f - (float)j / 10f);
                            Microsoft.Xna.Framework.Color color = Color.White;
                            color = dust.GetAlpha(color);
                            spriteBatch.Draw(TextureAssets.Dust.Value, value - screenPosition, dust.frame, color, dust.rotation, new Vector2(4f, 4f), scale2, SpriteEffects.None, 0f);
                        }
                    }
                    else if (dust.type == 278)
                    {
                        float num2 = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y);
                        num2 *= 0.3f;
                        num2 *= 10f;
                        if (num2 > 10f)
                            num2 = 10f;

                        Vector2 origin = new Vector2(4f, 4f);
                        for (int k = 0; (float)k < num2; k++)
                        {
                            Vector2 velocity2 = dust.velocity;
                            Vector2 value2 = dust.position - velocity2 * k;
                            float scale3 = dust.scale * (1f - (float)k / 10f);
                            Microsoft.Xna.Framework.Color color2 = Color.White;
                            color2 = dust.GetAlpha(color2);
                            spriteBatch.Draw(TextureAssets.Dust.Value, value2 - screenPosition, dust.frame, color2, dust.rotation, origin, scale3, SpriteEffects.None, 0f);
                        }
                    }
                    else if (dust.type >= 219 && dust.type <= 223 && dust.fadeIn == 0f)
                    {
                        float num3 = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y);
                        num3 *= 0.3f;
                        num3 *= 10f;
                        if (num3 > 10f)
                            num3 = 10f;

                        for (int l = 0; (float)l < num3; l++)
                        {
                            Vector2 velocity3 = dust.velocity;
                            Vector2 value3 = dust.position - velocity3 * l;
                            float scale4 = dust.scale * (1f - (float)l / 10f);
                            Microsoft.Xna.Framework.Color color3 = Color.White;
                            color3 = dust.GetAlpha(color3);
                            spriteBatch.Draw(TextureAssets.Dust.Value, value3 - screenPosition, dust.frame, color3, dust.rotation, new Vector2(4f, 4f), scale4, SpriteEffects.None, 0f);
                        }
                    }
                    else if (dust.type == 264 && dust.fadeIn == 0f)
                    {
                        float num4 = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y);
                        num4 *= 10f;
                        if (num4 > 10f)
                            num4 = 10f;

                        for (int m = 0; (float)m < num4; m++)
                        {
                            Vector2 velocity4 = dust.velocity;
                            Vector2 value4 = dust.position - velocity4 * m;
                            float scale5 = dust.scale * (1f - (float)m / 10f);
                            Microsoft.Xna.Framework.Color color4 = Color.White;
                            color4 = dust.GetAlpha(color4) * 0.3f;
                            spriteBatch.Draw(TextureAssets.Dust.Value, value4 - screenPosition, dust.frame, color4, dust.rotation, new Vector2(5f), scale5, SpriteEffects.None, 0f);
                            color4 = dust.GetColor(color4);
                            spriteBatch.Draw(TextureAssets.Dust.Value, value4 - screenPosition, dust.frame, color4, dust.rotation, new Vector2(5f), scale5, SpriteEffects.None, 0f);
                        }
                    }
                    else if ((dust.type == 226 || dust.type == 272) && dust.fadeIn == 0f)
                    {
                        float num5 = Math.Abs(dust.velocity.X) + Math.Abs(dust.velocity.Y);
                        num5 *= 0.3f;
                        num5 *= 10f;
                        if (num5 > 10f)
                            num5 = 10f;

                        for (int n = 0; (float)n < num5; n++)
                        {
                            Vector2 velocity5 = dust.velocity;
                            Vector2 value5 = dust.position - velocity5 * n;
                            float scale6 = dust.scale * (1f - (float)n / 10f);
                            Microsoft.Xna.Framework.Color color5 = Color.White;
                            color5 = dust.GetAlpha(color5);
                            spriteBatch.Draw(TextureAssets.Dust.Value, value5 - screenPosition, dust.frame, color5, dust.rotation, new Vector2(4f, 4f), scale6, SpriteEffects.None, 0f);
                        }
                    }

                    Microsoft.Xna.Framework.Color newColor = Color.White;
                    if (dust.type == 6 || dust.type == 15 || (dust.type >= 59 && dust.type <= 64))
                        newColor = Microsoft.Xna.Framework.Color.White;

                    newColor = dust.GetAlpha(newColor);
                    if (dust.type == 213)
                        scale = 1f;

                    ModDust modDust = DustLoader.GetDust(dust.type);

                    spriteBatch.Draw(TextureAssets.Dust.Value, dust.position - screenPosition, dust.frame, newColor, dust.GetVisualRotation(), new Vector2(4f, 4f), scale, SpriteEffects.None, 0f);
                    if (dust.color.PackedValue != 0)
                    {
                        Microsoft.Xna.Framework.Color color6 = dust.GetColor(newColor);
                        if (color6.PackedValue != 0)
                            spriteBatch.Draw(TextureAssets.Dust.Value, dust.position - screenPosition, dust.frame, color6, dust.GetVisualRotation(), new Vector2(4f, 4f), scale, SpriteEffects.None, 0f);
                    }

                    if (newColor == Microsoft.Xna.Framework.Color.Black)
                        dust.active = false;
                }
                else
                {
                    dust.active = false;
                }
            }
        }

        public Dust NewDust(Vector2 Position, int Type, Vector2? Velocity = null, int Alpha = 0, Color newColor = default(Color), float Scale = 1f)
        {
            if (Main.gameMenu || WorldGen.gen || Main.netMode == 2)
                return new Dust();

            if (Main.rand == null)
                Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);

                Dust dust = new Dust();
                dust.fadeIn = 0f;
                dust.active = true;
                dust.type = Type;
                dust.noGravity = false;
                dust.color = newColor;
                dust.alpha = Alpha;
                dust.frame.X = 10 * Type;
                dust.frame.Y = 10 * Main.rand.Next(3);
                dust.shader = null;
                dust.customData = null;
                dust.noLightEmittence = false;
                int num4 = Type;
                while (num4 >= 100)
                {
                    num4 -= 100;
                    dust.frame.X -= 1000;
                    dust.frame.Y += 30;
                }

                dust.frame.Width = 8;
                dust.frame.Height = 8;
                dust.rotation = 0f;
                dust.scale = 1f + (float)Main.rand.Next(-20, 21) * 0.01f;
                dust.scale *= Scale;
                dust.noLight = false;
                dust.firstFrame = true;
                if (dust.type == 228 || dust.type == 279 || dust.type == 269 || dust.type == 135 || dust.type == 6 || dust.type == 242 || dust.type == 75 || dust.type == 169 || dust.type == 29 || (dust.type >= 59 && dust.type <= 65) || dust.type == 158 || dust.type == 293 || dust.type == 294 || dust.type == 295 || dust.type == 296 || dust.type == 297 || dust.type == 298 || dust.type == 302)
                {
                    dust.scale *= 0.7f;
                }

                if (dust.type == 127 || dust.type == 187)
                {
                    dust.scale *= 0.7f;
                }

                if (dust.type == 33 || dust.type == 52 || dust.type == 266 || dust.type == 98 || dust.type == 99 || dust.type == 100 || dust.type == 101 || dust.type == 102 || dust.type == 103 || dust.type == 104 || dust.type == 105)
                {
                    dust.alpha = 170;
                }

                if (dust.type == 41)

                if (dust.type == 80)
                    dust.alpha = 50;
            

            dust.position = Position;
            if (Velocity.HasValue)
                dust.velocity = Velocity.Value;
            else
                dust.velocity = Vector2.Zero;

            simulatedDust.Add(dust);

            return dust;
        }
        */
    }
}
