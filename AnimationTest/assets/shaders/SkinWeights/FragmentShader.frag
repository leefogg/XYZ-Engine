#version 330

in vec2 uv;

out vec4 outputColor;

uniform sampler2D albedo;

void main()
{
    outputColor = texture(albedo, uv);
}