#version 460 core

in vec3 aTexCoord;

out vec4 fColor;

uniform samplerCube skybox;

void main() {
    fColor = texture(skybox, aTexCoord);
}
