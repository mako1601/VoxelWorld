#version 460 core

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vTexCoord; // not vec2, because xy = texCoord, z = texID
layout (location = 2) in vec4 vLight;

out vec3  aTexCoord;
out vec4  aColor;
out float aDistance;

uniform mat4  uModel;
uniform mat4  uView;
uniform mat4  uProjection;
uniform vec3  uViewPos;
uniform float uGamma;
uniform vec3  uSkyLightColor;

void main() {
    aTexCoord = vTexCoord;

    aColor = vec4(pow(vLight.rgb, vec3(uGamma)), 1.0);
    aColor.rgb = max(aColor.rgb, uSkyLightColor * vLight.a);

    vec3 pos3D = (uModel * vec4(vPosition, 1.0)).xyz - uViewPos.xyz;
    aDistance = length(uView * uModel * vec4(pos3D.x, 0.2 * pos3D.y, pos3D.z, 0.0));

    gl_Position = uProjection * uView * uModel * vec4(vPosition, 1.0);
}
