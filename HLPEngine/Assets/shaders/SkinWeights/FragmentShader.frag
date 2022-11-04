#version 330

in float weightStrength;

out vec4 outputColor;

uniform sampler2D albedo;

void main()
{
    outputColor = vec4(vec3(weightStrength), 1.0);
}