#version 420

layout (location = 0) in vec3 Position;
uniform vec4 color;

uniform mat4 ProjectionMatrix, ViewMatrix, ModelMatrix;
  
out vec4 outColor; 

void main()
{
    vec4 worldspacePos = ModelMatrix * vec4(Position, 1.0);
	gl_Position = ProjectionMatrix * ViewMatrix * worldspacePos;

    outColor = color;
}  