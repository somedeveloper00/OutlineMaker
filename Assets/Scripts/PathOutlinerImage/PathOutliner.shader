Shader "Path Outliner"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            
            uniform float4 _Centers[64];
            uniform float _Radiuses[64];
            float _Count;
            float _Length;
            fixed4 _Color;

            float is_inside(const float2 uv, const float thickness)
            {
                for (int i = 0; i < _Count; i++)
                {
                    const float2 c1 = _Centers[i].xy;
                    const float2 c2 = _Centers[i + 1].xy;
                    const float r1 = max(0, _Radiuses[i] - thickness);
                    const float r2 = max(0, _Radiuses[i + 1] - thickness);
                    
                    // check if inside circle
                    // if(distance(uv, c1) <= r1)
                    //     return 1;
                    
                    
                    // check if between two circles
                    if(i == _Count - 1) continue;
                    const float d = dot(uv - c1, c2 - c1) / distance(c2, c1);
                    const float v = saturate(d / distance(c1, c2));
                    const float2 p = lerp(c1, c2, v);
                    const float r = lerp(r1, r2, v);
                    if(distance(uv, p) <= r)
                        return 1;
                }
                return 0;
            }

            float is_outline(const float2 uv, float thickness)
            {
                if(is_inside(uv, -thickness/2) && !is_inside(uv, thickness/2))
                    return 1;
                
                return 0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; //TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // shade
                fixed4 col = is_outline(i.uv, _Length*1.5) * _Color * 0.5;
                
                // main color
                col += is_outline(i.uv, _Length) * _Color * 0.5;

                return col;
            }
            ENDCG
        }
    }
}
