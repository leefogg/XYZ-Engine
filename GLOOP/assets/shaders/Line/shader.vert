#version 430

layout (location = 0) in vec3 Position;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};


void main() {

	vec4 worldspacePos = vec4(Position, 1.0);
	gl_Position = ViewProjectionMatrix * worldspacePos;
}