#version 460 core

in vec3 aTexCoord;

out vec4 fColor;

uniform samplerCube uSkybox;

void main() {
    fColor = texture(uSkybox, aTexCoord);
}
