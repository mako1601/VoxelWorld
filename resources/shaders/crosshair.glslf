#version 460 core

in vec2 aTexCoord;

out vec4 fColor;

uniform sampler2D uTexture;

void main() {
    fColor = texture(uTexture, aTexCoord);
}