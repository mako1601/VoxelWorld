#version 460 core

in vec3  aTexCoord;
in vec4  aColor;
in float aDistance;

out vec4 fColor;

uniform bool           uIsWhiteWorld;
uniform vec3           uFogColor;
uniform float          uFogFactor;
uniform float          uFogCurve;
uniform sampler2DArray uTexture;

void main() {
    if (uIsWhiteWorld)
        fColor = aColor * vec4(1.0);
    else {
        vec4 texColor = texture(uTexture, aTexCoord);
        float depth = aDistance / 256.0;
        float alpha = aColor.a * texColor.a;
        if (alpha < 0.1) discard;
        fColor = mix(aColor * texColor, vec4(uFogColor, 1.0), min(1.0, pow(depth * uFogFactor, uFogCurve)));
        fColor.a = alpha;
    }
}
