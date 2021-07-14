using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Segments
{
	public class Batch
	{

		/// <summary> DO NOT call <see cref="buffer"/>.Dispose(). Call <see cref="Batch.Dispose()"/>; instead. </summary>
		/// <remarks> Calling <see cref="buffer"/>.Dispose() will result in undefined program behaviour (crash). </remarks>
		public NativeList<float3x2> buffer;

		public Material material;
		internal Mesh mesh;
		public JobHandle Dependency;
		internal bool isDisposed;
		
		public static readonly VertexAttributeDescriptor[] layout = new[]{ new VertexAttributeDescriptor( VertexAttribute.Position , VertexAttributeFormat.Float32 , 3 ) };

		public Batch ( NativeList<float3x2> buffer , Material mat )
		{
			this.buffer = buffer;
			this.isDisposed = false;
			
			this.material = new Material( mat );
			this.material.name = $"{mat.name} (runtime copy #{this.material.GetHashCode()})";
			
			this.mesh = new Mesh();
			this.mesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
			// this.mesh.SetSubMesh( 0 , new SubMeshDescriptor( indexStart:0, indexCount:0 , topology:MeshTopology.Lines ) );
			this.mesh.MarkDynamic();
			this.mesh.subMeshCount = 1;
			// this.mesh.SetVertexBufferParams( 2 , layout );
			// this.mesh.SetIndexBufferParams( 2 , IndexFormat.UInt32 );
			this.mesh.name = "batch mesh "+this.mesh.GetHashCode();
		}


		/// <summary> Deffered dispose. </summary>
		public void Dispose ()
		{
			if( this.isDisposed )
				return;
			
			this.buffer.Dispose();
			
			if( Application.isPlaying )
			{
				Object.Destroy( this.mesh );
				Object.Destroy( this.material );
			}
			else
			{
				Object.DestroyImmediate( this.mesh );
				Object.DestroyImmediate( this.material );
			}
			
			this.isDisposed = true;
		}


	}
}
