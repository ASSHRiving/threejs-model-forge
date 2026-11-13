using System.Collections.Generic;
using UnityEngine;

namespace ModelForge.GargantuaIX
{
    /// <summary>
    /// Pure astrophysical Unity URP runtime controller for GARGANTUA-IX Relativistic Singularity.
    /// Attach directly to the imported .glb root GameObject (GARGANTUA_IX_Singularity_Root).
    /// Drives Keplerian differential accretion rotation (innermost fastest, outer slowest),
    /// helical magnetic flux precession, polar jet luminosity scaling, and zero-GC MaterialPropertyBlock
    /// HDR _EmissionColor pulsing with blackbody Kelvin shift for Unity URP Post-Processing Bloom.
    /// </summary>
    public class SingularityAccretionController : MonoBehaviour
    {
        [Header("Relativistic Accretion & Blackbody Controls")]
        [Range(0f, 3f)] public float kerrSpinVelocity = 1.0f;
        [Range(3000f, 15000f)] public float blackbodyTemperatureK = 8800f;
        [Range(0f, 1f)] public float relativisticJetLuminosity = 0.80f;
        public bool showPolarJetsAndHelices = true;

        [Header("Keplerian Differential Angular Velocities (deg/sec at 1.0x)")]
        public float photonRingDegPerSec = 71.0f;
        public float innerHotDiskDegPerSec = 50.4f;
        public float midTurbulentDiskDegPerSec = 24.1f;
        public float outerDustHaloDegPerSec = 10.9f;
        public float polarJetDegPerSec = 37.2f;
        public float magneticHelixDegPerSec = 30.9f;

        [Header("Magnetic Flux Precession")]
        [Range(0f, 5f)] public float helixPrecessionAmplitudeDeg = 1.26f;
        public float helixPrecessionFreq = 0.75f;

        [Header("HDR Emission & Pulse Settings (URP Post-Processing Bloom)")]
        [Range(0.2f, 4f)] public float hdrEmissionMultiplier = 1.65f;
        [Range(0f, 0.5f)] public float plasmaPulseAmplitude = 0.10f;
        public float plasmaPulseFrequency = 2.6f;

        [Header("Pure Astrophysical Sub-Parts (Auto-Resolved)")]
        [SerializeField] private Transform partEventHorizon;
        [SerializeField] private Transform partPhotonRing;
        [SerializeField] private Transform partLensingUpperArc;
        [SerializeField] private Transform partLensingLowerArc;
        [SerializeField] private Transform partAccretionInnerHotDisk;
        [SerializeField] private Transform partAccretionMidTurbulentDisk;
        [SerializeField] private Transform partAccretionOuterDustHalo;
        [SerializeField] private Transform partNorthPolarJet;
        [SerializeField] private Transform partSouthPolarJet;
        [SerializeField] private Transform partMagneticFluxHelix;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _mpb;
        private float _helixYawDeg;

        private struct EmissiveBinding
        {
            public Renderer Renderer;
            public Color BaseEmission;
            public float RelativeWeight;
            public bool IsJetOrHelix;
            public bool IsTransparentJet;
        }

        private readonly List<EmissiveBinding> _emissiveBindings = new List<EmissiveBinding>(12);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (partEventHorizon == null) partEventHorizon = FindDeepChild(transform, "Part_EventHorizon");
            if (partPhotonRing == null) partPhotonRing = FindDeepChild(transform, "Part_PhotonRing");
            if (partLensingUpperArc == null) partLensingUpperArc = FindDeepChild(transform, "Part_LensingUpperArc");
            if (partLensingLowerArc == null) partLensingLowerArc = FindDeepChild(transform, "Part_LensingLowerArc");
            if (partAccretionInnerHotDisk == null) partAccretionInnerHotDisk = FindDeepChild(transform, "Part_AccretionInnerHotDisk");
            if (partAccretionMidTurbulentDisk == null) partAccretionMidTurbulentDisk = FindDeepChild(transform, "Part_AccretionMidTurbulentDisk");
            if (partAccretionOuterDustHalo == null) partAccretionOuterDustHalo = FindDeepChild(transform, "Part_AccretionOuterDustHalo");
            if (partNorthPolarJet == null) partNorthPolarJet = FindDeepChild(transform, "Part_NorthPolarJet");
            if (partSouthPolarJet == null) partSouthPolarJet = FindDeepChild(transform, "Part_SouthPolarJet");
            if (partMagneticFluxHelix == null) partMagneticFluxHelix = FindDeepChild(transform, "Part_MagneticFluxHelix");

