#version 420

uniform sampler2D diffuseMap;

in vec2 texCoord; 

layout (std140, binding = 3) uniform pointlights {
	float weights[24];
	float offsets[24];
};

layout (location = 0) out vec3 fragColor; 

void main()
{
	vec2 vOffsetMul = Direction;

	float fWeight = weights[0] * 0.5;
	vec3 vAmount = texture(diffuseMap, texCoord + offsets[0] * vOffsetMul).rgb * weights[0];
	
	for(int i=1; i<24; i+=1)
	{	
		vec2 vOffset = offsets[i] * vOffsetMul;

		//////////
		// sample in both positive and negative direction at the same time
		vec3 vColor = texture(diffuseMap, texCoord + vOffset).rgb + 
					  texture(diffuseMap, texCoord - vOffset).rgb;

		float fMul = weights[i];
		vAmount += vColor * fMul;
		fWeight += fMul;
	}
	
	vAmount /= (2.0 * fWeight);

	fragColor = vAmount;
}