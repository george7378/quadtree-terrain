////////////////////
//Global variables//
////////////////////
float4x4 World;
float4x4 WorldViewProjection;

bool EnableFog;

float LightPower;
float AmbientLightPower;
float TerrainTextureScale;
float FogStart;
float FogEnd;

float3 CameraPosition;
float3 LightDirection;
float3 FogColour;

float4 ClipPlane;

Texture TerrainTexture;
Texture TerrainSlopeTexture;
Texture TerrainDetailTexture;


//////////////////
//Sampler states//
//////////////////
sampler TerrainTextureSampler = sampler_state
{
	texture = <TerrainTexture>;
	magfilter = ANISOTROPIC;
	minfilter = ANISOTROPIC;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler TerrainSlopeTextureSampler = sampler_state
{
	texture = <TerrainSlopeTexture>;
	magfilter = ANISOTROPIC;
	minfilter = ANISOTROPIC;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler TerrainDetailTextureSampler = sampler_state
{
	texture = <TerrainDetailTexture>;
	magfilter = ANISOTROPIC;
	minfilter = ANISOTROPIC;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};


//////////////////
//I/O structures//
//////////////////
struct VertexShaderInput
{
	float4 Position	: POSITION;
	float3 Normal	: NORMAL;
	float3 Blend	: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position			: POSITION;
	float3 Normal     		: TEXCOORD0;
	float3 Blend			: TEXCOORD1;
	float4 PositionWorld	: TEXCOORD2;
};


/////////////
//Functions//
/////////////
float4 TriPlanarTextureSample(sampler textureSampler, float4 positionWorld, float3 blend)
{
	float4 xAxisColour = tex2D(textureSampler, positionWorld.yz*TerrainTextureScale);
	float4 yAxisColour = tex2D(textureSampler, positionWorld.xz*TerrainTextureScale);
	float4 zAxisColour = tex2D(textureSampler, positionWorld.xy*TerrainTextureScale);

	return xAxisColour*blend.x + yAxisColour*blend.y + zAxisColour*blend.z;
}


///////////
//Shaders//
///////////
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Normal = normalize(mul(input.Normal, (float3x3)World));
	output.Blend = input.Blend;
	output.PositionWorld = mul(input.Position, World);

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float clipDepth = dot(input.PositionWorld, ClipPlane.xyz) + ClipPlane.w;
	clip(clipDepth);

	float4 baseColour = TriPlanarTextureSample(TerrainTextureSampler, input.PositionWorld, input.Blend);

	float slope = 1 - input.Normal.y;
	if (slope > 0.02f)
	{
		float4 baseSlopeColour = TriPlanarTextureSample(TerrainSlopeTextureSampler, input.PositionWorld, input.Blend);
		baseColour = lerp(baseColour, baseSlopeColour, saturate((slope - 0.02f)/(0.05f - 0.02f)));
	}

	float viewDistance = length(CameraPosition - input.PositionWorld.xyz);
	if (viewDistance < 100)
	{
		float4 detailColour = TriPlanarTextureSample(TerrainDetailTextureSampler, input.PositionWorld, input.Blend);
		baseColour.rgb = lerp(baseColour.rgb*detailColour*2, baseColour.rgb, saturate(viewDistance/100));
	}

	float diffuseLightingFactor = saturate(dot(-normalize(LightDirection), input.Normal))*LightPower;
	float4 finalColour = baseColour*(AmbientLightPower + diffuseLightingFactor);

	finalColour = float4(lerp(finalColour.rgb, FogColour, saturate((viewDistance - FogStart)/(FogEnd - FogStart))*EnableFog), (clipDepth - 1)/20);

	return finalColour;
}

technique TerrainTechnique
{
	pass Pass1
	{
		//Fillmode = Wireframe;

		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
