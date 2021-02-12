using UnityEngine;

namespace Segments.Samples
{
	[AddComponentMenu("")]
	class TransformRotate : MonoBehaviour
	{

		[SerializeField] bool _onDrawGizmos = true;

		#if UNITY_EDITOR
		void OnDrawGizmos ()
		{
			if( _onDrawGizmos && !Application.isPlaying )
				Update();
		}
		#endif
		
		void Update ()
			=> transform.Rotate( 0f , 33f * Time.deltaTime , 0f );
		
	}
}
