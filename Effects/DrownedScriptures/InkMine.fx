matrix uWorldViewProjection;

float Scroll;
float2 Resolution;

float4 DeepColor;
float4 LitColorTone1;
float4 LitColorTone2;
float GlowIntensity;
float4 GlowColor;

float Tone2Factor;
float ShadingTranslucency;
float ShadingCurveExponent;
int ColorDepth;
bool Outline;

texture NoiseTexture;
sampler NoiseTextureSampler = sampler_state
{
    texture = <NoiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture NormalTexture;
sampler NormalTextureSampler = sampler_state
{
    texture = <NormalTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

#define PI 3.14159

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float3x3 vectorRotateX(float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    
    return float3x3
    (
        1, 0, 0,
        0, c, -s,
        0, s, c
    );
}

float3x3 vectorRotateY(float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    
    return float3x3
    (
        c, 0, s,
        0, 1, 0,
        -s, 0, c
    );
}

float2 warpUVs(float2 uv, inout float radius, float radiusMod = 1, float2 texScale = float2(1, 1))
{
    float2 modUV = (uv - float2(0.5, 0.5)) * 2;
    float r = sqrt(pow(modUV.x, 2) + pow(modUV.y, 2));
    r *= radiusMod;
    
    float d = asin(r) / r;
    float2 modUV2 = modUV * d;
    
    float2 finalUV = float2(
    (modUV2.x / (PI * texScale.x) + 0.5 + Scroll) % 2,
    modUV2.y / (PI * texScale.y) + 0.5
    );
    
    radius = r;
    
    return finalUV;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    float4 finalColor = float4(0, 0, 0, 0);
    
    //pixelation
    float2 modUV = floor(coords * Resolution) / Resolution;

    float2 noiseL1Offset = float2(0, 0.3f - Scroll * 0.2f);
    float2 noiseL2Offset = float2(Scroll * 0.5f, 0.3f - Scroll);
    
    // Map the noise onto a sphere to use for the radius distortion
    float radius = 1;
    float2 warpedUV = warpUVs(modUV, radius, 1, float2(4, 4));
    
    // Create noise map from current warped UV
    float4 noise = tex2D(NoiseTextureSampler, warpedUV * 2 + noiseL1Offset) * tex2D(NoiseTextureSampler, warpedUV * 2 + noiseL2Offset);
    
    // Use the previous sphere mapped noise to map the noise to a new sphere with radius distorted by the previous noise
    warpedUV = warpUVs(modUV, radius, 1 + (0.6 - pow(noise.r, 0.5) * 0.5), float2(4, 4));
        
    // Find colors if radius is less than 1
    if (radius <= 1)
    {
        if (Outline)
        {
            // Skip the other shit and only use outline color
            finalColor = lerp(DeepColor * color.rgba, GlowColor, min(GlowIntensity, 1));
        }
        else
        {
            // Create new noise map and normal map
            noise = tex2D(NoiseTextureSampler, warpedUV * 2 + noiseL1Offset) * tex2D(NoiseTextureSampler, warpedUV * 2 + noiseL2Offset);
            float3 normal = (tex2D(NormalTextureSampler, warpedUV * 2 + noiseL1Offset) + tex2D(NormalTextureSampler, warpedUV * 2 + noiseL2Offset)) / 2;
    
            // Convert normals from normal map color to a normal vector
            float3 normalVector = float3(normal.x * 2 - 1, normal.y * 2 - 1, normal.z * 2 - 1);
        
            // Flatten normals below a certain sphere 'depth'
            normalVector = lerp(float3(0, 0, 1), normalVector, ceil(noise.r - 0.1));
    
            // Rotate normals according to the sphere
            normalVector = mul(vectorRotateY(-(modUV.x - 0.5) * PI), normalVector);
            normalVector = mul(vectorRotateX((modUV.y - 0.5) * PI), normalVector);
    
            // Lighting and fresnel
            float3 lightVector = normalize(float3(0.5, 0.7, 0.7));
            float light = dot(normalVector, lightVector);
            float fresnel = saturate(1 - normalVector.z);
            float colorFac = max(light * 2 - 1, 0) * (1 - ShadingTranslucency) + fresnel * ShadingTranslucency;
    
            // Non linear lighting curve looks nicer
            colorFac = pow(colorFac, ShadingCurveExponent);
    
            // Extremely fake ambient occlusion
            colorFac *= 0.5 + min(noise.r / 0.5, 1) * 0.5;
    
            // Quantize the color
            colorFac = round(colorFac * ColorDepth) / ColorDepth;
    
            // Get actual color from the light brightness
            finalColor = lerp(DeepColor, LitColorTone1, colorFac);
            finalColor = lerp(finalColor, LitColorTone2, pow(colorFac, 1.2) * Tone2Factor);
            finalColor *= color;
    
            // Inner glow based on inverse fresnel
            if (GlowIntensity > 0)
            {
                float innerGlowFac = round(GlowIntensity * ColorDepth * normalVector.z) / ColorDepth;
                finalColor = lerp(finalColor, GlowColor, min(innerGlowFac, 1));
            }
    
            // Add a ripple effect to the bumps because it looks cool
            finalColor.rgb *= 1 - 0.4 * ceil(sin(noise.r * PI * 8)) * ceil(noise.r - 0.2);
        }

        finalColor.a = 1;
    }
    
    return finalColor;
}

technique Technique1
{
    pass InkMinePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}