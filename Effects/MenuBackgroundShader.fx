texture sampleTexture;
sampler2D samplerTex = sampler_state { texture = <sampleTexture>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp;};

texture bloomTex;
sampler2D backgroundBloom = sampler_state { texture = <bloomTex>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = clamp; AddressV = clamp;};
texture grimeTex;
sampler2D grimeNoise = sampler_state { texture = <grimeTex>;  magfilter = POINT; minfilter = POINT; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap;};
texture displaceTex;
sampler2D displaceNoise = sampler_state { texture = <displaceTex>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap;};
texture dustTex;
sampler2D dustNoise = sampler_state { texture = <dustTex>;  magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = wrap; AddressV = wrap;};
texture dustMaskTex;
sampler2D dustMaskNoise = sampler_state { texture = <dustMaskTex>;  magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap;};
texture squigglePaletteTexture;
sampler2D squigglePaletteTex = sampler_state { texture = <squigglePaletteTexture>;  magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp;};

float4 screenTint;
float4 tintDistance;

float time;
float2 pixelRes;
float3 underglowColor;// = float3(0.1, 0.3, 0.2);
float3 dustColor;// = float3(0.9, 0.9, 0.3);
float grimeThreshold;

float3 screen(float3 base, float3 scrn)
{
    return float3(1, 1, 1) - 2 * (float3(1, 1, 1) - base) * (float3(1, 1, 1) - scrn);
}

float3 GlowDodge(float3 bottom, float3 top)
{
    return clamp(bottom / (1 - top), 0, 1);
}

float GetDustValue(float2 uv)
{
    float dust = tex2D(dustNoise, uv + float2(sin(uv.y * 5 + time * 0.02) * 0.01, - time * 0.1 * 0.2)).r;
    dust = max(0, dust - 0.99) / 0.01;
    dust += max(0, tex2D(dustNoise, uv + float2(sin(uv.y * 5 + time * 0.02) * 0.01, - time * 0.2 * 0.2)).b - 0.99) / 0.01;
    
    //Get a mask for the falling dust
    float perlinMask = tex2D(dustMaskNoise, uv * 1 + float2(0, -time * 0.2)).x * tex2D(dustMaskNoise, uv * 2.2 - float2(0, 0.1 * time)).x;
    perlinMask = pow(perlinMask, 1.5);
    
    return dust * perlinMask;
}

float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}


float distTo(float2 uv, float2 distanceTo, float maxDist)
{
    return length(uv - distanceTo) + (1 - maxDist);
}

float squiggleBand(float dist, float start, float thickness)
{
    float smoothBandValue = invlerp(start + 0.01, start, dist) * invlerp(start - thickness - 0.01, start - thickness, dist);
    float smoothBandValueBloom = invlerp(start + 0.21, start, dist) * invlerp(start - thickness - 0.21, start - thickness, dist);
    
    return step(1, smoothBandValue) + (smoothBandValue % 1) * 0.3 + pow(smoothBandValueBloom, 5) * 0.2;
}

float4 getSquiggleMask(float2 uv)
{
    float4 pulsate = sin(time * float4(1.6, 1.5, 1.7, 1.1) + float4(0, 0.5, 3, 2.1)) * 0.03;
    
    float warble = sin(time + uv.y * 13 + uv.x * 9) * 0.02;
    uv.x += warble * (sin(time * 1.4 + 0.4 + uv.x) * 0.5 + 0.5);
    uv.y += warble * (sin(time * 1.73 + 2.4) * 0.5 + 0.5);
    //Wabrle uvs more
    uv.x = lerp(uv.x, uv.y, uv.x);
    
    float distanceTopRight = distTo(uv * float2(0.6, -0.2), float2(1.1, -0.1), 0.8) + pulsate.w;
    
    //Warble uvs more
    uv.y = lerp(uv.y, uv.x, uv.x);
    
    float4 squigMask = float4(0, 0, 0, 0);
    
    float distanceBotRight = min(distTo(uv, float2(0.98 + sin(time) * 0.04, 0.92), 0.24 + sin(time) * 0.06), distTo(uv, float2(1.1, 0.7), 0.26)) + pulsate.x;
    squigMask.x = squiggleBand(distanceBotRight, 1, 0.01);
    squigMask.x += squiggleBand(distanceBotRight, 0.98, 0.008) * 0.5;
    
    
    float distanceBotLeft = min(distTo(uv, float2(0.078, 0.86), 0.28), distTo(uv, float2(0.34, 1), 0.17)) + pulsate.y;
    squigMask.y = squiggleBand(distanceBotLeft, 1, 0.01);
    squigMask.y += squiggleBand(distanceBotLeft, 0.98, 0.02) * 0.5;
    
    squigMask.y += squiggleBand(distanceBotLeft, 0.87, 0.01) * 0.9;
    
    float distanceTopLeft = distTo(uv, float2(-0.05, 0.05), 0.5) + pulsate.z;
    squigMask.z = squiggleBand(distanceTopLeft, 1, 0.01);
    squigMask.z += squiggleBand(distanceTopLeft, 0.91, 0.1) * 0.4;
    
    squigMask.w = squiggleBand(distanceTopRight, 1, 0.004);
    squigMask.w += squiggleBand(distanceTopRight, 0.97, 0.003) * 0.4;
    
    return squigMask;
}

