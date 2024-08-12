#version 460 core

layout (location = 0) in vec3 vPosition;

out vec3 aTexCoord;

uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    aTexCoord = vPosition;
    gl_Position = vec4(uProjection * uView * vec4(vPosition, 1.0)).xyww;
}
