using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Rendering;
using Unity.Jobs;

namespace Segments
{

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
		[NativeDisableContainerSafetyRestriction][WriteOnly] DynamicBuffer<Bounds> Output;
		[ReadOnly] int OutputIndex;
		public BoundsJob ( NativeArray<float3x2> input , DynamicBuffer<Bounds> output , int outputIndex )
		{
			this.Input = input;
			this.InputLength = input.Length;
			this.Output = output;
			this.OutputIndex = outputIndex;

			Assert.IsTrue( outputIndex<output.Length , $"{nameof(outputIndex)}={outputIndex}, {nameof(output)}.Length={output.Length}" );
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
			Output[OutputIndex] = new Bounds{ min=combined.Min , max=combined.Max };
		}
	}

	[System.Obsolete("RenderMesh is no longer a temporary baking type now, replace")]
	[WithAll( typeof(Segment) )]
	partial struct PushBoundsJob : IJobEntity
	{
		[ReadOnly] public NativeArray<Bounds> Bounds;
		public void Execute ( [EntityIndexInQuery] in int entityIndexInQuery , in RenderMesh renderMesh )
		{
			if( entityIndexInQuery<Bounds.Length )
				renderMesh.mesh.bounds = Bounds[entityIndexInQuery];
		}
	}

	[System.Obsolete("RenderMesh is no longer a temporary baking type now, replace")]
	[WithAll( typeof(Segment) )]
	partial struct PushMeshDataJob : IJobEntity
	{
		[ReadOnly] public NativeArray<Mesh.MeshDataArray> MeshDataArrays;
		public void Execute ( [EntityIndexInQuery] in int entityIndexInQuery , in RenderMesh renderMesh )
		{
			var data = MeshDataArrays[entityIndexInQuery];
			Mesh.ApplyAndDisposeWritableMeshData(
				data: data ,
				mesh: renderMesh.mesh ,
				flags: MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
			);
			renderMesh.mesh.UploadMeshData( false );
		}
	}

}
