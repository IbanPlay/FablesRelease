float Resolution; //Pixel resolution

float time;
float2 pingCenter; //The origin of the ping wave.
float pingRadius; //The max radius of the ping.
float pingWaveThickness; //The thickness of the wave before it entirely fades out.
float pingProgress; //How far along the ping progressed
float pingTravelTime; //Percent of the ping's duration during which the ping expands to reach its full radius.
float pingFadePoint; //Percent of the ping's duration at which to start fading away.
float edgeBlendStrength; //How starkly should the ping's edge fade off.
float edgeBlendOutLenght; //Small lenght at the edge of the wave to blend away smoothly.
float tileEdgeBlendStrenght; //How hard the border of a tile should get blended away.

float4 baseTintColor; //Color of the tile's overlay
float3 tileEdgeColor; //Color of the tile's edge effects
float4 scanlineColor; //Color of the scanline effects
float4 waveColor; //Color of the ping wave

float4 ScanLines[10];
int ScanLinesCount;
int verticalScanLinesIndex;

//Per tile stuff
float2 tilePosition; //The position of the top left of the tile
bool4 cardinalConnections; //Up, Left, Right, Down connections.


texture sampleTexture;
sampler2D Texture1Sampler = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float MOD(float a, float n)
{
    return a - floor(a / n) * n;
}

float inverselerp(float x, float start, float end)
{
    return clamp((x - start) / (end - start), 0, 1);
}

//Checks if a corner should be drawn or excluded, based on how many edges the pixel is near to.
float cornerDrawCheck(float2 position)
{
    float oneEight = 1 / Resolution;
    float sevenEights = 7 / Resolution;
    
    //top
    float edgeChecks = position.y < oneEight && !cardinalConnections.x;
    //bottom
    edgeChecks += position.y >= sevenEights && !cardinalConnections.w;
    //left
    edgeChecks += position.x < oneEight && !cardinalConnections.y;
    //right
    edgeChecks += position.x >= sevenEights && !cardinalConnections.z;
    
    //if on 2 edges at once, youre on a corner
    return edgeChecks < 2;
}

//Gets the gradient from the tile outline effects
float getOpacityFromEdge(float2 position)
{
    //float surroundMask = length(cardinalConnections) < 4;
    
    float oneFourth = 1 / Resolution;
    float twoFourths = 3 / Resolution;
    float threeFourths = 4 / Resolution;
    float one = 7 / Resolution;
    
    float baseOpacity = 0;
        
    //up
    baseOpacity += pow(inverselerp(position.y, twoFourths, 0), tileEdgeBlendStrenght) * (!cardinalConnections.x);
    
    //down
    baseOpacity += pow(inverselerp(position.y, threeFourths, one), tileEdgeBlendStrenght) * (!cardinalConnections.w);
    
    //left
    baseOpacity += pow(inverselerp(position.x, twoFourths, 0), tileEdgeBlendStrenght) * (!cardinalConnections.y);
    
    //right
    baseOpacity += pow(inverselerp(position.x, threeFourths, one), tileEdgeBlendStrenght) * (!cardinalConnections.z);
        
    return baseOpacity;
}

//Gets the opacity of the scanline on the specific pixel
float getOpacityFromScanLine(float2 position)
{
    float opacity = 0;
    float4 scanline;
    
    //x = offset from origin, y = period, z = speed, w = opacity.
    for (int i = 0; i < ScanLinesCount; i++)
    {
        scanline = ScanLines[i];
        
        float pixelPos;
        if (i >= verticalScanLinesIndex)
            pixelPos = (position.x + tilePosition.x / 16) % scanline.y;
        else
            pixelPos = (position.y + tilePosition.y / 16) % scanline.y;
        
        //Custom mod in case scanline.z is negative, aka the scanline moves in the opposite direction.
        float scanlinePos = MOD((scanline.x + time * scanline.z), scanline.y);
        
        scanlinePos -= scanlinePos % (1 / Resolution);
        
        pixelPos -= pixelPos % (1 / Resolution);
        opacity += scanline.w * (pixelPos == scanlinePos);
    }
    
    
    return opacity * (scanlineColor.a - baseTintColor.a);
}

float4 main(float2 uv : TEXCOORD) : COLOR
{
    //Pixelate
    uv.x -= uv.x % (1 / Resolution);
    uv.y -= uv.y % (1 / Resolution);
    
    //Mask out the corners so its got a nice looking 1px rounding
    float cornerMask = cornerDrawCheck(uv);
        
    float distanceFromPingOrigin = length(pingCenter - (tilePosition + uv * 16));
    float waveExpansionPercent = pingProgress / pingTravelTime;
    float currentPingRadius = pingRadius * waveExpansionPercent;
    float realWaveExpansionPercent = min(waveExpansionPercent, 1);
    float realPingRadius = pingRadius * realWaveExpansionPercent;
    
    
    //Crop the ping wave into a circle
    float distanceMask = distanceFromPingOrigin <= realPingRadius;
    
    float waveEdgeDistanceFromCenter = max(currentPingRadius - pingWaveThickness, 0);
    float edgeBlendFactor = pow(min(max((distanceFromPingOrigin - (waveEdgeDistanceFromCenter)), 0) / (pingWaveThickness - edgeBlendOutLenght), 1), edgeBlendStrength);
    
    float3 ColorTotal = lerp(baseTintColor.rgb, waveColor.rgb, edgeBlendFactor);
    float OpacityTotal = lerp(baseTintColor.a, waveColor.a, edgeBlendFactor);
        
    //Do the outlines
    float tileEdgeBlend = getOpacityFromEdge(uv);
    OpacityTotal = lerp(OpacityTotal, 1, tileEdgeBlend);
    ColorTotal = lerp(ColorTotal, tileEdgeColor, tileEdgeBlend);
    
    float scanlineOpacity = getOpacityFromScanLine(uv);
    OpacityTotal += scanlineOpacity;
    ColorTotal = lerp(ColorTotal, scanlineColor.rgb, scanlineOpacity);
    
    
    //Fade away the border
    OpacityTotal *= 1 - inverselerp(realPingRadius, realPingRadius + edgeBlendOutLenght, distanceFromPingOrigin);
    
    //General fade out at the end.
    OpacityTotal *= 1 - max(pingProgress - pingFadePoint, 0) / (1 - pingFadePoint);
    
    ColorTotal = ColorTotal * OpacityTotal;
    return float4(ColorTotal, OpacityTotal) * cornerMask * distanceMask;
}

technique Technique1
{
    pass WulfrumTilePingPass
    {
        PixelShader = compile ps_3_0 main();
    }
}