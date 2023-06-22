using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Segments
{

	internal struct Singleton : IComponentData {}

	[InternalBufferCapacity(0)]
	internal struct MeshDataArrayElement : IBufferElementData
	{
		public Mesh.MeshDataArray Value;
	}

	[InternalBufferCapacity(0)]
	internal struct DeferredBoundsElement : IBufferElementData
	{
		public Bounds Value;
	}

	[InternalBufferCapacity(0)]
	internal struct DeferredBoundsJobsElement : IBufferElementData
	{
		public JobHandle Value;
	}

	[InternalBufferCapacity(0)]
	internal struct FillMeshDataArrayJobsElement : IBufferElementData
	{
		public JobHandle Value;
	}

	[InternalBufferCapacity(1)]
	internal struct NumBatchesToPushElement : IBufferElementData
	{
		public int Value;
	}
	
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	[Unity.Burst.BurstCompile]
	internal partial struct SegmentInitializationSystem : ISystem
	{
		static readonly ProfilerMarker
            ____deferred_dispose = new ProfilerMarker("deferred Dispose") ,
            ____complete_dependencies = new ProfilerMarker("Complete dependencies"),
            ____schedule_indices_job = new ProfilerMarker("Schedule indices job"),
            ____schedule_bounds_job = new ProfilerMarker("Schedule bounds job"),
            ____create_mesh_data = new ProfilerMarker("create mesh data"),
			____batch = new ProfilerMarker("batch"),
			____AllocateWritableMeshData = new ProfilerMarker("Mesh.AllocateWritableMeshData"),
			____SetBufferParams = new ProfilerMarker("Set___BufferParams"),
			____ScheduleJobs = new ProfilerMarker("Schedule jobs");

		[Unity.Burst.BurstCompile]
		public void OnCreate ( ref SystemState state )
		{
			Entity singleton = state.EntityManager.CreateSingleton<Singleton>("Segments");
			state.EntityManager.AddBuffer<MeshDataArrayElement>( singleton );
			state.EntityManager.AddBuffer<DeferredBoundsElement>( singleton );
			state.EntityManager.AddBuffer<DeferredBoundsJobsElement>( singleton );
			state.EntityManager.AddBuffer<FillMeshDataArrayJobsElement>( singleton );
			var numBatchesToPush = state.EntityManager.AddBuffer<NumBatchesToPushElement>( singleton );
			numBatchesToPush.Add( new NumBatchesToPushElement{ Value=0 } );
		}

		public void OnDestroy ( ref SystemState state )
		{
			state.Dependency.Complete();

			if( SystemAPI.TryGetSingletonEntity<Singleton>(out Entity singleton) )
				state.EntityManager.DestroyEntity( singleton );
			
			Core.DestroyAllBatches();
		}

		public void OnUpdate ( ref SystemState state )
		{
			Entity singleton = SystemAPI.GetSingletonEntity<Singleton>();
			var meshDataArrays = SystemAPI.GetBuffer<MeshDataArrayElement>( singleton );
			var deferredBounds = SystemAPI.GetBuffer<DeferredBoundsElement>( singleton );
			var deferredBoundsJobs = SystemAPI.GetBuffer<DeferredBoundsJobsElement>( singleton );
			var fillMeshDataArrayJobs = SystemAPI.GetBuffer<FillMeshDataArrayJobsElement>( singleton );
			var numBatchesToPush = SystemAPI.GetBuffer<NumBatchesToPushElement>( singleton );

			// fulfill deferred dispose requests:
			____deferred_dispose.Begin();
			var batches = Core.Batches;
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
			numBatchesToPush[0] = new NumBatchesToPushElement{ Value=0 };

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
			deferredBoundsJobs.Length = numBatches;
			deferredBounds.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();

				var jobHandle = new BoundsJob( buffer , deferredBounds , i ).Schedule();
				
				deferredBoundsJobs[i] = new DeferredBoundsJobsElement{ Value=jobHandle };
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
			}
			____schedule_bounds_job.End();

			// create mesh data:
			____create_mesh_data.Begin();
			meshDataArrays.Length = numBatches;
			fillMeshDataArrayJobs.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				____batch.Begin();
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;

				____AllocateWritableMeshData.Begin();
				meshDataArrays[i] = new MeshDataArrayElement{ Value = Mesh.AllocateWritableMeshData(1) };
				____AllocateWritableMeshData.End();

				____SetBufferParams.Begin();
				var meshData = meshDataArrays[i].Value[0];
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
				fillMeshDataArrayJobs[i] = new FillMeshDataArrayJobsElement{ Value=jobHandle };
				
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
				____ScheduleJobs.End();
				____batch.End();
			}

			allIndices.Dispose( JobHandle.CombineDependencies(fillMeshDataArrayJobs.Reinterpret<JobHandle>().AsNativeArray()) );
			____create_mesh_data.End();

			numBatchesToPush[0] = new NumBatchesToPushElement{ Value=numBatches };
		}
	}

	[Unity.Burst.BurstCompile]
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

	/// <summary> This job adds overhead but is here to isolate user-facing Batch data from the internal Mesh one. </summary>
	[Unity.Burst.BurstCompile]
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

	/// <summary> This job adds overhead but is here to isolate user-facing Batch data from the internal Mesh one. </summary>
	[Unity.Burst.BurstCompile]
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

	[Unity.Burst.BurstCompile]
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

	[Unity.Burst.BurstCompile]
	struct BoundsJob : IJob
	{
		[ReadOnly] NativeArray<float3x2> Input;
		[ReadOnly] int InputLength;
		[NativeDisableContainerSafetyRestriction][WriteOnly] DynamicBuffer<DeferredBoundsElement> Output;
		[ReadOnly] int OutputIndex;
		public BoundsJob ( NativeArray<float3x2> input , DynamicBuffer<DeferredBoundsElement> output , int outputIndex )
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
			Bounds result = !combined.IsEmpty
				?	new Bounds{ min=combined.Min , max=combined.Max }
				:	default(Bounds);
			Output[OutputIndex] = new DeferredBoundsElement{ Value=result };
		}
	}

}
