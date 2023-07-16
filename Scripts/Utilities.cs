using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

namespace Segments
{
	public static partial class Utilities
	{
		#region GetSegmentBuffer
		

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static DynamicBuffer<float3x2> GetSegmentBuffer ( Entity entity , EntityManager entityManager , bool isReadOnly = false )
		{
			return entityManager.GetBuffer<Segment>( entity , isReadOnly ).Reinterpret<float3x2>();
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void GetSegmentBuffer ( Entity entity , EntityManager entityManager , out DynamicBuffer<float3x2> buffer , bool isReadOnly = false  )
		{
			buffer = GetSegmentBuffer( entity , entityManager , isReadOnly );
		}


		#endregion
	}
}
