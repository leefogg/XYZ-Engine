#version 430

in vec3 fragPos;
in vec2 uv;
in mat3 TBNMatrix;
flat in uint DrawID;
uniform sampler2D diffuseTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;
uniform sampler2D illumTex;

layout (location = 0) out vec4 diffuse;
layout (location = 1) out vec3 position;
layout (location = 2) out vec3 normal;
layout (location = 3) out vec4 specular;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
	mat4 InverseView;
	mat4 InverseProjection;
};

vec3 getCamPos() {
	return InverseView[3].xyz;
}

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

	diffuse = diff;

#if (USE_NORMAL_MAP == 1)
	vec3 norm = UnpackNormalmapYW(texture(normalTex, textureCoord), mat.NormalStrength);
#else
	vec3 norm = vec3(0, 0, 1.0);
#endif
	normal = normalize(TBNMatrix * norm);

	vec3 vNormalWsDdx = dFdx( normal );
	vec3 vNormalWsDdy = dFdy( normal );
	float flGeometricRoughnessFactor = pow(clamp(max(dot(vNormalWsDdx.xyz, vNormalWsDdx.xyz), dot(vNormalWsDdy.xyz, vNormalWsDdy.xyz)), 0.0, 1.0), 0.05);

	normal = (normal + 1) / 2;
	
#if (USE_SPECULAR_MAP == 1)
	specular = texture(specularTex, textureCoord);
	specular.a *= flGeometricRoughnessFactor;
#else
	specular = vec4(0.0);
	specular.a = flGeometricRoughnessFactor;
#endif

#if (USE_ILLUM_MAP == 1)
	vec3 illum = texture(illumTex, textureCoord).rgb * mat.IlluminationColor;
    diffuse.rgb += illum;
#endif

	position = fragPos - getCamPos();
}