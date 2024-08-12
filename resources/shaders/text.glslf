#version 460 core

in vec2 aTexCoord;

out vec4 fColor;

uniform vec3      uColor;
uniform sampler2D uTexture0;

void main() {
    float text = texture(uTexture0, aTexCoord.xy).r;
    fColor = vec4(uColor.rgb * text, text);
}