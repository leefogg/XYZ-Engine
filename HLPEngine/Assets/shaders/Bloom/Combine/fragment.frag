#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform vec3 avSizeWeight = vec3(0.25, 0.5, 1) * 5;
uniform sampler2D blurMap0;
uniform sampler2D blurMap1;
uniform sampler2D blurMap2;
uniform sampler2D dirtMap;
uniform sampler2D crackMap;
uniform sampler2D diffuseMap;
uniform float dirtHighlightScalar = 2.0;
uniform float dirtGeneralScalar = 0.01;
uniform float crackScalar = 0.02;

out vec4 FragColor;

#define MOD3 vec3(443.8975,397.2973, 491.1871)
float rand(vec2 p) {
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

void main()
{
	vec2 crackOffset = (texture(crackMap, texCoord).xy * 2.0 - 1.0) * crackScalar;

	vec4 vBlurColor0 = texture(blurMap0, texCoord);
	vec4 vBlurColor1 = texture(blurMap1, texCoord);
	vec4 vBlurColor2 = texture(blurMap2, texCoord);
	
	//vec4 vBlurColor = sqrt((vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z) / dot(avSizeWeight, vec3(1.0)));
	vec4 vBlurColor = (vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z);
	
	// Perform the brightness check in normalized space since value can be more than 1.0
	vec2 vMax = max(vBlurColor.xy, vec2(vBlurColor.z, 0.001));
	vBlurColor *= dot(vBlurColor.xyz / max(vMax.x, vMax.y), vec3(0.3, 0.58, 0.12));

	float dirtOverlay = 1.0 - texture(dirtMap, texCoord * 2).r;

	vec4 diffuse = texture(diffuseMap, texCoord - crackOffset);
	
    FragColor = 
		  vBlurColor 
		+ (vBlurColor * dirtOverlay * dirtHighlightScalar) 
		+ vec4(dirtOverlay * dirtGeneralScalar) 
		+ diffuse;
}