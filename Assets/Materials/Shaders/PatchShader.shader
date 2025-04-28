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
        // contains fixed specular color in rgb and smoothness in alpha channel
		float4 _SpecColorSmoothness[MAX_NUMBER_MATERIALS];        
		float _OcclusionStrength[MAX_NUMBER_MATERIALS];
		float _BumpScale[MAX_NUMBER_MATERIALS];
        float _HeightScale[MAX_NUMBER_MATERIALS];
        float _HeightmapBlending[MAX_NUMBER_MATERIALS];
              
        float4 _DebugTerrainColor[MAX_NUMBER_MATERIALS];

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
            
            StructuredBuffer<TerrainCombination> _TerrainMap;

            
            #pragma vertex vert
            #pragma fragment frag

            // Shadows
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _HEIGHTMAP
            #pragma shader_feature_local _SPECULARMAP            
            // flip UVs horizontally to correct for back side projection
            #pragma shader_feature_local _TRIPLANAR_CORRECT_PROJECTED_U
            // offset UVs to prevent obvious mirroring
            #pragma shader_feature_local _TRIPLANAR_UV_OFFSET

            // indicates if triplanar blend is based on height information
            #pragma multi_compile _ _HEIGHTBASEDTRIBLEND

            // shows the terrain color on the mesh instead of sampling
            #pragma multi_compile _ _DEBUG_SHOW_TERRAIN_COLORS 

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
            void GenerateTriblend(half3 normalWS, out half4x3 triblendMat)
            {                
                half3 triblend = saturate(pow(normalWS, 4));
                triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);
                triblendMat = half4x3(triblend, triblend, triblend, triblend);
            }

            // Triblend based on height information from height maps
            // https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#ce80
            void GenerateHeightTriblend(half3 normalWS, half3 heights, out half3 triblend)
            {                
                triblend = abs(normalWS.xyz);
                triblend /= dot(triblend, float3(1,1,1));                
                heights += (triblend * 3.0);
                float heightStart = max(max(heights.x, heights.y), heights.z) - _HeightmapBlending[0];
                float3 h = max(heights - heightStart.xxx, float3(0,0,0));
                triblend = h / dot(h, float3(1,1,1));
            }

            half4x3 GenerateTerrainBlend(half3 normalWS, half4x3 heights, half4 weightings)
            {                
                half3 triblend = abs(normalWS.xyz);;
                triblend /= dot(triblend, half3(1,1,1));
                triblend *= 3.0;                
                half4x3 triblend4 = half4x3(triblend, triblend, triblend, triblend);
                heights += triblend4;
                half heightStart = 0;
                int i;
                [unroll]
                for (i = 0; i < 4; i++)
                {
                    if (i >= _SubmeshSplitLevel) 
                        break;
                    heightStart = max(max(max(heights[0][i], heights[1][i]), heights[2][i]), heightStart);
                }
                heightStart -= _HeightmapBlending[0];
                half4x3 h = max(heights - heightStart, half4x3(0,0,0,0,0,0,0,0,0,0,0,0));
                half3 ones = half3(1, 1, 1);
                // normalize each row by the sum of the  
                half4 normalizationPerTerrain = half4(dot(h[0], ones), dot(h[1], ones), dot(h[2], ones), dot(h[2], ones));
                [unroll]
                for (i = 0; i < 4; i++)
                {
                    triblend4[i] = h[i] / normalizationPerTerrain[i];
                }
                return triblend4;
            }

            // Generates triplanar UV according to 
            // https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#ce80
            // but for up to 
            TriplanarUV GenerateTriplanarUV(FragmentInput fragIn, int4 textureIndices)
            {             
                TriplanarUV triUV = (TriplanarUV) 0;  
                [unroll]
                for (int i = 0; i < 4; i++)
                {                       
                    if (i >= _SubmeshSplitLevel) 
                        break;                    
                    int ti = textureIndices[i];
                    triUV.x[i] = fragIn.posWS.zy * _BaseMap_ST[ti].xy + _BaseMap_ST[ti].zw;
                    triUV.y[i] = fragIn.posWS.xz * _BaseMap_ST[ti].xy + _BaseMap_ST[ti].zw;
                    triUV.z[i] = fragIn.posWS.xy * _BaseMap_ST[ti].xy + _BaseMap_ST[ti].zw;

                    // sign function which never returns 0
                    half3 axisSign = fragIn.normalWS.xyz < 0 ? -1 : 1;

                    // flip UVs horizontally to correct for back side projection
                    #if defined(_TRIPLANAR_CORRECT_PROJECTED_U)
                        triUV.x[i].x *= axisSign.x;
                        triUV.y[i].x *= axisSign.y;
                        triUV.z[i].x *= -axisSign.z;
                    #endif
                }
                // offset UVs to prevent obvious mirroring
                #if defined(_TRIPLANAR_UV_OFFSET)
                    triUV.y += 0.33;
                    triUV.z += 0.67;
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


            void InitializeSurfaceData(
                FragmentInput fragIn, 
                out SurfaceData surfaceData, 
                out half3 normalWS, 
                out half4x3 triblend,       // debug
                out half3 viewDirTS,        // debug
                out half4x3 heights,        // debug
                out int4 terrainIndices,    // debug
                out float4 terrainWeightings) // debug
                {
                surfaceData = (SurfaceData)0; 

                TerrainCombination combination =  _TerrainMap[0];// _TerrainMap[(int) round(fragIn.terrainCoordinateOS.y *_MeshX) + (int) round(fragIn.terrainCoordinateOS.x)];
                terrainIndices = combination.indices;
                terrainWeightings = combination.weightings;
                 
                TriplanarUV triUV = GenerateTriplanarUV( fragIn, terrainIndices);
                // half3x3 worldToTangentX = transpose(float3x3(fragIn.normalWS, fragIn.bitangentWS, fragIn.tangentWS.xyz));
                // half3x3 worldToTangentY = transpose(float3x3(fragIn.tangentWS.xyz, fragIn.normalWS, fragIn.bitangentWS));
                half3x3 worldToTangentZ = transpose(float3x3(fragIn.tangentWS.xyz, fragIn.bitangentWS, fragIn.normalWS));
            
                #ifdef _HEIGHTMAP
                    heights = SampleHeightTriplanar(triUV, terrainIndices);                    
                    #ifdef _HEIGHTBASEDTRIBLEND
                        triblend = GenerateTerrainBlend(fragIn.normalWS, heights, terrainWeightings);
                    #else
                        GenerateTriblend(fragIn.normalWS, triblend);
                    #endif
                    
                    half3 viewDirTSZ = TransformWorldToTangentDir(GetWorldSpaceNormalizeViewDir(fragIn.posWS), worldToTangentZ);       
                    // triUV.x -= viewDirTSZ.zy / viewDirTSZ.x * (heights.x * _HeightScale);
                    // triUV.y -= viewDirTSZ.xz / viewDirTSZ.y * (heights.y * _HeightScale);
                    // triUV.z -= viewDirTSZ.xy / viewDirTSZ.z * (heights.z * _HeightScale);

                    // triUV.x -= ParallaxOffset1Step( heights.x, _HeightScale, half3(viewDirTSZ.zy, viewDirTSZ.x));
                    // triUV.y -= ParallaxOffset1Step( heights.y, _HeightScale, half3(viewDirTSZ.xz, viewDirTSZ.y));                    
                    // triUV.z -= ParallaxOffset1Step( heights.z, _HeightScale, viewDirTSZ);
                    viewDirTS = half3(viewDirTSZ.zy, viewDirTSZ.x);  
                #else 
                    heights = 0; 
                    GenerateTriblend(fragIn.normalWS, triblend);
                    viewDirTS = 0;
                #endif                        

                // albedo textures                    
                surfaceData.albedo = SampleAlbedoTriplanar(triUV, triblend, terrainIndices);
                // currently not supporting alpha values
                surfaceData.alpha = 1;
                
                // normalWS.z *= -1;
                normalWS = SampleNormalWSTriplanar(fragIn, triUV, triblend, terrainIndices);
                surfaceData.normalTS = TransformWorldToTangent(normalWS, worldToTangentZ);

                surfaceData.occlusion = SampleOcclusionTriplanar(triUV, triblend, terrainIndices);                
                surfaceData.smoothness = 1 - SampleSmoothnessTriplanar(triUV, triblend, terrainIndices);
                surfaceData.specular = SampleSpecularTriplanar(triUV, triblend, terrainIndices);
                surfaceData.emission = 0; 
                surfaceData.metallic = 0.0h;  
            
            }


            half4 frag (FragmentInput fragIn) : SV_Target
            {                 
	            
				SurfaceData surfaceData;
                InputData inputData;
                half3 normalWS;
                half4x3 triblend;
                half3 viewDirTS;
                half4x3 heights;
                int4 terrainIndices;
                float4 terrainWeightings;
                // Setup SurfaceData
				InitializeSurfaceData(
                    fragIn, 
                    surfaceData, 
                    normalWS, 
                    triblend, 
                    viewDirTS, 
                    heights,
                    terrainIndices,
                    terrainWeightings);
				// Setup InputData				
				InitializeInputData(
                    fragIn, 
                    normalWS, 
                    inputData);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
                // half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

				// Fog
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				
                #ifdef _DEBUG_SHOW_TERRAIN_COLORS
                // return terrainWeightings;
                    half4 debugColor = 0;
                    int i;
                    [unroll]
                    for( i = 0; i < 3; i++)
                        debugColor[i] = dot(half4(
                            // _DebugTerrainColor[terrainIndices[0]][i], 
                            // _DebugTerrainColor[terrainIndices[1]][i],
                            // _DebugTerrainColor[terrainIndices[2]][i],
                            // _DebugTerrainColor[terrainIndices[3]][i]),
                            _DebugTerrainColor[0][i], 
                            _DebugTerrainColor[1][i],
                            _DebugTerrainColor[2][i],
                            _DebugTerrainColor[3][i]),
                            terrainWeightings);                                
                    return debugColor;
                #else
                // return half4(triblend[0], 0);
				// return half4(fragIn.normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(normalWS.xyz * 0.5 + 0.5, 0);
                // return half4(inputData.normalWS.xyz * 0.5 + 0.5, 0);
                return half4(surfaceData.albedo, 0);
                // return half4(surfaceData.occlusion, 0,0, 0);
                // return half4(surfaceData.smoothness, 0,0, 0);
                // return half4(heights[0], 0);
                // return half4(surfaceData.specular, 0);
                // return half4(surfaceData.normalTS *0.5 + 0.5, 0);                
                // return half4(inputData.viewDirectionWS.xyz * 0.5 + 0.5, 0);
                // return half4(viewDirTS * 0.5 + 0.5, 0);
                // return color;
                #endif

            }
            ENDHLSL
        }

        // Pass {
		// 	Name "ShadowCaster"
		// 	Tags { "LightMode"="ShadowCaster" }

		// 	ZWrite On
		// 	ZTest LEqual

		// 	HLSLPROGRAM
		// 	#pragma vertex ShadowPassVertex
		// 	#pragma fragment ShadowPassFragment

		// 	// Material Keywords
		// 	#pragma shader_feature_local_fragment _ALPHATEST_ON
		// 	#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

		// 	// GPU Instancing
		// 	#pragma multi_compile_instancing
		// 	//#pragma multi_compile _ DOTS_INSTANCING_ON

		// 	// Universal Pipeline Keywords
		// 	// (v11+) This is used during shadow map generation to differentiate between directional and punctual (point/spot) light shadows, as they use different formulas to apply Normal Bias
		// 	#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
        //     ENDHLSL
        // }
    }
}