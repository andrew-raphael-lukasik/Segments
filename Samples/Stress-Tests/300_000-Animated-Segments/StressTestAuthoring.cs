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
                everyFrame  = _everyFrame ? (byte)1 : (byte)0,
            } );
            entityManager.AddComponentData(_segments, new LocalToWorld{
                Value = transform.localToWorldMatrix
            } );
        }

        void OnDisable() => Segments.Core.Destroy(_segments);

        void Update()
        {
            // update transform (because lines are in local-space):
            var entityManager = Segments.Core.GetWorld().EntityManager;
            entityManager.SetComponentData(_segments, new LocalToWorld{
                Value = transform.localToWorldMatrix
            });
        }

        #if UNITY_EDITOR
        void OnValidate()
        {
            // update settings on inspector change:
            var entityManager = Segments.Core.GetWorld().EntityManager;
            if (entityManager.Exists(_segments))
            {
                entityManager.SetComponentData(_segments, new StressTestSettings{
                    numSegments = _numSegments,
                    frequency   = _frequency,
                    everyFrame  = _everyFrame ? (byte)1 : (byte)0,
                });
            }
        }
        #endif

        #if UNITY_EDITOR
        void OnDrawGizmos() => Gizmos.DrawIcon(transform.position, "");// draws a white square icon to help with object selection in Scene view
        #endif
    }
}
