#version 460 core

in vec2 aTexCoord;
in vec4 aColor;

out vec4 fColor;

uniform sampler2D uTexture;

void main() {
    vec4 texColor = texture(uTexture, aTexCoord);
    if (texColor.a < 0.5) discard;
    fColor = aColor * texColor;
}
