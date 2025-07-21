using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

namespace Samples
{
    /// <summary>
    /// Bare-minimum of code that will result in lines being drawn on screen.
    /// </summary>
    [ExecuteAlways]
    public class Basics : MonoBehaviour
    {

        Entity _segments;
        
        void OnEnable ()
        {
            // creates an Entity that will hold all the vertex data and will be responsible for drawing them
            Segments.Core.Create( out _segments );
        }

        void OnDisable ()
        {
            // destroys the entity and all data associated with it
            Segments.Core.Destroy( _segments );
        }
        
        void Update ()
        {
            // accesses the Segment beffer component of our Entity where every Segment is a pair of float3 values (start & end of a line segment)
            var segments = Segments.Core.GetBuffer( _segments );
            
            // we already know ahead of time that we want 3 segments here
            segments.Length = 3;

            // set points where all these segments will start and end
            Vector3 pos = transform.position;
            segments[0] = new float3x2( pos , pos+transform.right );
            segments[1] = new float3x2( pos , pos+transform.up );
            segments[2] = new float3x2( pos , pos+transform.forward );
        }

        #if UNITY_EDITOR
        void OnDrawGizmos () => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif

    }
}
