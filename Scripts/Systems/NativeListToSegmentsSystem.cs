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
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	[UpdateBefore( typeof(SegmentTransformSystem) )]
	public class NativeListToSegmentsSystem : SystemBase
	{

		public const int k_max_num_segments = 100_000;// arbitrary but reasonable
		
		List<Batch> _batches = new List<Batch>();
		public NativeList<JobHandle> Dependencies;
		public JobHandle ScheduledJobs => Dependency;

		bool _disposed = false;


		protected override void OnCreate ()
		{
			Dependencies = new NativeList<JobHandle>( Allocator.Persistent );
		}


		protected override void OnDestroy ()
		{
			Dependency.Complete();
			JobHandle.CombineDependencies( Dependencies ).Complete();
			Dependencies.Dispose();

			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
				DestroyBatch( i , true );
			Assert.AreEqual( _batches.Count , 0 );

			_disposed = true;
		}


		protected override void OnUpdate ()
		{
			if( Dependencies.Length!=0 )
			{
				Dependencies.Add( Dependency );
				Dependency = JobHandle.CombineDependencies( Dependencies );
				Dependencies.Clear();
			}
			if( _batches.Count==0 ) return;

			var entityManager = EntityManager;
			var segmentData = GetComponentDataFromEntity<Segment>( isReadOnly:false );

			for( int batchIndex=_batches.Count-1 ; batchIndex!=-1 ; batchIndex-- )
			{
				var batch = _batches[ batchIndex ];
				NativeList<float3x2> buffer = batch.buffer;
				NativeList<Entity> entities = batch.entities;

				// int bufferSize = buffer.Length;// throws dependency errors
				int bufferSize = buffer.AsParallelReader().Length;
				if( bufferSize<0 || bufferSize>k_max_num_segments )// ugly temporary workaround that guesses when collection became deallocated
				{
					throw new System.Exception($"emergency stop for bufferSize:{bufferSize}, <b>DO NOT call Dispose() on segment buffer</b> (my guess is you did) but call {GetType().Name}_Instance.{nameof(DestroyBatch)}( buffer )");
					// this is for safety reasons as not throwing here in such case could fill entire memory available and crash >= 1 applications
					// BUT will also be thrown when you surpass it's upper limit by mistake or intentionally
				}
				
				if( entities.Length!=bufferSize )
				{
					if( entities.Length<bufferSize )
					{
						NativeArray<Entity> instantiated = entityManager.Instantiate( batch.prefab , bufferSize-entities.Length , Allocator.Temp );
						entities.AddRange( instantiated );
						instantiated.Dispose();
					}
					else
					{
						entityManager.DestroyEntity( entities.AsArray().Slice(bufferSize) );
						entities.Length = bufferSize;
					}
				}
				
				Job
					.WithName("component_data_update_job")
					.WithReadOnly( buffer ).WithNativeDisableContainerSafetyRestriction( buffer )
					.WithNativeDisableContainerSafetyRestriction( segmentData )
					.WithCode( () =>
					{
						for( int i=0 ; i<bufferSize ; i++ )
							segmentData[ entities[i] ] = new Segment{ start=buffer[i].c0 , end=buffer[i].c1 };
					} )
					.WithBurst().Schedule();
			}
		}


		/// <summary> Creates a new buffer array and mathing entities. </summary>
		public void CreateBatch ( in Entity segmentPrefab , out NativeList<float3x2> buffer )
		{
			buffer = new NativeList<float3x2>( Allocator.Persistent );
			_batches.Add( new Batch{
				prefab		= segmentPrefab ,
				entities	= new NativeList<Entity>( Allocator.Persistent ) ,
				buffer		= buffer
			} );
		}
		

		/// <summary> Disposes this buffer and destroys it's entities. </summary>
		/// <remarks> Use this to dispose your buffer correctly. It will call buffer.Dispose() so don't do that elsewhere. </remarks>
		public void DestroyBatch ( ref NativeList<float3x2> buffer , bool destroyPrefabEntity = false )
		{
			if( _disposed ) return;
			
			Dependency.Complete();
			
			var bufferByValue = buffer;
			int index = _batches.FindIndex( (batch)=>batch.buffer.Equals(bufferByValue) );
			if( index!=-1 )
				DestroyBatch( index , destroyPrefabEntity );
		}
		void DestroyBatch ( int index , bool destroyPrefabEntity )
		{
			var batch = _batches[index];
			_batches.RemoveAt( index );
			EntityManager.DestroyEntity( batch.entities );
			batch.entities.Dispose();
			batch.buffer.Dispose();
			if( destroyPrefabEntity )
				EntityManager.DestroyEntity( batch.prefab );
		}
		

		struct Batch
		{
			public Entity prefab;
			public NativeList<Entity> entities;
			public NativeList<float3x2> buffer;
		}


	}
}
