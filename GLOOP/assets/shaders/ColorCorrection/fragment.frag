#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D texture0;

uniform float afKey = 0.5;
uniform float afExposure = 1.0;
uniform float afInvGammaCorrection = 0.454545;
uniform float afWhiteCut = 3.5;

out vec4 FragColor;

void main()
{
	vec4 vSourceColor = texture(texture0, texCoord);

	////////////
	// Apply the exposure and key and store the white cut in alpha
	vSourceColor = vSourceColor * afKey * afExposure;
	vSourceColor.w = afWhiteCut * afKey;

	////////////
	// Apply the uncharted tone mapping
	vSourceColor = vSourceColor * vec4(1.5 / 8.0, 1.5 / 8.0, 1.5 / 8.0, 2.0); // Rescale the color because of x 8.0 in helper_gamma_correction
	vSourceColor = ((vSourceColor*(0.15*vSourceColor+vec4(0.1*0.5))+vec4(0.2*0.02))/(vSourceColor*(0.15*vSourceColor+vec4(0.5))+vec4(0.2*0.3)))-vec4(0.02/0.3);

	/////////////
	// Cut all the light above the White Cut, making it 1.0
	vSourceColor *= 2.0 / vSourceColor.w;

	//////////
	// Perform the gamma correction
	vSourceColor = pow(vSourceColor, vec4(afInvGammaCorrection));

    FragColor = vSourceColor;
}