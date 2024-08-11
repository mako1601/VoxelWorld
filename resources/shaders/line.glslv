#version 460 core

layout (location = 0) in vec3 vPosition;

out vec4 aColor;

uniform vec4 color;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main() {
    gl_Position = projection * view * model * vec4(vPosition, 1.0);
    aColor = color;
}
