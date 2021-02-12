using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

using Segments.Internal;

namespace Segments
{
	public static class Prototypes
	{

		static Prototypes ()
		{
			worldSystems = new System.Type[]{
					typeof(NativeArrayToSegmentsSystem)
				,	typeof(NativeListToSegmentsSystem)
			};

			segment_components = new ComponentType[]{
					typeof(Segment)
				,	typeof(SegmentWidth)
				,	typeof(SegmentAspectRatio)
				
				,	typeof(LocalToWorld)
				,	typeof(RenderMesh)
				,	typeof(RenderBounds)
				,	typeof(WorldRenderBounds)
				,	ComponentType.ChunkComponent<ChunkWorldRenderBounds>()

				// ,	typeof(MaterialColor)// change: add this manually

				#if ENABLE_HYBRID_RENDERER_V2
				// ,   typeof(AmbientProbeTag)
				// ,   typeof(PerInstanceCullingTag)
				,   typeof(WorldToLocal_Tag)
				,   typeof(BuiltinMaterialPropertyUnity_RenderingLayer)
				,   typeof(BuiltinMaterialPropertyUnity_LightData)
				#endif
			};
			segment_component_types = new ComponentTypes( segment_components );
			
			segment_prefab_components =
				new ComponentType[]{ typeof(Prefab) }
				.Concat( segment_components )
				.ToArray();
			segment_prefab_components_types = new ComponentTypes( segment_prefab_components );
		}

		public static readonly RenderMesh renderMesh = new RenderMesh{
			mesh			= ResourceProvider.default_mesh ,
			material		= ResourceProvider.default_material ,
			subMesh			= 0 ,
			layer			= 1 ,
			castShadows		= UnityEngine.Rendering.ShadowCastingMode.Off ,
			receiveShadows	= false ,
		};

		public static readonly RenderBounds renderBounds = new RenderBounds{
			Value	= ResourceProvider.default_mesh.bounds.ToAABB()
		};

		public static readonly Segment segment = new Segment{
			start	= new float3{ y=1e4f } ,
			end		= new float3{ y=1e4f , x=1f }
		};

		public const float k_defaul_segment_width = 0.002f;
		public static readonly SegmentWidth segmentWidth = new SegmentWidth{
			Value	= (half) k_defaul_segment_width
		};

		public static readonly ComponentTypes segment_component_types;
		public static readonly ComponentType[] segment_components;

		public static readonly ComponentTypes segment_prefab_components_types;
		public static readonly ComponentType[] segment_prefab_components;

		public static readonly System.Type[] worldSystems;

	}
}
