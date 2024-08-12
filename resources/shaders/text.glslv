#version 460 core

layout (location = 0) in vec2 vPosition;
layout (location = 1) in vec2 vTexCoord;

out vec2 aTexCoord;

layout (location = 0) uniform mat4 uTransOrigin;
//layout (location = 1) uniform mat4 uRotate;
layout (location = 2) uniform mat4 uTransRel;
layout (location = 3) uniform mat4 uScale;
uniform mat4 uProjection;

void main() {
    gl_Position = uProjection * uTransOrigin /*uRotate*/ * uTransRel * uScale * vec4(vPosition.xy, 0.0, 1.0);
    aTexCoord = vTexCoord.xy;
}
