Shader "Hidden/SdfDepth"
{
	Properties
	{
			[_NoScaleOffset] _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		
		Pass
		{
			cull off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
	#pragma multi_compile __ GEOM_TYPE_LEAF
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3  wpos : TEXCOORD1;
			 
				float4 vertex : SV_POSITION;
			};

			 
			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.wpos = mul(unity_ObjectToWorld, v.vertex);//
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			 
				return o;
			}
			
			fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
			{
				 if (tex2D(_MainTex, i.uv).a < 0.5) {
					 return 100;
				 }
				float dis = length(i.wpos - _WorldSpaceCameraPos.xyz);
				#ifdef GEOM_TYPE_LEAF
					return dis;
                #else
					return  (facing<0?1:-1)* dis;//
				#endif
			}
			ENDCG
		}
		//Pass
		//{
		//	cull front
		//	CGPROGRAM
		//	#pragma vertex vert
		//	#pragma fragment frag


		//	#include "UnityCG.cginc"

		//	struct appdata
		//	{
		//		float4 vertex : POSITION;
		//		float2 uv : TEXCOORD0;
		//	};

		//	struct v2f
		//	{
		//		float2 uv : TEXCOORD0;
		//		float3  wpos : TEXCOORD1;

		//		float4 vertex : SV_POSITION;
		//	};

		//	 
		//	sampler2D _MainTex;
		//	float4 _MainTex_ST;

		//	v2f vert(appdata v)
		//	{
		//		v2f o;
		//		o.vertex = UnityObjectToClipPos(v.vertex);
		//		o.wpos = mul(unity_ObjectToWorld, v.vertex);//
		//		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		//		return o;
		//	}

		//	fixed4 frag(v2f i) : SV_Target
		//	{
		//		 clip(tex2D(_MainTex, i.uv).a - 0.5);

		//		return   -length(i.wpos - _WorldSpaceCameraPos.xyz);// _WorldSpaceCameraPos.xyz ); i.dis;
		//	}
		//	ENDCG
		//}
	}
}
