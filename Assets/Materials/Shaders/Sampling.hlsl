// Reoriented Normal Mapping
// http://blog.selfshadow.com/publications/blending-in-detail/
// Altered to take normals (-1 to 1 ranges) rather than unsigned bitangentOS maps (0 to 1 ranges)
half3 blendRNM(half3 n1, half3 n2)
{
    n1.z += 1;
    n2.xy = -n2.xy;

    return n1 * dot(n1, n2) / n1.z - n2;
}

half3 SampleAlbedoTriplanar(TriplanarUV triUV, half3 triblend, int textureIndex)
{
    half4 colX = SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.x, textureIndex);
    half4 colY = SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.y, textureIndex);
    half4 colZ = SAMPLE_TEXTURE2D_ARRAY(_BaseMap, sampler_BaseMap, triUV.z, textureIndex);
    return colX.rgb * triblend.x + colY.rgb * triblend.y + colZ.rgb * triblend.z;
}

half3 SampleNormalTriplanar(FragmentInput fragIn, TriplanarUV triUV, half3 triblend, int textureIndex)
{
    
    // tangent space normal maps
    half3 normalTSX = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x, textureIndex));
    half3 normalTSY = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x, textureIndex));
    half3 normalTSZ = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(_BumpMap, sampler_BumpMap, triUV.x, textureIndex));
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
    half3 outNormalTS = normalize(
        normalTSX * triblend.x +
        normalTSY * triblend.y +
        normalTSZ * triblend.z
        );
    return outNormalTS;
}

half3 SampleSpecularTriplanar(TriplanarUV triUV, half3 triblend, int textureIndex) {
    half3 specularX = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.x, textureIndex).xyz;
    half3 specularY = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.y, textureIndex).xyz;                                    
    half3 specularZ = SAMPLE_TEXTURE2D_ARRAY(_SpecularMap, sampler_SpecularMap, triUV.z, textureIndex).xyz;
    return specularX * triblend.x + specularY * triblend.y + specularZ * triblend.z;
}

half SampleOcclusion(float2 uv, int textureIndex) {
        #if defined(SHADER_API_GLES)
            return SAMPLE_TEXTURE2D_ARRAY(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).g;
        #else
            half occ = SAMPLE_TEXTURE2D_ARRAY(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).g;
            return LerpWhiteTo(occ, _OcclusionStrength[textureIndex]);
        #endif
}

half SampleOcclusionTriplanar(TriplanarUV triUV, half3 triblend, int textureIndex){
    half occlusionX = SampleOcclusion(triUV.x, textureIndex);
    half occlusionY = SampleOcclusion(triUV.y, textureIndex);
    half occlusionZ = SampleOcclusion(triUV.z, textureIndex);
    return occlusionX * triblend.x + occlusionY * triblend.y + occlusionZ * triblend.z;
}

half SampleSmoothnessTriplanar(TriplanarUV triUV, half3 triblend, int textureIndex){
    half smoothnessX = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.x, textureIndex).r;
    half smoothnessY = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.y, textureIndex).r;                                    
    half smoothnessZ = SAMPLE_TEXTURE2D_ARRAY(_SmoothnessMap, sampler_SmoothnessMap, triUV.z, textureIndex).r;
    return (smoothnessX * triblend.x + smoothnessY * triblend.y + smoothnessZ * triblend.z) * (1 - _Smoothness[textureIndex]);
}
