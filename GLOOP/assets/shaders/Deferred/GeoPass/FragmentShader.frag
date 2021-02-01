#version 330

in vec3 fragPos;
in vec2 uv;
in mat3 TBNMatrix;

uniform vec2 textureOffset = vec2(0.0);
uniform vec2 textureRepeat = vec2(1.0);
uniform bool isWorldSpaceUVs = false;
uniform float normalStrength = 1;

layout (location = 0) out vec3 diffuse;
layout (location = 1) out vec4 position;
layout (location = 2) out vec3 normal;
layout (location = 3) out vec4 specular;
layout (location = 4) out vec3 illum;

uniform sampler2D diffuseTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;
uniform sampler2D illumTex;
uniform uint diffuseSlice, normalSlice, specularSlice, illumSlice;
uniform vec3 illuminationColor;
uniform vec3 albedoColourTint;

vec3 UnpackNormalmapYW(in vec4 avNormalValue)
{
	vec3 vNormal = avNormalValue.wyx * 2 - 1;
	vNormal.z = sqrt(1 - min(dot(vNormal.xy, vNormal.xy), 1)) / normalStrength;
	return vNormal;	
}


void main()
{
	vec2 textureCoord;
	if (isWorldSpaceUVs) {
		textureCoord = fragPos.xz;
	} else {
		textureCoord = uv;
	}
	textureCoord += textureOffset;
	textureCoord *= textureRepeat;
	
	vec4 diff = texture(diffuseTex, textureCoord);
	if (diff.a < 0.1)
		discard;
	diff.rgb *= albedoColourTint;

	vec3 norm = UnpackNormalmapYW(texture(normalTex, textureCoord));
	
	specular = texture(specularTex, textureCoord);
	position = vec4(fragPos, 0.0);
	illum = texture(illumTex, textureCoord).rgb * illuminationColor;
    diffuse = diff.rgb + illum;
	normal = normalize(TBNMatrix * norm);
}