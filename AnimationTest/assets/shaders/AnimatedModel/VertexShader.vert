#version 420

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormals;
layout(location = 3) in vec3 aTangents;
layout(location = 4) in vec4 aBoneIds;
layout(location = 5) in vec4 aBoneWeights;

uniform mat4 ModelMatrix;
layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};
layout (std140, binding = 2) uniform BonePoses {
	mat4[128] BoneTransforms;
};

out vec2 uv;
out vec3 normal;

void main(void)
{
	vec4 totalLocalPos = vec4(0.0);
	vec4 totalNormal = vec4(0.0);
	for(int i=0; i<4; i++){
		mat4 jointTransform = BoneTransforms[int(aBoneIds[i])];
		vec4 posePosition = jointTransform * vec4(aPosition, 1.0);
		totalLocalPos += posePosition * aBoneWeights[i];
		
		vec4 worldNormal = jointTransform * vec4(aNormals, 0.0);
		totalNormal += worldNormal * aBoneWeights[i];
	}
	
	gl_Position = ViewProjectionMatrix * ModelMatrix * totalLocalPos;

	normal = (ModelMatrix * totalNormal).xyz;
	uv = aTexCoord;
}