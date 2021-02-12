#version 430

in vec3 fragPos;
in vec2 uv;
in mat3 TBNMatrix;
flat in uint DrawID;
uniform sampler2D diffuseTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;
uniform sampler2D illumTex;

layout (location = 0) out vec3 diffuse;
layout (location = 1) out vec4 position;
layout (location = 2) out vec3 normal;
layout (location = 3) out vec4 specular;
layout (location = 4) out vec3 illum;

struct Material {
	//mat4 ModelMatrix;
	//uint DiffuseMapSlice;
	//uint NormalMapSlice;
	//uint SpecularMapSlice;
	//uint IlluminationMapSlice;
	vec3 IlluminationColor;
	vec3 AlbedoColourTint;
	vec2 TextureRepeat;
	vec2 TextureOffset;
	float NormalStrength;
	bool IsWorldSpaceUVs;
};

layout (shared, binding = 2) buffer MaterialsBuffer {
	Material[] Materials;
};

vec3 UnpackNormalmapYW(in vec4 avNormalValue, float normalStrength)
{
	vec3 vNormal = avNormalValue.wyx * 2 - 1;
	vNormal.z = sqrt(1 - min(dot(vNormal.xy, vNormal.xy), 1)) / normalStrength;
	return vNormal;	
}


void main()
{
	Material mat = Materials[DrawID];
	vec2 textureCoord;
	if (mat.IsWorldSpaceUVs) {
		textureCoord = fragPos.xz;
	} else {
		textureCoord = uv;
	}
	textureCoord += mat.TextureOffset;
	textureCoord *= mat.TextureRepeat;
	
	vec4 diff = texture(diffuseTex, textureCoord);
	if (diff.a < 0.1)
		discard;
	diff.rgb *= mat.AlbedoColourTint;

	vec3 norm = UnpackNormalmapYW(texture(normalTex, textureCoord), mat.NormalStrength);
	
	specular = texture(specularTex, textureCoord);
	position = vec4(fragPos, 0.0);
	illum = texture(illumTex, textureCoord).rgb * mat.IlluminationColor;
    diffuse = diff.rgb + illum;
	normal = normalize(TBNMatrix * norm);
}