float noiseScale; //Zoom on the noise
float2 offset; //Offset on the noise

float resolution; //Resolution for the effect if pixelated

float edgeFadeDistance; //Percentage of the circle radius where it fades
float edgeFadePower; //Power of the fade

float shapeFadeTreshold; //Percentage of the circle radius where it fades
float shapeFadePower; //Power of the fade

float fresnelDistance;
float fresnelStrenght;
float fresnelOpacity;

float4 blastColor;
float treshold;

texture sampleTexture;
sampler2D Texture1Sampler = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };


float invlerp(float from, float to, float value)
{
    return clamp((value - from) / (to - from), 0.0, 1.0);
}


float4 main(float2 uv : TEXCOORD) : COLOR
{
    //Pixelate?
    uv.x -= uv.x % (1 / resolution);
    uv.y -= uv.y % (1 / resolution);
    
    
    //Crop in a circle
    float distanceFromCenter = length(uv - float2(0.5, 0.5)) * 2;
    float circleMask = distanceFromCenter <= 1;
    
    //"Blow up" the noise map so it looks circular.
    //float blownUpUVX = pow((abs(uv.x - 0.5)) * 2, blowUpPower);
    //float blownUpUVY = pow((abs(uv.y - 0.5)) * 2, blowUpPower);
    //float2 blownUpUV = float2(-blownUpUVY * blowUpSize * 0.5 + uv.x * (1 + blownUpUVY * blowUpSize), -blownUpUVX * blowUpSize * 0.5 + uv.y * (1 + blownUpUVX * blowUpSize));
    
    //Rescale
    uv *= noiseScale;
    
    //Get the noise color
    float4 noiseColor = tex2D(Texture1Sampler, uv + offset);
    
    //Cut off the noise if below the treshold
    circleMask *= noiseColor.r >= treshold;
    
    float opacity = 1;
    //Fade the edges of the NOISE
    opacity *= pow(invlerp(treshold, treshold + shapeFadeTreshold, noiseColor.r), shapeFadePower);
    
    //Fade the edges of the CIRCLE
    opacity *= pow(invlerp(1, 1 - edgeFadeDistance, distanceFromCenter), edgeFadePower);
    
    //Color intensity boost near the edges
    float colorBoost = 1;
    colorBoost += fresnelOpacity * pow(invlerp(fresnelDistance, 1, distanceFromCenter), fresnelStrenght);
     
    return float4(blastColor.rgb * opacity * colorBoost, blastColor.a * opacity) * circleMask;
}

technique Technique1
{
    pass GyroMortarBlastPass
    {
        PixelShader = compile ps_2_0 main();
    }
}