#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SourceTex;
Texture2D PerlinNoise;

float Time = 0.0f;
float FillPercentage = 0.0f;

float2 Resolution = float2(64.0f, 64.0f);
float Waviness = 0.0f;

sampler2D TexSampler = sampler_state
{
    Texture =  < SourceTex >;

};

sampler2D NoiseSampler = sampler_state
{
    Texture =  < PerlinNoise >;

};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float fillHeight = (1.0 - FillPercentage) * Resolution.y;
    float2 pixelPos = input.TextureCoordinates * Resolution;
    if (pixelPos.y > fillHeight)
    {
        discard;
    }
    
    return tex2D(TexSampler, input.TextureCoordinates) * input.Color;
}

technique FilledCircle
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    
    }
};