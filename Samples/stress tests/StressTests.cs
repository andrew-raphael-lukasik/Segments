using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Random = Unity.Mathematics.Random;

namespace Segments.Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	class StressTests : MonoBehaviour
	{
		
		[SerializeField] Material _srcMaterial = null;
		[SerializeField] int _numSegments = 128;

		[SerializeField] bool _everyFrame = false;

		public JobHandle Dependency;
		
		Segments.SegmentRenderingSystem _segmentsSystem;
		Segments.Batch _batch;
		

		void OnEnable ()
		{
			_segmentsSystem = Segments.Core.GetWorld().GetExistingSystem<Segments.SegmentRenderingSystem>();
			_segmentsSystem.CreateBatch( out _batch , _srcMaterial );
		}


		void OnDisable ()
		{
			Dependency.Complete();
			if( _batch!=null ) _batch.Dispose();
		}


		void Update ()
		{
			Dependency.Complete();

			if( _batch.Length!=_numSegments || _everyFrame )
			{
				_batch.Length = _numSegments;
				var job = new MyJob{
					transform		= transform.localToWorldMatrix ,
					numSegments		= _numSegments ,
					segments		= _batch.buffer.AsArray().Slice()
				};
				
				Dependency = job.Schedule( arrayLength:_batch.Length , innerloopBatchCount:128 , dependsOn:Dependency );
				_batch.Dependency = Dependency;
			}
		}


		[BurstCompile]
		public struct MyJob : IJobParallelFor
		{
			public float4x4 transform;
			public int numSegments;
			[WriteOnly] public NativeSlice<float3x2> segments;
			void IJobParallelFor.Execute ( int index )
			{
				float t0 = (float) index / (float) numSegments;
				float t1 = (float)( index+1 ) / (float) numSegments;
				
				float rnd0 = Random.CreateFromIndex( (uint) index ).NextFloat();
				float rnd1 = Random.CreateFromIndex( (uint) index + 1 ).NextFloat();
				float3 v0 =  math.transform( transform , new float3{ x=t0 , y=rnd0 } );
				float3 v1 =  math.transform( transform , new float3{ x=t1 , y=rnd1 } );
				
				segments[index] = new float3x2{ c0=v0 , c1=v1 };
			}
		}
		

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

				var NUM = new UnityEditor.UIElements.IntegerField("Num Segments");
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

				var LABEL = new Label("Batch data:");
				LABEL.style.marginTop = LABEL.style.marginTop.value.value + 12f;
				ROOT.Add( LABEL );

				var MATERIAL_INSTANCE = new UnityEditor.UIElements.ObjectField("Material instance (copy)");
				MATERIAL_INSTANCE.objectType = typeof(Material);
				MATERIAL_INSTANCE.value = instance._batch.material;
				ROOT.Add( MATERIAL_INSTANCE );

				var MESH_INSTANCE = new UnityEditor.UIElements.ObjectField("Mesh instance");
				MESH_INSTANCE.objectType = typeof(Mesh);
				MESH_INSTANCE.value = instance._batch.mesh;
				ROOT.Add( MESH_INSTANCE );

				
				var mf = instance.GetComponent<MeshFilter>();
				if( mf!=null )
					mf.mesh = instance._batch.mesh;
			}
			
		}

		
	}
}
