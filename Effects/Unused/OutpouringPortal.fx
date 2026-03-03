texture sampleTexture;
sampler2D samplerTex = sampler_state { texture = <sampleTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = wrap; AddressV = wrap; };

float time;
float4 color;

float4 MainPS(float2 coords : TEXCOORD0) : COLOR
{
    // Normalized pixel coordinates (from 0 to 1)
    float angle = atan2(coords.x - 0.5f, coords.y - 0.5f) + 3.14;
    //Get the coordinates of the pixel based on itsp osirtion around the center
    float2 circUv = float2(angle / 6.28, 2.0 * length(coords - float2(0.5, 0.5)));
    
    float offset = tex2D(samplerTex, float2(circUv.x + 0.4 * sin(time) * sin(circUv.y * 3.12), ((time * 0.4f) + circUv.y) % 1.0f)).x;
    
    float2 newcoords = float2(circUv.x, lerp(circUv.y, 0.5f + (sign(circUv.y - 0.5f) * 0.25f), pow(offset / 2.0, 0.2)));

    float pulse = pow(1.0 - abs(newcoords.y - 0.5), 14.0);
    
    // Output to screen
    float3 col = color.xyz * pulse * color.a;
    return float4(col, color.a);
}


technique Technique1
{
    pass OutpouringPortalPass
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}