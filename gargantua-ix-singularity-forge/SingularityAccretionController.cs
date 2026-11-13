using System.Collections.Generic;
using UnityEngine;

namespace ModelForge.GargantuaIX
{
    /// <summary>
    /// Unity URP runtime astrophysics and telemetry controller for GARGANTUA-IX Relativistic Singularity.
    /// Attach directly to the imported .glb root GameObject (GARGANTUA_IX_Singularity_Root).
    /// Drives Keplerian differential accretion rotation, relativistic polar jet scaling/precession,
    /// Kerr telemetry array orbital motion, and zero-GC MaterialPropertyBlock HDR _EmissionColor pulsing.
    /// </summary>
    public class SingularityAccretionController : MonoBehaviour
    {
        [Header("Relativistic Accretion & Simulation Controls")]
        [Range(0f, 3f)] public float accretionSpinSpeed = 1.0f;
        [Range(3000f, 15000f)] public float plasmaTemperatureK = 8800f;
        [Range(0f, 1f)] public float polarJetIntensity = 0.80f;
        public bool showTelemetryArray = true;

        [Header("Keplerian Angular Velocities (deg/sec at 1.0x)")]
        public float photonRingDegPerSec = 66f;
        public float innerDiskDegPerSec = 48.7f;
        public float midSpiralDegPerSec = 23.5f;
        public float outerDustDegPerSec = 11.5f;
        public float polarJetDegPerSec = 35.5f;
        public float magneticHelixDegPerSec = 31.5f;
        public float telemetryRingDegPerSec = 5.7f;

        [Header("HDR Emission & Pulse Settings (URP Post-Processing Bloom)")]
        [Range(0.2f, 4f)] public float hdrEmissionMultiplier = 1.65f;
        [Range(0f, 0.5f)] public float plasmaPulseAmplitude = 0.12f;
        public float plasmaPulseFrequency = 2.6f;

        [Header("Singularity Sub-Part Transforms (Auto-Resolved)")]
        [SerializeField] private Transform partPhotonRing;
        [SerializeField] private Transform partAccretionInnerDisk;
        [SerializeField] private Transform partAccretionMidSpiral;
        [SerializeField] private Transform partAccretionOuterDust;
        [SerializeField] private Transform partNorthPolarJet;
        [SerializeField] private Transform partSouthPolarJet;
        [SerializeField] private Transform partMagneticHelix;
        [SerializeField] private Transform partTelemetryRing;
        [SerializeField] private Transform partSensorNodes;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _mpb;

        private struct EmissiveBinding
        {
            public Renderer Renderer;
            public Color BaseEmission;
            public float RelativeWeight;
            public bool IsJetOrHelix;
            public bool IsTelemetryCore;
        }

        private readonly List<EmissiveBinding> _emissiveBindings = new List<EmissiveBinding>(16);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (partPhotonRing == null) partPhotonRing = FindDeepChild(transform, "Part_PhotonRing");
            if (partAccretionInnerDisk == null) partAccretionInnerDisk = FindDeepChild(transform, "Part_AccretionInnerDisk");
            if (partAccretionMidSpiral == null) partAccretionMidSpiral = FindDeepChild(transform, "Part_AccretionMidSpiral");
            if (partAccretionOuterDust == null) partAccretionOuterDust = FindDeepChild(transform, "Part_AccretionOuterDust");
            if (partNorthPolarJet == null) partNorthPolarJet = FindDeepChild(transform, "Part_NorthPolarJet");
            if (partSouthPolarJet == null) partSouthPolarJet = FindDeepChild(transform, "Part_SouthPolarJet");
            if (partMagneticHelix == null) partMagneticHelix = FindDeepChild(transform, "Part_MagneticHelix");
            if (partTelemetryRing == null) partTelemetryRing = FindDeepChild(transform, "Part_TelemetryRing");
            if (partSensorNodes == null) partSensorNodes = FindDeepChild(transform, "Part_SensorNodes");

            CacheEmissiveRenderers();
        }

