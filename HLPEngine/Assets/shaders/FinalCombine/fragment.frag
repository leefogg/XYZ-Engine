#version 330 core

in vec2 texCoord;

uniform sampler2D lightMap;
uniform sampler2D noiseMap;
uniform float timeMilliseconds;
uniform float noiseScalar = 0.00005;

out vec3 FragColor;

#define MOD3 vec3(443.8975,397.2973, 491.1871)
float rand(vec2 p) {
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

void main()
{
	vec3 lighting = texture(lightMap, texCoord).rgb;

    // This will mess with FXAA so should be done after
    vec3 noise = texture(noiseMap, vec2(rand(texCoord), rand(texCoord)) + vec2(timeMilliseconds)).rgb;
    noise *= noiseScalar;
    noise -= noiseScalar / 2.0;

    FragColor = lighting + noise;
}