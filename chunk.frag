#version 330

in vec3 fragNormal;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;

out vec4 finalColor;

float faceShade(vec3 n) {
    if (abs(n.y) > 0.5) return 1.0;
    if (abs(n.x) > 0.5) return 0.8;
    return 0.6;
}

void main() {
    float diff = max(dot(fragNormal, lightDir), 0.0);
    vec3 lighting = ambientColor + lightColor * diff;
    finalColor = vec4(lighting * faceShade(fragNormal), 1.0);
}