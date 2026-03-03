texture sampleTexture;
sampler2D samplerTex = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
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

float remap(float value, float2 coords)
{
    value -= smoothstep(0.1, 0.9, length(coords - float2(0.5, 0.5)));
    
    float remappedValue = invlerp(0, 4, value);
    return pow(remappedValue, 0.5);
}

float ditherPattern(float2 coords, float opacity, float steps)
{
    //Dither is done in 2x2
    float2 resScaled = coords * textureResolution;
    
    //int x = int(resScaled.x + worldPos.x) % 8;
    //int y = int(resScaled.y + worldPos.y) % 8;
    //float threshold = bayerBiggestMatrix[y][x] / 64;
    
    int x = int(resScaled.x + worldPos.x) % 4;
    int y = int(resScaled.y + worldPos.y) % 4;
    float threshold = bayerBigMatrix[y][x] / 16;
    
    
    opacity += threshold / steps;
    return floor(opacity * steps) / steps;
}

float4 Recolor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    coords = floor(coords * textureResolution) / textureResolution;
    float steps = 2;
    float minValue = 0.3;
    
    float mist = remap(tex2D(samplerTex, coords).b, coords);
    mist = min(mist, 1);
    // mist *= 0.3 + 0.7 * step(minValue, mist);
    
    mist = mist * 0.4 + ditherPattern(coords, mist, steps) * 0.3;
    float4 retColor = baseColor;
    retColor *= mist;
    return retColor * sampleColor;
}

technique Technique1
{
    pass BlizzardMistPass
    {
        PixelShader = compile ps_3_0 Recolor();
    }
}