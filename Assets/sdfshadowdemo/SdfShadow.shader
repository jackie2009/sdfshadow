Shader "Unlit/SdfShadow"
{
	Properties
	{
		 
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
	 
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			 
			};

			struct v2f
			{
				float3 wpos : TEXCOORD0;
			 
				float4 vertex : SV_POSITION;
			};

			sampler3D _TestSdfTex;
			sampler3D _TestSdfTreeTex;
		 
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.wpos = mul(unity_ObjectToWorld, v.vertex);
				 
			 
				return o;
			}
			half getSdf(float3 wpos) {
				half3 uv = wpos.xzy / half3(100, 100, 20);
				uv.xy += 0.5;
				if (max(max(uv.x, uv.y), uv.z) > 1) return 10;
				return tex3D(_TestSdfTex, saturate(uv)).r;
			}	
		 
			fixed4 frag(v2f i) : SV_Target
			{
				half sdf = 1000;
			half3 ray = normalize(_WorldSpaceLightPos0.xyz);// +half3(sin(_Time.x * 10000), cos(_Time.x * 13001), cos(_Time.x * 10101)) * 0.01);

		
			 float3 nextWpos = i.wpos;
			 float dis = 1;
			 float csdf = 1.5;//表面1.5米开始避免自我遮挡
			for (int k = 0; k < 20; k++)
			{
				 nextWpos +=  ray *  abs(csdf);
				 
				 
				 csdf = getSdf(nextWpos);
				 sdf=min(sdf, csdf) ;
			  
			}
			 
			 
		return   (saturate(sdf)+0.5)/1.5;
		 
		  
			}
			ENDCG
		}
	}
}
