#version 330

in vec2 texCoord;

uniform sampler2D 
	positionTexture,
	normalTexture;
uniform float 	Time = 1;
uniform int 	MinSamples = 8;
uniform int 	MaxSamples = 32;
uniform float   MinSamplesDistance = 1;
uniform float   MaxSamplesDistance = 0;
uniform float 	Intensity = 3;
uniform float  	Bias = 0.25;
uniform float 	SampleRadius = 0.005;
uniform float 	MaxDistance = 0.1;

out vec4 pixelColor;


float Map(in float x, in float in_min, in float in_max, in float out_min, in float out_max)
{
    return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}

#define MOD3 vec3(.1031, .11369, .13787)

float hash12(vec2 p)
{
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p)
{
	vec3 p3 = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx+19.19);
    return fract(vec2((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y));
}

vec3 getPosition(vec2 uv) {
    vec3 position = texture(positionTexture, uv).xyz;
    return position;
}

vec3 getNormal(vec2 uv) {
    vec4 normal = texture(normalTexture, uv);
	normal.xyz = normal.xyz * 2.0 - 1.0;
	
	return normal.xyz;
}

float doAmbientOcclusion(in vec2 tcoord, in vec2 uv, in vec3 p, in vec3 cnorm)
{
    vec3 diff = getPosition(tcoord + uv) - p;
    float len = length(diff);
    vec3 v = diff / len;
    float ao = max(0.0, dot(cnorm , v) - Bias) * (1.0 / (1.0 + len));
    ao *= smoothstep(MaxDistance, MaxDistance * 0.5, len);
    
	return ao;
}

float spiralAO(vec2 uv, vec3 p, vec3 n, float initradius, int numSamples)
{
    float goldenAngle = 2.4;
    float ao = 0.;
    float inv = 1. / float(numSamples);
    float radius = 0.;

    float rotatePhase = hash12( uv*100. ) * 6.28 * (Time * 8.72);
    float rStep = inv * initradius;
    vec2 spiralUV;

    for (int i = 0; i < numSamples; i++) {
        spiralUV.x = sin(rotatePhase);
        spiralUV.y = cos(rotatePhase);
        radius += rStep;
        ao += doAmbientOcclusion(uv, spiralUV * radius, p, n);
        rotatePhase += goldenAngle;
    }
    ao *= inv;
	
    return ao;
}

void main(void) {
    pixelColor = vec4(0);

	vec2 uv = texCoord;
	
	vec3 p = getPosition(uv);
    vec3 n = getNormal(uv);
		
    float distFromCamera = length(p);
    if (distFromCamera > 10)
        return;
    float radius = SampleRadius / abs(distFromCamera);
    float numSamples = min(1.0, (MaxSamplesDistance / distFromCamera)) * (MaxSamples - MinSamples) + MinSamples;
    float ao = spiralAO(uv, p, n, radius, int(numSamples));

    ao *= Intensity;

	pixelColor = vec4(ao);
}