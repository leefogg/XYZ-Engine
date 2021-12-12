#version 420
#define PI 3.141592
#define POINT 0
#define SPOT 1

in vec3 fragPos;
in vec4 clipSpace;
flat in int instance;

out vec3 fragColor;

#if (LIGHTTYPE == POINT)
	struct PointLight {
		vec3 position;
		vec3 color;
		float brightness;
		float radius;
		float falloffPow;
		float diffuseScalar;
		float specularScalar;
	};
	in PointLight light;
#else
	struct SpotLight {
		mat4 modelMatrix;
		vec3 position;
		vec3 color;
		vec3 direction;
		vec3 scale;
		float ar;
		float brightness;
		float radius;
		float falloffPow;
		float angularFalloffPow;
		float FOV;
		float diffuseScalar;
		float specularScalar;
		mat4 viewProjection;
	};
	in SpotLight light;
#endif

uniform sampler2D diffuseTex;
uniform sampler2D positionTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;

uniform float offsetByNormalScalar = 0.05;
uniform float lightBrightnessMultiplier = 16.0;
uniform float specularPowerScalar = 8.0;
uniform float lightingScalar = 8.0;
uniform float diffuseScalar = 1.0;
uniform float specularScalar = 1.0;
uniform bool useLightSpecular = true;
uniform bool useLightDiffuse = true;
uniform float lightScatterScalar = 1.0;

uniform vec3 camPos;

vec3 power(vec3 source, float power)
{
	return vec3(
		pow(source.r, power),
		pow(source.g, power),
		pow(source.b, power)
	);
}

void main()
{
	vec2 ndc = (clipSpace.xy / clipSpace.w) / 2.0 + 0.5;

	vec4 rawPos = texture(positionTex, ndc);
	if (rawPos.a == 0.0)
		discard;
	vec3 fragPos = rawPos.rgb + camPos;
	
	vec3 normal = texture(normalTex, ndc).rgb * 2 - 1;
	
	// Diffuse
	vec4 albedo = texture(diffuseTex, ndc);
	vec3 diffuse = albedo.rgb;
	vec3 vLightDir = light.position.xyz - fragPos;
	float fDistance = length(vLightDir);
	float localDist =  1.0 - min(fDistance * (1.0 / light.radius * 2.1), 1.0); // Converts to [1,0] inside sphere
	vLightDir = normalize(vLightDir);

	fragPos += normal * (1 - dot(normal, vLightDir)) * offsetByNormalScalar;
	// fragPos was modified
	vLightDir = light.position.xyz - fragPos;
	fDistance = length(vLightDir);
	localDist =  1.0 - min(fDistance * (1.0 / light.radius * 2.1), 1.0); // Converts to [1,0] inside sphere
	vLightDir = normalize(vLightDir);

	vec3 vPos = fragPos - camPos;
	vec3 vEye = -normalize(vPos);

	float fAttenuation = localDist;
	
	#if (LIGHTTYPE == SPOT)
		vec4 ProjectedUv = light.viewProjection * vec4(fragPos, 1.0);
		ProjectedUv = vec4(ProjectedUv.xyz, 1.0) / ProjectedUv.w;
		fAttenuation *= pow(max(0.0, 1.0 - length(ProjectedUv.xy)), light.angularFalloffPow);
		// Clamp so there no light directly backwards
		fAttenuation *= max(0, ProjectedUv.z) * clamp((1.0 - ProjectedUv.z) * 128.0, 0.0, 1.0);
	#endif

	if (fAttenuation <= 0.0)
		discard;
		
	vec3 color = light.color * light.brightness * lightBrightnessMultiplier;
	
	///////////
	// A cheaper version of Dice Translucency
	// Refract the incoming light with the normal
	float fLDotN = dot(vLightDir, normal);
	float fLightScatterAmount = max(-fLDotN, 0.0);
	float fLightTransport = clamp(dot(vEye, vPos + normal * 0.2), -1.0, 0.0);
	fLightTransport = pow(fLightTransport, 16.0);
	
	////////
	// Apply energy conservation and ambient scattering
	float fLightScatter = (min(2.0, fLightTransport * 16.0) + fLightScatterAmount) * albedo.a;
	float fLightAmount = max(fLDotN, 0.0);

	// Specular
	vec4 specular = texture(specularTex, ndc);
	vec3 vSpecIntensity = specular.rgb;
	float fSpecPower = specular.a;
		
	vec3 vHalfVec = normalize(vLightDir + vEye);
	fSpecPower = exp2(fSpecPower * specularPowerScalar) + 1.0; // Range 0 - 1024
	
	// Calculate the enegry conservation value, this will make sure that the intergral of the specular is 1.0
	float fEngeryConservation = (fSpecPower + specularPowerScalar) * (1.0 / (8.0 * PI));
	vec3 vSpecular = vSpecIntensity * min(fEngeryConservation * pow(max(dot(vHalfVec, normal), 0.0), fSpecPower), 1.0);
	vSpecular *= fSpecPower;
	
	if (useLightSpecular)
		vSpecular *= light.specularScalar;
	vSpecular *= specularScalar;

	vec3 lighting = diffuse * color;
	lighting *= fLightAmount + fLightScatter * fLightScatterAmount * lightScatterScalar;
	if (useLightDiffuse)
		lighting *= light.diffuseScalar;
	lighting *= diffuseScalar;
	lighting += vSpecular;
	lighting *= fAttenuation;

	// Multiply with 8.0 to increase precision
	lighting *= lightingScalar;
	fragColor = lighting;
}