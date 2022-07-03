#version 460
#define MAX_LIGHTS 512

layout (location = 0) in vec3 Position;

out vec3 fragPos;
out vec2 texCoord;
out vec3 norm;
out vec4 clipSpace;
flat out int instance;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
	mat4 InverseView;
	mat4 InverseProjection;
};

#if (LIGHTTYPE == POINT)
	struct PointLight {
		vec3 position;
		float brightness;
		vec3 color;
		float radius;
		float falloffPow;
		float diffuseScalar;
		float specularScalar;
	};
	layout (std140, binding = 1) buffer pointlights {
		PointLight[] lights;
	};
	layout (std140, binding = 1) uniform pointLightIndexes {
		ivec4[MAX_LIGHTS / 4] indexes;
	};
	out PointLight light;
#else
	struct SpotLight {
		mat4 modelMatrix;
		vec3 position;
		vec3 color;
		vec3 direction;
		vec3 scale;
		float ar;
		float brightness;
		float radius;
		float falloffPow;
		float angularFalloffPow;
		float FOV;
		float diffuseScalar;
		float specularScalar;
		mat4 viewProjection;
	};
	layout (std140, binding = 1) buffer spotlights {
		SpotLight[] lights;
	};
	layout (std140, binding = 1) uniform spotLightIndicies {
		ivec4[MAX_LIGHTS / 4] indexes;
	};
	out SpotLight light;
#endif

void main(void) {
	light = lights[indexes[gl_InstanceID / 4][gl_InstanceID % 4]];

#if (LIGHTTYPE == POINT)
	vec3 v = Position;

	mat4 modelMatrix = mat4(1);
	modelMatrix[3].xyz = light.position;
	modelMatrix[0][0] = modelMatrix[1][1] = modelMatrix[2][2] = light.radius;
	//modelMatrix[3][3] = 1;
#else
	vec3 v = Position;
	if (gl_VertexID > 0) {
		v = normalize(v * light.scale * vec3(1, 1, light.ar));
        v *= light.scale.z;
        v.z = -v.z;
	}
	mat4 modelMatrix = light.modelMatrix;
#endif

    vec4 worldspacePos = modelMatrix * vec4(v, 1.0);
	clipSpace = ViewProjectionMatrix * worldspacePos;
	gl_Position = clipSpace;

	fragPos = worldspacePos.xyz;
	instance = gl_InstanceID;
}