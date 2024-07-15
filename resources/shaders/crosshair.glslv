#version 460 core

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoord;

out vec2 aTexCoord;

uniform mat4 projection;
uniform mat4 scale;

void main() {
    gl_Position = projection * scale * vec4(vPosition, 0.0, 1.0);
    aTexCoord = vTexCoord;
}