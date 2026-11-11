// Skybox for the demo: a dusty gradient and the planet's two suns.
// Sun directions and colours come from the scene's lights (see SkySuns.cs), so the
// discs always sit where the light and shadows come from. Sharp discs, no halo:
// the brief bans anything that glows.
Shader "Stealth Raider/Two Sun Sky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.34, 0.21, 0.16, 1)
        _HorizonColor ("Horizon haze (match the fog)", Color) = (0.55, 0.38, 0.28, 1)
        _BelowColor ("Below horizon", Color) = (0.36, 0.22, 0.16, 1)
        _HazeHeight ("Haze height", Range(0.05, 1)) = 0.5

        _Sun1Radius ("Primary sun radius (degrees)", Range(0.2, 10)) = 1.6
        _Sun2Radius ("Secondary sun radius (degrees)", Range(0.2, 10)) = 3.4
        _SunEdge ("Disc edge softness (degrees)", Range(0.01, 1)) = 0.15
        _SunBrightness ("Disc brightness", Range(0.2, 3)) = 1.1

        _DustTint ("Dust tint around the secondary sun", Range(0, 1)) = 0.3
        _DustBands ("Dust bands near the horizon", Range(0, 0.2)) = 0.035
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _BelowColor;
                float _HazeHeight;
                float _Sun1Radius;
                float _Sun2Radius;
                float _SunEdge;
                float _SunBrightness;
                float _DustTint;
                float _DustBands;
            CBUFFER_END

            // Set every frame by SkySuns: direction towards each sun (world space) and its colour.
            float3 _DA_SunDir1;
            float3 _DA_SunDir2;
            half3 _DA_SunColor1;
            half3 _DA_SunColor2;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // The skybox mesh is centred on the camera, so its vertex is the view direction.
                output.direction = input.positionOS.xyz;
                return output;
            }

            // 1 inside the disc, 0 outside, with a thin soft edge. Angles in radians.
            float Disc(float3 direction, float3 sunDirection, float radius, float edge)
            {
                float angle = acos(clamp(dot(direction, normalize(sunDirection)), -1.0, 1.0));
                return 1.0 - smoothstep(radius - edge, radius + edge, angle);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float height = direction.y;

                // Haze sits on the horizon and thins upwards into the dark zenith.
                float up = smoothstep(0.0, _HazeHeight, height);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, sqrt(up));
                sky = lerp(sky, _BelowColor.rgb, saturate(-height * 6.0));

                // Faint wavy dust bands low in the sky, so the gradient doesn't look perfect.
                float azimuth = atan2(direction.x, direction.z);
                float bands = sin(height * 70.0 + sin(azimuth * 3.0) * 1.5);
                sky *= 1.0 + bands * _DustBands * (1.0 - up);

                // The secondary sun stains the dust around it, without a glowing ring.
                float nearSun2 = pow(saturate(dot(direction, normalize(_DA_SunDir2))), 12.0);
                sky = lerp(sky, sky * (0.6 + _DA_SunColor2 * 0.8), nearSun2 * _DustTint);

                float edge = radians(_SunEdge);
                float sun2 = Disc(direction, _DA_SunDir2, radians(_Sun2Radius), edge);
                float sun1 = Disc(direction, _DA_SunDir1, radians(_Sun1Radius), edge);
                sky = lerp(sky, _DA_SunColor2 * _SunBrightness, sun2);
                sky = lerp(sky, _DA_SunColor1 * _SunBrightness, sun1);

                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
