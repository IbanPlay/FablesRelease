sampler2D uImage0 : register(s0);

float4 cloudColor1;
float4 cloudColor2;
float3 shadowsColorMultiply;
float3 sunGlowColor;
float2 textureSize;
float time;
float3 outlineColorMult;
float3 illuminatedOutlineColorMult;

texture noiseTex;
sampler2D noiseSampler = sampler_state { texture = <noiseTex>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture displacementMap;
sampler2D displacementMapSampler = sampler_state { texture = <displacementMap>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };
float4 displaceFactors;

float2 noiseBaseDisplacement;
float2 noiseScalar;
float2 screenRatio;
float2 displaceMapStrenght;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 baseNoiseUV = noiseBaseDisplacement + uv * noiseScalar;
    
    uv.x += (tex2D(displacementMapSampler, baseNoiseUV + time * displaceFactors.xy).x - 0.5) * displaceMapStrenght.x;
    uv.y += (tex2D(displacementMapSampler, baseNoiseUV + time * displaceFactors.zw).x - 0.5) * displaceMapStrenght.y * screenRatio;
    
    float4 color = tex2D(uImage0, uv);
    float noiseOpacity = tex2D(noiseSampler, baseNoiseUV + float2(time * 2.1, time * 1.332)).x * tex2D(noiseSampler, baseNoiseUV + float2(time * -5.1, time * 2.332)).x;
    float4 baseColor = lerp(cloudColor1, cloudColor2, noiseOpacity);
    //Blue channel for shadows
    baseColor.rgb *= lerp(float3(1, 1, 1), shadowsColorMultiply, color.b);
    //Red channel for lighting
    baseColor.rgb += sunGlowColor * color.r;
    
    //Heavily tint the outline near the sun glow
    //Done with math instead of a branching statement
    /*
    if (color.r > 0.5 && outlineColorMult.x != 1)
        baseColor.rgb *= illuminatedOutlineColorMult;
    else
        baseColor.rgb *= outlineColorMult;
    */
    baseColor.rgb *= lerp(outlineColorMult, illuminatedOutlineColorMult, step(0.5, color.r) * ceil(1 - outlineColorMult.x));
    
    //Zeroes out the color if green is 0
    //Done instead of a branching return statement at the start because apparently that's no bueno.
    baseColor *= ceil(color.g);
    return baseColor;
}

technique Technique1
{
    pass CloudMetaballPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}