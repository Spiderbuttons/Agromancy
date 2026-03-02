#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D CircleTexture;
Texture2D PerlinNoise;

float Time;
float yCoordOffset;

float FillPercentage;

float2 Resolution;
float CircleRadius;
float Waviness;

sampler2D TexSampler = sampler_state
{
    Texture =  < CircleTexture >;

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
    // Don't need to bother drawing pixels outside the circle.
    float2 centerPos = Resolution * 0.5f;
    float2 pixelPos = input.TextureCoordinates * Resolution;
    float distFromCenter = distance(pixelPos, centerPos);
    if (distFromCenter > CircleRadius)
    {
        discard;
    }
    
    // Then we use our FillPercentage to determine how high up a pixel should be drawn.
    float fillHeight = CircleRadius * 2.0f * FillPercentage;
    
    // We can use Perlin Noise here to add some wiggly effect to the top.
    float2 noiseCoords = float2(input.TextureCoordinates.x * 0.5f + Time * 0.1f, FillPercentage * 0.5f + Time * 0.1f + yCoordOffset);
    float4 noiseColour = tex2D(NoiseSampler, noiseCoords);
    float totalColour = noiseColour.r + noiseColour.g + noiseColour.b / 3.0f;
    fillHeight += totalColour * Waviness;
    
    if (pixelPos.y < centerPos.y - CircleRadius + fillHeight)
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