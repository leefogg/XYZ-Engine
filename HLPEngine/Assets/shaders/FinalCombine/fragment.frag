#version 330 core

in vec4 outColor;
in vec2 texCoord;

uniform sampler2D texture0;
uniform sampler2D texture1;
uniform int frame;
uniform vec2 resolution;

out vec3 FragColor;

// provides 16 psuedo-random bits
// conviently packaged in a float
// in the [0,1] range.
float squares16(uint ctr) {
    const uint key = uint(0x7a1a912f);
    const float two16 = 65536.0;

    uint x, y, z;

    // initialize
    // ==================================
    // Weyl sequence vars, y and z
    y = ctr * key;
    z = (ctr + uint(1)) * key;

    // init the mixing var, x
    x = y;

    // begin the mixing rounds
    // ===================================

    // round 1
    x = x*x + y; x = (x>>16) | (x<<16);

    // round 2
    x = x*x + z; x = (x>>16) | (x<<16);

    // round 3
    x = (x*x + y) >> 16;

    return float(x)/two16;
}

float pixel_id(vec2 fragCoord) {
    return dot(fragCoord.xy,
               vec2(1, resolution.x));
}

void main()
{
	vec3 albedo =  texture(texture0, texCoord).rgb;
	vec3 lighting = texture(texture1, texCoord).rgb;
	
	vec2 fragCoord = texCoord * resolution;
	float id = pixel_id(fragCoord);
    int cnt_pixels = int(resolution.x * resolution.y);

	// 0.01% dither. Measured to take about 0.02ms
	// This will mess with FXAA so should be done after
    float dither = squares16(uint(id) + uint(frame * cnt_pixels));
	dither = dither * 0.0005;
    dither -= 0.000025;

    FragColor = albedo * (lighting - vec3(dither));
}