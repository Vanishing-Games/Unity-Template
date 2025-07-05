
#ifndef PREVIEW_INCLUDED
#define PREVIEW_INCLUDED

#include "UnityCG.cginc"

float3 preview_WorldSpaceCameraPos;
float4x4 preview_WorldToObject;
float4x4 preview_ObjectToWorld;
float4x4 preview_MatrixV;
float4x4 preview_MatrixInvV;

inline float3 PreviewFragmentPositionOS( float2 uv )
{
	float2 xy = 2 * uv - 1;
	float z = -sqrt( 1 - saturate( dot( xy, xy ) ) );
	return float3( xy, z );
}

inline float3 PreviewFragmentNormalOS( float2 uv, bool normalized = true )
{
	float3 positionOS = PreviewFragmentPositionOS( uv );
	float3 normalOS = positionOS;
	if ( normalized )
	{
		normalOS = normalize( normalOS );
	}
	return normalOS;
}

inline float3 PreviewFragmentTangentOS( float2 uv, bool normalized = true )
{
	float3 positionOS = PreviewFragmentPositionOS( uv );
	float3 tangentOS = float3( -positionOS.z, positionOS.y * 0.01, positionOS.x );
	if ( normalized )
	{
		tangentOS = normalize( tangentOS );
	}
	return tangentOS;
}

inline float3 PreviewWorldSpaceViewDir( in float3 worldPos, bool normalized )
{
	float3 vec = preview_WorldSpaceCameraPos.xyz - worldPos;
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

inline float3 PreviewWorldToObjectDir( in float3 dir, const bool normalized )
{
	float3 vec = mul( ( float3x3 )preview_WorldToObject, dir );
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

inline float3 PreviewObjectToWorldDir( in float3 dir, const bool normalized )
{
	float3 vec = mul( ( float3x3 )preview_ObjectToWorld, dir );
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

inline float3 PreviewWorldToViewDir( in float3 dir, const bool normalized )
{
	float3 vec = mul( ( float3x3 )preview_MatrixV, dir );
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

inline float3 PreviewViewToWorldDir( in float3 dir, const bool normalized )
{
	float3 vec = mul( ( float3x3 )preview_MatrixInvV, dir );
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

float3 PreviewFragmentTangentToWorldDir( in float2 uv, in float3 normalTS, const bool normalized )
{
	float3 vertexPos = PreviewFragmentPositionOS( uv );
	float3 tangent = PreviewFragmentTangentOS( uv );
	float3 worldPos = mul( unity_ObjectToWorld, float4( vertexPos, 1 ) ).xyz;
	float3 normal = PreviewFragmentNormalOS( uv );
	float3 worldNormal = UnityObjectToWorldNormal( normal );
	float3 worldTangent = UnityObjectToWorldDir( tangent );

	const float tangentSign = -1;
	float3 worldBinormal = normalize( cross( worldNormal, worldTangent ) * tangentSign );
	float4 tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
	float4 tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
	float4 tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );

	float3 vec = float3( dot( tSpace0.xyz, normalTS ), dot( tSpace1.xyz, normalTS ), dot( tSpace2.xyz, normalTS ) );
	if ( normalized )
	{
		vec = normalize( vec );
	}
	return vec;
}

float2 PreviewFragmentSphericalUV( in float2 uv )
{
	float3 vertexPos = PreviewFragmentPositionOS( uv );
	return float2( atan2( vertexPos.x, -vertexPos.z ) / UNITY_PI + 0.5, uv.y );
}

#endif // PREVIEW_INCLUDED