            CacheEmissiveRenderers();
        }

        private void CacheEmissiveRenderers()
        {
            _emissiveBindings.Clear();
            RegisterNodeRenderers(partPhotonRing, 2.35f, false, false);
            RegisterNodeRenderers(partLensingUpperArc, 1.80f, false, false);
            RegisterNodeRenderers(partLensingLowerArc, 1.80f, false, false);
            RegisterNodeRenderers(partAccretionInnerHotDisk, 1.95f, false, false);
            RegisterNodeRenderers(partAccretionMidTurbulentDisk, 1.38f, false, false);
            RegisterNodeRenderers(partAccretionOuterDustHalo, 0.72f, false, false);
            RegisterNodeRenderers(partNorthPolarJet, 1.55f, true, true);
            RegisterNodeRenderers(partSouthPolarJet, 1.55f, true, true);
            RegisterNodeRenderers(partMagneticFluxHelix, 1.75f, true, false);
        }

        private void RegisterNodeRenderers(Transform root, float weight, bool isJetOrHelix, bool isTransparentJet)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Material mat = r.sharedMaterial;
                if (mat == null) continue;

                Color baseEm = mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : Color.white;
                if (baseEm.maxColorComponent < 0.01f)
                {
                    baseEm = isJetOrHelix ? new Color(0.55f, 0.85f, 1.0f) : new Color(1.0f, 0.82f, 0.56f);
                }

