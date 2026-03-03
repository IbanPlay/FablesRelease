texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = POINT;
    minfilter = POINT;
    mipfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};
float2 textureResolution;

texture displacementMap;
sampler2D displacementMapSampler = sampler_state
{
    texture = <displacementMap>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};
float4 displaceFactors;
float time;
float2 worldPos;
float displaceMapStrenght;

float4 baseColor;
float4 outlineColor;


matrix<float, 2, 2> bayerMatrix =
{
    0, 2, 3, 1 
};

matrix<float, 4, 4> bayerBigMatrix =
{
    0.0, 8.0, 2.0, 10.0,
    12.0, 4.0, 14.0, 6.0,
    3.0, 11.0, 1.0, 9.0,
    15.0, 7.0, 13.0, 5.0
};

/*
matrix<float, 8, 8> bayerBiggestMatrix =
{
    0.0 , 48.0 , 12.0 , 60.0 , 3.0 , 51.0 , 15.0 , 63.0 ,
  32.0 , 16.0 , 44.0 , 28.0 , 35.0 , 19.0 , 47.0 , 31.0 ,
    8.0 , 56.0 , 4.0 , 52.0 , 11.0 , 59.0 , 7.0 , 55.0 ,
  40.0 , 24.0 , 36.0 , 20.0 , 43.0 , 27.0 , 39.0 , 23.0 ,
    2.0 , 50.0 , 14.0 , 62.0 , 1.0 , 49.0 , 13.0 , 61.0 ,
  34.0 , 18.0 , 46.0 , 30.0 , 33.0 , 17.0 , 45.0 , 29.0 ,
  10.0 , 58.0 , 6.0 , 54.0 , 9.0 , 57.0 , 5.0 , 53.0 ,
  42.0 , 26.0 , 38.0 , 22.0 , 41.0 , 25.0 , 37.0 , 21.0 
};
*/


float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}

float ditherPattern(float2 coords, float opacity, float steps)
{
    //Dither is done in 2x2
    float2 resScaled = coords * textureResolution;
    
    int x = int(resScaled.x + worldPos.x) % 4;
    int y = int(resScaled.y + worldPos.y) % 4;
    float threshold = bayerBigMatrix[y][x] / 16;
    
    //int x = int(resScaled.x + worldPos.x) % 8;
    //int y = int(resScaled.y + worldPos.y) % 8;
    //float threshold = bayerBiggestMatrix[y][x] / 64;
    
    
    opacity += threshold / steps;
    return floor(opacity * steps) / steps;
}

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = floor(coords * textureResolution) / textureResolution;
    float2 distortedcoords = coords + (tex2D(displacementMapSampler, worldPos / textureResolution + coords * 2.3 + float2(0, -time * 0.2)).xy - float2(0.5, 0.5)) * displaceMapStrenght;
    
    float4 mist = tex2D(samplerTex, distortedcoords).rgba;
    mist.x = pow(mist.x, 2);
    
    mist.x = mist.x * 0.1 + ditherPattern(coords, mist.x, 3) * 0.8;

    float outlineMask = (1 - mist.g) * mist.x;
    
    float4 retColor = baseColor;
    retColor.xyzw *= mist.x;
    
    retColor += outlineMask * outlineColor;
    return retColor * mist.a * sampleColor;
}

technique Technique1
{
    pass BlizzardSmokePass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}