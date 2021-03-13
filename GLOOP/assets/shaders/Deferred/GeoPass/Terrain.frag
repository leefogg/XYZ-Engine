#version 430

in vec3 fragPos;
in vec2 uv;
in vec3 norm;

uniform sampler2D diffuseTex;

layout (location = 0) out vec3 diffuse;
layout (location = 1) out vec3 position;
layout (location = 2) out vec3 normal;
layout (location = 3) out vec4 specular;


void main()
{
	diffuse = texture(diffuseTex, uv).rgb;
	position = fragPos;
	specular = vec4(1.0, 1.0, 1.0, 0.0);
	normal = normalize(norm);
	normal = (normal + 1) / 2;
}