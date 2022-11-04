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

out float weightStrength;

void main(void)
{
	int desiredBone = 3;
	weightStrength = 0.1;
	for(int i=0; i<4; i++) {
		if (aBoneIds[i] == desiredBone) {
			weightStrength = aBoneWeights[i];
			break;
		}
	}

	vec4 worldspacePos = ModelMatrix * vec4(aPosition, 1.0);
	gl_Position = ViewProjectionMatrix * worldspacePos;
}