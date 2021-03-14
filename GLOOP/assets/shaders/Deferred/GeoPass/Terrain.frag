#version 430

in vec3 fragPos;
in vec2 uv;
in vec3 norm;
in vec4 splat;

uniform float SpecularPower;
uniform sampler2D 
	diffuseTex, 
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
	diffuse = texture(diffuseTex, uv / vec2(BaseTextureTileAmount)).rgb;
	diffuse = mix(diffuse, texture(blendLayer0Tex0, uv / vec2(BlendLayer0TileAmount.x)).rgb, splat.r);
	diffuse = mix(diffuse, texture(blendLayer0Tex1, uv / vec2(BlendLayer0TileAmount.y)).rgb, splat.g);
	diffuse = mix(diffuse, texture(blendLayer0Tex2, uv / vec2(BlendLayer0TileAmount.z)).rgb, splat.b);
	diffuse = mix(diffuse, texture(blendLayer0Tex3, uv / vec2(BlendLayer0TileAmount.w)).rgb, splat.a);
	position = fragPos;
	specular = vec4(1.0, 1.0, 1.0, SpecularPower);
	normal = norm;
}