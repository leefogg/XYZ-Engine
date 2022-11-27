#version 460
#define MaxBonesPerModel 1024

layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec3 Normal;
layout (location = 3) in vec3 Tangent;
#if (IS_SKINNED_MESH == 1)
	layout(location = 4) in vec4 BoneIds;
	layout(location = 5) in vec4 BoneWeights;
#endif

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
	mat4 InverseView;
	mat4 InverseProjection;
};

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

#if (IS_SKINNED_MESH == 1)	
	layout (std140, binding = 2) buffer BonePoses {
		mat4 BoneTransforms[];
	};
#endif

struct Model {
	mat4 ModelMatrix;
	Material ModelMaterial;
};

layout (std430, binding = 1) buffer ModelBuffer {
	Model Models[];
};

out vec3 fragPos;
out vec2 uv;
out mat3 TBNMatrix;
out Material material;

void main(void) {
	uv = UV;
	

	uint DrawID = gl_BaseInstance;
	material = Models[DrawID].ModelMaterial;
	mat4 ModelMatrix = Models[DrawID].ModelMatrix;

	
	vec4 worldspacePos = ModelMatrix * vec4(Position, 1.0);
	vec3 normal = Normal;
	vec3 tangent = Tangent;
	
	#if (IS_SKINNED_MESH == 1)
		vec4 totalLocalPos = vec4(0.0);
		vec4 totalNormal = vec4(0.0);
		vec4 totalTangent = vec4(0.0);
		for(int i=0; i<4; i++){
			uint startBoneIndex = DrawID * MaxBonesPerModel;
			uint boneOffset = uint(BoneIds[i]);
			mat4 jointTransform = BoneTransforms[startBoneIndex + boneOffset];
			float boneWeight = BoneWeights[i];
			
			vec4 posePosition = jointTransform * vec4(Position, 1.0);
			totalLocalPos += posePosition * boneWeight;
			
			vec4 worldNormal = jointTransform * vec4(Normal, 0.0);
			totalNormal += worldNormal * boneWeight;
			
			vec4 worldTangent = jointTransform * vec4(Tangent, 0.0);
			totalTangent += worldTangent * boneWeight;
		}
		
		worldspacePos = ModelMatrix * totalLocalPos;
		normal = totalNormal.xyz;
		tangent = totalTangent.xyz;
	#endif
	
	
	fragPos = worldspacePos.xyz;
	gl_Position = ViewProjectionMatrix * worldspacePos;
	
	mat3 normalMatrix = transpose(inverse(mat3(ModelMatrix)));
	vec3 T = normalize(normalMatrix * tangent);
	vec3 N = normalize(normalMatrix * normal);
	vec3 B = cross(N, T);
	T = normalize(T - dot(T, N) * N); // re-orthogonalize T with respect to N
	
	TBNMatrix = mat3(T, B, N);
}