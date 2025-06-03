
using System;
using System.Collections.Generic;
using MultiTerrain.Segmentation;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultiTerrain
{
    public class MaterialTools
    {
        public enum DebugView
        {
            None,
            TerrainColors,
            Submeshes,
            Coordinates,
            Albedo,
            UV
        }

        public static List<Material> SetProperties(
            Renderer renderer,
            Shader shader,
            List<TerrainType> terrainTypes,
            NativeArray<TerrainCombination> terrainMap,
            int meshResolution,
            float meshX,
            float meshZ,
            TerrainGenerator.TextureSizeEnum textureSize,
            int numSamplingClasses,
            out ComputeBuffer terrainBuffer)
        {
            List<Material> materials = new(numSamplingClasses);;
            renderer.GetSharedMaterials(materials);
            int len = terrainTypes.Count;
            Texture2DArray diffuse = new((int)textureSize, (int)textureSize, len, TextureFormat.RGB24, false);
            Vector4[] tilingOffset = new Vector4[len];

            Texture2DArray bump = new((int)textureSize, (int)textureSize, len, TextureFormat.RGBA32, true);
            float[] bumpScale = new float[len];

            Texture2DArray height = new((int)textureSize, (int)textureSize, len, TextureFormat.R8, true);
            float[] parallaxHeightScale = new float[len];
            float[] blendingScale = new float[len];

            Texture2DArray occlusion = new((int)textureSize, (int)textureSize, len, TextureFormat.R8, true);
            float[] occlusionStrength = new float[len];

            Texture2DArray smoothness = new((int)textureSize, (int)textureSize, len, TextureFormat.R8, true);
            Texture2DArray specular = new((int)textureSize, (int)textureSize, len, TextureFormat.RGB24, true);
            Vector4[] specColorSmoothness = new Vector4[len];
            bool specularMissing = false;

            Vector4[] debugTerrainColor = new Vector4[len];

            for (int i = 0; i < len; i++)
            {
                TerrainType type = terrainTypes[i];
                Graphics.CopyTexture(type._diffuse, 0, 0, diffuse, i, 0);
                Graphics.CopyTexture(type._normalMap, 0, 0, bump, i, 0);
                Graphics.CopyTexture(type._heightMap, 0, 0, height, i, 0);
                Graphics.CopyTexture(type._occlusionMap, 0, 0, occlusion, i, 0);
                Graphics.CopyTexture(type._smoothnessMap, 0, 0, smoothness, i, 0);
                // if(type._specularMap != null)
                // {
                //     // Graphics.CopyTexture(type._specularMap, 0, 0, specular, i, 0);
                // }
                // else
                // {
                specularMissing = true;
                // }

                tilingOffset[i] = new Vector4(type._tiling.x, type._tiling.y, type._offset.x, type._offset.y);
                bumpScale[i] = type._bumpScale;
                parallaxHeightScale[i] = type._parallaxHeightScale;
                blendingScale[i] = type._triplanarBlending;
                occlusionStrength[i] = type._occlusionStrength;
                specColorSmoothness[i] = new Vector4(type._specColor.r, type._specColor.b, type._specColor.g, type._smoothness);
                debugTerrainColor[i] = new Vector4(type._color.r, type._color.g, type._color.b, 0);
            }
            Shader.SetGlobalInteger("_MeshResolution", meshResolution);
            Shader.SetGlobalFloat("_MeshX", meshX);
            Shader.SetGlobalFloat("_MeshZ", meshZ);

            Shader.SetGlobalTexture("_DiffuseMap", diffuse);
            Shader.SetGlobalVectorArray("_DiffuseST", tilingOffset);

            Shader.SetGlobalTexture("_BumpMap", bump);
            Shader.SetGlobalFloatArray("_BumpScale", bumpScale);

            Shader.SetGlobalTexture("_HeightMap", height);
            Shader.SetGlobalFloatArray("_ParallaxHeightScale", parallaxHeightScale);
            Shader.SetGlobalFloatArray("_HeightmapBlending", blendingScale);

            Shader.SetGlobalTexture("_OcclusionMap", occlusion);
            Shader.SetGlobalFloatArray("_OcclusionStrength", occlusionStrength);

            Shader.SetGlobalTexture("_SmoothnessMap", smoothness);
            Shader.SetGlobalVectorArray("_SpecColorSmoothness", specColorSmoothness);

            Shader.SetGlobalVectorArray("_DebugTerrainColor", debugTerrainColor);

            terrainBuffer = new(terrainMap.Length, TerrainCombination.SizeInBytes);
            terrainBuffer.SetData(terrainMap);
            Shader.SetGlobalBuffer("_TerrainMap", terrainBuffer);

            for (int i = 0; i < numSamplingClasses; i++)
            {
                Material material = new(shader);
                // If a material doesn't have specular maps, only a fixed specular color per material is used
                LocalKeyword specularMapKeyword = new(shader, "_SPECULARMAP");
                if (!specularMissing)
                {
                    material.SetTexture("_SpecularMap", specular);
                    material.SetKeyword(specularMapKeyword, true);
                }
                else
                {
                    material.SetKeyword(specularMapKeyword, false);
                }
                LocalKeyword heightBasedBlendKeyword = new(shader, "_HEIGHTBASEDTRIBLEND");
                material.SetKeyword(heightBasedBlendKeyword, true);

                material.SetInteger("_SamplingLevel", i);
                materials.Add(material);
            }


            return materials;

        }

        internal static void SetDebugMode(
            DebugView debugView)
        {
            
            GlobalKeyword debugShowTerrainColors = GlobalKeyword.Create("_DEBUG_SHOW_TERRAIN_COLORS");
            GlobalKeyword debugShowSubmeshes = GlobalKeyword.Create("_DEBUG_SHOW_SUBMESHES");
            GlobalKeyword debugShowCoordinates = GlobalKeyword.Create("_DEBUG_SHOW_COORDINATES");
            GlobalKeyword debugShowAlbedo = GlobalKeyword.Create("_DEBUG_SHOW_ALBEDO");
            GlobalKeyword debugShowUV = GlobalKeyword.Create("_DEBUG_UV");
            switch (debugView)
            {
                case DebugView.None:
                    Shader.SetKeyword(debugShowTerrainColors, false);
                    Shader.SetKeyword(debugShowSubmeshes, false);
                    Shader.SetKeyword(debugShowCoordinates, false);
                    Shader.SetKeyword(debugShowAlbedo, false);
                    Shader.SetKeyword(debugShowUV, false);
                    break;
                case DebugView.TerrainColors:
                    Shader.SetKeyword(debugShowTerrainColors, true);
                    Shader.SetKeyword(debugShowSubmeshes, false);
                    Shader.SetKeyword(debugShowCoordinates, false);
                    Shader.SetKeyword(debugShowAlbedo, false);
                    Shader.SetKeyword(debugShowUV, false);
                    break;
                case DebugView.Submeshes:
                    Shader.SetKeyword(debugShowTerrainColors, false);
                    Shader.SetKeyword(debugShowSubmeshes, true);
                    Shader.SetKeyword(debugShowCoordinates, false);
                    Shader.SetKeyword(debugShowAlbedo, false);
                    Shader.SetKeyword(debugShowUV, false);
                    break;
                case DebugView.Coordinates:
                    Shader.SetKeyword(debugShowTerrainColors, false);
                    Shader.SetKeyword(debugShowSubmeshes, false);
                    Shader.SetKeyword(debugShowCoordinates, true);
                    Shader.SetKeyword(debugShowAlbedo, false);
                    Shader.SetKeyword(debugShowUV, false);
                    break;
                case DebugView.Albedo:
                    Shader.SetKeyword(debugShowTerrainColors, false);
                    Shader.SetKeyword(debugShowSubmeshes, false);
                    Shader.SetKeyword(debugShowCoordinates, false);
                    Shader.SetKeyword(debugShowAlbedo, true);
                    Shader.SetKeyword(debugShowUV, false);
                    break;
                case DebugView.UV:
                    Shader.SetKeyword(debugShowTerrainColors, false);
                    Shader.SetKeyword(debugShowSubmeshes, false);
                    Shader.SetKeyword(debugShowCoordinates, false);
                    Shader.SetKeyword(debugShowAlbedo, false);
                    Shader.SetKeyword(debugShowUV, true);
                    break;

            }

        }

        public static void DebugSetChessTerrain(ComputeBuffer terrainBuffer, NativeArray<TerrainCombination> terrainMap, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int4 ids = new int4(1, 2, 0, 0);
                    float val = (Mathf.Floor(j * 10f / height) + Mathf.Floor(i * 10f / width)) % 2f;
                    float4 weightings = new float4(val, (val + 1f) % 2, 0f, 0f);
                    terrainMap[j * width + i] = new TerrainCombination(ids, weightings);
                }
            }
            terrainBuffer.SetData(terrainMap);
            Shader.SetGlobalBuffer("_TerrainMap", terrainBuffer);
        }


    }

}