float3 getSquiggles(float2 uv)
{
    float4 squiggleMask = getSquiggleMask(uv);
    float4 squigglePaletteOscillation = sin(time * 3 + float4(3, 0.3, 2, 5.1) * 5 + uv.y * 6);
    float4 squigglePaletteUV = float4(0, 0.25, 0.5, 0.75) + (squigglePaletteOscillation * 0.125 + 0.125);
    
    float3 squiggleResult = tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.x * 0.4, squigglePaletteUV.x)) * squiggleMask.r * 0.8;
    squiggleResult += tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.y * 0.4, squigglePaletteUV.y)) * squiggleMask.g * 0.4;
    squiggleResult += tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.z * 0.4, squigglePaletteUV.z)) * squiggleMask.b * 0.14;
    squiggleResult += tex2D(squigglePaletteTex, float2(squigglePaletteOscillation.w * 0.4, squigglePaletteUV.w)) * squiggleMask.a * 0.24;
    return squiggleResult;
}


float4 Main(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR
{
    float2 pixelUv = floor(uv * pixelRes) / pixelRes;
    
    float2 displace = tex2D(displaceNoise, pixelUv * 0.12 + float2(0, time * 0.01)).xy;
    displace = tex2D(displaceNoise, pixelUv * 0.1 + displace * 0.08 + float2(0, time * 0.01)).xy;
    
    float2 displacedUv = pixelUv + (displace - 0.5) * 0.05;
    float4 color = tex2D(samplerTex, displacedUv);
    
    //Use glow dodge with a brighter version
    color.rgb = GlowDodge(color.rgb, tex2D(backgroundBloom, uv).rgb * 0.4);
    
    //Get a mask for the dark parts of the image
    float darknessMask = max(0, 1 - length(color.rgb) / 3 - (1 - grimeThreshold)) / grimeThreshold;
    
    //Add scrolling downwards grime noise
    float scrollSpeed = 0.1;
    float3 grime = tex2D(grimeNoise, uv * 1 + float2(0, -time * scrollSpeed)).rgb + tex2D(grimeNoise, uv * 1 + float2(0.5, -time * scrollSpeed * 0.5)).rgb ;
    
    //The grime only appears in the darker parts of the image
    color.rgb = lerp(color.rgb, color.rgb + grime, 0.1 * darknessMask);
    
    //Add dust that falls down from the top
    float dust = GetDustValue(uv);
    color.rgb += dust * dustColor;
    
    //Teal undercurrent
    float tealGlow = max(uv.y - 0.7, 0) / 0.45;
    color.rgb -= color.rgb * tealGlow;
    color.rgb += underglowColor * tealGlow;
    
    color.rgb += getSquiggles(pixelUv);
    
    //Tint depending on menu
    float distToCenter = length((uv - float2(0.5, 0.5)) * float2(1, 0.7));
    float screenMask = invlerp(0.4, 0.9, distToCenter);
    color.rgb = lerp(color.rgb, screen(color.rgb, screenTint.rgb), screenTint.a * distToCenter);
    
    return color * sampleColor;
}

technique Technique1
{
    pass MenuBackgroundShaderPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}