#version 460 core

layout (location = 0) in vec3  vPosition;
layout (location = 1) in vec3  vTexCoord; // not vec2, because xy = texCoord, z = texID
layout (location = 2) in float vBrightness;

out vec3 aTexCoord;
out vec4 aColor;

uniform mat4 uModel1;
uniform mat4 uModel2;
uniform mat4 uModel3;
uniform mat4 uModel4;
uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    gl_Position = uProjection * uView * uModel1 * uModel2 * uModel3 * uModel4 * vec4(vPosition, 1.0);
    aTexCoord = vTexCoord;
    aColor = vec4(vec3(vBrightness), 1.0);
}
