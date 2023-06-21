using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

using BurstCompile = Unity.Burst.BurstCompileAttribute;

namespace Segments
{
	internal struct Singleton : IComponentData {} 
	internal struct SegmentsSharedData : ISharedComponentData, System.IEquatable<SegmentsSharedData>
    {
		public NativeList<Mesh.MeshDataArray> MeshDataArrays;
		public NativeList<Bounds> DeferredBounds;
		public NativeList<JobHandle> DeferredBoundsJobs;
		public NativeList<JobHandle> FillMeshDataArrayJobs;
		public NativeArray<int> NumBatchesToPush;

        public bool Equals ( SegmentsSharedData other ) => this.GetHashCode()==other.GetHashCode();
        public override int GetHashCode () => System.HashCode.Combine( base.GetHashCode() , MeshDataArrays , DeferredBounds , DeferredBoundsJobs , FillMeshDataArrayJobs , NumBatchesToPush );
    }

	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	[BurstCompile]
	internal partial struct SegmentInitializationSystem : ISystem
	{
		static readonly ProfilerMarker
            ____deferred_dispose = new ProfilerMarker("deferred Dispose") ,
            ____complete_dependencies = new ProfilerMarker("Complete dependencies"),
            ____schedule_indices_job = new ProfilerMarker("Schedule indices job"),
            ____schedule_bounds_job = new ProfilerMarker("Schedule bounds job"),
            ____create_mesh_data = new ProfilerMarker("create mesh data"),
			____AllocateWritableMeshData = new ProfilerMarker("Mesh.AllocateWritableMeshData"),
			____SetBufferParams = new ProfilerMarker("Set___BufferParams"),
			____ScheduleJobs = new ProfilerMarker("Schedule jobs");

