using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Transforms;

namespace EcsLineRenderer
{
	[WorldSystemFilter( WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor )]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public class SegmentInitializationSystem : SystemBase
	{

		EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;

		protected override void OnCreate ()
		{
			_endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate ()
		{
			var renderMesh = Prototypes.renderMesh;
			var renderBounds = Prototypes.renderBounds;
			var ecb = _endSimulationEcbSystem.CreateCommandBuffer();

			Entities
				.WithName("add_components_job_shared_segment_material_override")
				.WithNone<RenderMesh>()
				.ForEach( ( in Entity entity , in SegmentSharedMaterialOverride material , in Segment segment ) =>
				{
					renderMesh.material = material.Value;
					ecb.AddSharedComponent( entity , renderMesh );
					ecb.RemoveComponent<SegmentSharedMaterialOverride>( entity );

					ecb.AddComponent<LocalToWorld>( entity );
					
					ecb.AddComponent<RenderBounds>( entity );
					ecb.SetComponent( entity , renderBounds );

					ecb.AddComponent<WorldRenderBounds>( entity );
					ecb.AddComponent<SegmentAspectRatio>( entity );
				})
				.WithoutBurst().Run();

			Entities
				.WithName("add_components_job")
				.WithNone<RenderMesh>()
				.ForEach( ( in Entity entity , in SegmentMaterialOverride material , in Segment segment ) =>
				{
					renderMesh.material = material;
					ecb.AddSharedComponent( entity , renderMesh );
					ecb.RemoveComponent<SegmentMaterialOverride>( entity );

					ecb.AddComponent<LocalToWorld>( entity );
					
					ecb.AddComponent<RenderBounds>( entity );
					ecb.SetComponent( entity , renderBounds );

					ecb.AddComponent<WorldRenderBounds>( entity );
					ecb.AddComponent<SegmentAspectRatio>( entity );
				})
				.WithoutBurst().Schedule();

			_endSimulationEcbSystem.AddJobHandleForProducer( Dependency );
		}

	}
}
