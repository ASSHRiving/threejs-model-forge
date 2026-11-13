using UnityEngine;

namespace ModelForge.ArcaneAstrolabeAltar
{
    /// <summary>
    /// Animates the concentric gimbal rings, levitating relic crystal core, equatorial halo,
    /// and orbiting containment shards exported from Arcane Astrolabe Altar Forge.
    /// Attach to the root Arcane_Astrolabe_Altar GameObject.
    /// </summary>
    public class ArcaneAstrolabeAltarController : MonoBehaviour
    {
        [Header("Gimbal & Core Nodes (Auto-Resolved if Null)")]
        public Transform outerRing;
        public Transform middleRing;
        public Transform innerRing;
        public Transform crystalCore;
        public Transform innerEnergyCore;
        public Transform equatorialHalo;
        public Transform containmentShards;

        [Header("Motion Settings")]
        public float spinSpeed = 1f;
        public float hoverAmplitude = 0.065f;

        private Vector3 _coreRestLocalPos;
        private bool _hasCoreRestPos;

        private void Awake()
        {
            if (!outerRing) outerRing = FindDeepChild(transform, "Gimbal_Outer_Ring");
            if (!middleRing) middleRing = FindDeepChild(transform, "Gimbal_Middle_Ring");
            if (!innerRing) innerRing = FindDeepChild(transform, "Gimbal_Inner_Ring");
            if (!crystalCore) crystalCore = FindDeepChild(transform, "Relic_Crystal_Core");
            if (!innerEnergyCore) innerEnergyCore = FindDeepChild(transform, "Relic_Inner_Energy_Core");
            if (!equatorialHalo) equatorialHalo = FindDeepChild(transform, "Relic_Equatorial_Halo");
            if (!containmentShards) containmentShards = FindDeepChild(transform, "Containment_Shards");

            if (crystalCore != null)
            {
                _coreRestLocalPos = crystalCore.localPosition;
                _hasCoreRestPos = true;
            }
        }

        private void Update()
        {
            float simTime = Time.time * spinSpeed;

            if (outerRing != null)
                outerRing.localRotation = Quaternion.Euler(Mathf.Sin(simTime * 0.65f) * 24.06f, simTime * 16.04f, 0f);

            if (middleRing != null)
                middleRing.localRotation = Quaternion.Euler(Mathf.Cos(simTime * 0.45f) * 20.05f, 0f, simTime * 31.51f);

            if (innerRing != null)
                innerRing.localRotation = Quaternion.Euler(0f, -simTime * 48.70f, Mathf.Sin(simTime * 0.8f) * 27.50f);

            if (crystalCore != null)
            {
                if (_hasCoreRestPos)
                {
                    Vector3 pos = _coreRestLocalPos;
                    pos.y += Mathf.Sin(simTime * 2.1f) * hoverAmplitude;
                    crystalCore.localPosition = pos;
                }
                crystalCore.localRotation = Quaternion.Euler(0f, simTime * 42.97f, 0f);
            }

            if (innerEnergyCore != null)
                innerEnergyCore.localRotation = Quaternion.Euler(-simTime * 80.21f, 0f, simTime * 63.03f);

            if (equatorialHalo != null)
                equatorialHalo.localRotation = Quaternion.Euler(0f, 0f, simTime * 68.75f);

            if (containmentShards != null)
                containmentShards.localRotation = Quaternion.Euler(0f, -simTime * 37.24f, 0f);
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
