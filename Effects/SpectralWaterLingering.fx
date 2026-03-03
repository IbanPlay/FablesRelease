float2 Resolution;
float time;

float3 mainColor;
float3 secondaryColor;
float4 overlayColor;
float4 finalColorMultiplier;
float3 maxColor;

float upwardsFadeStrenght;

texture voronoiTex;
sampler2D VoronoiSampler = sampler_state { texture = <voronoiTex>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

texture noiseTex;
sampler2D NoiseMap = sampler_state { texture = <noiseTex>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };


//This is unused
float4 main(float2 uv : TEXCOORD) : COLOR
{
    //Pixelate
    uv.x -= uv.x % (1 / (Resolution.x * 2));
    uv.y -= uv.y % (1 / (Resolution.y * 2));
    
    //Get a scrolling voronoi base
    float3 color = tex2D(VoronoiSampler, float2(uv.x, uv.y - time) * 0.6).xyz;
    
    //Add scrolling noise
    color += mainColor * tex2D(NoiseMap, float2(uv.x, uv.y - time * 1.1)).xyz;
    //color += 0.3 * secondaryColor * tex2D(NoiseMap, vec2(uv.x - iTime * 0.4, uv.y - iTime * 1.1)).xyz;
    
    //Cut out holes in the voronoi, with more stretched holes near the top
    float uvScale = 1.0 + 0.7 * pow(1 - uv.y, 2.0);
    color *= tex2D(NoiseMap, 0.8 * float2(uv.x * uvScale, uv.y - time * 1.1)).xyz;
    
    //Fade off towards the top
    color *= pow(uv.y, upwardsFadeStrenght);
    
    //Make the bottom of the fire be brighter and more saturated
    color *= (1.0 + 7.0 * pow(uv.y, 6.0));
    
    
    float distanceFromCenter = abs(0.5 - uv.x);
    
    //Add an overlay thats just bright
    color += overlayColor.a * overlayColor.xyz * (1.0 - pow((distanceFromCenter / 0.4), 1.2)) * pow(uv.y, 1.5);
    
    //Multiply the color by the final result
    color.xyz *= finalColorMultiplier.xyz;
    
    //Fade off the edges
    if (distanceFromCenter > 0.3)
        color *= 1.0 - (distanceFromCenter - 0.3) / 0.2;
    
    color = min(color, maxColor);
    return float4(color, color.r + color.g + color.b / 3);
}

technique Technique1
{
    pass SpectralWaterLingeringPass
    {
        PixelShader = compile ps_3_0 main();
    }
}