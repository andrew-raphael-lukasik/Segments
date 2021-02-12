using UnityEngine;

namespace EcsLineRenderer.Samples
{
	[AddComponentMenu("")]
	class TransformRotate : MonoBehaviour
	{

		[SerializeField] bool _onDrawGizmos = true;

		void OnDrawGizmos ()
		{
			if( _onDrawGizmos )
				Update();
		}
		
		void Update ()
			=> transform.Rotate( 0f , 33f * Time.deltaTime , 0f );
		
	}
}
