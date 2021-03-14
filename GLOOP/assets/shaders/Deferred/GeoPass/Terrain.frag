#version 430

in vec3 fragPos;
in vec2 uv;
in vec3 norm;

uniform sampler2D 
	diffuseTex, 
	splatTex, 
	blendLayer0Tex0, 
	blendLayer0Tex1, 
	blendLayer0Tex2,
	blendLayer0Tex3;
uniform float BaseTextureTileAmount;
uniform vec4 BlendLayer0TileAmount;

layout (location = 0) out vec3 diffuse;
layout (location = 1) out vec3 position;
layout (location = 2) out vec3 normal;
layout (location = 3) out vec4 specular;


void main()
{
	vec4 splatMap = texture(splatTex, uv);
	diffuse = texture(diffuseTex, uv / vec2(BaseTextureTileAmount)).rgb;
	diffuse = mix(diffuse, texture(blendLayer0Tex0, uv / vec2(BlendLayer0TileAmount.x)).rgb, splatMap.r);
	diffuse = mix(diffuse, texture(blendLayer0Tex1, uv / vec2(BlendLayer0TileAmount.y)).rgb, splatMap.g);
	diffuse = mix(diffuse, texture(blendLayer0Tex2, uv / vec2(BlendLayer0TileAmount.z)).rgb, splatMap.b);
	diffuse = mix(diffuse, texture(blendLayer0Tex3, uv / vec2(BlendLayer0TileAmount.w)).rgb, splatMap.a);

	//iffuse = mix(diffuse, vec3(1,0,0), splatMap.r);
	//iffuse = mix(diffuse, vec3(0,1,0), splatMap.g);
	//iffuse = mix(diffuse, vec3(0,0,1), splatMap.b);


	position = fragPos;
	specular = vec4(1.0, 1.0, 1.0, 0.0);
	normal = normalize(norm);
	normal = (normal + 1) / 2;
}