		public void OnCreate ( ref SystemState state )
		{
			Entity singleton = state.EntityManager.CreateSingleton<Singleton>( typeof(Singleton).FullName );
			state.EntityManager.AddSharedComponentManaged( singleton , new SegmentsSharedData{
				MeshDataArrays = new NativeList<Mesh.MeshDataArray>( initialCapacity:2 , Allocator.Persistent ) ,
				DeferredBounds = new NativeList<Bounds>( initialCapacity:2 , Allocator.Persistent ) ,
				DeferredBoundsJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent ) ,
				FillMeshDataArrayJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent ) ,
				NumBatchesToPush = new NativeArray<int>( 1 , Allocator.Persistent ) ,
			} );
		}

		public void OnDestroy ( ref SystemState state )
		{
			state.Dependency.Complete();

			if( SystemAPI.TryGetSingletonEntity<Singleton>(out Entity entity) )
			{
				var systemData = state.EntityManager.GetSharedComponentManaged<SegmentsSharedData>(entity);
				JobHandle.CompleteAll( systemData.DeferredBoundsJobs.AsArray() );
				JobHandle.CompleteAll( systemData.FillMeshDataArrayJobs.AsArray() );
				systemData.DeferredBounds.Dispose();
				systemData.DeferredBoundsJobs.Dispose();
				systemData.FillMeshDataArrayJobs.Dispose();
				systemData.NumBatchesToPush.Dispose();
			}
			Core.DestroyAllBatches();
		}

		public void OnUpdate ( ref SystemState state )
		{
			var batches = Core.Batches;
			var systemData = state.EntityManager.GetSharedComponentManaged<SegmentsSharedData>( SystemAPI.GetSingletonEntity<Singleton>() );

			// fulfill deferred dispose requests:
			____deferred_dispose.Begin();
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				if( batch.disposeRequested )
				{
					batch.DisposeImmediate();
					batches.RemoveAt(i);
				}
			}
			____deferred_dispose.End();

			int numBatches = batches.Count;
			systemData.NumBatchesToPush[0] = 0;

			// complete all batch dependencies:
			____complete_dependencies.Begin();
			var batchDependencies = new NativeArray<JobHandle>( numBatches , Allocator.Temp );
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				batchDependencies[i] = batches[i].Dependency;
			JobHandle.CompleteAll( batchDependencies );
			____complete_dependencies.End();

			// schedule indices job:
			____schedule_indices_job.Begin();
			int numAllIndices = 0;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				numAllIndices = math.max( numAllIndices , batches[i].buffer.Length*2 );
			var allIndices = new NativeArray<uint>( numAllIndices , Allocator.TempJob );
			var allIndicesJobHandle = new IndicesJob(allIndices).Schedule( allIndices.Length , 1024 );
			____schedule_indices_job.End();

			// schedule bounds job:
			____schedule_bounds_job.Begin();
			systemData.DeferredBoundsJobs.Length = numBatches;
			systemData.DeferredBounds.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();

				var jobHandle = new BoundsJob( buffer , systemData.DeferredBounds.AsArray() , i ).Schedule();
				
				systemData.DeferredBoundsJobs[i] = jobHandle;
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
			}
			____schedule_bounds_job.End();

			// create mesh data:
			____create_mesh_data.Begin();
			systemData.MeshDataArrays.Length = numBatches;
			systemData.FillMeshDataArrayJobs.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;

				____AllocateWritableMeshData.Begin();
				systemData.MeshDataArrays[i] = Mesh.AllocateWritableMeshData(1);
				____AllocateWritableMeshData.End();

				____SetBufferParams.Begin();
				var meshData = systemData.MeshDataArrays[i][0];
				meshData.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor( VertexAttribute.Position , VertexAttributeFormat.Float32 , 3 ) );
				meshData.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				____SetBufferParams.End();
				
				____ScheduleJobs.Begin();
				JobHandle setupSubmeshJob = new SetupSubmeshJob{
					meshData = meshData ,
					numIndices = numIndices ,
				}.Schedule( default(JobHandle) );
				
				JobHandle copyVerticesJob = new CopyVerticesJob{
					meshData = meshData ,
					buffer = buffer ,
				}.Schedule( setupSubmeshJob );
				
				JobHandle copyIndicesJob = new CopyIndicesJob{
					meshData = meshData ,
					numIndices = numIndices ,
					allIndices = allIndices ,
				}.Schedule( JobHandle.CombineDependencies(setupSubmeshJob,allIndicesJobHandle) );
				
				JobHandle jobHandle = JobHandle.CombineDependencies( copyVerticesJob , copyIndicesJob );
				systemData.FillMeshDataArrayJobs[i] = jobHandle;
				
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
				____ScheduleJobs.End();
			}

			allIndices.Dispose( JobHandle.CombineDependencies(systemData.FillMeshDataArrayJobs.AsArray()) );
			____create_mesh_data.End();

			systemData.NumBatchesToPush[0] = numBatches;
		}
	}

	[BurstCompile]
	struct SetupSubmeshJob : IJob
	{
		public Mesh.MeshData meshData;
		public int numIndices;
		void IJob.Execute ()
		{
			meshData.subMeshCount = 1;
			meshData.SetSubMesh(
				index:	0 ,
				desc:	new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) ,
				flags:	MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
			);
		}
	}

	[BurstCompile]
	struct CopyVerticesJob : IJob
	{
		public Mesh.MeshData meshData;
		[ReadOnly] public NativeArray<float3x2> buffer;
		void IJob.Execute ()
		{
			var vertexBuffer = meshData.GetVertexData<float3x2>();
			buffer.CopyTo( vertexBuffer );
		}
	}

	[BurstCompile]
	struct CopyIndicesJob : IJob
	{
		public Mesh.MeshData meshData;
		public int numIndices;
		[ReadOnly] public NativeArray<uint> allIndices;
		void IJob.Execute ()
		{
			var indices = allIndices.GetSubArray( 0 , numIndices );
			var indexBuffer = meshData.GetIndexData<uint>();
			indices.CopyTo( indexBuffer );
		}
	}

	[BurstCompile]
	struct IndicesJob : IJobParallelFor
	{
		[WriteOnly] NativeArray<uint> Output;
		public IndicesJob ( NativeArray<uint> output )
		{
			this.Output = output;

			Assert.IsTrue( this.Output.IsCreated );
		}
		void IJobParallelFor.Execute ( int index ) => Output[index] = (uint) index;
	}

	[BurstCompile]
	struct BoundsJob : IJob
	{
		[ReadOnly] NativeArray<float3x2> Input;
		[ReadOnly] int InputLength;
		[NativeDisableContainerSafetyRestriction][WriteOnly] NativeArray<Bounds> Output;
		[ReadOnly] int OutputIndex;
		public BoundsJob ( NativeArray<float3x2> input , NativeArray<Bounds> output , int outputIndex )
		{
			this.Input = input;
			this.InputLength = input.Length;
			this.Output = output;
			this.OutputIndex = outputIndex;

			Assert.IsTrue( outputIndex<output.Length );
			Assert.IsTrue( this.Input.IsCreated );
			Assert.IsTrue( this.Output.IsCreated );
		}
		void IJob.Execute ()
		{
			MinMaxAABB combined = MinMaxAABB.Empty;
			for( int i=InputLength-1 ; i!=-1 ; i-- )
			{
				combined.Encapsulate( Input[i].c0 );
				combined.Encapsulate( Input[i].c1 );
			}
			Output[OutputIndex] = !combined.IsEmpty
				?	new Bounds{ min=combined.Min , max=combined.Max }
				:	default(Bounds);
		}
	}

}
