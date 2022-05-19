#version 420

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 ModelMatrix;
layout (std140, binding = 0) uniform CameraMatricies {
	mat4 ProjectionMatrix;
	mat4 ViewMatrix;
	mat4 ViewProjectionMatrix;
};

out vec2 texCoord;

void main(void)
{
    vec4 worldspacePos = ModelMatrix * vec4(aPosition, 1.0);
	gl_Position =  ViewProjectionMatrix * worldspacePos;

    texCoord = aTexCoord;
}