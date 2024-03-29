#version 150

in vec2 texCoord;

out vec3 pixelColor;

uniform sampler2D texture0;
uniform float Span = 8.0;

void main(void) {
    float FXAA_REDUCE_MUL = 1.0 / Span;
    float FXAA_REDUCE_MIN = 1.0 / (Span * 16.0);

    ivec2 texturesize = textureSize(texture0, 0).xy;
	vec2 texture_size = vec2(float(texturesize.x), float(texturesize.y));

    vec3 rgbNW = texture(texture0, texCoord+(vec2(-1.0,-1.0)/texture_size)).xyz;
    vec3 rgbNE = texture(texture0, texCoord+(vec2(1.0,-1.0)/texture_size)).xyz;
    vec3 rgbSW = texture(texture0, texCoord+(vec2(-1.0,1.0)/texture_size)).xyz;
    vec3 rgbSE = texture(texture0, texCoord+(vec2(1.0,1.0)/texture_size)).xyz;
    vec3 rgbM =  texture(texture0, texCoord).xyz;

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
		
	pixelColor = vec3(dir,0);

    vec3 rgbA = 0.5 * 				(texture(texture0, texCoord.xy + dir * (1.0/3.0 - 0.5)).xyz + texture(texture0, texCoord.xy + dir * (2.0/3.0 - 0.5)).xyz);
    vec3 rgbB = rgbA * 0.5 + 0.25 *	(texture(texture0, texCoord.xy + dir * (0.0/3.0 - 0.5)).xyz + texture(texture0, texCoord.xy + dir * (3.0/3.0 - 0.5)).xyz);
    float lumaB = dot(rgbB, luma);
	
	float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
	
    vec3 finalcolor = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;
	
	pixelColor = finalcolor;
}