#version 330

in vec2 texCoord;
out vec3 fragColor;

struct PointLight {
	vec3 position;
	vec3 color;
	float brightness;
	float radius;
	float falloffPow;
};

uniform sampler2D diffuseTex;
uniform sampler2D positionTex;
uniform sampler2D normalTex;
uniform PointLight pointLights[100];
uniform int numPointLights;
uniform vec3 camPos;

vec3 newCalculateLighting(vec3 pixelColor, vec3 normal, vec3 fragPos) {
	vec3 lighting = vec3(0.0);
	
	for (int i=0; i<numPointLights; i++) {
		PointLight light = pointLights[i];
		
		vec3 vLightDir = light.position - fragPos;
		float fDistance = sqrt(dot(vLightDir, vLightDir));
		float fAttenuatuion =  pow(1.0 - min(fDistance * (1.0 / light.radius), 1.0), light.falloffPow * 2);
		
		float fLDotN = dot(vLightDir, normal);
		float fLightAmount = max(fLDotN, 0.0);
		
		float fLightScatterAmount = max(-fLDotN, 0.0);
		float fLightTransport = clamp(dot(camPos, vLightDir + normal * 0.2), -1.0, 0.0);
		fLightTransport = pow(fLightTransport, 16.0);
		float fLightScatter = (min(2.0, fLightTransport * 16.0) + fLightScatterAmount);
		
		vec3 diffuse = (light.color * light.brightness) * fLightAmount;
		diffuse *= fAttenuatuion;
		
		lighting += diffuse;
	}
	
	return lighting;
}

void main()
{
	vec3 diffuseColor = texture(diffuseTex, texCoord).rgb;
	vec3 fragPos = texture(positionTex, texCoord).rgb;
	float fragDist = length(camPos - fragPos);
	vec3 normal = texture(normalTex, texCoord).rgb;
	
	vec3 lighting = newCalculateLighting(diffuseColor, normal, fragPos);
	fragColor = diffuseColor * lighting;
}