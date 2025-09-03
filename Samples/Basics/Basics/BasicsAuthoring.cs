using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

namespace Samples
{
    /// <summary>
    /// Bare-minimum of code that will result in lines being drawn on screen.
    /// </summary>
    [ExecuteAlways]
    public class BasicsAuthoring : MonoBehaviour
    {
        Entity _segments;
        
        void OnEnable()
        {
            // creates an Entity that will hold all the vertex data and will be responsible for drawing them
            Segments.Core.Create(out _segments);
        }

        void OnDisable()
        {
            // destroys the entity and all data associated with it
            Segments.Core.Destroy(_segments);
        }
        
        void Update()
        {
            // accesses the Segment component of our Entity
            Segments.Segment segment = Segments.Core.GetSegment(_segments);
            
            // we already know ahead of time that we want 3 segments here
            segment.Buffer.Length = 3;

            // set points where all these segments will start and end
            Vector3 pos = transform.position;
            segment.Buffer[0] = new float3x2(pos, pos+transform.right);
            segment.Buffer[1] = new float3x2(pos, pos+transform.up);
            segment.Buffer[2] = new float3x2(pos, pos+transform.forward);

            // notifies the segment update systems that line buffer changed and needs updating
            Segments.Core.SetSegmentChanged(_segments);
        }

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif
    }
}
