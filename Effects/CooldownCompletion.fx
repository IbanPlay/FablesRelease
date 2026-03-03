sampler uImage0 : register(s0);

float completion;

//Draw variables
float2 resolution;
float opacity;

//Colors
float3 outlineColor;
float3 highlightColor;
float3 gradientTopColor;
float3 gradientBottomColor;


float4 MainPS(float2 coords : TEXCOORD0) : COLOR
{
    //Pixelate
    float2 pixelCoords = coords;
    pixelCoords = floor(coords * resolution) / resolution;
    
    //get the vector from the center of the sprite
    float2 between = pixelCoords - float2(0.5, 0.5);
    
    //Get how far along the circle we are (1 at the top, and then in a counter clockwise motion to reach 0 at the same point
    float anglePercent = (atan2(between.x, between.y) + 3.1415926) / 6.28318531;

    //if we're in the incomplete part of the pie graph, just don't draw anything
    float crop = anglePercent >= completion;

    float4 color = tex2D(uImage0, coords);
    
    //Outline uses the green channel
    float outlineMask = color.g > 0;
    
    //Highlight uses the red channel
    float highlightMask = color.r > 0;
    
    //Main gradient uses the blue channel
    float3 gradientColor = lerp(gradientTopColor, gradientBottomColor, color.b);
    float3 returnTint = outlineMask * outlineColor + highlightMask * highlightColor + (1 - outlineMask - highlightMask) * gradientColor;
    
    return float4(returnTint, 1) * opacity * color.a * crop;
}


technique Technique1
{
    pass CooldownCompletionPass
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}