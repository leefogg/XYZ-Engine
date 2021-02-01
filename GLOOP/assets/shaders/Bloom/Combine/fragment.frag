#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform vec3 avSizeWeight = vec3(0.5, 1, 2);

uniform sampler2D blurMap0;
uniform sampler2D blurMap1;
uniform sampler2D blurMap2;

out vec4 FragColor;

void main()
{
	vec4 vBlurColor0 = texture(blurMap0, texCoord);
	vec4 vBlurColor1 = texture(blurMap1, texCoord);
	vec4 vBlurColor2 = texture(blurMap2, texCoord);
	
	//vec4 vBlurColor = sqrt((vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z) / dot(avSizeWeight, vec3(1.0)));
	vec4 vBlurColor = (vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z);
	
	// Perform the brightness check in normalized space since value can be more than 1.0
	vec2 vMax = max(vBlurColor.xy, vec2(vBlurColor.z, 0.001));
	vBlurColor *= dot(vBlurColor.xyz / max(vMax.x, vMax.y), vec3(0.3, 0.58, 0.12));
	
    FragColor = vBlurColor;
}