#version 460 core

layout (location = 0) in vec3 vPosition;

out vec3 aTexCoord;

uniform mat4 view;
uniform mat4 projection;

void main() {
    aTexCoord = vPosition;
    vec4 pos = projection * view * vec4(vPosition, 1.0);
    gl_Position = pos.xyww;
}