        private void CacheEmissiveRenderers()
        {
            _emissiveBindings.Clear();
            RegisterNodeRenderers(partPhotonRing, 2.35f, false, false);
            RegisterNodeRenderers(FindDeepChild(transform, "Part_LensingUpperArc"), 1.72f, false, false);
            RegisterNodeRenderers(FindDeepChild(transform, "Part_LensingLowerArc"), 1.72f, false, false);
            RegisterNodeRenderers(partAccretionInnerDisk, 1.85f, false, false);
            RegisterNodeRenderers(partAccretionMidSpiral, 1.38f, false, false);
            RegisterNodeRenderers(partAccretionOuterDust, 0.88f, false, false);
            RegisterNodeRenderers(partNorthPolarJet, 1.60f, true, false);
            RegisterNodeRenderers(partSouthPolarJet, 1.60f, true, false);
            RegisterNodeRenderers(partMagneticHelix, 1.72f, true, false);
            RegisterNodeRenderers(partTelemetryRing, 1.85f, false, true);
        }

        private void RegisterNodeRenderers(Transform root, float weight, bool isJetOrHelix, bool isTelemetry)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                var mat = r.sharedMaterial;
                if (mat == null) continue;

                // Skip non-emissive metallic/gold/quartz surfaces when caching telemetry array
                if (isTelemetry && !mat.name.Contains("Emissive") && !mat.name.Contains("Strip") && !mat.name.Contains("Core"))
                {
                    continue;
                }

                Color baseEm = mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : Color.white;
                if (baseEm.maxColorComponent < 0.01f)
                {
                    baseEm = isJetOrHelix ? new Color(0.45f, 0.82f, 1.0f) : new Color(1.0f, 0.78f, 0.42f);
                }

