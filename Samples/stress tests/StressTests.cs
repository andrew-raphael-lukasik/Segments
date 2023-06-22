using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Segments.Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	class StressTests : MonoBehaviour
	{
		
		[SerializeField] Material _srcMaterial = null;
		[SerializeField] int _numSegments = 128;
		[SerializeField] float _frequency = 16;
		[SerializeField] bool _everyFrame = false;

		Segments.Batch _segments;
		

		void OnEnable ()
		{
			Segments.Core.CreateBatch( out _segments , _srcMaterial );

			#if UNITY_EDITOR
			this.Update();// just to kickstart 1st frame rendering in editor
			#endif
		}


		void OnDisable ()
		{
			if( _segments!=null )
			{
				_segments.Dispose();
				_segments = null;
			}
		}


		void Update ()
		{
			if( _segments==null ) return;

			_segments.Dependency.Complete();

			if( _segments.buffer.Length!=_numSegments || _everyFrame )
			{
				_segments.buffer.Length = _numSegments;

				// schedule new job:
				var job = new StressTestJob{
					Transform		= transform.localToWorldMatrix ,
					NumSegments		= _numSegments ,
					Segments		= _segments.buffer.AsArray() ,
					Offset			= Time.time ,
					Frequency		= _frequency ,
				};
				_segments.Dependency = job.Schedule(
					arrayLength:			_numSegments ,
					indicesPerJobCount:		64 ,
					dependsOn:				_segments.Dependency
				);
			}
		}


		[BurstCompile]
		public struct StressTestJob : IJobParallelForBatch
		{
			public float4x4 Transform;
			public int NumSegments;
			public float Offset;
			public float Frequency;
			[WriteOnly] public NativeArray<float3x2> Segments;
			void IJobParallelForBatch.Execute ( int startIndex , int count )
			{
				float3 translation = new float3( Transform.c3.x , Transform.c3.y , Transform.c3.z );
				float3 right = Transform.Right();
				float3 up = Transform.Up();

				for( int i=0 ; i<count ; i++ )
				{
					int index = startIndex + i;

					float t0 = (float) index / (float) NumSegments;
					float t1 = (float)( index+1 ) / (float) NumSegments;
					float2 amp = math.sin( Frequency * new float2{
						x = t0*math.PI*2f + Offset ,
						y = t1*math.PI*2f + Offset
					} );
					float3 vec0 = translation + right*t0 + up*amp.x;
					float3 vec1 = translation + right*t1 + up*amp.y;
					Segments[index] = new float3x2{ c0=vec0 , c1=vec1 };
				}
			}
		}
		

		#if UNITY_EDITOR
		[UnityEditor.CustomEditor( typeof(StressTests) )]
		public class MyEditor : UnityEditor.Editor
		{
			public override void OnInspectorGUI ()
			{
				DrawDefaultInspector();
				if( GUILayout.Button("Update batch") )
				{
					var instance = (StressTests) target;
					instance.OnDisable();
					instance.OnEnable();
				}
			}

			public override VisualElement CreateInspectorGUI ()
			{
				var ROOT = new VisualElement();
				Rebind(ROOT);
				return ROOT;
			}

			void Rebind ( VisualElement ROOT )
			{
				var instance = (StressTests) target;
				System.Action rebuild = ()=>{
					instance.OnDisable();
					instance.OnEnable();
				};

				var MATERIAL = new UnityEditor.UIElements.ObjectField("Material");
				MATERIAL.objectType = typeof(Material);
				MATERIAL.value = instance._srcMaterial;
				MATERIAL.RegisterValueChangedCallback( (ctx) => {
					instance._srcMaterial = (Material) ctx.newValue;
				} );
				ROOT.Add( MATERIAL );

				var NUM = new UnityEngine.UIElements.IntegerField("Num Segments");
				NUM.value = instance._numSegments;
				NUM.RegisterValueChangedCallback( (ctx) => {
					instance._numSegments = ctx.newValue;

					// const int max = 100_000;
					// if( instance._numSegments > max )
					// {
					// 	instance._numSegments = max;
					// 	NUM.SetValueWithoutNotify( max );
					// }

					const int min = 1;
					if( instance._numSegments < min )
					{
						instance._numSegments = min;
						NUM.SetValueWithoutNotify( min );
					}
					
					// rebuild();
					// ROOT.Clear();
					// Rebind(ROOT);
				} );
				ROOT.Add( NUM );

				var FREQ = new UnityEngine.UIElements.FloatField("Frequency");
				FREQ.value = instance._frequency;
				FREQ.RegisterValueChangedCallback( (ctx) => {
					instance._frequency = ctx.newValue;
				} );
				ROOT.Add( FREQ );

				var LOOP = new Toggle("Every Frame");
				LOOP.value = instance._everyFrame;
				LOOP.RegisterValueChangedCallback( (ctx) => {
					instance._everyFrame = ctx.newValue;
				} );
				ROOT.Add( LOOP );
				
				var BUTTON = new Button( ()=>{
					rebuild();
					ROOT.Clear();
					Rebind(ROOT);
				} );
				BUTTON.text = "Update batch";
				ROOT.Add( BUTTON );

				if( instance._segments!=null )
				{
					var LABEL = new Label("Batch data:");
					LABEL.style.marginTop = LABEL.style.marginTop.value.value + 12f;
					ROOT.Add( LABEL );

					var BATCH_MATERIAL = new UnityEditor.UIElements.ObjectField("Material");
					BATCH_MATERIAL.objectType = typeof(Material);
					BATCH_MATERIAL.value = instance._segments.material;
					ROOT.Add( BATCH_MATERIAL );

					var BATCH_MESH = new UnityEditor.UIElements.ObjectField("Mesh");
					BATCH_MESH.objectType = typeof(Mesh);
					BATCH_MESH.value = instance._segments.mesh;
					ROOT.Add( BATCH_MESH );
				}
				else
				{
					var LABEL = new Label("( no batch data )");
					LABEL.style.marginTop = LABEL.style.marginTop.value.value + 12f;
					ROOT.Add( LABEL );
				}
			}
			
		}
		#endif

		
	}
}
