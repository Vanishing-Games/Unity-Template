Shader "Hidden/IndirectSpecularLight"
{
	Properties
	{
		_Skybox("_Skybox", CUBE) = "white" {}
		_A ("Normal", 2D) = "white" {}
		_B ("Smoothness", 2D) = "white" {}
		_C ("Occlusion", 2D) = "white" {}
	}

	SubShader
	{
		Pass // not connected
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			sampler2D _A;
			sampler2D _B;
			sampler2D _C;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = normalize( reflect( -worldViewDir, worldNormal ) );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( tex2D( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * tex2D( _C, i.uv ).r, 1 );
			}
			ENDCG
		}

		Pass // connected tangent
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			sampler2D _A;
			sampler2D _B;
			sampler2D _C;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 tangentNormal = tex2D( _A, PreviewFragmentSphericalUV( i.uv ) ).xyz;
				float3 worldNormal = PreviewFragmentTangentToWorldDir( i.uv, tangentNormal, true );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = reflect( -worldViewDir, worldNormal );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( tex2D( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * tex2D( _C, i.uv ).r, 1 );
			}
			ENDCG
		}

		Pass // connected world
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			sampler2D _A;
			sampler2D _B;
			sampler2D _C;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = tex2D( _A, i.uv );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = reflect( -worldViewDir, worldNormal );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( tex2D( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * tex2D( _C, i.uv ).r, 1 );
			}
			ENDCG
		}
	}
}
