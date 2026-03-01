#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D BlurrableTex;

float2 Resolution;
float2 ClarityCenter;
float ClarityRadius;
bool InvertClarity = false;
float BlurMultiplier = 1.0f;

float Saturation = 1.0;

sampler2D Sampler = sampler_state
{
    Texture = <BlurrableTex>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 BlurHoriz(VertexShaderOutput input) : COLOR
{
    if (ClarityRadius > 0)
    {
        float2 toCenter = input.TextureCoordinates - ClarityCenter;
        float distance = length(toCenter * Resolution);
        if ((!InvertClarity && distance < ClarityRadius) || (InvertClarity && distance >= ClarityRadius))
        {
            float4 col = tex2D(Sampler, input.TextureCoordinates) * input.Color;
            float grayscale = dot(col.rgb, float3(0.3, 0.59, 0.11));
            float3 finalColor = lerp(grayscale, col.rgb, Saturation);
            return float4(finalColor, col.a);
        }
    }

	float4 colour = float4(0, 0, 0, 0);
    colour += tex2D(Sampler, input.TextureCoordinates + float2(-4.0f * BlurMultiplier / Resolution.x, 0)) * 0.05f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(-3.0f * BlurMultiplier / Resolution.x, 0)) * 0.09f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(-2.0f * BlurMultiplier / Resolution.x, 0)) * 0.12f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(-1.0f * BlurMultiplier / Resolution.x, 0)) * 0.15f;
    colour += tex2D(Sampler, input.TextureCoordinates) * 0.16f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(1.0f * BlurMultiplier / Resolution.x, 0)) * 0.15f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(2.0f * BlurMultiplier / Resolution.x, 0)) * 0.12f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(3.0f * BlurMultiplier / Resolution.x, 0)) * 0.09f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(4.0f * BlurMultiplier / Resolution.x, 0)) * 0.05f;
    
    return colour;
}

float4 BlurVert(VertexShaderOutput input) : COLOR
{
    if (ClarityRadius > 0)
    {
        float2 toCenter = input.TextureCoordinates - ClarityCenter;
        float distance = length(toCenter * Resolution);
        if ((!InvertClarity && distance < ClarityRadius) || (InvertClarity && distance >= ClarityRadius))
        {
            float4 col = tex2D(Sampler, input.TextureCoordinates) * input.Color;
            float grayscale = dot(col.rgb, float3(0.3, 0.59, 0.11));
            float3 finalColor = lerp(grayscale, col.rgb, Saturation);
            return float4(finalColor, col.a);
        }
    }
    
    float4 colour = float4(0, 0, 0, 0);
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, -4.0f * BlurMultiplier / Resolution.y)) * 0.05f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, -3.0f * BlurMultiplier / Resolution.y)) * 0.09f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, -2.0f * BlurMultiplier / Resolution.y)) * 0.12f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, -1.0f * BlurMultiplier / Resolution.y)) * 0.15f;
    colour += tex2D(Sampler, input.TextureCoordinates) * 0.16f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, 1.0f * BlurMultiplier / Resolution.y)) * 0.15f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, 2.0f * BlurMultiplier / Resolution.y)) * 0.12f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, 3.0f * BlurMultiplier / Resolution.y)) * 0.09f;
    colour += tex2D(Sampler, input.TextureCoordinates + float2(0, 4.0f * BlurMultiplier / Resolution.y)) * 0.05f;
    
    return colour;
}

technique GaussianBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL BlurHoriz();
    }
	pass P1
    {
        PixelShader = compile PS_SHADERMODEL BlurVert();
    }
};