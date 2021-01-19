#version 420
#define PI 3.141592
#define POINT 0
#define SPOT 1

in vec3 fragPos;
in vec2 texCoord;
in vec3 norm;
in vec4 clipSpace;
flat in int instance;

out vec3 fragColor;

#if (LIGHTTYPE == POINT)
	struct PointLight {
		vec4 position;
		vec4 color;
		float brightness;
		float radius;
		float falloffPow;
		float diffuseScalar;
		float specularScalar;
	};
	layout (std140, binding = 1) uniform pointlights {
		PointLight[500] pointLights;
	};
#else
	struct SpotLight {
		vec4 position;
		vec4 color;
		vec4 direction;
		float brightness;
		float radius;
		float falloffPow;
		float angularFalloffPow;
		float FOV;
		float diffuseScalar;
		float specularScalar;
	};
	layout (std140, binding = 2) uniform spotlights {
		SpotLight[500] spotLights;
	};
#endif

uniform sampler2D diffuseTex;
uniform sampler2D positionTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;

uniform vec3 camPos;

vec3 power(vec3 source, float power){
	return vec3(
		pow(source.r, power),
		pow(source.g, power),
		pow(source.b, power)
	);
}

void main()
{
#if (LIGHTTYPE == POINT)
	PointLight light = pointLights[instance];
#else
	SpotLight light = spotLights[instance];
#endif

	vec2 ndc = (clipSpace.xy / clipSpace.w) / 2.0 + 0.5;

	vec3 fragPos = texture(positionTex, ndc).rgb;
	vec3 normal = texture(normalTex, ndc).rgb;
	vec4 specular = texture(specularTex, ndc);
	
	vec3 vPos = fragPos - camPos;
	vec3 vEye = -normalize(vPos);

	// Diffuse
	vec3 vLightDir = light.position.xyz - fragPos;
	float fDistance = length(vLightDir);
	float localDist =  1.0 - min(fDistance * (1.0 / light.radius), 1.0); // Converts to [1,0] inside sphere
	float fAttenuatuion = localDist;
	fAttenuatuion = pow(fAttenuatuion, light.falloffPow);

	#if (LIGHTTYPE == SPOT)
		vec3 toLight = normalize(light.position.xyz - fragPos);
		float theta = max(0.0, dot(toLight, light.direction.xyz));
		//theta = pow(theta, 16.0 * light.angularFalloffPow);
		float falloff = max(0.0, theta - (light.FOV / 180));
		falloff = pow(falloff, 2.0 * light.angularFalloffPow);
		fAttenuatuion *= falloff;
	#endif
		
	vec3 color = light.color.rgb * light.brightness / 25;
	
	float fLDotN = dot(vLightDir, normal);
	float fLightScatterAmount = max(-fLDotN, 0.0);
	float fLightTransport = clamp(dot(vEye, vPos + normal * 0.2), -1.0, 0.0);
	fLightTransport = pow(fLightTransport, 16.0);
	float fLightScatter = min(2.0, fLightTransport * 16.0) + fLightScatterAmount;
		
	float fLightAmount = max(fLDotN, 0.0);
	vec3 diffuse = color * fLightAmount;
	diffuse *= fLightScatter;
	diffuse *= light.diffuseScalar;

	// Specular
	vec3 vSpecIntensity = specular.rgb;
	float fSpecPower = 1.0 - specular.a; //specular.w
		
	vec3 vHalfVec = normalize(vLightDir + vEye);
	fSpecPower = exp2(fSpecPower * 10.0) + 1.0; //Range 0 - 1024
	// Calculate the enegry conservation value, this will make sure that the intergral of the specular is 1.0
	float fEngeryConservation = (fSpecPower + 8.0) * (1.0 / (8.0 * PI));
	vec3 vSpecular = vSpecIntensity * min(fEngeryConservation * pow(max(dot(vHalfVec, normal), 0.0), fSpecPower), 1.0);
	vSpecular *= fSpecPower;
	vSpecular *= color;
	vSpecular *= fAttenuatuion;
	vSpecular *= light.specularScalar;
	
	vec3 lighting = diffuse + vSpecular;
	lighting *= fAttenuatuion;

	fragColor = lighting;
}