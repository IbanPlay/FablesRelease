sampler backgroundTex : register(s0);
texture backgroundRampTexture;
sampler2D backgroundRampTex = sampler_state { texture = <backgroundRampTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture backgroundPaletteTexture;
sampler2D backgroundPaletteTex = sampler_state { texture = <backgroundPaletteTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

texture insigniaFillTexture;
sampler2D insigniaFillTex = sampler_state { texture = <insigniaFillTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture insigniaOutlineTexture;
sampler2D insigniaOutlineTex = sampler_state { texture = <insigniaOutlineTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture insigniaBloomTexture;
sampler2D insigniaBloomTex = sampler_state { texture = <insigniaBloomTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture insigniaShadowTexture;
sampler2D insigniaShadowTex = sampler_state { texture = <insigniaShadowTexture>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

texture squigglePaletteTexture;
sampler2D squigglePaletteTex = sampler_state { texture = <squigglePaletteTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp; };

float time;
float intensify;
float4 bloomTint;

float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float4 alphaBlend(float4 bottomLayer, float4 topLayer)
{
    float4 returnColor = bottomLayer * (1 - topLayer.a) + topLayer * topLayer.a;
    returnColor.a = bottomLayer.a;
    return returnColor;
}

float distTo(float2 uv, float2 distanceTo, float maxDist)
{
    return length(uv - distanceTo) + (1 - maxDist);
}

float4 getBackground(float2 uv)
{
    float4 background = tex2D(backgroundTex, uv);
     
    //Get a gradient on the background 
    float2 bgRamp = tex2D(backgroundRampTex, uv).ra;
    float paletteUV = bgRamp.x;
    float paletteOscillation = sin(time * 2 + bgRamp.x * 5 + uv.y * 6);
    paletteUV += paletteOscillation * 0.05;
    float4 backgroundGradient = tex2D(backgroundPaletteTex, float2(paletteOscillation * 0.4, 1 - paletteUV));
    background = alphaBlend(background, backgroundGradient * bgRamp.y);
    
    return background;
}

float squiggleBand(float dist, float start, float thickness)
{
    float smoothBandValue = invlerp(start + 0.01, start, dist) * invlerp(start - thickness - 0.01, start - thickness, dist);
    float smoothBandValueBloom = invlerp(start + 0.21, start, dist) * invlerp(start - thickness - 0.21, start - thickness, dist);
    
    return step(1, smoothBandValue) + (smoothBandValue % 1) * 0.3 + pow(smoothBandValueBloom, 5) * 0.2;
}

float3 getSquiggleMask(float2 uv)
{
    float2 pixUv = floor(uv * float2(39, 40)) / float2(39, 40);
    
    float warble = sin(time + pixUv.y * 13 + pixUv.x) * 0.05;
    pixUv.x += warble * (sin(time * 1.4 + 0.4) * 0.5 + 0.5);
    
    float pulsate = sin(time * 1.6) * 0.03;
    
    //squiggleMask += step(squiggles - pulsate, 0.87) * step(0.85, squiggles - pulsate);
    float3 squigMask = float3(0, 0, 0);
    
    float distanceBotRight = min(distTo(pixUv, float2(0.98, 1), 0.34), distTo(pixUv, float2(1, 0.67), 0.16)) + pulsate;
    squigMask.x = squiggleBand(distanceBotRight, 1, 0.04);
    squigMask.x += squiggleBand(distanceBotRight, 0.91, 0.03) * 0.5;
    
    float distanceBotLeft = min(distTo(pixUv, float2(-0.2, 0.96), 0.48), distTo(pixUv, float2(-0.1, 0.5), 0.2)) + pulsate;
    squigMask.y = squiggleBand(distanceBotLeft, 1, 0.03);
    squigMask.y += squiggleBand(distanceBotLeft, 0.91, 0.1) * 0.1;
    squigMask.y += squiggleBand(distanceBotLeft, 0.91, 0.03) * 0.6;
    
    float distanceTopLeft = distTo(pixUv, float2(-0.05, 0.05), 0.5) + pulsate;
    squigMask.z = squiggleBand(distanceTopLeft, 1, 0.035);
    squigMask.z += squiggleBand(distanceTopLeft, 0.91, 0.2) * 0.4;
    
    return squigMask;
}

float3 getSquiggles(float2 uv)
{
    float3 squiggleMask = getSquiggleMask(uv);
    float3 squigglePaletteOscillation = sin(time * 3 + float3(3, 0.3, 2) * 5 + uv.y * 6);
    float3 squigglePaletteUV = float3(0, 0.25, 0.75) + (squigglePaletteOscillation * 0.125 + 0.125);
    
    float3 squiggleResult = tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.x * 0.4, squigglePaletteUV.x)) * squiggleMask.r * 0.8;
    squiggleResult += tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.y * 0.4, squigglePaletteUV.y)) * squiggleMask.g * 0.4;
    squiggleResult += tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.z * 0.4, squigglePaletteUV.z)) * squiggleMask.b * 0.1;
    return squiggleResult;
}

float2 rotateUv(float2 uv, float rotateBy)
{
    float cosAngle = cos(rotateBy);
    float sinAngle = sin(rotateBy);
    
    return float2(uv.x * cosAngle + uv.y * -sinAngle, uv.x * sinAngle + uv.y * cosAngle);
}

float4 Main(float2 uv : TEXCOORD0) : COLOR0
{
    //Get the background with a shifting holographic color palette 
    float4 finalRet = getBackground(uv);
    
    //Add little squiggles
    finalRet.rgb += getSquiggles(uv);
    
    //Add the insignia on top
    float bloomOpacity = 0.5 + 0.5 * sin(time * 1.12 + 4);
    float4 bloom = tex2D(insigniaBloomTex, uv) * bloomTint;
    finalRet.rgb += bloom.rgb * bloomOpacity;
    
    float insigniaRotation = sin(time) * 0.04;
    float insigniaScale = 1 + (0.5 + 0.5 * sin(time * 2 + 3.5)) * 0.04;
    float insigniaShadowScale = 1 + (0.5 + 0.5 * sin(time * 2 + 3.5)) * 0.07;
    
    //float insigniaShadowOpacity = (1 - (0.5 - 0.5 * sin(time * 2 + 3.5)));
    //insigniaShadowOpacity = 0.6 + 0.4 * invlerp(0, 0.4, insigniaShadowOpacity);
    float insigniaShadowOpacity = 0.6 + 0.4 * invlerp(1.07, 1, insigniaShadowScale);
    
    float2 insigniaUv = uv;
    float2 insigniaShadowUv = uv;
    insigniaUv -= float2(0.5, 0.5);
    
    insigniaUv = rotateUv(insigniaUv, insigniaRotation);
    insigniaShadowUv = insigniaUv;
    insigniaUv *= insigniaScale;
    insigniaShadowUv *= insigniaShadowScale;
    insigniaShadowUv.y -= 0.09 + (1 - (insigniaShadowScale - 1) / 0.07) * 0.05;
    
    insigniaUv += float2(0.5, 0.5);
    insigniaShadowUv += float2(0.5, 0.5);
    float4 fill = tex2D(insigniaFillTex, insigniaUv);
    float4 outline = tex2D(insigniaOutlineTex, insigniaUv);
    float4 shadow = tex2D(insigniaShadowTex, insigniaShadowUv);
    
    finalRet = alphaBlend(finalRet, shadow * 0.4 * insigniaShadowOpacity);
    finalRet = alphaBlend(finalRet, fill);
    finalRet.rgb += outline.rgb;

    return finalRet * intensify;
}

technique Technique1
{
    pass AnimatedModIconPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}