#version 420

layout (location = 0) in vec3 Position;

uniform mat4 ModelMatrix;
layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};
uniform vec3 scale;
uniform float ar;
  
out vec4 outColor; 

void main()
{
	vec3 v = Position;
	vec3 ff = vec3(0,0,0);

	if (gl_VertexID > 0) {
		ff = normalize(vec3(v.x * scale.x, v.y * scale.y, scale.z * ar));
        ff *= scale.z;
        ff.z = -ff.z;
	}

    vec4 worldspacePos = ModelMatrix * vec4(ff, 1.0);

	gl_Position = ViewProjectionMatrix * worldspacePos;
}  