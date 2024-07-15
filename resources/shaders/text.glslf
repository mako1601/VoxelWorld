#version 460 core

in vec2 aTexCoord;

uniform sampler2D texture0;
uniform vec3 color;

out vec4 fColor;

void main() {
    float text = texture(texture0, aTexCoord.xy).r;
    fColor = vec4(color.rgb * text, text);
}