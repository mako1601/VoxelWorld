#version 460 core

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoord;

out vec2 aTexCoord;

layout (location = 0) uniform mat4 transOrigin;
//layout (location = 1) uniform mat4 rotate;
layout (location = 1) uniform mat4 transRel;
layout (location = 2) uniform mat4 scale;
uniform mat4 projection;

void main() {
    gl_Position = projection * transOrigin /*rotate*/ * transRel * scale * vec4(vPosition.xy, 0.0, 1.0);
    aTexCoord = vTexCoord.xy;
}
