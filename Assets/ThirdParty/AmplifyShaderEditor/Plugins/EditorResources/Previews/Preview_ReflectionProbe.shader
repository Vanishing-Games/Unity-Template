Shader "Hidden/IndirectSpecularLight"
{
	Properties
	{
		_Skybox("_Skybox", CUBE) = "white" {}
		_A ("View Dir OS", 2D) = "white" {}
		_B ("Normal OS", 2D) = "white" {}
		_C ("LOD", 2D) = "white" {}
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

			uint viewDirInput;
			uint normalInput;
			uint lodInput;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float lod = 0;

				float2 sphereUV = PreviewFragmentSphericalUV( i.uv );

				if ( viewDirInput != 0 )
				{
					float3 viewDirOS = tex2D( _A, sphereUV );
					worldViewDir = UnityObjectToWorldDir( viewDirOS );
				}

				if ( normalInput != 0 )
				{
					float3 normalOS = tex2D( _B, i.uv );
					worldNormal = UnityObjectToWorldNormal( normalOS );
				}

				if ( lodInput != 0 )
				{
					lod = tex2D( _C, i.uv );
				}

				float3 worldRefl = normalize( reflect( -worldViewDir, worldNormal ) );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, lod ) ).rgb;

				return float4( sky, 1 );
}
			ENDCG
		}
	}
}
