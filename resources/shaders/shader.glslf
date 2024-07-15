#version 460 core

in vec3 aTexCoord;
in vec4 aColor;
in vec3 aFragPos;

out vec4 fColor;

uniform sampler2DArray uTexture;

// White textures
uniform bool IsWhiteWorld;

// Fog
uniform float farPlane = 128.0;
uniform float nearPlane = 130.0;
uniform vec3 viewPos;
uniform vec3 fogColor = vec3(0.8, 0.9, 1.0);

float getFogFactor(float distance, float near, float far) {
    float FogMax = 1.0 * far;
    float FogMin = 0.5 * near;

    if (distance >= FogMax) return 1.0;
    if (distance <= FogMin) return 0.0;

    return 1.0 - (FogMax - distance) / (FogMax - FogMin);
}

float getFogFactorAlpha(float distance, float near, float far) {
    float FogMax = 1.0 * far;
    float FogMin = 0.7 * near;

    if (distance >= FogMax) return 1.0;
    if (distance <= FogMin) return 0.0;

    return 1.0 - (FogMax - distance) / (FogMax - FogMin);
}

void main() {
    if (IsWhiteWorld == true) {
        fColor = aColor * vec4(1.0);
    }
    else {
        vec4 texColor = texture(uTexture, aTexCoord);
        if (texColor.a < 0.5) discard;

        float distance = distance(viewPos, aFragPos);
        float fogFactor = getFogFactor(distance, nearPlane, farPlane);
        float alpha = getFogFactorAlpha(distance, nearPlane, farPlane);

        fColor = aColor * texColor * mix(aColor, vec4(fogColor, 1.0), fogFactor) * (1 - alpha);
    }
}
