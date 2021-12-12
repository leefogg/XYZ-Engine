#version 330

in vec2 texCoord;

uniform vec4 color;

out vec4 outputColor;

void main()
{
    outputColor = color;
}