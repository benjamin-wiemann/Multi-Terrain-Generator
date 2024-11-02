// This shader fills the mesh shape with a color predefined in the code.
Shader "Terrain/PatchShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // flip UVs horizontally to correct for back side projection
            #define TRIPLANAR_CORRECT_PROJECTED_U

            // offset UVs to prevent obvious mirroring
            // #define TRIPLANAR_UV_OFFSET

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
            };

            Texture2D _MainTex;
            
            float4 _MainTex_ST;

            Texture2D _BumpMap;

            SamplerState _Sampler_MainTex_Trilinear_Repeat;

            half4 _LightColor0;


            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz).xyz;//mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1)).xyz;
                o.worldNormal = TransformObjectToWorldNormal(v.normal);

                return o;
            }

            // Reoriented Normal Mapping
            // http://blog.selfshadow.com/publications/blending-in-detail/
            // Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
            half3 blend_rnm(half3 n1, half3 n2)
            {
                n1.z += 1;
                n2.xy = -n2.xy;

                return n1 * dot(n1, n2) / n1.z - n2;
            }

            half4 frag (v2f i) : SV_Target
            {
                // calculate triplanar blend
                half3 triblend = saturate(pow(i.worldNormal, 4));
                triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

                // preview blend
                // return half4(triblend.xyz, 1);

                // calculate triplanar uvs
                // applying texture scale and offset values ala TRANSFORM_TEX macro
                float2 uvX = i.worldPos.zy * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvY = i.worldPos.xz * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 uvZ = i.worldPos.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                // offset UVs to prevent obvious mirroring
            #if defined(TRIPLANAR_UV_OFFSET)
                uvY += 0.33;
                uvZ += 0.67;
            #endif

                // minor optimization of sign(). prevents return value of 0
                half3 axisSign = i.worldNormal < 0 ? -1 : 1;

                // flip UVs horizontally to correct for back side projection
            #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
                uvX.x *= axisSign.x;
                uvY.x *= axisSign.y;
                uvZ.x *= -axisSign.z;
            #endif

                // albedo textures
                half4 colX = _MainTex.Sample(_Sampler_MainTex_Trilinear_Repeat, uvX);
                half4 colY = _MainTex.Sample(_Sampler_MainTex_Trilinear_Repeat, uvY);
                half4 colZ = _MainTex.Sample(_Sampler_MainTex_Trilinear_Repeat, uvZ);
                half4 col = colX * triblend.x + colY * triblend.y + colZ * triblend.z;

                // tangent space normal maps
                half3 tnormalX = _BumpMap.Sample(_Sampler_MainTex_Trilinear_Repeat, uvX).xyz;
                half3 tnormalY = _BumpMap.Sample(_Sampler_MainTex_Trilinear_Repeat, uvY).xyz;
                half3 tnormalZ = _BumpMap.Sample(_Sampler_MainTex_Trilinear_Repeat, uvZ).xyz;

                // flip normal maps' x axis to account for flipped UVs
            #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
                tnormalX.x *= axisSign.x;
                tnormalY.x *= axisSign.y;
                tnormalZ.x *= -axisSign.z;
            #endif

                half3 absVertNormal = abs(i.worldNormal);

                // swizzle world normals to match tangent space and apply reoriented normal mapping blend
                tnormalX = blend_rnm(half3(i.worldNormal.zy, absVertNormal.x), tnormalX);
                tnormalY = blend_rnm(half3(i.worldNormal.xz, absVertNormal.y), tnormalY);
                tnormalZ = blend_rnm(half3(i.worldNormal.xy, absVertNormal.z), tnormalZ);

                // apply world space sign to tangent space Z
                tnormalX.z *= axisSign.x;
                tnormalY.z *= axisSign.y;
                tnormalZ.z *= axisSign.z;

                // sizzle tangent normals to match world normal and blend together
                half3 worldNormal = normalize(
                    tnormalX.zyx * triblend.x +
                    tnormalY.xzy * triblend.y +
                    tnormalZ.xyz * triblend.z
                    );

                // preview world normals
                return half4(worldNormal * 0.5 + 0.5, 1);
                               

            }
            ENDHLSL
        }
    }
}