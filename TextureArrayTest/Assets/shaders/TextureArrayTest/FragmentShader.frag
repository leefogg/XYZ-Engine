#version 330

in vec2 texCoord;

out vec4 outputColor;

uniform sampler2DArray texture0;
uniform uint slice;

void main()
{
    outputColor = texture(texture0, vec3(texCoord, slice));
}