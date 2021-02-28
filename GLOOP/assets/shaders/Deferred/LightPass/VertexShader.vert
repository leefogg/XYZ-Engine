#version 420

layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec3 Normal;
layout (location = 3) in vec3 Tangent;

out vec3 fragPos;
out vec2 texCoord;
out vec3 norm;
out vec4 clipSpace;
flat out int instance;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};

#if (LIGHTTYPE == POINT)
	struct PointLight {
		vec4 position;
		vec4 color;
		float brightness;
		float radius;
		float falloffPow;
		float diffuseScalar;
		float specularScalar;
	};
	layout (std140, binding = 1) uniform pointlights {
		PointLight[500] pointLights;
	};
#else
	struct SpotLight {
		vec4 position;
		vec4 color;
		vec4 direction;
		float brightness;
		float radius;
		float falloffPow;
		float angularFalloffPow;
		float FOV;
		float diffuseScalar;
		float specularScalar;
	};
	layout (std140, binding = 1) uniform spotlights {
		SpotLight[500] spotLights;
	};
#endif

void main(void) {
#if (LIGHTTYPE == POINT)
	PointLight light = pointLights[gl_InstanceID];
#else
	SpotLight light = spotLights[gl_InstanceID];
#endif

	mat4 modelMatrix = mat4(1);
	modelMatrix[3].xyz = light.position.xyz;
	modelMatrix[0][0] = light.radius * 2;
	modelMatrix[1][1] = light.radius * 2;
	modelMatrix[2][2] = light.radius * 2;
	modelMatrix[3][3] = 1;
	vec4 worldspacePos = modelMatrix * vec4(Position, 1.0);
	clipSpace = ViewProjectionMatrix * worldspacePos;
	gl_Position = clipSpace;

	fragPos = worldspacePos.xyz;
	texCoord = UV;
	norm = mat3(transpose(inverse(modelMatrix))) * Normal;
	instance = gl_InstanceID;
}