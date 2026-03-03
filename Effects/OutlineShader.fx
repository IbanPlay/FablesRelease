sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;

// This is a shader. You are on your own with shaders. Compile shaders in an XNB project.

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
	float2 pixelCoords = 2 / uImageSize0;
	float localY = frac((coords.y * uImageSize0.y) / uSourceRect.w);

	float time = sin(uTime * 4) * 0.5f + 0.5f;
	float3 colour = uColor * time + uSecondaryColor * (1 - time);
	
	float4 color = tex2D(uImage0, coords);
	float2 outlineCoords = coords;
	float luminosity = (color.r + color.g + color.b) / 3;

	outlineCoords.x += pixelCoords.x;
	float4 outline = tex2D(uImage0, outlineCoords);
	outlineCoords.x -= pixelCoords.x * 2;
	outline *= tex2D(uImage0, outlineCoords);
	outlineCoords.x = coords.x;
	outlineCoords.y += pixelCoords.y;
	outline *= tex2D(uImage0, outlineCoords);
	outlineCoords.y -= pixelCoords.y * 2;
	outline *= tex2D(uImage0, outlineCoords);

	outline.a = 1 - outline.a;
	
	//float scroll = sin(localY + uTime) * -0.5 + 0.5;

	color.rgb = color.rgb * (1 - outline.a) + (color.rgb * 1 + colour) * outline.a;

	return color * color.a * 0.6 * sampleColor;
}

technique Technique1
{
    pass OutlineShader
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}