                _emissiveBindings.Add(new EmissiveBinding
                {
                    Renderer = r,
                    BaseEmission = baseEm,
                    RelativeWeight = weight,
                    IsJetOrHelix = isJetOrHelix,
                    IsTransparentJet = isTransparentJet
                });
            }
        }

        private void Update()
        {
            UpdateKeplerianRotationsAndPrecession();
            UpdatePolarJetsAndHelicesVisibility();
            UpdateZeroGcEmissionProperties();
        }

        private void UpdateKeplerianRotationsAndPrecession()
        {
            float dt = Time.deltaTime;
            float speed = kerrSpinVelocity;
            if (speed <= 0.0001f) return;

            if (partPhotonRing != null)
                partPhotonRing.Rotate(Vector3.up, photonRingDegPerSec * speed * dt, Space.Self);

            if (partAccretionInnerHotDisk != null)
                partAccretionInnerHotDisk.Rotate(Vector3.up, innerHotDiskDegPerSec * speed * dt, Space.Self);

            if (partAccretionMidTurbulentDisk != null)
                partAccretionMidTurbulentDisk.Rotate(Vector3.up, midTurbulentDiskDegPerSec * speed * dt, Space.Self);

            if (partAccretionOuterDustHalo != null)
                partAccretionOuterDustHalo.Rotate(Vector3.up, outerDustHaloDegPerSec * speed * dt, Space.Self);

            if (partNorthPolarJet != null)
                partNorthPolarJet.Rotate(Vector3.up, polarJetDegPerSec * speed * dt, Space.Self);

            if (partSouthPolarJet != null)
                partSouthPolarJet.Rotate(Vector3.up, -polarJetDegPerSec * speed * dt, Space.Self);

            if (partMagneticFluxHelix != null)
            {
                _helixYawDeg = (_helixYawDeg + magneticHelixDegPerSec * speed * dt) % 360f;
                float tiltZ = Mathf.Sin(Time.time * helixPrecessionFreq * speed) * helixPrecessionAmplitudeDeg;
                partMagneticFluxHelix.localRotation = Quaternion.Euler(0f, _helixYawDeg, tiltZ);
            }
        }

        private void UpdatePolarJetsAndHelicesVisibility()
        {
            bool jetsVisible = showPolarJetsAndHelices && relativisticJetLuminosity > 0.01f;
            float sXZ = Mathf.Lerp(0.38f, 1.04f, relativisticJetLuminosity);
            float sY = Mathf.Lerp(0.18f, 1.00f, relativisticJetLuminosity);
            Vector3 jetScale = new Vector3(sXZ, sY, sXZ);

            ApplyJetTransform(partNorthPolarJet, jetsVisible, jetScale);
            ApplyJetTransform(partSouthPolarJet, jetsVisible, jetScale);
            ApplyJetTransform(partMagneticFluxHelix, jetsVisible, jetScale);
        }

        private static void ApplyJetTransform(Transform target, bool visible, Vector3 scale)
        {
            if (target == null) return;
            if (target.gameObject.activeSelf != visible)
                target.gameObject.SetActive(visible);
            if (visible)
                target.localScale = scale;
        }

        private void UpdateZeroGcEmissionProperties()
        {
            float tempNorm = Mathf.Clamp01((blackbodyTemperatureK - 3000f) / 12000f);

            // Relativistic Doppler & Blackbody temperature shift palette
            Color coolShift = new Color(0.85f, 0.28f, 0.12f); // #D9481E
            Color midShift = new Color(1.00f, 0.78f, 0.45f);  // Warm gold-amber
            Color hotShift = new Color(0.90f, 0.97f, 1.00f);  // #E6F7FF

            Color kelvinColor = tempNorm < 0.5f
                ? Color.Lerp(coolShift, midShift, tempNorm * 2f)
                : Color.Lerp(midShift, hotShift, (tempNorm - 0.5f) * 2f);

            float wave = 1f + plasmaPulseAmplitude * Mathf.Sin(Time.time * plasmaPulseFrequency);
            float tempGain = Mathf.Lerp(0.76f, 1.32f, tempNorm);

            for (int i = 0; i < _emissiveBindings.Count; i++)
            {
                EmissiveBinding b = _emissiveBindings[i];
                if (b.Renderer == null || !b.Renderer.gameObject.activeInHierarchy) continue;

                b.Renderer.GetPropertyBlock(_mpb);

                Color hdrCol;
                if (b.IsJetOrHelix)
                {
                    float jetPulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 3.2f);
                    float jetBoost = relativisticJetLuminosity * hdrEmissionMultiplier * b.RelativeWeight * jetPulse;
                    hdrCol = b.BaseEmission * jetBoost;

                    if (b.IsTransparentJet)
                    {
                        Color baseAlpha = Color.white;
                        baseAlpha.a = Mathf.Lerp(0f, 0.64f, relativisticJetLuminosity);
                        _mpb.SetColor(BaseColorId, baseAlpha);
                    }
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
        /// Resets all rotating plasma rings, jets, and magnetic helices back to canonical rest-pose (0, 0, 0).
        /// </summary>
        public void ResetToRestPose()
        {
            _helixYawDeg = 0f;
            if (partPhotonRing != null) partPhotonRing.localRotation = Quaternion.identity;
            if (partAccretionInnerHotDisk != null) partAccretionInnerHotDisk.localRotation = Quaternion.identity;
            if (partAccretionMidTurbulentDisk != null) partAccretionMidTurbulentDisk.localRotation = Quaternion.identity;
            if (partAccretionOuterDustHalo != null) partAccretionOuterDustHalo.localRotation = Quaternion.identity;
            if (partNorthPolarJet != null) { partNorthPolarJet.localRotation = Quaternion.identity; partNorthPolarJet.localScale = Vector3.one; }
            if (partSouthPolarJet != null) { partSouthPolarJet.localRotation = Quaternion.identity; partSouthPolarJet.localScale = Vector3.one; }
            if (partMagneticFluxHelix != null) { partMagneticFluxHelix.localRotation = Quaternion.identity; partMagneticFluxHelix.localScale = Vector3.one; }
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
