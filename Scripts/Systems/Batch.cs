using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Segments
{
	[System.Serializable]
	public class Batch
	{

		/// <summary> DO NOT call batch.Segments.Dispose() EVER. Call batch.Dispose(); instead. </summary>
		/// <remarks> Calling Buffer.Dispose() will result in undefined program behaviour (crash). </remarks>
		public NativeList<float3x2> Segments;
		
		public int Length
		{
			get => this.Segments.Length;
			set => this.Segments.Length = value;
		}

		public Material material;

		internal Mesh mesh;
		internal float[] shaderData;

		public JobHandle Dependency;

		internal bool isDisposed;


		public Batch ( NativeList<float3x2> buffer , Material material )
		{
			this.Segments = buffer;
			this.isDisposed = false;
			this.material = new Material( material );
			this.material.name = $"{this.material} (runtime copy)";
			this.mesh = new Mesh();
			this.shaderData = new float[0];
		}


		/// <summary> Deffered dispose. </summary>
		public void Dispose ()
		{
			if( this.isDisposed )
				return;
			
			this.Segments.Dispose();
			
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
