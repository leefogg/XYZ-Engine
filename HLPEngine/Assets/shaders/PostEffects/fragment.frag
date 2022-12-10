#version 330

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D 
	diffuseMap,
	positionTexture,
	normalTexture,
	noiseMap,
	lightMap;

// Bloom Combine
uniform bool EnableBloom = true;
uniform vec3 avSizeWeight = vec3(0.25, 0.5, 1) * 5;
uniform sampler2D blurMap0;
uniform sampler2D blurMap1;
uniform sampler2D blurMap2;
uniform sampler2D dirtMap;
uniform sampler2D crackMap;
// Lens dirt
uniform float dirtHighlightScalar = 2.0;
uniform float dirtGeneralScalar = 0.01;
// Lens crack
uniform float crackScalar = 0.02;

// SSAO
uniform bool 	EnableSSAO = true;
uniform float 	TimeMilliseconds = 1.0;
uniform int 	MinSamples = 8;
uniform int 	MaxSamples = 32;
uniform float   MinSamplesDistance = 1;
uniform float   MaxSamplesDistance = 0;
uniform float 	Intensity = 3;
uniform float  	Bias = 0.25;
uniform float 	SampleRadius = 0.005;
uniform float 	MaxDistance = 0.1;

// Noise
uniform float noiseScalar = 0.01;

// Color Correction
uniform float afKey = 0.5;
uniform float afExposure = 1.0;
uniform float afInvGammaCorrection = 0.454545;
uniform float afWhiteCut = 3.5;

// FXAA
uniform bool EnableFXAA = true;
uniform float Span = 8.0;

out vec4 FragColor;

#define MOD3 vec3(443.8975,397.2973, 491.1871)
float hash12(vec2 p)
{
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}
float rand(vec2 p) {
	return hash12(p);
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


float Map(in float x, in float in_min, in float in_max, in float out_min, in float out_max)
{
    return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
}


vec3 CombineBloomBuffers() 
{
	vec2 crackOffset = (texture(crackMap, texCoord).xy * 2.0 - 1.0) * crackScalar;

	vec3 vBlurColor0 = texture(blurMap0, texCoord).xyz;
	vec3 vBlurColor1 = texture(blurMap1, texCoord).xyz;
	vec3 vBlurColor2 = texture(blurMap2, texCoord).xyz;
	
	//vec4 vBlurColor = sqrt((vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z) / dot(avSizeWeight, vec3(1.0)));
	vec3 vBlurColor = (vBlurColor0 * avSizeWeight.x + vBlurColor1 * avSizeWeight.y + vBlurColor2 * avSizeWeight.z);
	
	// Perform the brightness check in normalized space since value can be more than 1.0
	vec2 vMax = max(vBlurColor.xy, vec2(vBlurColor.z, 0.001));
	vBlurColor *= dot(vBlurColor.xyz / max(vMax.x, vMax.y), vec3(0.3, 0.58, 0.12));

	float dirtOverlay = 1.0 - texture(dirtMap, texCoord * 2).r;

	vec3 diffuse = texture(diffuseMap, texCoord - crackOffset).xyz;
	
    return 
		  vBlurColor 
		+ (vBlurColor * dirtOverlay * dirtHighlightScalar) 
		+ vec3(dirtOverlay * dirtGeneralScalar); 
		//+ diffuse;
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

    float rotatePhase = hash12( uv*100. ) * 6.28 * (TimeMilliseconds * 8.72);
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

float CalculateSSAO() 
{
	vec2 uv = texCoord;
	
	vec3 p = getPosition(uv);
    vec3 n = getNormal(uv);
		
    float distFromCamera = length(p);
    if (distFromCamera > 10.0)
        return 0.0;

    float radius = SampleRadius / abs(distFromCamera);
    float numSamples = min(1.0, (MaxSamplesDistance / distFromCamera)) * (MaxSamples - MinSamples) + MinSamples;
    float ao = spiralAO(uv, p, n, radius, int(numSamples));

    ao *= Intensity;
	return ao;
}

vec3 CalculateFXAA() 
{
	float FXAA_REDUCE_MUL = 1.0 / Span;
    float FXAA_REDUCE_MIN = 1.0 / (Span * 16.0);

    ivec2 texturesize = textureSize(diffuseMap, 0).xy;
	vec2 texture_size = vec2(float(texturesize.x), float(texturesize.y));

    vec3 rgbNW = texture(diffuseMap, texCoord+(vec2(-1.0,-1.0)/texture_size)).xyz;
    vec3 rgbNE = texture(diffuseMap, texCoord+(vec2(1.0,-1.0)/texture_size)).xyz;
    vec3 rgbSW = texture(diffuseMap, texCoord+(vec2(-1.0,1.0)/texture_size)).xyz;
    vec3 rgbSE = texture(diffuseMap, texCoord+(vec2(1.0,1.0)/texture_size)).xyz;
    vec3 rgbM =  texture(diffuseMap, texCoord).xyz;

    vec3 luma=vec3(0.299, 0.587, 0.114);
    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    vec2 dir = vec2(
		-((lumaNW + lumaNE) - (lumaSW + lumaSE)),
		((lumaNW + lumaSW) - (lumaNE + lumaSE))
	);

    float dirReduce = max(
        (lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 * FXAA_REDUCE_MUL),
        FXAA_REDUCE_MIN
    );

    float rcpDirMin = 1.0/(min(abs(dir.x), abs(dir.y)) + dirReduce);

    dir = min(
        	vec2(Span, Span),
          	max(
            	vec2(-Span, -Span),
          		dir * rcpDirMin
            )
    	) / texture_size;

    vec3 rgbA = 0.5 * 				(texture(diffuseMap, texCoord.xy + dir * (1.0/3.0 - 0.5)).xyz + texture(diffuseMap, texCoord.xy + dir * (2.0/3.0 - 0.5)).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 *	(texture(diffuseMap, texCoord.xy + dir * (0.0/3.0 - 0.5)).xyz + texture(diffuseMap, texCoord.xy + dir * (3.0/3.0 - 0.5)).xyz);
    float lumaB = dot(rgbB, luma);
	
	float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
	
    vec3 finalcolor = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;
	
	return finalcolor;
}

float CalculateNoise() {
	float noise = texture(noiseMap, vec2(rand(texCoord), rand(texCoord)) + vec2(TimeMilliseconds)).r;
    noise *= noiseScalar;
    noise -= noiseScalar / 2.0;
	return noise;
}

void ColorCorrection(inout vec4 vSourceColor) 
{
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
}

//TODO: Probably worth making this use shader variants as they wouldn't change often
void main()
{
	vec3 diffuse = EnableFXAA ?  CalculateFXAA() : texture(diffuseMap, texCoord).xyz;
	vec3 lighting = texture(lightMap, texCoord).xyz;

	vec4 finalColor = vec4(diffuse * lighting, 0.0);

	if (EnableSSAO) {
		float ao = CalculateSSAO();
		finalColor -= vec4(ao);
	}
	finalColor = max(vec4(0), finalColor);
    
	ColorCorrection(finalColor);
	
	if (EnableBloom) {
		vec3 combinedBloomBuffers = CombineBloomBuffers();
    	finalColor.xyz += combinedBloomBuffers;
	}
	
	float noise = CalculateNoise();
	finalColor += vec4(noise);

	FragColor = finalColor;
}