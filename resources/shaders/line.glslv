#version 460 core

layout (location = 0) in vec3 vPosition;

out vec4 aColor;

uniform vec4 uColor;
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    gl_Position = uProjection * uView * uModel * vec4(vPosition, 1.0);
    aColor = uColor;
}
