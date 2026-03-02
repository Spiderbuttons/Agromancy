#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D BlobTexture;
Texture2D PerlinNoise;

float Time;

float StatPercentage;

float2 Resolution;
float2 BlobCenter;
float BlobMaxRadius;
float BlobMinRadius;

bool UseNoiseColour = false;
bool FadeOut = true;

float Saturation;

sampler2D TexSampler = sampler_state
{
    Texture =  < BlobTexture >;

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
    // Don't need to bother drawing pixels outside the circle that holds the stats blob.
    float2 toCenter = input.TextureCoordinates - BlobCenter;
    float distance = length(toCenter * Resolution);
    if (distance > BlobMaxRadius)
    {
        return float4(0, 0, 0, 0);
    }
    
    // If we ARE in the stats blob, then first we need to figure out what direction from the center of the blob this pixel is.
    float2 direction = normalize(toCenter);
    // That way, we can use that as coordinates to sample our noise texture...
    float4 noise = tex2D(NoiseSampler, direction * 0.5 + Time * 0.01);
    float totalNoise = noise.r + noise.g + noise.b;
    // ...and use the noise value as a percentage from no colour to full colour to determine how much to multiply our radius limit by for this pixel.
    float newRadius = lerp(BlobMinRadius, BlobMaxRadius * StatPercentage, totalNoise / 3.0);
    // If the pixel is outside the new radius, we make it transparent.
    if (distance > newRadius)
    {
        return float4(0, 0, 0, 0);
    }
    
    
    // Otherwise we just draw the input colour.
    float4 retColour = input.Color * tex2D(TexSampler, input.TextureCoordinates);
    if (UseNoiseColour)
    {
        retColour.rgb = noise.rgb;
    }
    
    // Gradually get closer to transparent the closer we are to the center, using our new radius as the maximum distance for the fade out.
    // For some reason, I also have to scale the rgb values, otherwise it... doesn't really look right in game.
    if (FadeOut)
    {
        retColour.a *= 1.0f - distance / newRadius;
        retColour.rgb *= 1.0f - distance / newRadius;
    }
    retColour.rgb = lerp(dot(retColour.rgb, float3(0.3, 0.59, 0.11)), retColour.rgb, Saturation);
    return retColour;
}

technique GaussianBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    
    }
};