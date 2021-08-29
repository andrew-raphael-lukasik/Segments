using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
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
		internal bool disposeRequested;

		public Batch ( NativeList<float3x2> buffer , Material mat )
		{
			this.buffer = buffer;
			this.disposeRequested = false;
			
			this.material = mat;
			this.mesh = new Mesh();
			this.mesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
			this.mesh.MarkDynamic();
			this.mesh.subMeshCount = 1;
			this.mesh.name = "batch mesh "+this.mesh.GetHashCode();
		}


		/// <summary> Deffered dispose. </summary>
		public void Dispose ()
		{
			this.Dependency.Complete();
			this.disposeRequested = true;
		}

		public void DisposeImmediate ()
		{
			this.Dependency.Complete();
			this.buffer.Dispose();
			
			if( Application.isPlaying ) Object.Destroy( this.mesh );
			else Object.DestroyImmediate( this.mesh );
		}


	}
}
