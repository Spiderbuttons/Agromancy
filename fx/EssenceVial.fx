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
float Waviness = 0.0f;
float TopOfVial = 0.5f; // In texture coordinate space.
float BottomOfVial = 1.0f - 0.125f; // Also texture coordinate space.

bool Flipped = false;

float4 PrismaticColour = float4(1.0f, 1.0f, 1.0f, 1.0f);
float4 GlassShineColour = float4(219.0f / 255.0f, 211.0f / 255.0f, 206.0f / 255.0f, 182.0f / 255.0f);

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
    float4 sample = tex2D(TexSampler, input.TextureCoordinates);
    bool sampleIsMagenta = sample.r * 255 >= 255 && sample.g <= 0.0f && sample.b * 255 >= 255;
    bool sampleIsYellow = sample.r * 255 >= 255 && sample.g * 255 >= 255 && sample.b <= 0.0f;
    if (sampleIsMagenta || sampleIsYellow)
    {
        // I control the texture, so I know that these pixels are my "greenscreen" pixels.
        float heightOfVial = BottomOfVial - TopOfVial;
        float distanceToBottom = (Flipped ? TopOfVial : BottomOfVial) - input.TextureCoordinates.y;
        float progressToBottom = distanceToBottom / heightOfVial;
        
        float2 noiseCoords = float2(input.TextureCoordinates.x * 4.0f * 0.5f + Time * 0.1f, FillPercentage * 0.5f + Time * 0.1f);
        float4 noiseColour = tex2D(NoiseSampler, noiseCoords);
        float totalColour = noiseColour.r + noiseColour.g + noiseColour.b / 3.0f;
        progressToBottom += totalColour * Waviness;
        

        if (progressToBottom < FillPercentage)
        {
            float4 finalColour = sampleIsYellow ? PrismaticColour * GlassShineColour : PrismaticColour;
            finalColour.a = 0.85f;
            return finalColour;
        }
        return sampleIsYellow ? float4(GlassShineColour.r, GlassShineColour.g, GlassShineColour.b, 182.0f / 255.0f) : float4(0, 0, 0, 0);
    }
    
    return tex2D(TexSampler, input.TextureCoordinates) * input.Color;
}

technique EssenceVial
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    
    }
};