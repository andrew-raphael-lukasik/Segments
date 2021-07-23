using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

namespace Segments
{
	public class Batch : IBatch
	{

		/// <summary> DO NOT call <see cref="buffer"/>.Dispose(). Call <see cref="Batch.Dispose()"/>; instead. </summary>
		/// <remarks> Calling <see cref="buffer"/>.Dispose() will result in undefined program behaviour (crash). </remarks>
		public NativeList<float3x2> buffer;

		public Material material;
		internal Mesh mesh;
		public JobHandle Dependency;
		internal bool disposeRequested;

		#region IBatch implementaion
		NativeArray<float3x2> IBatch.buffer => this.buffer.AsArray();
		Material IBatch.material => this.material;
		Mesh IBatch.mesh => this.mesh;
		JobHandle IBatch.Dependency { get => this.Dependency; set => this.Dependency=value; }
		bool IBatch.disposeRequested { get => this.disposeRequested; set => this.disposeRequested=value; }
		void IBatch.DisposeNow ()
		{
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
		}
		#endregion
		

		public Batch ( NativeList<float3x2> buffer , Material mat )
		{
			this.buffer = buffer;
			this.disposeRequested = false;
			
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
		public void Dispose () => this.disposeRequested = true;


	}
}
