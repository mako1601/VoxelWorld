#version 460 core

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoord;

out vec2 aTexCoord;

uniform mat4 uProjection;
uniform mat4 uScale;

void main() {
    gl_Position = uProjection * uScale * vec4(vPosition, 0.0, 1.0);
    aTexCoord = vTexCoord;
}