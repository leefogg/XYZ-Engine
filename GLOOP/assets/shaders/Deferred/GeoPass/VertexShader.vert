#version 420

layout (location = 0) in vec3 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec3 Normal;
layout (location = 3) in vec3 Tangent;

out vec3 fragPos;
out vec2 uv;
out mat3 TBNMatrix;

uniform mat4 ModelMatrix;
layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
};

void main(void) {
	vec4 worldspacePos = ModelMatrix * vec4(Position, 1.0);
	gl_Position =  ProjectionMatrix * ViewMatrix * worldspacePos;

	fragPos = worldspacePos.xyz;
	uv = UV;
	
	mat3 normalMatrix = transpose(inverse(mat3(ModelMatrix)));
	vec3 T = normalize(normalMatrix * Tangent);
	vec3 N = normalize(normalMatrix * Normal);
	vec3 B = cross(N, T);
	T = normalize(T - dot(T, N) * N); // re-orthogonalize T with respect to N
	
	TBNMatrix = mat3(T, B, N);
}