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
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	internal partial class SegmentInitializationSystem : SystemBase
	{

		static readonly ProfilerMarker
            ____deferred_dispose = new ProfilerMarker("deferred_dispose") ,
            ____complete_dependencies = new ProfilerMarker("complete_dependencies"),
            ____schedule_indices_job = new ProfilerMarker("schedule_indices_job"),
            ____schedule_bounds_job = new ProfilerMarker("schedule_bounds_job"),
            ____create_mesh_data = new ProfilerMarker("create_mesh_data");
		internal NativeList<Bounds> DefferedBounds = new NativeList<Bounds>( initialCapacity:2 , Allocator.Persistent );
		internal NativeList<JobHandle> DefferedBoundsJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent );
		internal NativeList<JobHandle> FillMeshDataArrayJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent );
		internal Mesh.MeshDataArray[] MeshDataArrays = new Mesh.MeshDataArray[0];
		internal int numBatchesToPush;

		protected override void OnCreate ()
		{
			this.OnUpdate();
		}

		protected override void OnDestroy ()
		{
			Dependency.Complete();
			JobHandle.CompleteAll( DefferedBoundsJobs.AsArray() );
			JobHandle.CompleteAll( FillMeshDataArrayJobs.AsArray() );
			if( DefferedBounds.IsCreated ) DefferedBounds.Dispose();
			if( DefferedBoundsJobs.IsCreated ) DefferedBoundsJobs.Dispose();
			if( FillMeshDataArrayJobs.IsCreated ) FillMeshDataArrayJobs.Dispose();
			Core.DestroyAllBatches();
		}

		protected override void OnUpdate ()
		{
			var batches = Core.Batches;

			// fulfill deffered dispose requests:
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
			numBatchesToPush = 0;

			// complete all batch dependencies:
			____complete_dependencies.Begin();
			NativeArray<JobHandle> batchDependencies = new NativeArray<JobHandle>( numBatches , Allocator.Temp );
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
			DefferedBoundsJobs.Length = numBatches;
			DefferedBounds.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();

				var jobHandle = new BoundsJob( buffer , DefferedBounds.AsArray() , i ).Schedule();
				
				DefferedBoundsJobs[i] = jobHandle;
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
			}
			____schedule_bounds_job.End();

			// create mesh data:
			____create_mesh_data.Begin();
			if( MeshDataArrays.Length!=numBatches ) MeshDataArrays = new Mesh.MeshDataArray[ numBatches ];
			FillMeshDataArrayJobs.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer.AsArray();
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;

				MeshDataArrays[i] = Mesh.AllocateWritableMeshData(1);
				var meshData = MeshDataArrays[i][0];
				meshData.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor( VertexAttribute.Position , VertexAttributeFormat.Float32 , 3 ) );
				meshData.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				
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
				FillMeshDataArrayJobs[i] = jobHandle;
				batch.Dependency = JobHandle.CombineDependencies( batch.Dependency , jobHandle );
			}

			allIndices.Dispose( JobHandle.CombineDependencies(FillMeshDataArrayJobs.AsArray()) );
			____create_mesh_data.End();

			numBatchesToPush = numBatches;
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
