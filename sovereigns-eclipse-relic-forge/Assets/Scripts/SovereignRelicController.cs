using UnityEngine;

namespace ModelForge.SovereignsEclipse
{
    /// <summary>
    /// Animates the unmerged dynamic crossguard gimbal rings and aether core
    /// exported from Sovereign's Eclipse Forge.
    /// Attach to the root Sovereigns_Eclipse_Relic GameObject.
    /// </summary>
    public class SovereignRelicController : MonoBehaviour
    {
        [Header("Crossguard Gimbal & Core")]
        public Transform guardGyroOuter;
        public Transform guardGyroInner;
        public Transform guardAetherCore;

        [Header("Optional Shrine Orbit Shards")]
        public Transform[] shrineShards;

        [Header("Motion Settings")]
        public float gyroSpeed = 65f;
        public float corePulseAmplitude = 0.012f;
        public float shardOrbitSpeed = 28f;

        private Vector3 _coreRestPos;

        private void Start()
        {
            if (!guardGyroOuter) guardGyroOuter = FindDeepChild(transform, "Guard_Gyro_Outer");
            if (!guardGyroInner) guardGyroInner = FindDeepChild(transform, "Guard_Gyro_Inner");
            if (!guardAetherCore) guardAetherCore = FindDeepChild(transform, "Guard_Aether_Core");

            if (guardAetherCore != null)
                _coreRestPos = guardAetherCore.localPosition;
        }

        private void Update()
        {
            float t = Time.time;

            if (guardGyroOuter != null)
                guardGyroOuter.Rotate(Vector3.up, gyroSpeed * Time.deltaTime, Space.Self);

            if (guardGyroInner != null)
                guardGyroInner.Rotate(Vector3.right, -gyroSpeed * 1.45f * Time.deltaTime, Space.Self);

            if (guardAetherCore != null)
            {
                guardAetherCore.Rotate(Vector3.up, gyroSpeed * 0.8f * Time.deltaTime, Space.Self);
                guardAetherCore.localPosition = _coreRestPos + Vector3.up * Mathf.Sin(t * 3.2f) * corePulseAmplitude;
            }

            if (shrineShards != null)
            {
                for (int i = 0; i < shrineShards.Length; i++)
                {
                    if (shrineShards[i] == null) continue;
                    shrineShards[i].RotateAround(transform.position, Vector3.up, shardOrbitSpeed * Time.deltaTime);
                }
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
