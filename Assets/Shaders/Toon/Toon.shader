// Toon.shader
//
// Stylized toon shader for URP, hand-written HLSL (not Shader Graph -- see
// project notes for why). Two passes:
//   1. Outline -- inverted hull: the mesh is duplicated, pushed outward along
//      vertex normals by _OutlineWidth, and only its BACK faces are drawn
//      (front-face culled), so it appears as a rim around the silhouette
//      sitting behind the real surface. A low-frequency noise term jitters
//      the push distance per-vertex (stable across frames, since it's keyed
//      off object-space position, not time) for the "hand-drawn, not
//      perfectly even" look the design doc asks for, rather than a clean
//      machine-perfect outline.
//   2. Toon lit -- quantizes the main light's N dot L term into a small number
//      of hard bands instead of a smooth gradient, plus a rim-light term,
//      giving the flat-shaded cel-shaded look.
//
// Artist-facing: every property below shows up in the Inspector on any
// Material using this shader -- no code changes needed to retint, adjust
// band count, or change outline thickness per-material.
Shader "Custom/Toon"
{
    Properties
    {
        [Header(Base Surface)]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}

        [Header(Toon Lighting)]
        [IntRange] _LightBands ("Light Bands", Range(1, 6)) = 3
        _ShadowColor ("Shadow Tint", Color) = (0.55, 0.55, 0.65, 1)
        _BandSoftness ("Band Edge Softness", Range(0.001, 0.3)) = 0.02

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.4

        [Header(Buff Miniboss Glow Runtime Driven)]
        _GlowColor ("Glow Color", Color) = (1, 0.85, 0.2, 1)
        _GlowIntensity ("Glow Intensity (Script Driven)", Range(0, 3)) = 0
        _GlowPulseSpeed ("Glow Pulse Speed", Range(0, 10)) = 2.5
        _GlowPulseMinScale ("Glow Pulse Min Scale", Range(0, 1)) = 0.6

        [Header(Hand Drawn Outline)]
        _OutlineColor ("Outline Color", Color) = (0.05, 0.05, 0.08, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.015
        _OutlineWobbleAmount ("Outline Wobble Amount", Range(0, 0.02)) = 0.004
        _OutlineWobbleFrequency ("Outline Wobble Frequency", Range(0.1, 20)) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        // ------------------------------------------------------------
        // Pass 0: Outline (inverted hull, back faces only)
        // ------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineWobbleAmount;
                float _OutlineWobbleFrequency;
            CBUFFER_END

            // Cheap deterministic hash, stable per-vertex (keyed off object-space
            // position) so the wobble doesn't shimmer/flicker as the object moves
            // or the camera rotates -- a real hand-drawn line is imperfect but
            // doesn't crawl around.
            float HashPositionToWobble(float3 positionOS)
            {
                float3 scaled = positionOS * _OutlineWobbleFrequency;
                float n = sin(dot(scaled, float3(12.9898, 78.233, 37.719))) * 43758.5453;
                return frac(n) * 2.0 - 1.0; // remap 0..1 to -1..1
            }

            OutlineVaryings OutlineVert(OutlineAttributes IN)
            {
                OutlineVaryings OUT;

                float wobble = HashPositionToWobble(IN.positionOS.xyz);
                float pushDistance = _OutlineWidth + wobble * _OutlineWobbleAmount;

                float3 pushedPositionOS = IN.positionOS.xyz + normalize(IN.normalOS) * pushDistance;
                OUT.positionCS = TransformObjectToHClip(pushedPositionOS);
                return OUT;
            }

            half4 OutlineFrag(OutlineVaryings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // Pass 1: Toon-lit main surface
        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct ToonAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct ToonVaryings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _LightBands;
                half4 _ShadowColor;
                float _BandSoftness;
                half4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                half4 _GlowColor;
                float _GlowIntensity;
                float _GlowPulseSpeed;
                float _GlowPulseMinScale;
            CBUFFER_END

            ToonVaryings ToonVert(ToonAttributes IN)
            {
                ToonVaryings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(positionInputs);

                return OUT;
            }

            // Quantizes a smooth 0..1 light value into _LightBands hard steps,
            // with a soft transition (smoothstep) at each step edge rather than
            // an aliased hard cut -- this is what gives toon shading its
            // characteristic "flat shapes with a visible but not jagged edge"
            // look instead of either a smooth PBR gradient or a harshly
            // pixelated one.
            half QuantizeLight(half lightAmount, float bandCount, float softness)
            {
                // Clamp the position itself first -- at lightAmount == 1.0 exactly,
                // bandPosition would equal bandCount, pushing bandIndex one band
                // past the top band and producing a quantized value slightly
                // above 1.0 once divided back down. Clamping the position keeps
                // the result inside [0, bandCount - 1] before quantizing.
                float bandPosition = clamp(lightAmount * bandCount, 0.0, bandCount - 0.0001);
                float bandIndex = floor(bandPosition);
                float bandFraction = bandPosition - bandIndex;

                float edge = smoothstep(0.5 - softness * bandCount, 0.5 + softness * bandCount, bandFraction);
                return saturate((bandIndex + edge) / bandCount);
            }

            half4 ToonFrag(ToonVaryings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                Light mainLight = GetMainLight(IN.shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half shadowAttenuation = mainLight.shadowAttenuation;
                half quantizedLight = QuantizeLight(NdotL * shadowAttenuation, _LightBands, _BandSoftness);

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Lerp between the shadow tint and full base color/light color,
                // rather than multiplying by black -- toon shadows are usually a
                // tinted color, not a pure darkening, which reads as more
                // intentional/stylized than a realistic occlusion falloff.
                half3 litColor = lerp(baseColor.rgb * _ShadowColor.rgb, baseColor.rgb * mainLight.color, quantizedLight);

                // Rim light: brightens silhouette edges (where the surface normal
                // points away from the viewer), a common cel-shading accent that
                // helps shapes read clearly against busy backgrounds -- useful
                // here given how many enemies/VFX can be on screen at once.
                half rimAmount = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimContribution = _RimColor.rgb * rimAmount * _RimIntensity * quantizedLight;

                // Buff/miniboss glow -- folded into THIS pass rather than a
                // second material/renderer, since duplicating geometry per
                // enemy (or even a baked-in second submesh on every enemy
                // prefab) costs draw calls and vertices for every enemy at
                // all times, just to support the minority that are ever
                // buffed. _GlowIntensity defaults to 0 (driven up at runtime
                // by script via MaterialPropertyBlock) so unbuffed enemies
                // pay only a few extra ALU ops, no extra geometry or passes.
                // Reuses the rim's fresnel term -- glow reads as "the rim
                // light itself turning a different color and brightening,"
                // a cheap and visually coherent way to layer the effect.
                float pulse = lerp(_GlowPulseMinScale, 1.0, (sin(_Time.y * _GlowPulseSpeed) + 1.0) * 0.5);
                half3 glowContribution = _GlowColor.rgb * rimAmount * _GlowIntensity * pulse;

                half3 finalColor = litColor + rimContribution + glowContribution;
                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
