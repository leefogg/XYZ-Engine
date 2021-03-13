#version 460

layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec3 Normal;

out vec3 fragPos;
out vec2 uv;
out vec3 norm;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};

uniform mat4 ModelMatrix;

void main(void) {
	vec4 worldspacePos = ModelMatrix * vec4(Position, 1.0);
	gl_Position =  ViewProjectionMatrix * worldspacePos;

	fragPos = worldspacePos.xyz;
	uv = UV;
	norm = Normal;
}