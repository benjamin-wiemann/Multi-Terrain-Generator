
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultiTerrain
{
    public class MaterialTools
    {
        public static void SetProperties(List<TerrainType> terrainTypes, 
            int meshResolution, 
            float meshX, 
            float meshZ,
            TerrainGenerator.TextureSizeEnum textureSize, 
            ref Material material )
        {
            var shader = material.shader;
            int len = terrainTypes.Count;
            Texture2DArray diffuse = new((int) textureSize, (int) textureSize, len, TextureFormat.RGB24, true);
            Vector4[] tilingOffset = new Vector4[len];     

            Texture2DArray bump = new((int) textureSize, (int) textureSize, len, TextureFormat.RGBA32, true);
            float[] bumpScale = new float[len];

            Texture2DArray height = new((int) textureSize, (int) textureSize, len, TextureFormat.R8, true);
            float[] heightScale = new float[len];
            float[] blendingScale = new float[len];

            Texture2DArray occlusion = new((int) textureSize, (int) textureSize, len, TextureFormat.R8, true);
            float[] occlusionStrength = new float[len];

            Texture2DArray smoothness = new((int) textureSize, (int) textureSize, len, TextureFormat.R8, true);
            Texture2DArray specular = new((int) textureSize, (int) textureSize, len, TextureFormat.RGB24, true);
            Vector4[] specColorSmoothness = new Vector4[len];
            bool specularMissing = false;

            for ( int i = 0; i < len; i++)
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
                
            }
            Shader.SetGlobalInteger("_MeshResolution", meshResolution);
            Shader.SetGlobalFloat("_MeshX", meshX);
            Shader.SetGlobalFloat("_MeshZ", meshZ);
            Shader.SetGlobalTexture("_BaseMap", diffuse); 
            Shader.SetGlobalVectorArray("_BaseMap_ST", tilingOffset);

            Shader.SetGlobalTexture("_BumpMap", bump);
            Shader.SetGlobalFloatArray("_BumpScale", bumpScale);

            Shader.SetGlobalTexture("_HeightMap", height);
            Shader.SetGlobalFloatArray("_HeightScale", heightScale);
            Shader.SetGlobalFloatArray("_HeightmapBlending", blendingScale);

            Shader.SetGlobalTexture("_OcclusionMap", occlusion);
            Shader.SetGlobalFloatArray("_OcclusionStrength", occlusionStrength);

            Shader.SetGlobalTexture("_SmoothnessMap", smoothness);
            Shader.SetGlobalVectorArray("_SpecColorSmoothness" , specColorSmoothness);
            
            // If a material doesn't have specular maps, only a fixed specular color per material is used
            LocalKeyword specularMapKeyword = new(shader, "_SPECULARMAP");
            if(!specularMissing)
            {
                material.SetTexture("_SpecularMap", specular);                
                material.SetKeyword(specularMapKeyword, true);
            }                
            else
            {                
                material.SetKeyword(specularMapKeyword, false);                
            }

        }
    }

}
