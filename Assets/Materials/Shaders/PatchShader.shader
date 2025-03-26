// This shader is responsible for the texture sampling and PBR in general. 
Shader "Terrain/PatchShader"
{
   
    SubShader
    {
        Tags {
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}
        
        HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define MAX_NUMBER_MATERIALS 100
		
        int _MeshResolution;
        float _MeshX;
        float _MeshZ;
        int _SubmeshSplitLevel;

		float4 _BaseMap_ST[MAX_NUMBER_MATERIALS];
		float3 _SpecColor[MAX_NUMBER_MATERIALS];
        float _Smoothness[MAX_NUMBER_MATERIALS];
		float _OcclusionStrength[MAX_NUMBER_MATERIALS];
		float _BumpScale[MAX_NUMBER_MATERIALS];
        float _HeightScale[MAX_NUMBER_MATERIALS];
        float _HeightmapBlending[MAX_NUMBER_MATERIALS];
              

		ENDHLSL

        // LOD 100

        Pass
        {
            Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            TEXTURE2D_ARRAY(_BaseMap); 	    SAMPLER(sampler_BaseMap);
            TEXTURE2D_ARRAY(_BumpMap); 	    SAMPLER(sampler_BumpMap);
            TEXTURE2D_ARRAY(_SpecularMap); 	SAMPLER(sampler_SpecularMap);
            TEXTURE2D_ARRAY(_SmoothnessMap); SAMPLER(sampler_SmoothnessMap);
            TEXTURE2D_ARRAY(_OcclusionMap); 	SAMPLER(sampler_OcclusionMap);
            TEXTURE2D_ARRAY(_HeightMap);     SAMPLER(sampler_HeightMap);

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
            #include "Structs.hlsl"
            #include "Sampling.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            // Shadows
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _SPECULARMAP
            #pragma shader_feature_local _HEIGHTBASEDTRIBLEND
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
                        
            StructuredBuffer<TerrainWeighting> _TerrainMap;

            FragmentInput vert (VertexInput vertIn)
            {
                FragmentInput fragIn;
                fragIn.posCS = TransformObjectToHClip(vertIn.positionOS.xyz);
                fragIn.posWS = TransformObjectToWorld(vertIn.positionOS.xyz);
                fragIn.tangentWS = half4(TransformObjectToWorldDir(vertIn.tangentOS.xyz), vertIn.tangentOS.w);                
                fragIn.normalWS = TransformObjectToWorldNormal(vertIn.normalOS);
                fragIn.bitangentWS = TransformObjectToWorldDir(cross( vertIn.tangentOS.xyz * vertIn.tangentOS.w, vertIn.normalOS.xyz ));
                // fragIn.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    fragIn.shadowCoord = TransformWorldToShadowCoord(fragIn.posWS);
                #endif

                OUTPUT_LIGHTMAP_UV(vertIn.lightmapUV, unity_LightmapST, fragIn.lightmapUV);
				OUTPUT_SH(fragIn.normalWS, fragIn.vertexSH);
                fragIn.terrainCoordinateOS = half2(vertIn.positionOS.xz * _MeshResolution);
                
                return fragIn;
            }

            // Simple triblend generation
            void GenerateTriblend(half3 normalWS, out half3 triblend)
            {                
                triblend = saturate(pow(normalWS, 4));
                triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);
            }

            // Triblend based on height information from height maps
            // https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#ce80
            void GenerateHeightTriblend(half3 normalWS, float3 heights, out half3 triblend)
            {                
                triblend = abs(normalWS.xyz);
                triblend /= dot(triblend, float3(1,1,1));                
                heights += (triblend * 3.0);
                float heightStart = max(max(heights.x, heights.y), heights.z) - _HeightmapBlending[0];
                float3 h = max(heights - heightStart.xxx, float3(0,0,0));
                triblend = h / dot(h, float3(1,1,1));
            }

            // Generates triplanar UV according to 
            // https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#ce80
            TriplanarUV GenerateTriplanarUV(FragmentInput fragIn)
            {               

                TriplanarUV triUV = (TriplanarUV) 0;
                triUV.x = fragIn.posWS.zy * _BaseMap_ST[0].xy + _BaseMap_ST[0].zw;
                triUV.y = fragIn.posWS.xz * _BaseMap_ST[0].xy + _BaseMap_ST[0].zw;
                triUV.z = fragIn.posWS.xy * _BaseMap_ST[0].xy + _BaseMap_ST[0].zw;

                // offset UVs to prevent obvious mirroring
            #if defined(TRIPLANAR_UV_OFFSET)
                triUV.y += 0.33;
                triUV.z += 0.67;
            #endif

                // sign function which never returns 0
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


            void InitializeSurfaceData(FragmentInput fragIn, out SurfaceData surfaceData, out half3 normalWS, out half3 triblend, out half3 viewDirTS){
                surfaceData = (SurfaceData)0; 

                               
                TriplanarUV triUV = GenerateTriplanarUV( fragIn);

                TerrainWeighting weighting = _TerrainMap[(int) round(fragIn.terrainCoordinateOS.y *_MeshX) + (int) round(fragIn.terrainCoordinateOS.x)];
                int4 terrainIndices = weighting.indices;
                float4 terrainIntensities = weighting.intensities;
                normalWS = fragIn.normalWS;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    if (i >= _SubmeshSplitLevel) break;
                    
                    int terrainIndex = terrainIndices[i];
                
                    #ifdef _HEIGHTMAP
                        half3 heights = half3(
                            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.x, terrainIndex).r,
                            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.y, terrainIndex).r,
                            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.z, terrainIndex).r);
                        #ifdef _HEIGHTBASEDTRIBLEND
                            GenerateHeightTriblend( fragIn.normalWS, heights, triblend);
                        #else
                            GenerateTriblend(fragIn.normalWS, triblend);
                        #endif
                        half3x3 worldToTangentX = transpose(float3x3(fragIn.normalWS, fragIn.bitangentWS, fragIn.tangentWS.xyz));
                        half3x3 worldToTangentY = transpose(float3x3(fragIn.tangentWS.xyz, fragIn.normalWS, fragIn.bitangentWS));
                        half3x3 worldToTangentZ = transpose(float3x3(fragIn.tangentWS.xyz, fragIn.bitangentWS, fragIn.normalWS));
                        // viewDirTSX = GetViewDirectionTangentSpace(fragIn.tangentWS, fragIn.normalWS, GetWorldSpaceNormalizeViewDir(fragIn.posWS));
                        // half3 viewDirTSX = TransformWorldToTangentDir(GetWorldSpaceNormalizeViewDir(fragIn.posWS), worldToTangentX);
                        // half3 viewDirTSY = TransformWorldToTangentDir(GetWorldSpaceNormalizeViewDir(fragIn.posWS), worldToTangentY);

                        half3 viewDirTSZ = TransformWorldToTangentDir(GetWorldSpaceNormalizeViewDir(fragIn.posWS), worldToTangentZ);       
                        // triUV.x -= viewDirTSZ.zy / viewDirTSZ.x * (heights.x * _HeightScale);
                        // triUV.y -= viewDirTSZ.xz / viewDirTSZ.y * (heights.y * _HeightScale);
                        // triUV.z -= viewDirTSZ.xy / viewDirTSZ.z * (heights.z * _HeightScale);

                        // triUV.x -= ParallaxOffset1Step( heights.x, _HeightScale, half3(viewDirTSZ.zy, viewDirTSZ.x));
                        // triUV.y -= ParallaxOffset1Step( heights.y, _HeightScale, half3(viewDirTSZ.xz, viewDirTSZ.y));                    
                        // triUV.z -= ParallaxOffset1Step( heights.z, _HeightScale, viewDirTSZ);
                        viewDirTS = half3(viewDirTSZ.zy, viewDirTSZ.x);  
                    #else 
                        GenerateTriblend(fragIn.normalWS, triblend);
                        viewDirTS = 0;
                    #endif                        

                    // albedo textures                    
                    surfaceData.albedo = SampleAlbedoTriplanar(triUV, triblend, terrainIndex);
                    // currently not supporting alpha values
                    surfaceData.alpha = 1;

                    
                    // normalWS.z *= -1;
                    surfaceData.normalTS = SampleNormalTriplanar(fragIn, triUV, triblend, terrainIndex);

                    surfaceData.occlusion = SampleOcclusionTriplanar(triUV, triblend, terrainIndex);                
                    surfaceData.smoothness = 1 - SampleSmoothnessTriplanar(triUV, triblend, terrainIndex);
                    #ifdef _SPECULARMAP      
                        surfaceData.specular = SampleSpecularTriplanar(triUV, triblend, terrainIndex);
                    #else
                        surfaceData.specular = _SpecColor[terrainIndex];
                    #endif
                    surfaceData.emission = 0; 
                    surfaceData.metallic = 0.0h;  
                }
            }


            half4 frag (FragmentInput fragIn) : SV_Target
            {                 
	            
				SurfaceData surfaceData;
                InputData inputData;
                half3 normalWS;
                half3 triblend;
                half3 viewDirTS;
                // Setup SurfaceData
				InitializeSurfaceData(fragIn, surfaceData, normalWS, triblend, viewDirTS);
				// Setup InputData				
				InitializeInputData(fragIn, normalWS, inputData);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
                // half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

				// Fog
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				
                // return half4(triblend, 0);
				// return half4(fragIn.normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(inputData.normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(surfaceData.albedo, 0);
                // return half4(surfaceData.occlusion, 0,0, 0);
                // return half4(surfaceData.specular, 0);
                // return half4(surfaceData.normalTS *0.5 + 0.5, 0);                
                // return half4(inputData.viewDirectionWS.xyz * 0.5 + 0.5, 0);
                // return half4(viewDirTS * 0.5 + 0.5, 0);
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