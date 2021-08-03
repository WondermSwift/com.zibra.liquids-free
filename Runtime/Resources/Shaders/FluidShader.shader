Shader "MLSMPM/FluidShader"
{
	Properties
	{
		_Smoothness("Smoothness", Range(0, 1)) = 0.85
		_Metal("Metal", Range(0, 1)) = 0.25
		_Opacity("Opacity", Range(0, 10)) = 0.4
		_Shadowing("Shadowing", Range(0, 1)) = 0.4
		_RefrDistort("Refraction distort", Range(0,1.0)) = 0.2
		_RefrColor("Refraction color", Color) = (.34, .85, .92, 1) // color
		_ReflColor("Reflection Mods", Color) = (1, 1, 1, 1) // color
	}

	SubShader
	{

		Pass
		{
			CGPROGRAM
			// Physically based Standard lighting model
			#pragma multi_compile_instancing
			#pragma multi_compile __ CUSTOM_REFLECTION_PROBE
			#pragma instancing_options procedural:setup
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "Utils.cginc"
			#include "UnityStandardBRDF.cginc"
			#include "UnityImageBasedLighting.cginc"

			struct VIn
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct VOut
			{
				float4 position : POSITION;
				float3 raydir : TEXCOORD1;
				float2 uv : TEXCOORD0;
			};

			struct PSout
			{
				float4 color : COLOR;
				float depth : DEPTH;
			};

			float4 color;
			float diameter;
			float _Smoothness;
			float _Opacity;
			float _RefrDistort;
			float4 _RefrColor;
			float4 _ReflColor;
			float _Metal;
			float _Shadowing;
			float _Foam;
			float _FoamDensity;

			UNITY_DECLARE_TEXCUBE(_ReflProbe);

			float4 _ReflProbe_BoxMax;
			float4 _ReflProbe_BoxMin;
			float4 _ReflProbe_ProbePosition;
			float4 _ReflProbe_HDR;

			VOut vert(VIn input)
			{
				VOut output;
				float4x4 inverseP = inverse(UNITY_MATRIX_P);

				output.position = float4(2.0 * input.position.x, -2.0 * input.position.y, 0.001, 1.0);
				float3 direction = mul(inverseP, float4(output.position.xy, 1.0f, 1.0f)).xyz;

				output.raydir = mul(transpose(UNITY_MATRIX_V), float4(direction, 0.0f)).xyz;
				output.uv = input.uv;

				return output;
			}

	#ifdef SHADER_API_D3D11
			StructuredBuffer<float4> GridNormal;

			bool inContainer(float3 p)
			{
				float3 dx = (p - (containerPos - containerScale * 0.5)) / containerScale;
				return all(dx > 0.0) && all(dx < 1.0);
			}

			//trilinear grid interpolation 
			float4 trilinear(float3 p)
			{
				float3 node = getNodeF(p);
				float3 ni = floor(node);
				float3 nf = frac(node);

				//load the 8 node values
				float4 n000 = GridNormal[getNodeID(ni + float3(0,0,0))];
				float4 n001 = GridNormal[getNodeID(ni + float3(0,0,1))];
				float4 n010 = GridNormal[getNodeID(ni + float3(0,1,0))];
				float4 n011 = GridNormal[getNodeID(ni + float3(0,1,1))];
				float4 n100 = GridNormal[getNodeID(ni + float3(1,0,0))];
				float4 n101 = GridNormal[getNodeID(ni + float3(1,0,1))];
				float4 n110 = GridNormal[getNodeID(ni + float3(1,1,0))];
				float4 n111 = GridNormal[getNodeID(ni + float3(1,1,1))];

				//interpolate the node pairs along Z
				float4 n00 = lerp(n000, n001, nf.z);
				float4 n01 = lerp(n010, n011, nf.z);
				float4 n10 = lerp(n100, n101, nf.z);
				float4 n11 = lerp(n110, n111, nf.z);

				//interpolate the interpolated pairs along Y
				float4 n0 = lerp(n00, n01, nf.y);
				float4 n1 = lerp(n10, n11, nf.y);

				//interpolate the rest along X
				return lerp(n0, n1, nf.x);
			}
	#endif

			inline float ClipDepth(float z) //inverse of linearEyeDepth
			{
				return  (1.0 / z - _ZBufferParams.w) / _ZBufferParams.z;
			}

			sampler2D _Background;
			sampler2D _FluidColor;
			sampler2D _CameraDepthTexture;

			float3 BoxProjection(float3 rayOrigin, float3 rayDir, float3 cubemapPosition, float3 boxMin, float3 boxMax) {
				float3 tMin = (boxMin - rayOrigin) / rayDir;
				float3 tMax = (boxMax - rayOrigin) / rayDir;
				float3 t1 = min(tMin, tMax);
				float3 t2 = max(tMin, tMax);
				float tFar = min(min(t2.x, t2.y), t2.z);
				return normalize(rayOrigin + rayDir*tFar - cubemapPosition);
			};

			PSout frag(VOut output)
			{
				PSout fo;
				fo.color = fixed4(0,0,0,0);

				float4 pos = tex2D(_FluidColor, output.uv);
				float3 cameraPos = _WorldSpaceCameraPos;
				float3 cameraRay = normalize(output.raydir);
				float depth = pos.w;
				float3 newPos = cameraPos + cameraRay * depth;

				if (depth > 0.0 && depth < 1e4) //has hit the liquid
				{
					float4 f0 = trilinear(newPos.xyz);
					float3 normal = -normalize(f0.xyz);
					//which normal to use? make it density dependent
					float nu = smoothstep(0.0, _FoamDensity, f0.w);
					normal = normalize(lerp(normal,normalize(pos.xyz), 1.0 - nu));
					float rdotn = dot(normal, cameraRay);

					// lighting vectors:
					float3 worldView = -cameraRay;
					float3 lightDirWorld = normalize(_WorldSpaceLightPos0.xyz);
					half3 h = normalize(lightDirWorld + worldView);

					float nh = BlinnTerm(normal, h);
					float nl = DotClamped(normal, lightDirWorld);
					float nlsmooth = dot(normal, lightDirWorld) * 0.5 + 0.65;
					float nv = max(abs(dot(normal, worldView)), 1 - _ReflColor.w); //hardcoded to not be orthogonal - Mykhail varsion - mod

					float foamamount = _Foam * (1.0 - nu);

					float rough = clamp(1 - _Smoothness + 0.5 * foamamount, 0., 1.0);

					half V = SmithBeckmannVisibilityTerm(nl, nv, rough);
					half D = NDFBlinnPhongNormalizedTerm(nh, RoughnessToSpecPower(rough));
					float spec = (V * D) * (UNITY_PI / 4);
					spec = max(0, spec * nl);

					Unity_GlossyEnvironmentData g;
					g.roughness = rough;

				#ifdef CUSTOM_REFLECTION_PROBE
					g.reflUVW = BoxProjection(newPos.xyz, reflect(cameraRay, normal), 
						_ReflProbe_ProbePosition,
						_ReflProbe_BoxMin, _ReflProbe_BoxMax
					);
					float3 reflection = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(_ReflProbe), _ReflProbe_HDR, g); // * _ReflColor.xyz - bad result
				#else
					g.reflUVW = reflect(cameraRay, normal);
					g.reflUVW.y = g.reflUVW.y; //don't render the bottom part of the cubemap
					g.roughness = rough;
					float3 reflection = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g); // * _ReflColor.xyz - bad result
				#endif

					float fresnel = FresnelTerm(_Metal, nv) * _ReflColor.z;
					//refraction direction
					float3 refr = refract(cameraRay,normal, 0.8);
					//virtual camera plane, TODO: use the MVP matrix instead
					float3 d1 = normalize(cross(cameraRay, float3(0.0,1.0,0.0)));
					float3 d2 = normalize(cross(cameraRay, d1));
					//camera plane projection
					float2 del = _RefrDistort * float2(dot(d1, refr),dot(d2, refr));
					float3 refrcolor = tex2D(_Background, 0.95 * (output.uv - 0.5) + 0.5 + del).xyz;

					float depth1 = tex2D(_CameraDepthTexture, output.uv + del).x;

					float4 clipPos = mul(UNITY_MATRIX_VP, float4(newPos, 1));
					float smoothdepth = ClipDepth(clipPos.w);

					float fluid_thickness = clamp(abs(0.01 * (LinearEyeDepth(smoothdepth) - LinearEyeDepth(depth1)) / diameter), 0., 2.0);
					float3 opacity = 1.0 - exp(-fluid_thickness * _Opacity); //Beer–Lambert law

					reflection = lerp(reflection, reflection * reflection * 8.0 * _ReflColor.y , (1.0 - _ReflColor.x));

					float opac = clamp(opacity + foamamount, 0., 1.0);
					float3 foam = lerp(lerp(1., nlsmooth, opac) * _RefrColor.xyz, nlsmooth * 1.0, foamamount);
					float3 rcol = lerp(refrcolor, foam, opac);
					float3 base = lerp(rcol * (1. - (1. - foamamount) * _Shadowing * opacity), reflection, fresnel * _ReflColor.w);

					fo.depth = smoothdepth;
					fo.color.rgb = saturate(base + spec);
				}
				else
				{
					fo.color.xyz = tex2D(_Background, output.uv).xyz;
					fo.depth = 0.0;
				}

				return fo;
			}
			ENDCG
		}
	}
}
