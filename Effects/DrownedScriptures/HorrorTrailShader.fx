matrix uWorldViewProjection;

float Scroll;
float2 Resolution;

float4 DeepColor;
float4 LitColorTone1;
float4 LitColorTone2;

float Tone2Factor;
float ShadingCurveExponent;
int ColorDepth;
float NoiseScale;
float FadePower;
float NoiseFadePower;

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = float4(0, 0, 0, 0);

    // Width of a pixel
    float pixWidth = 1 / Resolution.y;
    
    // The base width of the trail
    // Use noise to add some variation
    float baseWidth = (1 - pixWidth * 2) * pow(coords.x, 0.5f);
    float widthMult = pow(tex2D(NoiseTextureSampler, coords * 0.26 + float2(Scroll, 0)).x, 0.6);
    widthMult = max(widthMult, pow(coords.x, 2));   // Limit variation towards start of trail
    baseWidth *= widthMult;

    float innerEdge = (1 - baseWidth) / 2;
    float outerEdge = innerEdge + baseWidth;
    
    // Apply colors within width of trail
    if (coords.y >= innerEdge && coords.y <= outerEdge)
    {
        // Outline
        if (coords.y <= innerEdge + pixWidth || coords.y >= outerEdge - pixWidth)
            color = DeepColor;
        // Colorful interior
        else
        {
            // Get distance from starting point. Used for shader brightness
            float distance = sqrt(pow(coords.x - 1, 2) + pow(coords.y - 0.5f, 2));
            
            // Stretch UV for more normal noise pattern
            float lengthRatio = Resolution.y / Resolution.x;
            float2 modUV = coords;
            modUV.y = modUV.y * lengthRatio;
            modUV *= NoiseScale;
            
            float2 noiseL1Offset = float2(Scroll * 0.2f - 0.7f, 0);
            float2 noiseL2Offset = float2(Scroll - 0.3f, Scroll * 0.5f);
    
            float3 noise = (tex2D(NoiseTextureSampler, modUV * 0.5f + noiseL1Offset) + tex2D(NoiseTextureSampler, modUV + noiseL2Offset)) / 2;
    
            // Add ripple distortion and non-linear lighting curve
            noise.rgb *= 1 - 0.5 * ceil(sin(noise.r * PI * 10)) * ceil(noise.r - 0.2);
            noise = pow(noise, ShadingCurveExponent);
    
            // Final light value
            // Multiply noise by distance to fade in towards the tip
            float light = noise * min(pow(distance, NoiseFadePower), 1);
            
            // Set brightness further along the trail
            light = max(light, min(pow(distance, FadePower), 1));
        
            // Quantize the color
            light = round(light * ColorDepth) / ColorDepth;
    
            // Get actual color from light
            color = lerp(DeepColor, LitColorTone1, light);
            color = lerp(color, LitColorTone2, pow(light, 1.2) * Tone2Factor);
        }
        
        color.a = 1;
    }
    
    return color * input.Color;
}

technique Technique1
{
    pass HorrorTrailShaderPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
