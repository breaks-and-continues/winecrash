#version 330

out vec4 outputColor;
in vec2 texCoord;
in vec3 Normal;

uniform sampler2D albedo;
uniform vec4 color;

void main()
{
    //vec4 diffuse = texture(albedo, texCoord);

    //outputColor = vec4(diffuse.xyz, 1.0) * color;

    vec4 texColor = texture(albedo, texCoord);

    outputColor = texColor * color;

}