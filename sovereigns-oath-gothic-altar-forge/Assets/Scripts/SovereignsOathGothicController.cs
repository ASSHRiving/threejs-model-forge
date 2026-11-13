using System.Collections.Generic;
using UnityEngine;

namespace ModelForge.SovereignsOathGothic
{
    /// <summary>
    /// Animates the Spectral Guardian Oath-Swords, Celestial Prayer Halo, and Royal Gem Core
    /// exported from Sovereign's Oath Royal Greatsword & Gothic Altar Forge.
    /// </summary>
    public class SovereignsOathGothicController : MonoBehaviour
    {
        public Transform celestialHalo;
        public Transform swordGemCore;
        public float phalanxOrbitSpeed = 24f;
        public float bobAmplitude = 0.035f;

        private readonly List<Transform> _guardianBlades = new List<Transform>();
        private readonly List<Vector3> _restPositions = new List<Vector3>();

        private void Start()
        {
            if (!celestialHalo) celestialHalo = FindDeepChild(transform, "Celestial_Halo_Ring");
            if (!swordGemCore) swordGemCore = FindDeepChild(transform, "Sword_Gem_Core");

            for (int i = 1; i <= 8; i++)
            {
                Transform gb = FindDeepChild(transform, $"Guardian_Blade_0{i}");
                if (gb != null)
                {
                    _guardianBlades.Add(gb);
                    _restPositions.Add(gb.localPosition);
                }
            }
        }

        private void Update()
        {
            float t = Time.time;
            if (celestialHalo != null)
                celestialHalo.Rotate(Vector3.forward, 12f * Time.deltaTime, Space.Self);

            for (int i = 0; i < _guardianBlades.Count; i++)
            {
                Transform blade = _guardianBlades[i];
                blade.RotateAround(transform.position, Vector3.up, phalanxOrbitSpeed * Time.deltaTime);
                Vector3 p = blade.localPosition;
                p.y = _restPositions[i].y + Mathf.Sin(t * 2.4f + i * 1.05f) * bobAmplitude;
                blade.localPosition = p;
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
