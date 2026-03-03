matrix uWorldViewProjection;

float Scroll;
float Repeats;
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


texture NoiseOverlayTexture;
sampler2D NoiseOverlayTextureSampler = sampler_state
{
    texture = <NoiseOverlayTexture>;
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

float3x3 vectorRotateZ(float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    
    return float3x3
    (
        c, -s, 0,
        s, c, 0,
        0, 0, 1
    );
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = input.Color;
    float4 finalColor = float4(0, 0, 0, 0);
    
    float2 modUV = coords;
    modUV.x *= Repeats;
    
    // Distort UV using noise
    float noise = max(pow(tex2D(NoiseTextureSampler, float2(modUV.x + Scroll * 0.1, Scroll * 0.05)).x, 1.3) * color.a - (1 - color.a) * 0.3, 0.000001);  
    modUV.y = modUV.y * 2 - 1;
    modUV.y /= noise;
    
    float radius = abs(modUV.y);    
    modUV.y = modUV.y * asin(radius) / radius;
    modUV.y = modUV.y / (PI * 0.5f) + 0.5f;
    
    if (radius <= 1)
    {
        if (Outline)
        {
            // Skip the other shit and only use outline color
            finalColor = lerp(DeepColor * color.rgba, GlowColor, min(GlowIntensity, 1));
        }
        else
        {
            float2 noiseUVs = modUV / float2(0.5f, 8) + float2(Scroll * 0.05, Scroll * 0.1);
            noise = pow(tex2D(NoiseTextureSampler, noiseUVs).x, 1.8f);
            float3 normal = tex2D(NormalTextureSampler, noiseUVs);
            
            // Convert normals from normal map color to a normal vector
            float3 normalVector = float3(normal.x * 2 - 1, normal.y * 2 - 1, normal.z * 2 - 1);
                    
            // Flatten normals below a certain sphere 'depth'
            normalVector = lerp(float3(0, 0, 1), normalVector, ceil(noise - 0.3f));
            
            // Rotate normals according to the sphere
            normalVector = mul(vectorRotateX((modUV.y - 0.5) * PI), normalVector);
            normalVector = mul(vectorRotateZ(-coords.x * PI * 2), normalVector);

            // Lighting and fresnel
            float3 lightVector = normalize(float3(0.5, 0.7, 0.3));
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
            finalColor *= color * (1 - normalVector.z * 0.5);
            
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
    pass InkExplosionRingPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}