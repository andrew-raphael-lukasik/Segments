using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Mathematics;

namespace Segments
{
	[WorldSystemFilter(0)]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	public class NativeArrayToSegmentsSystem : SystemBase
	{
		
		EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;
		List<Batch> _batches = new List<Batch>();

		public NativeList<JobHandle> Dependencies;
		public JobHandle ScheduledJobs => Dependency;


		protected override void OnCreate ()
		{
			_endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			Dependencies = new NativeList<JobHandle>( Allocator.Persistent );
		}


		protected override void OnDestroy ()
		{
			Dependency.Complete();
			JobHandle.CombineDependencies( Dependencies ).Complete();
			Dependencies.Dispose();

			foreach( var batch in _batches )
			{
				if( batch.entities.IsCreated )
				{
					EntityManager.DestroyEntity( batch.entities );
					batch.entities.Dispose();
				}
				// if( batch.buffer.IsCreated ) batch.buffer.Dispose();// don't - it's not my responsibility
			}
			_batches.Clear();
		}


		protected override void OnUpdate ()
		{
			var entityManager = EntityManager;
			var cmd = _endSimulationEcbSystem.CreateCommandBuffer();
			var segmentData = GetComponentDataFromEntity<Segment>( isReadOnly:true );

			if( Dependencies.Length!=0 )
			{
				Dependencies.Add( Dependency );
				Dependency = JobHandle.CombineDependencies( Dependencies );
				Dependencies.Clear();
			}

			for( int batchIndex=_batches.Count-1 ; batchIndex!=-1 ; batchIndex-- )
			{
				var batch = _batches[ batchIndex ];
				NativeArray<float3x2> buffer = batch.buffer;
				NativeArray<Entity> entities = batch.entities;
				int length = batch.length;

				if( buffer.IsCreated )
				{
					Job
						.WithName("component_data_update_job")
						.WithReadOnly( buffer ).WithNativeDisableContainerSafetyRestriction( buffer )
						.WithCode( () =>
						{
							for( int i=0 ; i<length ; i++ )
								cmd.SetComponent( entities[i] , new Segment{ start=buffer[i].c0 , end=buffer[i].c1 } );
						} )
						.WithBurst().Schedule();
				}
				else if( entities.IsCreated )
				{
					_batches.RemoveAt( batchIndex );
					entityManager.DestroyEntity( entities );
				}
			}

			_endSimulationEcbSystem.AddJobHandleForProducer( Dependency );
		}


		public void CreateBatch ( in Entity segmentPrefab , in int length , out NativeArray<float3x2> buffer )
		{
			buffer = new NativeArray<float3x2>( length , Allocator.Persistent );
			NativeArray<Entity> entities = EntityManager.Instantiate( segmentPrefab , length , Allocator.Persistent );
			_batches.Add( new Batch{
				prefab		= segmentPrefab ,
				length		= length ,
				entities	= entities ,
				buffer		= buffer
			} );
		}
		

		struct Batch
		{
			public Entity prefab;
			public int length;
			public NativeArray<Entity> entities;
			public NativeArray<float3x2> buffer;
		}


	}
}