                _emissiveBindings.Add(new EmissiveBinding
                {
                    Renderer = r,
                    BaseEmission = baseEm,
                    RelativeWeight = weight,
                    IsJetOrHelix = isJetOrHelix,
                    IsTelemetryCore = isTelemetry
                });
            }
        }

        private void Update()
        {
            UpdateKeplerianRotations();
            UpdatePolarJetsAndTelemetry();
            UpdateZeroGcEmissionProperties();
        }

        private void UpdateKeplerianRotations()
        {
            float dt = Time.deltaTime;
            float speed = accretionSpinSpeed;
            if (speed <= 0.0001f) return;

            if (partPhotonRing != null)
                partPhotonRing.Rotate(Vector3.up, photonRingDegPerSec * speed * dt, Space.Self);

            if (partAccretionInnerDisk != null)
                partAccretionInnerDisk.Rotate(Vector3.up, innerDiskDegPerSec * speed * dt, Space.Self);

            if (partAccretionMidSpiral != null)
                partAccretionMidSpiral.Rotate(Vector3.up, midSpiralDegPerSec * speed * dt, Space.Self);

            if (partAccretionOuterDust != null)
                partAccretionOuterDust.Rotate(Vector3.up, outerDustDegPerSec * speed * dt, Space.Self);

            if (partNorthPolarJet != null)
                partNorthPolarJet.Rotate(Vector3.up, polarJetDegPerSec * speed * dt, Space.Self);

            if (partSouthPolarJet != null)
                partSouthPolarJet.Rotate(Vector3.up, -polarJetDegPerSec * speed * dt, Space.Self);

            if (partMagneticHelix != null)
                partMagneticHelix.Rotate(Vector3.up, magneticHelixDegPerSec * speed * dt, Space.Self);

            if (partTelemetryRing != null)
                partTelemetryRing.Rotate(Vector3.up, telemetryRingDegPerSec * speed * dt, Space.Self);
        }

        private void UpdatePolarJetsAndTelemetry()
        {
            bool jetsVisible = polarJetIntensity > 0.01f;
            float sXZ = Mathf.Lerp(0.35f, 1.05f, polarJetIntensity);
            float sY = Mathf.Lerp(0.15f, 1.0f, polarJetIntensity);
            Vector3 jetScale = new Vector3(sXZ, sY, sXZ);

            if (partNorthPolarJet != null)
            {
                if (partNorthPolarJet.gameObject.activeSelf != jetsVisible)
                    partNorthPolarJet.gameObject.SetActive(jetsVisible);
                if (jetsVisible) partNorthPolarJet.localScale = jetScale;
            }

            if (partSouthPolarJet != null)
            {
                if (partSouthPolarJet.gameObject.activeSelf != jetsVisible)
                    partSouthPolarJet.gameObject.SetActive(jetsVisible);
                if (jetsVisible) partSouthPolarJet.localScale = jetScale;
            }

            if (partMagneticHelix != null)
            {
                if (partMagneticHelix.gameObject.activeSelf != jetsVisible)
                    partMagneticHelix.gameObject.SetActive(jetsVisible);
                if (jetsVisible) partMagneticHelix.localScale = jetScale;
            }

            if (partTelemetryRing != null && partTelemetryRing.gameObject.activeSelf != showTelemetryArray)
            {
                partTelemetryRing.gameObject.SetActive(showTelemetryArray);
            }
        }

        private void UpdateZeroGcEmissionProperties()
        {
            float tempNorm = Mathf.Clamp01((plasmaTemperatureK - 3000f) / 12000f);
            Color coolShift = new Color(0.85f, 0.28f, 0.12f);
            Color midShift = new Color(1.0f, 0.74f, 0.35f);
            Color hotShift = new Color(0.85f, 0.95f, 1.0f);

            Color kelvinColor = tempNorm < 0.5f
                ? Color.Lerp(coolShift, midShift, tempNorm * 2f)
                : Color.Lerp(midShift, hotShift, (tempNorm - 0.5f) * 2f);

            float wave = 1f + plasmaPulseAmplitude * Mathf.Sin(Time.time * plasmaPulseFrequency);
            float tempGain = Mathf.Lerp(0.75f, 1.38f, tempNorm);

            for (int i = 0; i < _emissiveBindings.Count; i++)
            {
                EmissiveBinding b = _emissiveBindings[i];
                if (b.Renderer == null || !b.Renderer.gameObject.activeInHierarchy) continue;

                b.Renderer.GetPropertyBlock(_mpb);

                Color hdrCol;
                if (b.IsJetOrHelix)
                {
                    float jetBoost = polarJetIntensity * hdrEmissionMultiplier * b.RelativeWeight * wave;
                    hdrCol = b.BaseEmission * jetBoost;
                    Color baseAlpha = Color.white;
                    baseAlpha.a = Mathf.Lerp(0f, 0.86f, polarJetIntensity);
                    _mpb.SetColor(BaseColorId, baseAlpha);
                }
                else if (b.IsTelemetryCore)
                {
                    float corePulse = 1f + 0.32f * Mathf.Sin(Time.time * 2.8f);
                    hdrCol = b.BaseEmission * (hdrEmissionMultiplier * b.RelativeWeight * corePulse);
                }
                else
                {
                    Color tinted = Color.Lerp(b.BaseEmission, kelvinColor, 0.65f);
                    hdrCol = tinted * (hdrEmissionMultiplier * b.RelativeWeight * tempGain * wave);
                }

                _mpb.SetColor(EmissionColorId, hdrCol);
                b.Renderer.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>
        /// Resets all rotating singularities and rings back to canonical rest-pose (0, 0, 0).
        /// </summary>
        public void ResetToRestPose()
        {
            if (partPhotonRing != null) partPhotonRing.localRotation = Quaternion.identity;
            if (partAccretionInnerDisk != null) partAccretionInnerDisk.localRotation = Quaternion.identity;
            if (partAccretionMidSpiral != null) partAccretionMidSpiral.localRotation = Quaternion.identity;
            if (partAccretionOuterDust != null) partAccretionOuterDust.localRotation = Quaternion.identity;
            if (partNorthPolarJet != null) { partNorthPolarJet.localRotation = Quaternion.identity; partNorthPolarJet.localScale = Vector3.one; }
            if (partSouthPolarJet != null) { partSouthPolarJet.localRotation = Quaternion.identity; partSouthPolarJet.localScale = Vector3.one; }
            if (partMagneticHelix != null) { partMagneticHelix.localRotation = Quaternion.identity; partMagneticHelix.localScale = Vector3.one; }
            if (partTelemetryRing != null) partTelemetryRing.localRotation = Quaternion.identity;
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
