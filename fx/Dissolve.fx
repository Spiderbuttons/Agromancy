#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D TextureImage;
Texture2D PerlinNoise;

float ClipThreshold = 0;
bool UseNoiseColour = false;

sampler2D TexSampler = sampler_state
{
    Texture =  < TextureImage >;

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
    // Pretty simple dissolve but with ~rainbow~ colours because we're reusing our perlin noise from elsewhere.
    // Remember to wrap the texture coordinate if the texture UV is bigger than our PerlinNoise
    float4 noiseColour = tex2D(NoiseSampler, input.TextureCoordinates * 128); // Scale the texture coordinates to increase the frequency of the noise
    float totalNoise = noiseColour.r + noiseColour.g + noiseColour.b;
    float noiseValue = totalNoise / 3.0f; // Average the RGB values
    float saturatedNoiseValue = saturate(noiseValue); // Ensure the noise value is between 0 and 1
    
    if (saturatedNoiseValue < ClipThreshold)
    {
        discard;
    }
    
    return UseNoiseColour ? noiseColour : tex2D(TexSampler, input.TextureCoordinates);
}

technique GaussianBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    
    }
};