#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D texture0;
uniform sampler2D texture1;

out vec4 FragColor;

void main()
{
    FragColor = texture(texture0, texCoord) * texture(texture1, texCoord);
}