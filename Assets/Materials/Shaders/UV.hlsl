// This file contains functions related to UV generation


// Generates triplanar UV according to 
// https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#ce80
// but for up to 4 different textures
TriplanarUV GenerateTriplanarUV(FragmentInput fragIn, int4 textureIndices)
{             
    TriplanarUV triUV = (TriplanarUV) 0;  
    [unroll]
    for (int i = 0; i < 4; i++)
    {                       
        if (i > _SamplingLevel) 
            break;                    
        int ti = textureIndices[i];
        triUV.x[i].xy = fragIn.posWS.zy * _DiffuseST[ti].xy + _DiffuseST[ti].zw;
        triUV.y[i].xy = fragIn.posWS.xz * _DiffuseST[ti].xy + _DiffuseST[ti].zw;
        triUV.z[i].xy = fragIn.posWS.xy * _DiffuseST[ti].xy + _DiffuseST[ti].zw;

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