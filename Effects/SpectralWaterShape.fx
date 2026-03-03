texture sampleTexture;
sampler2D SpriteTextureSampler = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float2 noiseSize;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 pos = input.TextureCoordinates - float2(0.5, 0.5);
    float angle = atan2(pos.y, pos.x);
    
    //Dust lifetime completion was encoded into the red channel
    float completion = input.Color.r;
    float distortionStrenght = 0.025 + completion * 0.03;
    float minimumTreshold = 0.4 + 0.6 * completion;
    float3 color = float3(1 - completion, completion, 0);

    //Dust index encoded in the green channel
    float dustIndex = input.Color.g;
    float2 noisePositionOrigin = float2(dustIndex * 0.1, dustIndex * -0.024);
    
    //Dust distortion noise offset encoded in the blue channel
    float distortionOffset = input.Color.b;
    
    //return (tex2D(SpriteTextureSampler, noisePositionOrigin + (pos + float2(0.5, 0.5)) * noiseSize)) * pow(clamp(1 - length(pos) / 0.5, 0, 1), 0.5);
    
    float distortion = sin((angle + distortionOffset * 1.2) * 4.6) * 2;
    distortion -= sin(1.57 + (angle - distortionOffset * 1.8) * 6.8) * 1.2;

    pos *= 1 + distortion * distortionStrenght * 2;

    float value = tex2D(SpriteTextureSampler, noisePositionOrigin + (pos + float2(0.5, 0.5)) * noiseSize).x;
    value *= pow(clamp(1 - length(pos) / 0.5, 0, 1), 0.5);
    
    float tresholdMask = value >= minimumTreshold;
    return float4(color, 1) * tresholdMask;
}

technique SpectralWaterShapePass
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};
