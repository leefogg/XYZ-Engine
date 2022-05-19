#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform vec3 avSizeWeight = vec3(0.25, 0.5, 1) * 5;

uniform sampler2D blurMap0;
uniform sampler2D blurMap1;
uniform sampler2D blurMap2;
uniform sampler2D noiseMap;
uniform float timeMilliseconds;
uniform vec2 avInvScreenSize;

out vec4 FragColor;

#define MOD3 vec3(443.8975,397.2973, 491.1871)
float rand(vec2 p) {
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

void main()
{
	vec4 vNoise = texture(noiseMap, vec2(rand(texCoord), rand(texCoord)) + vec2(timeMilliseconds));

	vec2 vBloomNoise = (vNoise.rg - vec2(0.5)) * 5 * avInvScreenSize;

	vec4 vBlurColor0 = texture(blurMap0, texCoord + vBloomNoise * 1);
	vec4 vBlurColor1 = texture(blurMap1, texCoord + vBloomNoise * 2);
	vec4 vBlurColor2 = texture(blurMap2, texCoord + vBloomNoise * 3);
	
	//vec4 vBlurColor = sqrt((vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z) / dot(avSizeWeight, vec3(1.0)));
	vec4 vBlurColor = (vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z);
	
	// Perform the brightness check in normalized space since value can be more than 1.0
	vec2 vMax = max(vBlurColor.xy, vec2(vBlurColor.z, 0.001));
	vBlurColor *= dot(vBlurColor.xyz / max(vMax.x, vMax.y), vec3(0.3, 0.58, 0.12));
	
    FragColor = vBlurColor;
}