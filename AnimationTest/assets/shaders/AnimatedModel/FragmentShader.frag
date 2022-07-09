#version 330

in vec2 uv;
in vec3 normal;

out vec4 outputColor;

uniform sampler2D albedo;

void main()
{
    outputColor = texture(albedo, uv);
}