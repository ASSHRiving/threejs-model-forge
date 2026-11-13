using UnityEngine;

namespace ModelForge.AstralChronos
{
    /// <summary>
    /// Drives smooth multi-axis precession on the nested astrolabe rings
    /// exported from the Astral Chronos Reliquary GLB/FBX prefab.
    /// Attach to the root Astral_Chronos_Reliquary GameObject.
    /// </summary>
    public class AstrolabeOrbitController : MonoBehaviour
    {
        [Header("Child Node References (Auto-Resolved if Null)")]
        [SerializeField] private Transform ringOuter;
        [SerializeField] private Transform ringMiddle;
        [SerializeField] private Transform ringInner;
        [SerializeField] private Transform coreAssembly;
        [SerializeField] private Transform orbitRuneShards;

        [Header("Ring Angular Velocities (deg/sec)")]
        [SerializeField] private Vector3 outerSpeed = new Vector3(6f, 14f, 0f);
        [SerializeField] private Vector3 middleSpeed = new Vector3(-12f, 9f, 8f);
        [SerializeField] private Vector3 innerSpeed = new Vector3(18f, -15f, 11f);
        [SerializeField] private float shardOrbitSpeed = -22f;

        [Header("Core Levitation")]
        [SerializeField] private float floatAmplitude = 0.045f;
        [SerializeField] private float floatFrequency = 1.4f;
        [SerializeField] private float coreSpinSpeed = 28f;

        private Vector3 _coreRestLocalPos;
        private bool _hasCoreRestPos;

        private void Awake()
        {
            if (ringOuter == null) ringOuter = FindDeepChild(transform, "Ring_Outer");
            if (ringMiddle == null) ringMiddle = FindDeepChild(transform, "Ring_Middle");
            if (ringInner == null) ringInner = FindDeepChild(transform, "Ring_Inner");
            if (coreAssembly == null) coreAssembly = FindDeepChild(transform, "Core_Crystal_Assembly");
            if (orbitRuneShards == null) orbitRuneShards = FindDeepChild(transform, "Orbit_Rune_Shards");

            if (coreAssembly != null)
            {
                _coreRestLocalPos = coreAssembly.localPosition;
                _hasCoreRestPos = true;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            if (ringOuter != null)
                ringOuter.Rotate(outerSpeed * dt, Space.Self);

            if (ringMiddle != null)
                ringMiddle.Rotate(middleSpeed * dt, Space.Self);

            if (ringInner != null)
                ringInner.Rotate(innerSpeed * dt, Space.Self);

            if (orbitRuneShards != null)
                orbitRuneShards.Rotate(0f, shardOrbitSpeed * dt, 0f, Space.Self);

            if (coreAssembly != null && _hasCoreRestPos)
            {
                coreAssembly.Rotate(0f, coreSpinSpeed * dt, 0f, Space.Self);
                coreAssembly.localPosition = _coreRestLocalPos + Vector3.up * (Mathf.Sin(t * floatFrequency) * floatAmplitude);
            }
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == targetName) return child;
                Transform found = FindDeepChild(child, targetName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
