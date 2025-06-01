// This file contains triplanar sampling functions

// Reoriented Normal Mapping
// http://blog.selfshadow.com/publications/blending-in-detail/
// Altered to take normals (-1 to 1 ranges) rather than unsigned bitangentOS maps (0 to 1 ranges)
half3 blendRNM(half3 n1, half3 n2)
{
    n1.z += 1;
    n2.xy = -n2.xy;

    return n1 * dot(n1, n2) / n1.z - n2;
}

half3 SampleAlbedoTriplanar(TriplanarUV triUV, half4x3 triblend, int4 textureIndices)
{
    half4x3 albedoMat = 0;
    half3 albedo;
    int i;
    [unroll]
    for (i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        half3 colX = SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.x[i], textureIndices[i]).xyz;
        half3 colY =  SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.y[i], textureIndices[i]).xyz;
        half3 colZ =  SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.z[i], textureIndices[i]).xyz;
        // albedoMat[i] = colX * triblend[i].x + colY * triblend[i].y + colZ * triblend[i].z;
        albedoMat[i] = colX * triblend[i].x + colY * triblend[i].y + colZ * triblend[i].z;
    }
    [unroll]
    for (i = 0; i < 3; i++)
    {
        albedo[i] = dot(half4(albedoMat[0][i], albedoMat[1][i], albedoMat[2][i], albedoMat[3][i]), half4(1,1,1,1));
    }
    return albedo;
}

half4x3 SampleHeightTriplanar(TriplanarUV triUV, int4 textureIndices)
{
    half4x3 heights = 0;
    [unroll]
    for (int i = 0; i < 4; i++)
    {                            
        if (i > _SamplingLevel) 
            break;      
        heights[i] = half3(
            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.x[i], textureIndices[i]).r,
            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.y[i], textureIndices[i]).r,
            SAMPLE_TEXTURE2D_ARRAY(_HeightMap, sampler_HeightMap, triUV.z[i], textureIndices[i]).r);         
    }
    return heights;
}

half3 SampleNormalWSTriplanar(FragmentInput fragIn, TriplanarUV triUV, half4x3 triblend, int4 textureIndices)
{
    half4x3 normalWSMat = 0;
    half3 normalOut;
    int i;
    [unroll]
    for (i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        // tangent space normal maps
        half3 normalTSX = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x[i], textureIndices[i]));
        half3 normalTSY = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x[i], textureIndices[i]));
        half3 normalTSZ = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x[i], textureIndices[i]));
        half3 axisSign = fragIn.normalWS < 0 ? -1 : 1;

        // flip normal maps' x axis to account for flipped UVs
        #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
            normalTSX.x *= axisSign.x;
            normalTSY.x *= axisSign.y;
            normalTSZ.x *= -axisSign.z;
        #endif

        half3 absVertNormal = abs(fragIn.normalWS);
        // swizzle world normals to match tangent space and apply reoriented normal mapping blend
        normalTSX = blendRNM(half3(fragIn.normalWS.zy, absVertNormal.x), normalTSX);
        normalTSY = blendRNM(half3(fragIn.normalWS.xz, absVertNormal.y), normalTSY);
        normalTSZ = blendRNM(half3(fragIn.normalWS.xy, absVertNormal.z), normalTSZ);

        // apply world space sign to tangent space Z
        normalTSX.z *= axisSign.x;
        normalTSY.z *= axisSign.y;
        normalTSZ.z *= axisSign.z;
        
        // swizzle tangent normals to match world normal and blend together
        normalWSMat[i] = normalize(
            normalTSX.zyx * triblend[i].x +
            normalTSY.xzy * triblend[i].y +
            normalTSZ.xyz * triblend[i].z
        );
    }
    [unroll]
    for (i = 0; i < 3; i++)
    {
        normalOut[i] = dot(half4(normalWSMat[0][i], normalWSMat[1][i], normalWSMat[2][i], normalWSMat[3][i]), half4(1,1,1,1));
    }

    return normalize(normalOut);
}

half3 SampleSpecularTriplanar(TriplanarUV triUV, half4x3 triblend, int4 textureIndices) {
    half4x3 specMat = 0;
    half3 specular;
    int i;
    [unroll]
    for (i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        #ifdef _SPECULARMAP
            half3 specularX = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.x[i], textureIndices[i]).xyz;
            half3 specularY = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.y[i], textureIndices[i]).xyz;
            half3 specularZ = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.z[i], textureIndices[i]).xyz;
            specMat[i] = specularX * triblend[i].x + specularY * triblend[i].y + specularZ * triblend[i].z;
        #else
            specMat[i] = _SpecColorSmoothness[textureIndices[i]].rgb;
        #endif
    }
    [unroll]
    for (i = 0; i < 3; i++)
    {
        specular[i] = dot(half4(specMat[0][i], specMat[1][i], specMat[2][i], specMat[3][i]), half4(1,1,1,1));
    }
    return specular;    
}

half SampleOcclusion(float2 uv, int textureIndex) {
        #if defined(SHADER_API_GLES)
            return SAMPLE_TEXTURE2D_ARRAY(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).r;
        #else
            half occ = SAMPLE_TEXTURE2D_ARRAY(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).r;
            return LerpWhiteTo(occ, _OcclusionStrength[textureIndex]);
        #endif
}

half SampleOcclusionTriplanar(TriplanarUV triUV, half4x3 triblend, int4 textureIndices){
    half4 occlusionVec = 0;
    half3 triOcclusion = 0;
    [unroll]
    for (int i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        
        triOcclusion.x = SampleOcclusion(triUV.x[i], textureIndices[i]);
        triOcclusion.y = SampleOcclusion(triUV.y[i], textureIndices[i]);
        triOcclusion.z = SampleOcclusion(triUV.z[i], textureIndices[i]);
        occlusionVec[i] = dot(triOcclusion, triblend[i]) * _OcclusionStrength[textureIndices[i]];
    }
    return triOcclusion.x; //return dot(occlusionVec, half4(1,1,1,1));
}

half SampleSmoothnessTriplanar(TriplanarUV triUV, half4x3 triblend, int4 textureIndices){
    half4 smoothnessVec = 0;
    [unroll]
    for (int i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        half3 triSmoothness;
        triSmoothness.x = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.x[i], textureIndices[i]).r;
        triSmoothness.y = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.y[i], textureIndices[i]).r;                                    
        triSmoothness.z = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.z[i], textureIndices[i]).r;
        smoothnessVec[i] = dot(triSmoothness, triblend[i]) * (1 - _SpecColorSmoothness[textureIndices[i]].a);
    }
    return dot(smoothnessVec, half4(1, 1, 1, 1));
}
