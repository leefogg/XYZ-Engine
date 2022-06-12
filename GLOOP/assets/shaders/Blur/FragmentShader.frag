#version 420

uniform sampler2D texture0;
uniform int NumSamples;

in vec2 texCoord;

struct uniformData {
	float weight;
	float offset;
};

layout (shared, binding = 3) uniform uniforms {
	uniformData data[32];
};

layout (location = 0) out vec3 fragColor; 

void main()
{
	vec2 vOffsetMul = Direction;

	float fWeight = data[0].weight * 0.5;
	vec3 vAmount = texture(texture0, texCoord + data[0].offset * vOffsetMul).rgb * data[0].weight;
	
	for(int i=1; i<NumSamples; i+=1)
	{	
		vec2 vOffset = data[i].offset * vOffsetMul;

		//////////
		// sample in both positive and negative direction at the same time
		vec3 vColor = texture(texture0, texCoord + vOffset).rgb + 
					  texture(texture0, texCoord - vOffset).rgb;

		float fMul = data[i].weight;
		vAmount += vColor * fMul;
		fWeight += fMul;
	}
	
	vAmount /= (2.0 * fWeight);

	fragColor = vAmount;
}