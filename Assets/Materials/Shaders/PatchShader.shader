// This shader is responsible for the texture sampling and PBR in general. 
Shader "Terrain/PatchShader"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [Toggle(_NORMALMAP)] _NormalMapToggle ("Use Normal Map", Float) = 0
        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale("Bump Scale", Float) = 1

        [NoScaleOffset] _SpecGlossMap ("Specular Map", 2D) = "specular" {}
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.3
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        [NoScaleOffset] _OcclusionMap("Occlusion Map", 2D) = "occlusion" {}
		_OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 0.5

        [Toggle(_HEIGHTMAP)] _HeightMapToggle ("Use Height Map", Float) = 0
        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "height" {}
        _HeightScale("HeightScale", Float) = 0
        
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        Tags {
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}
        
        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
		float4 _BaseMap_ST;
		float4 _SpecColor;
		float _Smoothness;
		float _OcclusionStrength;
		float _BumpScale;
        float _HeightScale;
		CBUFFER_END
		ENDHLSL

        // LOD 100

        Pass
        {
            Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"

            // Shadows
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _HEIGHTMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // Baked Lightmap
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // Other
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION

            // flip UVs horizontally to correct for back side projection
            // #define TRIPLANAR_CORRECT_PROJECTED_U

            // offset UVs to prevent obvious mirroring
            #define TRIPLANAR_UV_OFFSET

            TEXTURE2D(_SpecGlossMap); 	SAMPLER(sampler_SpecGlossMap);
            TEXTURE2D(_OcclusionMap); 	SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_HeightMap);      SAMPLER(sampler_HeightMap);

            struct VertexInput
            {
                float4 positionOS   : POSITION;
                //
                float3 normalOS      : NORMAL;
                half4 tangentOS     : TANGENT;
                float2 lightmapUV	: TEXCOORD1;
            };

            struct FragmentInput
            {
                float4 posCS        : SV_POSITION;
                float3 posWS        : TEXCOORD0;
                half3 normalWS		: TEXCOORD1;    
				half3 tangentWS		: TEXCOORD2;    
				half3 bitangentWS	: TEXCOORD3;
                // half3 viewDirWS       : TEXCOORD4;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					float4 shadowCoord 				: TEXCOORD5;
				#endif
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
            };

            // used to pass triplanar uv coordinates ( two coordinates for each axis )
            struct TriplanarUV
            {
                float2 x;
                float2 y;
                float2 z;
            };


            FragmentInput vert (VertexInput vertIn)
            {
                FragmentInput fragIn;
                fragIn.posCS = TransformObjectToHClip(vertIn.positionOS.xyz);
                fragIn.posWS = TransformObjectToWorld(vertIn.positionOS.xyz);
                fragIn.tangentWS = TransformObjectToWorldDir(vertIn.tangentOS.xyz);                
                fragIn.normalWS = TransformObjectToWorldNormal(vertIn.normalOS);
                fragIn.bitangentWS = TransformObjectToWorldDir(cross( vertIn.tangentOS.xyz * vertIn.tangentOS.w, vertIn.normalOS.xyz )  );
                // fragIn.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    fragIn.shadowCoord = TransformWorldToShadowCoord(fragIn.posWS);
                #endif

                OUTPUT_LIGHTMAP_UV(vertIn.lightmapUV, unity_LightmapST, fragIn.lightmapUV);
				OUTPUT_SH(fragIn.normalWS, fragIn.vertexSH);

                return fragIn;
            }

            // Reoriented Normal Mapping
            // http://blog.selfshadow.com/publications/blending-in-detail/
            // Altered to take normals (-1 to 1 ranges) rather than unsigned bitangentOS maps (0 to 1 ranges)
            half3 blend_rnm(half3 n1, half3 n2)
            {
                n1.z += 1;
                n2.xy = -n2.xy;

                return n1 * dot(n1, n2) / n1.z - n2;
            }

            TriplanarUV GenerateTriplanarUV(FragmentInput fragIn, half3 triblend, half3x3 worldToTangent )
            {               

                // calculate triplanar uvs
                // applying texture scale and offset values ala TRANSFORM_TEX macro
                TriplanarUV triUV = (TriplanarUV) 0;
                triUV.x = fragIn.posWS.zy * _BaseMap_ST.xy + _BaseMap_ST.zw;
                triUV.y = fragIn.posWS.xz * _BaseMap_ST.xy + _BaseMap_ST.zw;
                triUV.z = fragIn.posWS.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

                // half viewDirXTS = fragIN.posWS
                // triUV.x = triUV.x + ParallaxMapping(_Heightmap, sampler_Heightmap, IN.TangentSpaceViewDirection, _HeightScale * 0.01, triUV.x);


                // offset UVs to prevent obvious mirroring
            #if defined(TRIPLANAR_UV_OFFSET)
                triUV.y += 0.33;
                triUV.z += 0.67;
            #endif

                // minor optimization of sign(). prevents return value of 0
                half3 axisSign = fragIn.normalWS.xyz < 0 ? -1 : 1;

                // flip UVs horizontally to correct for back side projection
            #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
                triUV.x.x *= axisSign.x;
                triUV.y.x *= axisSign.y;
                triUV.z.x *= -axisSign.z;
            #endif
                
                return triUV; 
            }

            

            void InitializeInputData(FragmentInput fragIn, half3 normalWS, out InputData inputData) {
	            inputData = (InputData) 0; 

	            inputData.positionWS = fragIn.posWS;	

	            inputData.normalWS = NormalizeNormalPerPixel(normalWS);
	            inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);

	        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		        inputData.shadowCoord = fragIn.shadowCoord;
	        #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
	        #else
		        inputData.shadowCoord = float4(0, 0, 0, 0);
	        #endif

	        // Fog
	        // #ifdef _ADDITIONAL_LIGHTS_VERTEX
		    //     inputData.fogCoord = fragIn.fogFactorAndVertexLight.x;
    	    //     inputData.vertexLighting = fragIn.fogFactorAndVertexLight.yzw;
	        // #else
		    //     inputData.fogCoord = fragIn.fogFactor;
		    //     inputData.vertexLighting = half3(0, 0, 0);
	        // #endif

	            inputData.bakedGI = SAMPLE_GI(fragIn.lightmapUV, fragIn.vertexSH, inputData.normalWS);
	            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(fragIn.posCS);
	            inputData.shadowMask = SAMPLE_SHADOWMASK(fragIn.lightmapUV);
            }

            half4 SampleSpecGloss(float2 uv) {
                half4 specGloss = 0;
                // #ifdef _SPECGLOSSMAP
                    specGloss = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv);
                    specGloss.a *= _Smoothness;                    
                // #endif
                return specGloss;
            }

            half SampleOcclusion(float2 uv) {
                // #ifdef _OCCLUSIONMAP
                    #if defined(SHADER_API_GLES)
                        return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                    #else
                        half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
                        return LerpWhiteTo(occ, _OcclusionStrength);
                    #endif
                // #else
                //     return 1.0;
                // #endif
            }


            void InitializeSurfaceData(FragmentInput fragIn, out SurfaceData surfaceData, out half3 normalWS){
                surfaceData = (SurfaceData)0; // avoids "not completely initalized" errors

                // calculate triplanar blend
                half3 triblend = saturate(pow(fragIn.normalWS, 4));
                triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

                half3x3 worldToTangent = transpose(float3x3(fragIn.tangentWS, fragIn.bitangentWS, fragIn.normalWS));

                TriplanarUV triUV = GenerateTriplanarUV( fragIn, triblend, worldToTangent );           

                // albedo textures
                half4 colX = SampleAlbedoAlpha(triUV.x, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half4 colY = SampleAlbedoAlpha(triUV.y, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                half4 colZ = SampleAlbedoAlpha(triUV.z, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
                surfaceData.albedo = colX.rgb * triblend.x + colY.rgb * triblend.y + colZ.rgb * triblend.z;
                // currently not supporting alpha values
                surfaceData.alpha = 1;

                // tangent space normal maps
                half3 normalTSX = SampleNormal(triUV.x, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
                half3 normalTSY = SampleNormal(triUV.y, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
                half3 normalTSZ = SampleNormal(triUV.z, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
                half3 axisSign = fragIn.normalWS < 0 ? -1 : 1;

                // flip normal maps' x axis to account for flipped UVs
                #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
                    normalTSX.x *= axisSign.x;
                    normalTSY.x *= axisSign.y;
                    normalTSZ.x *= -axisSign.z;
                #endif

                half3 absVertNormal = abs(fragIn.normalWS);

                // swizzle world normals to match tangent space and apply reoriented normal mapping blend
                normalTSX = blend_rnm(half3(fragIn.normalWS.zy, absVertNormal.x), normalTSX);
                normalTSY = blend_rnm(half3(fragIn.normalWS.xz, absVertNormal.y), normalTSY);
                normalTSZ = blend_rnm(half3(fragIn.normalWS.xy, absVertNormal.z), normalTSZ);

                // apply world space sign to tangent space Z
                normalTSX.z *= axisSign.x;
                normalTSY.z *= axisSign.y;
                normalTSZ.z *= axisSign.z;

                // // sizzle tangent normals to match world normal and blend together
                normalWS = normalize(
                    normalTSX.zyx * triblend.x +
                    normalTSY.xzy * triblend.y +
                    normalTSZ.xyz * triblend.z
                    );
        
                surfaceData.normalTS = TransformWorldToTangent(normalWS, worldToTangent);

                half occlusionX = SampleOcclusion(triUV.x);
                half occlusionY = SampleOcclusion(triUV.y);
                half occlusionZ = SampleOcclusion(triUV.z);
                surfaceData.occlusion = occlusionX * triblend.x + occlusionY * triblend.y + occlusionZ * triblend.z;
                                
                half4 specGlossX = SampleSpecGloss(triUV.x);
                half4 specGlossY = SampleSpecGloss(triUV.y);
                half4 specGlossZ = SampleSpecGloss(triUV.z);
                half4 specGloss = specGlossX * triblend.x + specGlossY * triblend.y + specGlossZ * triblend.z;
                surfaceData.specular = specGloss.rgb;               
                surfaceData.smoothness = specGloss.a;

                surfaceData.emission = 0; 
                surfaceData.metallic = 0.0h;                            
                
            }


            half4 frag (FragmentInput fragIn) : SV_Target
            {                 
	            
				SurfaceData surfaceData;
                InputData inputData;
                half3 normalWS;
                // Setup SurfaceData
				InitializeSurfaceData(fragIn, surfaceData, normalWS);
				// Setup InputData				
				InitializeInputData(fragIn, normalWS, inputData);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
                // half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

				// Fog
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				
				// return half4(fragIn.normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(inputData.normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(surfaceData.albedo, 0);
                // return half4(surfaceData.occlusion, 0,0, 0);
                // return half4(surfaceData.specular, 0);
                // return half4(surfaceData.normalTS *0.5 + 0.5, 0);                
                // return half4(inputData.viewDirectionWS.xyz * 0.5 + 0.5, 0);
                // half4(inputData.viewDirectionWS.xyz * 0.5 + 0.5, 0);
                return color;

            }
            ENDHLSL
        }

        Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			// GPU Instancing
			#pragma multi_compile_instancing
			//#pragma multi_compile _ DOTS_INSTANCING_ON

			// Universal Pipeline Keywords
			// (v11+) This is used during shadow map generation to differentiate between directional and punctual (point/spot) light shadows, as they use different formulas to apply Normal Bias
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}