#version 460 core

layout (location = 0) in vec3  vPosition;
layout (location = 1) in vec3  vTexCoord; // not vec2, because xy = texCoord, z = texID
layout (location = 2) in float vBrightness;

out vec3 aTexCoord;
out vec4 aColor;

uniform mat4 model1;
uniform mat4 model2;
uniform mat4 model3;
uniform mat4 model4;
uniform mat4 view;
uniform mat4 projection;

void main() {
    gl_Position = projection * view * model1 * model2 * model3 * model4 * vec4(vPosition, 1.0);
    aTexCoord = vTexCoord;
    aColor = vec4(vBrightness, vBrightness, vBrightness, 1.0);
}
