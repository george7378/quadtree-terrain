////////////////////
//Global variables//
////////////////////
float4x4 World;
float4x4 WorldViewProjection;

float LightPower;
float SpecularPower;
float WaterRefractiveIndexScale;
float FogStart;
float FogEnd;

float2 TexCoordDeltaFirst;
float2 TexCoordDeltaSecond;

float3 CameraPosition;
float3 LightDirection;
float3 FogColour;
float3 WaterIntrinsicColour;

Texture WaterNormalMap;
Texture WaterRefractionMap;
Texture WaterReflectionMap;


//////////////////
//Sampler states//
//////////////////
sampler WaterNormalMapSampler = sampler_state
{
texture = <WaterNormalMap>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler WaterRefractionMapSampler = sampler_state
{
	texture = <WaterRefractionMap>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = Mirror;
	AddressV = Mirror;
};

sampler WaterReflectionMapSampler = sampler_state
{
	texture = <WaterReflectionMap>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = Mirror;
	AddressV = Mirror;
};


//////////////////
//I/O structures//
//////////////////
struct VertexShaderInput
{
	float4 Position			: POSITION;
	float2 TexCoords		: TEXCOORD0;
	float3 Normal			: NORMAL;
	float3 Tangent			: TANGENT;
	float3 Binormal			: BINORMAL;
};

struct VertexShaderOutput
{
	float4 Position				: POSITION;
	float2 TexCoords			: TEXCOORD0;
	float4 SamplingPosition     : TEXCOORD1;
	float4 PositionWorld		: TEXCOORD2;
	float3 Normal				: TEXCOORD3;
	float3 Tangent				: TEXCOORD4;
	float3 Binormal				: TEXCOORD5;
};


///////////
//Shaders//
///////////
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	output.Position = mul(input.Position, WorldViewProjection);
	output.TexCoords = input.TexCoords;
	output.SamplingPosition = output.Position;
	output.PositionWorld = mul(input.Position, World);
	output.Normal = normalize(mul(input.Normal, (float3x3)World));
	output.Tangent = normalize(mul(input.Tangent, (float3x3)World));
	output.Binormal = normalize(mul(input.Binormal, (float3x3)World));

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 normalMapColour1 = 2*tex2D(WaterNormalMapSampler, input.TexCoords*20 + TexCoordDeltaFirst).xyz - 1;
    float3 normalMapColour2 = 2*tex2D(WaterNormalMapSampler, input.TexCoords*20 + TexCoordDeltaSecond).xyz - 1;
    float3 normalMapCombinedColour = (normalMapColour1 + normalMapColour2)/2;
	float3 normal = normalize((normalMapCombinedColour.x*input.Tangent) + (normalMapCombinedColour.y*input.Binormal) + (normalMapCombinedColour.z*input.Normal));

    float2 projectedTexCoords;
	projectedTexCoords.x = input.SamplingPosition.x/input.SamplingPosition.w/2 + 0.5f;
	projectedTexCoords.y = -input.SamplingPosition.y/input.SamplingPosition.w/2 + 0.5f;

    float4 refractionColour = tex2D(WaterRefractionMapSampler, projectedTexCoords + normal.xz*WaterRefractiveIndexScale);
    float4 reflectionColour = tex2D(WaterReflectionMapSampler, projectedTexCoords + normal.xz*WaterRefractiveIndexScale/2);

	refractionColour.rgb = lerp(refractionColour.rgb, WaterIntrinsicColour, refractionColour.a);

	float3 eyeVector = CameraPosition - input.PositionWorld.xyz;
	float3 eyeVectorNormalised = normalize(eyeVector);
    float3 reflectionVector = normalize(reflect(LightDirection, normal));
	
	float specularLightingFactor = pow(saturate(dot(reflectionVector, eyeVectorNormalised)), SpecularPower)*LightPower;
    
    float fresnelTerm = 1/pow(1 + dot(eyeVectorNormalised, float3(0, 1, 0)), 5);
    float4 waterColour = lerp(refractionColour, reflectionColour, fresnelTerm) + float4(float3(1, 1, 1)*specularLightingFactor, 1);

	float shorelineDepth = tex2D(WaterRefractionMapSampler, projectedTexCoords).a;
	waterColour = float4(lerp(waterColour.rgb, FogColour, saturate((length(eyeVector) - FogStart)/(FogEnd - FogStart))), shorelineDepth*5);

	return waterColour;
}

technique WaterTechnique
{
    pass Pass1
    {
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
