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

float sum( half3 v ) { return v.x+v.y+v.z; }

// Sampling technique to avoid texture repetition
// https://iquilezles.org/articles/texturerepetition/
void SampleTextureNoTile( Texture2DArray textureName, SamplerState samplerName, half2 uv, int textureIndex, out half4 cola, out half4 colb, out half fraction )
{
    // sample variation pattern    
    half k = SAMPLE_TEXTURE2D( _Noise, sampler_Noise, 0.2*uv ).x; // cheap (cache friendly) lookup    
    
    // compute index    
    half index = k*8.0;
    half i = floor( index );
    fraction = frac( index );

    // offsets for the different virtual patterns    
    half2 offsetA = sin( half2(3.0,7.0)*(i+0.0)); // can replace with any other hash    
    half2 offsetB = sin( half2(3.0,7.0)*(i+1.0)); // can replace with any other hash    

    // compute derivatives for mip-mapping    
    half2 dx = ddx(uv), dy = ddy(uv);
    
    // sample the two closest virtual patterns    
    cola = SAMPLE_TEXTURE2D_ARRAY_GRAD( textureName, samplerName, uv + offsetA, textureIndex, dx, dy );
    colb = SAMPLE_TEXTURE2D_ARRAY_GRAD( textureName, samplerName, uv + offsetB, textureIndex, dx, dy );
  
}

half3 SampleColorNoTile( Texture2DArray textureName, SamplerState samplerName, half2 uv, int textureIndex )
{
    half4 cola, colb;
    half fraction;
    SampleTextureNoTile( textureName, samplerName, uv, textureIndex, cola, colb, fraction);
    // interpolate between the two virtual patterns    
    return lerp( cola.xyz, colb.xyz, smoothstep(0.2,0.8,fraction-0.1*sum(cola.xyz-colb.xyz)) );
}

half3 SampleNormalNoTile( Texture2DArray textureName, SamplerState samplerName, half2 uv, int textureIndex )
{
    half4 normalA, normalB;
    half fraction;
    SampleTextureNoTile( textureName, samplerName, uv, textureIndex, normalA, normalB, fraction);
    // return blendRNM(UnpackNormal(normalA), UnpackNormal(normalB));
    half3 normA = UnpackNormal(normalA).xyz;
    half3 normB = UnpackNormal(normalB).xyz;
    return lerp( normA, normB, smoothstep(0.2,0.8,fraction-0.1*sum(normA-normB)) );
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
        half3 colX = SampleColorNoTile(_DiffuseMap, sampler_DiffuseMap, triUV.x[i], textureIndices[i]);
        half3 colY = SampleColorNoTile(_DiffuseMap, sampler_DiffuseMap, triUV.y[i], textureIndices[i]);
        half3 colZ = SampleColorNoTile(_DiffuseMap, sampler_DiffuseMap, triUV.z[i], textureIndices[i]);
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
            SampleColorNoTile(_HeightMap, sampler_HeightMap, triUV.x[i], textureIndices[i]).r,
            SampleColorNoTile(_HeightMap, sampler_HeightMap, triUV.y[i], textureIndices[i]).r,
            SampleColorNoTile(_HeightMap, sampler_HeightMap, triUV.z[i], textureIndices[i]).r);         
    }
    return heights;
}

half3 SampleNormalWSTriplanar(FragmentInput fragIn, TriplanarUV triUV, half4x3 triblend, int4 textureIndices)
{
    // prepare world normals for x, y and z plane
    half3 absVertNormal = abs(fragIn.normalWS);
    half3 normalOut = 0;
    
    // swizzle world normals to match tangent space
    half3 normalTSXOut = half3(fragIn.normalWS.zy, absVertNormal.x);
    half3 normalTSYOut = half3(fragIn.normalWS.xz, absVertNormal.y);
    half3 normalTSZOut = half3(fragIn.normalWS.xy, absVertNormal.z);
    
    half3 axisSign = fragIn.normalWS < 0 ? -1 : 1;

    half3 triblendPerPlane = 0;

    int i;
    [unroll]
    for (i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;

        // tangent space normal maps
        half3 normalTSX = SampleNormalNoTile(_NormalMap, sampler_NormalMap, triUV.x[i], textureIndices[i]);
        half3 normalTSY = SampleNormalNoTile(_NormalMap, sampler_NormalMap, triUV.y[i], textureIndices[i]);
        half3 normalTSZ = SampleNormalNoTile(_NormalMap, sampler_NormalMap, triUV.z[i], textureIndices[i]);
        
        // flip normal maps' x axis to account for flipped UVs
        #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
            normalTSX.x *= axisSign.x;
            normalTSY.x *= axisSign.y;
            normalTSZ.x *= -axisSign.z;
        #endif
        
        // apply reoriented normal mapping blend 
        // use linear interpolation to rotate normalTSOut proportionally to the current terrain weighting (only approximation)
        normalTSXOut = lerp(normalTSXOut, blendRNM(normalTSXOut, normalTSX), triblend[i].x);
        normalTSYOut = lerp(normalTSYOut, blendRNM(normalTSYOut, normalTSY), triblend[i].y);
        normalTSZOut = lerp(normalTSZOut, blendRNM(normalTSZOut, normalTSZ), triblend[i].z);

        triblendPerPlane += triblend[i];
    }

    // apply world space sign to tangent space Z
    normalTSXOut.z *= axisSign.x;
    normalTSYOut.z *= axisSign.y;
    normalTSZOut.z *= axisSign.z;
    
    // swizzle tangent normals to match world normal and blend together
    return normalize(
        normalTSXOut.zyx * triblendPerPlane.x +
        normalTSYOut.xzy * triblendPerPlane.y +
        normalTSZOut.xyz * triblendPerPlane.z
    );
    
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
            half3 specularX = SampleColorNoTile(_SpecularMap, sampler_SpecularMap, triUV.x[i], textureIndices[i]).xyz;
            half3 specularY = SampleColorNoTile(_SpecularMap, sampler_SpecularMap, triUV.y[i], textureIndices[i]).xyz;
            half3 specularZ = SampleColorNoTile(_SpecularMap, sampler_SpecularMap, triUV.z[i], textureIndices[i]).xyz;
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
            return SampleColorNoTile(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).r;
        #else
            half occ = SampleColorNoTile(_OcclusionMap, sampler_OcclusionMap, uv, textureIndex).r;
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
        occlusionVec[i] = dot(triOcclusion, triblend[i]);
    }
    return dot(occlusionVec, half4(1,1,1,1));
}

half SampleSmoothnessTriplanar(TriplanarUV triUV, half4x3 triblend, int4 textureIndices){
    half4 smoothnessVec = 0;
    [unroll]
    for (int i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;
        half3 triSmoothness;
        triSmoothness.x = SampleColorNoTile(_SmoothnessMap, sampler_SmoothnessMap, triUV.x[i], textureIndices[i]).r;
        triSmoothness.y = SampleColorNoTile(_SmoothnessMap, sampler_SmoothnessMap, triUV.y[i], textureIndices[i]).r;                                    
        triSmoothness.z = SampleColorNoTile(_SmoothnessMap, sampler_SmoothnessMap, triUV.z[i], textureIndices[i]).r;
        smoothnessVec[i] = dot(1 - triSmoothness, triblend[i]) * _SpecColorSmoothness[textureIndices[i]].a;
    }
    return dot(smoothnessVec, half4(1, 1, 1, 1));
}
