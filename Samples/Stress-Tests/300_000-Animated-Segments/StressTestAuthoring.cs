using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace Samples
{
    [ExecuteAlways]
    class StressTestAuthoring : MonoBehaviour
    {
        [SerializeField] internal Material _srcMaterial;
        [SerializeField] internal int _numSegments = 128;
        [SerializeField] internal float _frequency = 16;
        [SerializeField] internal bool _everyFrame;

        Entity _segments;

        void OnEnable()
        {
            Segments.Core.Create(out _segments, _srcMaterial);

            var entityManager = Segments.Core.GetWorld().EntityManager;
            entityManager.AddComponentData(_segments, new StressTestSettings{
                numSegments = _numSegments,
                frequency   = _frequency,
                everyFrame  = _everyFrame,
            } );
            entityManager.AddComponentData(_segments, new LocalToWorld{
                Value = transform.localToWorldMatrix
            } );
            entityManager.AddComponentObject(_segments, this);// adds this StressTestsAuthoring component as a managed obj reference
        }

        void OnDisable() => Segments.Core.Destroy(_segments);

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif
    }
}
