using UnityEngine;
using UnityEngine.Assertions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

namespace EcsLineRenderer.Authoring
{
	[DisallowMultipleComponent]
	// [RequiresEntityConversion]// doesn't, since Awake() converts to entity too
	public class EcsLineAuthoring : MonoBehaviour, IConvertGameObjectToEntity, ISerializationCallbackReceiver
	{


		public float3 start = new float3{};
		public float3 end = new float3{ x=1 , y=1 , z=1 };
		public float width = 0.1f;
		public Material materialOverride = null;
		public Color color = Color.white;

		
		#region OnValidate
		void ISerializationCallbackReceiver.OnAfterDeserialize () {}
		void ISerializationCallbackReceiver.OnBeforeSerialize ()
		{
			if( color.a==0 ) color.a = 1f;
		}
		#endregion


		void Awake ()
		{
			// convert to entity:
			var world = LineRendererWorld.GetOrCreateWorld();
			var entityManager = world.EntityManager;
			Entity entity = entityManager.CreateEntity();
			Convert( entity:entity , dstManager:entityManager , conversionSystem:null );
			
			Destroy( this );
		}


		public void Convert ( Entity entity , EntityManager dstManager , GameObjectConversionSystem conversionSystem )
		{
			dstManager.AddComponents( entity , Prototypes.segment_component_types );

			dstManager.SetComponentData( entity , new Segment{
				start	= this.start ,
				end		= this.end
			});
			dstManager.SetComponentData( entity , new SegmentWidth{
				Value	= (half) this.width
			});

			var renderMesh = Prototypes.renderMesh;
			if( materialOverride!=null ) renderMesh.material = materialOverride;
			Assert.IsNotNull(renderMesh.material,"renderMesh.material is null");
			dstManager.SetSharedComponentData( entity , renderMesh );
			
			dstManager.SetComponentData<RenderBounds>( entity , Prototypes.renderBounds );

			dstManager.AddComponent<MaterialColor>( entity );
			dstManager.SetComponentData( entity , new MaterialColor{
				Value = new float4{ x=color.r , y=color.g , z=color.b , w=color.a }
			});

			// we don't need those components:
			dstManager.RemoveComponent<Translation>( entity );
			dstManager.RemoveComponent<Rotation>( entity );
			dstManager.RemoveComponent<LocalToParent>( entity );
			dstManager.RemoveComponent<Parent>( entity );
			
			#if DEBUG
			dstManager.SetName( entity , gameObject.name );
			#endif
		}

		
	}
}
