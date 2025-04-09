
// used to pass triplanar uv coordinates ( two coordinates for each axis )
struct TriplanarUV
{
    float4x2 x;
    float4x2 y;
    float4x2 z;
};

struct TerrainCombination
{
    int4 indices;
    float4 weightings;
};

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
    half4 tangentWS		: TEXCOORD2;    
    half3 bitangentWS	: TEXCOORD3;
    // half3 viewDirWS       : TEXCOORD4;
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord 				: TEXCOORD5;
    #endif
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
    half2 terrainCoordinateOS   : TEXCOORD7;
};
