#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4
	ProjectionMatrix,
	ViewMatrix,
	ModelMatrix;

out vec2 texCoord;

void main(void)
{
    vec4 worldspacePos = ModelMatrix * vec4(aPosition, 1.0);
	gl_Position =  ProjectionMatrix * ViewMatrix * worldspacePos;

    texCoord = aTexCoord;
}