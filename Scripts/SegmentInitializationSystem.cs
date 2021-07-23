using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using BurstCompile = Unity.Burst.BurstCompileAttribute;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	internal class SegmentInitializationSystem : SystemBase
	{


		internal NativeList<Bounds> DefferedBounds = new NativeList<Bounds>( initialCapacity:2 , Allocator.Persistent );
		internal NativeList<JobHandle> DefferedBoundsJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent );
		internal NativeList<JobHandle> FillMeshDataArrayJobs = new NativeList<JobHandle>( initialCapacity:2 , Allocator.Persistent );
		internal Mesh.MeshDataArray[] MeshDataArrays = new Mesh.MeshDataArray[0];
		internal int numBatchesToProcess;


		protected override void OnCreate ()
		{
			this.OnUpdate();
		}


		protected override void OnDestroy ()
		{
			Dependency.Complete();
			JobHandle.CompleteAll( DefferedBoundsJobs );
			JobHandle.CompleteAll( FillMeshDataArrayJobs );
			if( DefferedBounds.IsCreated ) DefferedBounds.Dispose();
			if( DefferedBoundsJobs.IsCreated ) DefferedBoundsJobs.Dispose();
			if( FillMeshDataArrayJobs.IsCreated ) FillMeshDataArrayJobs.Dispose();
			Core.DestroyAllBatches();
		}


		protected override unsafe void OnUpdate ()
		{
			var batches = Core.Batches;

			// fulfill deffered dispose requests:
			Profiler.BeginSample("deffered_dispose");
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				if( batch.disposeRequested )
				{
					batch.DisposeNow();
					batches.RemoveAt(i);
				}
			}
			Profiler.EndSample();

			int numBatches = batches.Count;
			numBatchesToProcess = 0;

			// complete all batch dependencies:
			Profiler.BeginSample("complete_dependencies");
			NativeArray<JobHandle> batchDependencies = new NativeArray<JobHandle>( numBatches , Allocator.Temp );
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				batchDependencies[i] = batches[i].Dependency;
			JobHandle.CompleteAll( batchDependencies );
			Profiler.EndSample();

			// shedule indices job:
			Profiler.BeginSample("shedule_indices_job");
			int numAllIndices = 0;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				numAllIndices = math.max( numAllIndices , batches[i].buffer.Length*2 );
			var allIndices = new NativeArray<uint>( numAllIndices , Allocator.TempJob );
			var allIndicesJobHandle = new IndicesJob(allIndices).Schedule( allIndices.Length , 1024 );
			Profiler.EndSample();

			// shedule bounds job:
			Profiler.BeginSample("shedule_bounds_job");
			DefferedBoundsJobs.Length = numBatches;
			DefferedBounds.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				DefferedBoundsJobs[i] = new BoundsJob( buffer , DefferedBounds , i ).Schedule();
			}
			Profiler.EndSample();

			// create mesh data:
			Profiler.BeginSample("create_mesh_data");
			if( MeshDataArrays.Length!=numBatches ) MeshDataArrays = new Mesh.MeshDataArray[ numBatches ];
			FillMeshDataArrayJobs.Length = numBatches;
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				NativeArray<float3x2> buffer = batch.buffer;
				Mesh mesh = batch.mesh;
				int numVertices = buffer.Length * 2;
				int numIndices = numVertices;

				var indices = AsArray<uint>( allIndices.GetUnsafeReadOnlyPtr() , numIndices );
				MeshDataArrays[i] = Mesh.AllocateWritableMeshData(1);
				var meshData = MeshDataArrays[i][0];
				meshData.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor( VertexAttribute.Position , VertexAttributeFormat.Float32 , 3 ) );
				meshData.SetIndexBufferParams( numIndices , IndexFormat.UInt32 );
				var bufferFloat3 = AsArray<float3>( buffer.GetUnsafeReadOnlyPtr() , numVertices );
				
				JobHandle jobHandle =
				Job
					.WithName("fill_MeshDataArray_job")
					.WithCode( () =>
					{
						meshData.subMeshCount = 1;
						meshData.SetSubMesh(
							index:	0 ,
							desc:	new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) ,
							flags:	MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
						);

						var vertexData = meshData.GetVertexData<float3>();
						UnsafeUtility.MemCpy(
							destination:	vertexData.GetUnsafePtr() ,
							source:			buffer.GetUnsafeReadOnlyPtr() ,
							size:			UnsafeUtility.SizeOf<float3x2>()*buffer.Length
						);
						
						var indexBuffer = meshData.GetIndexData<uint>();
						UnsafeUtility.MemCpy(
							destination:	indexBuffer.GetUnsafePtr() ,
							source:			indices.GetUnsafeReadOnlyPtr() ,
							size:			UnsafeUtility.SizeOf<int>()*indices.Length
						);
					} )
					.WithBurst()
					.Schedule( allIndicesJobHandle );
				
				FillMeshDataArrayJobs[i] = jobHandle;
				batch.Dependency = jobHandle;
			}
			Profiler.EndSample();

			allIndices.Dispose();
			numBatchesToProcess = numBatches;
		}

		public unsafe NativeArray<T> AsArray <T> ( void* dataPtr , int dataLength ) where T : unmanaged
		{
			var nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>( dataPtr , dataLength , Allocator.None );
			#if ENABLE_UNITY_COLLECTIONS_CHECKS
			NativeArrayUnsafeUtility.SetAtomicSafetyHandle( ref nativeArray , AtomicSafetyHandle.Create() );
			#endif
			return nativeArray;
		}


	}


	[BurstCompile]
	unsafe struct IndicesJob : IJobParallelFor
	{
		[NativeDisableUnsafePtrRestriction][WriteOnly] uint* Ptr;
		public IndicesJob ( NativeArray<uint> output )
		{
			this.Ptr = (uint*) output.GetUnsafePtr();

			Assert.IsTrue( this.Ptr!=null );
		}
		void IJobParallelFor.Execute ( int index ) => Ptr[index] = (uint) index;
	}


	[BurstCompile]
	unsafe struct BoundsJob : IJob
	{
		[NativeDisableUnsafePtrRestriction][ReadOnly] float3x2* InputPtr;
		[ReadOnly] int InputLength;
		[NativeDisableUnsafePtrRestriction][WriteOnly] Bounds* OutputPtr;
		public BoundsJob ( NativeArray<float3x2> input , NativeArray<Bounds> output , int outputIndex )
		{
			this.InputPtr = (float3x2*) input.GetUnsafeReadOnlyPtr();
			this.InputLength = input.Length;
			this.OutputPtr = ( (Bounds*) output.GetUnsafePtr() ) + outputIndex;

			Assert.IsTrue( outputIndex<output.Length );
			Assert.IsTrue( this.InputPtr!=null );
			Assert.IsTrue( this.OutputPtr!=null );
		}
		void IJob.Execute ()
		{
			MinMaxAABB combined = MinMaxAABB.Empty;
			for( int i=InputLength-1 ; i!=-1 ; i-- )
			{
				combined.Encapsulate( InputPtr[i].c0 );
				combined.Encapsulate( InputPtr[i].c1 );
			}
			*OutputPtr = !combined.IsEmpty ? new Bounds{ min=combined.Min , max=combined.Max } : default(Bounds);
		}
	}


}
