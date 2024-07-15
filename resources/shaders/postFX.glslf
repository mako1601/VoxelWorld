#version 460 core

out vec4 fColor;

uniform sampler2D texture0;
uniform float screenWidth;
uniform float screenHeight;

float getDepth(vec2 texCoords) {
    return texture(texture0, texCoords).r;
}

void main() {
    vec2 texCoords = gl_FragCoord.xy / vec2(screenWidth, screenHeight);
    
    float depth = getDepth(texCoords);
    float contour = 0.0;
    
    // ��������� ������� �������� ��������
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            if ( x == 0 && y == 0) continue;
            
            float neighborDepth = getDepth(texCoords + vec2(x, y) / vec2(screenWidth, screenHeight));
            if (abs(depth - neighborDepth) > 0.01) { // ��������� �������� ��� �������
                contour = 1.0;
            }
        }
    }
    
    if (contour > 0.0) {
        fColor = vec4(1.0, 0.0, 0.0, 1.0); // ������ ������� ������
    }
    else {
        discard; // ���������� ��������� ��� �������
    }
}
