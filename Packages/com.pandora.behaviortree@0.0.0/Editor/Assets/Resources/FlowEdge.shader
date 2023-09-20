Shader "Unlit/FlowEdge"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _UITime ("Time", Float) = 1.0
        _Speed ("Speed", Float) = 5.0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float4 _Color;
        
            float _UITime;
            float _Speed;
        
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // float t = _UITime * _Speed;
                // float3 color = _Color.rgb;
                // color *= (sin(t) + 1) * 0.5; 
                // return float4(color, _Color.a);

                float t = _UITime * _Speed;
                return _Color * (sin(t) + 1) * 0.5;
            }

            ENDHLSL
        }

    }
}
