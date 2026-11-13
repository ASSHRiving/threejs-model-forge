using System.Collections.Generic;
using UnityEngine;

namespace ModelForge.EnochianTower
{
    /// <summary>
    /// Unity URP runtime controller for ENOCHIAN-TOWER Ancient Spire.
    /// Attach directly to the imported .glb root GameObject (ENOCHIAN_TOWER_Root).
    /// Locates Part_SwirlingClouds and Part_RuinedCrown, drives smooth orbital mist rotation
    /// and subtle vertical ribbon undulation, and pulses arcane rune _EmissionColor via
    /// zero-allocation MaterialPropertyBlock without creating material instances.
    /// </summary>
    public class EnochianTowerController : MonoBehaviour
    {
        [Header("Swirling Cloud & Mist Controls")]
        [Range(0f, 3f)] public float cloudSwirlSpeed = 1.0f;
        public float cloudOrbitDegPerSec = 9.2f;
        [Range(0f, 0.35f)] public float mistUndulationAmplitude = 0.095f;
        [Range(0.1f, 3f)] public float mistUndulationFrequency = 0.9f;

        [Header("Arcane Rune Emission & Abyss Light (URP Lit)")]
        [Range(0f, 3f)] public float arcaneGlowMultiplier = 1.0f;
        [Range(0f, 0.5f)] public float runePulseAmplitude = 0.14f;
        [Range(0.2f, 6f)] public float runePulseFrequency = 2.4f;
        public Color arcaneBaseColor = new Color(1.0f, 0.70f, 0.22f, 1.0f);
        [Tooltip("Optional PointLight positioned inside Part_RuinedCrown abyss.")]
        [SerializeField] private Light crownAbyssLight;
        public float baseCrownLightIntensity = 15.0f;

        [Header("Tower Sub-Part Transforms (Auto-Resolved)")]
        [SerializeField] private Transform partSwirlingClouds;
        [SerializeField] private Transform partRuinedCrown;
        [SerializeField] private Transform partTowerCore;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _mpb;

        private struct CloudRibbonState
        {
            public Transform Transform;
            public Vector3 RestLocalPosition;
            public float PhaseOffset;
            public float FreqMultiplier;
        }

        private struct EmissiveBinding
        {
            public Renderer Renderer;
            public Color BaseEmission;
            public float RelativeWeight;
        }

        private readonly List<CloudRibbonState> _cloudRibbons = new List<CloudRibbonState>(4);
        private readonly List<EmissiveBinding> _emissiveBindings = new List<EmissiveBinding>(8);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (partSwirlingClouds == null) partSwirlingClouds = FindDeepChild(transform, "Part_SwirlingClouds");
            if (partRuinedCrown == null) partRuinedCrown = FindDeepChild(transform, "Part_RuinedCrown");
            if (partTowerCore == null) partTowerCore = FindDeepChild(transform, "Part_TowerCore");
            if (crownAbyssLight == null && partRuinedCrown != null)
            {
                crownAbyssLight = partRuinedCrown.GetComponentInChildren<Light>(true);
            }

            CacheCloudRibbons();
            CacheEmissiveRenderers();
        }

        private void CacheCloudRibbons()
        {
            _cloudRibbons.Clear();
            if (partSwirlingClouds == null) return;

            for (int i = 0; i < partSwirlingClouds.childCount; i++)
            {
                Transform child = partSwirlingClouds.GetChild(i);
                _cloudRibbons.Add(new CloudRibbonState
                {
                    Transform = child,
                    RestLocalPosition = child.localPosition,
                    PhaseOffset = i * 1.4f,
                    FreqMultiplier = 1.0f + i * 0.24f
                });
            }
        }

        private void CacheEmissiveRenderers()
        {
            _emissiveBindings.Clear();
            RegisterEmissiveUnder(partRuinedCrown, 1.35f);
            RegisterEmissiveUnder(partTowerCore, 1.0f);
        }

        private void RegisterEmissiveUnder(Transform root, float weight)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Material mat = r.sharedMaterial;
                if (mat == null) continue;

                // Match emissive rune core & window slit materials while leaving stone untouched
                bool isRuneMat = mat.name.Contains("Rune") || mat.name.Contains("Emissive")
                    || r.name.Contains("ArcaneCore") || r.name.Contains("ArcaneSlits");
                if (!isRuneMat) continue;

                Color baseEm = mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : arcaneBaseColor;
                if (baseEm.maxColorComponent < 0.01f) baseEm = arcaneBaseColor;

                _emissiveBindings.Add(new EmissiveBinding
                {
                    Renderer = r,
                    BaseEmission = baseEm,
                    RelativeWeight = weight
                });
            }
        }

        private void Update()
        {
            UpdateCloudMotion();
            UpdateArcaneEmission();
        }

        private void UpdateCloudMotion()
        {
            if (partSwirlingClouds == null || cloudSwirlSpeed <= 0.0001f) return;

            float dt = Time.deltaTime;
            partSwirlingClouds.Rotate(Vector3.up, cloudOrbitDegPerSec * cloudSwirlSpeed * dt, Space.Self);

            float t = Time.time;
            for (int i = 0; i < _cloudRibbons.Count; i++)
            {
                CloudRibbonState ribbon = _cloudRibbons[i];
                if (ribbon.Transform == null) continue;
                float yOffset = Mathf.Sin(t * mistUndulationFrequency * ribbon.FreqMultiplier + ribbon.PhaseOffset)
                    * mistUndulationAmplitude;
                Vector3 pos = ribbon.RestLocalPosition;
                pos.y += yOffset;
                ribbon.Transform.localPosition = pos;
            }
        }

        private void UpdateArcaneEmission()
        {
            float pulse = 1f + runePulseAmplitude * Mathf.Sin(Time.time * runePulseFrequency);
            float intensity = arcaneGlowMultiplier * pulse;

            for (int i = 0; i < _emissiveBindings.Count; i++)
            {
                EmissiveBinding binding = _emissiveBindings[i];
                if (binding.Renderer == null || !binding.Renderer.gameObject.activeInHierarchy) continue;

                binding.Renderer.GetPropertyBlock(_mpb);
                Color hdrColor = binding.BaseEmission * (intensity * binding.RelativeWeight);
                _mpb.SetColor(EmissionColorId, hdrColor);
                binding.Renderer.SetPropertyBlock(_mpb);
            }

            if (crownAbyssLight != null)
            {
                crownAbyssLight.intensity = baseCrownLightIntensity * intensity;
            }
        }

        /// <summary>
        /// Resets Part_SwirlingClouds and all mist ribbons back to canonical rest-pose (0, 0, 0).
        /// </summary>
        public void ResetToRestPose()
        {
            if (partSwirlingClouds != null)
            {
                partSwirlingClouds.localRotation = Quaternion.identity;
                partSwirlingClouds.localPosition = Vector3.zero;
            }

            for (int i = 0; i < _cloudRibbons.Count; i++)
            {
                CloudRibbonState ribbon = _cloudRibbons[i];
                if (ribbon.Transform == null) continue;
                ribbon.Transform.localPosition = ribbon.RestLocalPosition;
                ribbon.Transform.localRotation = Quaternion.identity;
            }
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == targetName) return child;
                Transform found = FindDeepChild(child, targetName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
