#version 450

// One bubble layer of CompBubbleOverlay: the layer is scaled around its pivot and shows only where the mask is red.
// The mask stays put; only the layer moves. _Bubble is xy pivot, z one over scale, w opacity.

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;      // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _Bubble;     // @default = vec4(0.5, 0.5, 1.0, 1.0)
} u;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;

void main()
{
    // Mesh UVs start at the bottom left, as the pivots do. _MainTex_ST then maps them onto the texture, which also
    // flips them to how the texture is stored, so mask and layer go through it the same way.
    vec2 layerUv = u._Bubble.xy + (vUv - u._Bubble.xy) * u._Bubble.z;
    if (any(lessThan(layerUv, vec2(0.0))) || any(greaterThan(layerUv, vec2(1.0))))
        discard;

    float mask = step(0.5, texture(_MaskTex, vUv * u._MainTex_ST.zw + u._MainTex_ST.xy).r);
    vec4 color = texture(_MainTex, layerUv * u._MainTex_ST.zw + u._MainTex_ST.xy) * u._Color * vColor;
    color.a *= mask * u._Bubble.w;
    if (color.a <= 0.001)
        discard;

    outColor = color;
}
