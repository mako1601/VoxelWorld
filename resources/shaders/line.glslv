#version 460 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vColor;

out vec4 aColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    gl_Position = uProjection * uView * uModel * vec4(vPosition, 1.0);
    aColor = vec4(vColor, 1.0);
}
