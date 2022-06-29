#version 420

layout (location = 0) in vec3 Position;
uniform vec4 color;

uniform mat4 ModelMatrix;

layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};
  
out vec4 outColor; 

void main()
{
    vec4 worldspacePos = ModelMatrix * vec4(Position, 1.0);
	gl_Position = ViewProjectionMatrix * worldspacePos;

    outColor = color;
}  