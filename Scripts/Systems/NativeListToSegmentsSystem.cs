using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Burst;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	[UpdateBefore( typeof(SegmentTransformSystem) )]
	public class NativeListToSegmentsSystem : SystemBase
	{

		public const int k_max_num_segments_default = 100_000;// arbitrary but reasonable
		public int MAX_NUM_SEGMENTS = k_max_num_segments_default;
		
		List<Batch> _batches = new List<Batch>();
		bool _isSystemDestroyed = false;


		protected override void OnDestroy ()
		{
			Dependency.Complete();

			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
			{
				_batches[i].dependency.Complete();
				DestroyBatch( i , true );
			}
			Assert.AreEqual( _batches.Count , 0 );

			_isSystemDestroyed = true;
		}


		protected override void OnUpdate ()
		{
			for( int i=_batches.Count-1 ; i!=-1 ; i-- )
				if( _batches[i].destroy )
					DestroyBatch( index:i , destroyPrefabEntity:_batches[i].destroyPrefabEntity );

			int numBatches = _batches.Count;
			{
				var deps = new NativeArray<JobHandle>( numBatches+1 , Allocator.Temp );
				for( int i=0 ; i<numBatches ; i++ )
					deps[i] = _batches[i].dependency;
				deps[numBatches] = Dependency;
				Dependency = JobHandle.CombineDependencies( deps );
			}
			if( numBatches==0 ) return;

			var entityManager = EntityManager;
			var segmentData = GetComponentDataFromEntity<Segment>( isReadOnly:false );

			var dependencies = new NativeList<JobHandle>( initialCapacity:numBatches+1 , Allocator.Temp );
			for( int batchIndex=numBatches-1 ; batchIndex!=-1 ; batchIndex-- )
			{
				var batch = _batches[ batchIndex ];
				if( !batch.isBufferDirty )
					return;
				
				NativeList<float3x2> buffer = batch.buffer;
				NativeList<Entity> entities = batch.entities;

				// int bufferSize = buffer.Length;// throws dependency errors
				int bufferSize = buffer.AsParallelReader().Length;
				if( bufferSize<0 )// ugly temporary workaround that guesses when collection became deallocated
				{
					throw new System.Exception($"emergency stop for bufferSize:{bufferSize}, <b>DO NOT call Dispose() on segment buffer</b> (my guess is you did) but call {GetType().Name}_Instance.{nameof(DestroyBatch)}( buffer )");
					// this is for safety reasons as not throwing here in such case could fill entire memory available and crash >= 1 applications
					// BUT will also be thrown when you surpass it's upper limit by mistake or intentionally
				}
				bufferSize = math.min( bufferSize , MAX_NUM_SEGMENTS );// max is max, ignores everything north of that
				
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
				
				var job = new SegmentUpdateJob{
					entities		= entities ,
					buffer			= buffer ,
					segmentData		= segmentData
				};
				
				var jobHandle = job.Schedule( arrayLength:bufferSize , innerloopBatchCount:128 , Dependency );
				dependencies.Add( jobHandle );
			}
			if( dependencies.Length!=0 )
			{
				dependencies.Add( Dependency );
				Dependency = JobHandle.CombineDependencies( dependencies );
				// dependencies.Clear();
			}
		}


		public void CreateBatch ( in Entity segmentPrefab , out Batch batch )
		{
			var buffer = new NativeList<float3x2>( Allocator.Persistent );
			batch = new Batch(
				prefab:		segmentPrefab ,
				entities:	new NativeList<Entity>( Allocator.Persistent ) ,
				buffer:		buffer
			);
			_batches.Add( batch );
		}

		[System.Obsolete("Replace with CreateBatch( Entity segmentPrefab , out Batch batch )")]
		/// <summary> Creates a new buffer array and pool of entities to mirror that buffer. </summary>
		public void CreateBatch ( in Entity segmentPrefab , out NativeList<float3x2> buffer )
		{
			buffer = new NativeList<float3x2>( Allocator.Persistent );
			_batches.Add( new Batch(
				prefab:		segmentPrefab ,
				entities:	new NativeList<Entity>( Allocator.Persistent ) ,
				buffer:		buffer
			) );
		}
		

		[System.Obsolete("Replace with batch.Destroy()")]
		/// <summary> Disposes this buffer and destroys it's entities. </summary>
		/// <remarks> Use this to dispose your buffer correctly. It will call buffer.Dispose() so don't do that elsewhere. </remarks>
		public void DestroyBatch ( ref NativeList<float3x2> buffer , bool destroyPrefabEntity = false )
		{
			if( _isSystemDestroyed ) return;
			
			Dependency.Complete();
			
			var bufferByValue = buffer;
			int index = _batches.FindIndex( (batch)=>batch.buffer.Equals(bufferByValue) );
			if( index!=-1 )
				DestroyBatch( index , destroyPrefabEntity );
		}
		void DestroyBatch ( int index , bool destroyPrefabEntity )
		{
			var batch = _batches[index];
			if( batch.isDisposed ) return;

			_batches.RemoveAt( index );
			EntityManager.DestroyEntity( batch.entities );
			batch.entities.Dispose();
			batch.buffer.Dispose();
			if( destroyPrefabEntity )
				EntityManager.DestroyEntity( batch.prefab );
			
			batch.isDisposed = true;
		}
		

		public class Batch
		{
			public readonly Entity prefab;
			internal NativeList<Entity> entities;
			public NativeList<float3x2> buffer;
			public JobHandle dependency;
			public bool isBufferDirty;
			internal bool destroy;
			internal bool destroyPrefabEntity;
			internal bool isDisposed;
			public Batch ( Entity prefab , NativeList<Entity> entities , NativeList<float3x2> buffer )
			{
				this.prefab = prefab;
				this.entities = entities;
				this.buffer = buffer;
				this.isBufferDirty = true;
				this.destroy = false;
				this.destroyPrefabEntity = false;
				this.isDisposed = false;
			}
			public void SetDirty ( bool value ) => this.isBufferDirty = value;
			/// <summary> Deffered dispose. </summary>
			/// <remarks> Flags batch data and it's entities for disposal. </summary>
			public void Destroy ( bool destroyPrefabEntity = false )
			{
				this.destroy = true;
				this.destroyPrefabEntity = destroyPrefabEntity;
			}
			public int Length { get=> this.buffer.Length; set=>this.buffer.Length=value; }
		}

		
		[BurstCompile]
		public struct SegmentUpdateJob : IJobParallelFor
		{
			[ReadOnly]
				public NativeList<Entity> entities;
			[ReadOnly]
				public NativeList<float3x2> buffer;
			[WriteOnly][NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction]
				public ComponentDataFromEntity<Segment> segmentData;
			void IJobParallelFor.Execute ( int index )
			{
				segmentData[ entities[index] ] = new Segment{ start=buffer[index].c0 , end=buffer[index].c1 };
			}
		}


	}
}
