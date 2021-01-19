#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D texture0;
uniform sampler2D texture1;
uniform float gamma = 1.4;

out vec4 FragColor;

void main()
{
	vec4 albedo =  texture(texture0, texCoord);
	vec4 lighting = texture(texture1, texCoord);
	
    FragColor = albedo * lighting;
	FragColor.rgb = pow(FragColor.rgb, vec3(1.0 / gamma));
}