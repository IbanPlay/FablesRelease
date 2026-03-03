float time;
float outlineThickness; //The thickness of the side outline
float outlineBlendStrength; //How hard the border of a tile should get blended away.
float centerFadeStrentgh;
float noiseFadeStrength;
float noiseOpacity;

float4 baseTintColor; //Color of the shield's overlay
float3 outlineColor; //Color of the shield's edge effects
float4 scanlineColor; //Color of the scanline effects

float4 ScanLines[10];
int ScanLinesCount;
int verticalScanLinesIndex;

float2 spriteResolution;


texture sampleTexture;
sampler2D Texture1Sampler = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

bool AND(bool4 value)
{
    return value.x && value.y && value.z && value.w;
}


float MOD(float a, float n)
{
    return a - floor(a / n) * n;
}

float inverselerp(float x, float start, float end)
{
    return clamp((x - start) / (end - start), 0, 1);
}

//Checks if a corner should be drawn or excluded, based on how many edges the pixel is near to.
bool cornerDrawCheck(float2 position)
{
    float firstPixelRowX = 1 / spriteResolution.x;
    float lastPixelRowX = 1 - firstPixelRowX;
    
    float firstPixelRowY = 1 / spriteResolution.y;
    float lastPixelRowY = 1 - firstPixelRowY;
    
    //bool4 edgeChecks = bool4(false, false, false, false);
    
    //Top
    float edgeChecks = position.y < firstPixelRowY;
    
    //bottom
    edgeChecks += position.y >= lastPixelRowY;
    
    //left
    edgeChecks += position.x < firstPixelRowX;
    
    //right
    edgeChecks += position.x >= lastPixelRowX;
    
    //if on 2 edges at once, youre on a corner
    return edgeChecks < 2;
}

//Gets the gradient from the tile outline effects
float getOpacityFromEdge(float2 position)
{
    //if we dont give em a 1px offset, the bottom & right edges are trimmed 1 px short fsr
    position.x += 1 / spriteResolution.x * (position.x >= 0.5);
    position.y += 1 / spriteResolution.y * (position.y >= 0.5);
    
    position.x = 0.5 - abs(0.5 - position.x);
    position.y = 0.5 - abs(0.5 - position.y);
    
    float2 adjustedOutlineThickness = (1 / spriteResolution) * outlineThickness;
    
    float baseOpacity = 0;
    baseOpacity += pow(inverselerp(position.y, adjustedOutlineThickness.y, 0), outlineBlendStrength);
    baseOpacity += pow(inverselerp(position.x, adjustedOutlineThickness.x, 0), outlineBlendStrength);
        
    return baseOpacity;
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
    //Pixelate
    uv.x -= uv.x % (1 / spriteResolution.x);
    uv.y -= uv.y % (1 / spriteResolution.y);
    
    //Mask out the pixels on the corners of te shield
    float cornerMask = cornerDrawCheck(uv);
    
    float3 ColorTotal = baseTintColor.rgb;
    float OpacityTotal = baseTintColor.a;
        
    //Do the outlines
    float tileEdgeBlend = getOpacityFromEdge(uv);
    OpacityTotal = lerp(OpacityTotal, 1, tileEdgeBlend);
    ColorTotal = lerp(ColorTotal, outlineColor, tileEdgeBlend);
    
    float distanceFromEdge = 1 - abs(uv.x - 0.5) * 2;
    
    float2 shiftingUVs = uv;
    shiftingUVs.x += time;
    float4 noiseColor = tex2D(Texture1Sampler, shiftingUVs);
    OpacityTotal += noiseColor.x * pow(distanceFromEdge, noiseFadeStrength) * noiseOpacity;
    
    OpacityTotal = min(1, OpacityTotal);
    OpacityTotal -= pow(distanceFromEdge, centerFadeStrentgh);
    OpacityTotal = max(0, OpacityTotal);
    
    ColorTotal = ColorTotal * OpacityTotal;
    return float4(ColorTotal, OpacityTotal) * cornerMask;
}

technique Technique1
{
    pass WulfrumRoverShieldPass
    {
        PixelShader = compile ps_3_0 main();
    }
}