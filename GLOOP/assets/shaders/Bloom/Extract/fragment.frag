#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D diffuseMap;
uniform float afBrightPass;
uniform vec2 avInvScreenSize;

out vec4 FragColor;

void main()
{
	vec4 vDiffuseColor = vec4(0);
	for(int x = 0; x < 4; ++x)
	{
		for(int y = 0; y < 4; ++y)
		{
			vec4 vSample = texture(diffuseMap, texCoord + vec2(x, y) * avInvScreenSize);
			vDiffuseColor += max(vec4(0), vSample - vec4(afBrightPass));
		}
	}

	vec2 ndc = abs(texCoord * vec2(2.0) - vec2(1));
	float fWeight = 1 - max(ndc.x, ndc.y);
	FragColor = max(vec4(0.00000001), vDiffuseColor / 16.0 * fWeight * 2.0);
}