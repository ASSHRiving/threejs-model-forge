using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.NPC
{
    /// <summary>
    /// Unity URP runtime controller for NPC_01_SkeletonMerchant (Dark Medieval ARPG Dungeon).
    /// Attach directly to the imported NPC_01_SkeletonMerchant Prefab root GameObject.
    /// Automatically binds Part_Torso_Ribcage, Part_Head, Part_Jaw, Part_Arm_R/L, Part_Cloak,
    /// Part_Belt_Accessories, and articulated crouched leg joints (Part_PelvisRoot, Part_Shin_R/L, Part_Thigh_R/L)
    /// via transform.Find, drives living bent-knee crouch breathing, torso hunch, scanning head tilt,
    /// grounded left walking-cane compensation, and coin-beckoning arm motion (jaw kept still in idle),
    /// and modulates MAT_Emissive_Rune _EmissionColor using zero-allocation MaterialPropertyBlock.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkeletonMerchantIdleController : MonoBehaviour
    {
        [Header("Merchant Idle Animation")]
        [Range(0f, 2.5f)]
        [Tooltip("Master multiplier for all idle motion speeds (0 = frozen rest pose).")]
        public float idleSpeed = 1.0f;

        [Tooltip("Living ankle/knee crouch flexion angle per breath cycle in degrees.")]
        [Range(0f, 8f)] public float kneeFlexDegrees = 3.9f;

        [Tooltip("Torso extra forward hunch pitch in degrees over crouched breath cycle.")]
        [Range(0f, 4f)] public float breathPitchDegrees = 1.05f;

        [Tooltip("Head subtle scanning yaw/roll amplitude in degrees.")]
        [Range(0f, 10f)] public float headSwayDegrees = 3.2f;

        [Tooltip("Right arm coin-rubbing / customer beckoning swing amplitude in degrees.")]
        [Range(0f, 12f)] public float rightArmBeckonDegrees = 3.6f;

        [Header("Floating Gold Coin (Optional Child Under Part_Arm_R)")]
        [Range(0f, 0.03f)] public float coinBobAmplitude = 0.008f;
        public float coinSpinDegPerSec = 55f;

        [Header("Soul-Fire & Candle Emission (MAT_Emissive_Rune)")]
        [Range(0f, 3f)] public float emissionIntensity = 1.65f;
        [Range(0f, 0.5f)] public float soulPulseAmplitude = 0.22f;
        [Range(0.2f, 8f)] public float soulPulseFrequency = 2.6f;

        [Tooltip("HDR fallback emission color when material _EmissionColor is black or unset.")]
        [ColorUsage(false, true)]
        public Color fallbackRuneEmission = new Color(0.55f, 0.92f, 1.0f, 1.0f);

        [Tooltip("Optional URP PointLight representing the iron-cage soul lantern / hat candle.")]
        [SerializeField] private Light merchantLanternLight;
        public float baseLightIntensity = 2.4f;

        [Header("Auto-Bound Sub-Parts (Resolved via transform.Find)")]
        [SerializeField] private Transform partTorsoRibcage;
        [SerializeField] private Transform partCloak;
        [SerializeField] private Transform partBeltAccessories;
        [SerializeField] private Transform partPelvisRoot;
        [SerializeField] private Transform partShinR;
        [SerializeField] private Transform partShinL;
        [SerializeField] private Transform partThighR;
        [SerializeField] private Transform partThighL;
        [SerializeField] private Transform partHead;
        [SerializeField] private Transform partJaw;
        [SerializeField] private Transform partArmR;
        [SerializeField] private Transform partArmL;
        [SerializeField] private Transform propFloatingCoin;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _mpb;
        private float _animClock;

        private Vector3 _torsoRestPos, _cloakRestPos, _beltRestPos, _pelvisRestPos;
        private Vector3 _thighRRestPos, _thighLRestPos, _armLRestPos, _coinRestPos;
        private Quaternion _torsoRestRot, _cloakRestRot, _beltRestRot, _pelvisRestRot;
        private Quaternion _shinRRestRot, _shinLRestRot, _thighRRestRot, _thighLRestRot;
        private Quaternion _headRestRot, _jawRestRot, _armRRestRot, _armLRestRot, _coinRestRot;

        private Vector2 _shinKneeOffsetR = new Vector2(0.249f, 0.0243f);
        private Vector2 _shinKneeOffsetL = new Vector2(0.246f, 0.0486f);
        private float _thighLenR = 0.3968f, _thighLenL = 0.3935f;
        private float _restThighPitchR = 0.244f, _restThighPitchL = 0.1326f;

        private struct EmissiveSlot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color BaseEmission;
        }

        private readonly List<EmissiveSlot> _emissiveSlots = new List<EmissiveSlot>(8);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            BindMerchantParts();
            CacheRestTransforms();
            CacheEmissiveRuneRenderers();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _mpb != null)
            {
                ApplyEmissionBlock(Time.time);
            }
        }

        /// <summary>
        /// Uses transform.Find path checks with recursive deep fallback so both flat and hierarchical
        /// glTFast / Blender prefab layouts bind automatically.
        /// </summary>
        public void BindMerchantParts()
        {
            if (partTorsoRibcage == null)
            {
                partTorsoRibcage = transform.Find("Part_Torso_Ribcage")
                    ?? FindDeepChild(transform, "Part_Torso_Ribcage");
            }
            if (partCloak == null)
            {
                partCloak = transform.Find("Part_Cloak")
                    ?? FindDeepChild(transform, "Part_Cloak");
            }
            if (partBeltAccessories == null)
            {
                partBeltAccessories = transform.Find("Part_Belt_Accessories")
                    ?? FindDeepChild(transform, "Part_Belt_Accessories");
            }
            if (partPelvisRoot == null)
            {
                partPelvisRoot = transform.Find("Part_Legs_Boots/Part_PelvisRoot")
                    ?? transform.Find("Part_PelvisRoot")
                    ?? FindDeepChild(transform, "Part_PelvisRoot");
            }
            if (partShinR == null)
            {
                partShinR = transform.Find("Part_Legs_Boots/Part_Shin_R")
                    ?? transform.Find("Part_Shin_R")
                    ?? FindDeepChild(transform, "Part_Shin_R");
            }
            if (partShinL == null)
            {
                partShinL = transform.Find("Part_Legs_Boots/Part_Shin_L")
                    ?? transform.Find("Part_Shin_L")
                    ?? FindDeepChild(transform, "Part_Shin_L");
            }
            if (partThighR == null)
            {
                partThighR = transform.Find("Part_Legs_Boots/Part_Thigh_R")
                    ?? transform.Find("Part_Thigh_R")
                    ?? FindDeepChild(transform, "Part_Thigh_R");
            }
            if (partThighL == null)
            {
                partThighL = transform.Find("Part_Legs_Boots/Part_Thigh_L")
                    ?? transform.Find("Part_Thigh_L")
                    ?? FindDeepChild(transform, "Part_Thigh_L");
            }

            if (partHead == null)
            {
                partHead = transform.Find("Part_Torso_Ribcage/Part_Head")
                    ?? transform.Find("Part_Head")
                    ?? FindDeepChild(transform, "Part_Head");
            }

            if (partJaw == null)
            {
                partJaw = transform.Find("Part_Torso_Ribcage/Part_Head/Part_Jaw")
                    ?? transform.Find("Part_Head/Part_Jaw")
                    ?? transform.Find("Part_Jaw")
                    ?? FindDeepChild(transform, "Part_Jaw");
            }

            if (partArmR == null)
            {
                partArmR = transform.Find("Part_Torso_Ribcage/Part_Arm_R")
                    ?? transform.Find("Part_Arm_R")
                    ?? FindDeepChild(transform, "Part_Arm_R");
            }

            if (partArmL == null)
            {
                partArmL = transform.Find("Part_Torso_Ribcage/Part_Arm_L")
                    ?? transform.Find("Part_Arm_L")
                    ?? FindDeepChild(transform, "Part_Arm_L");
            }

            if (propFloatingCoin == null)
            {
                propFloatingCoin = transform.Find("Part_Torso_Ribcage/Part_Arm_R/Prop_FloatingCoin")
                    ?? transform.Find("Part_Arm_R/Prop_FloatingCoin")
                    ?? FindDeepChild(transform, "Prop_FloatingCoin");
            }

            if (merchantLanternLight == null)
            {
                merchantLanternLight = GetComponentInChildren<Light>(true);
            }
        }

        private void CacheRestTransforms()
        {
            if (partTorsoRibcage != null) { _torsoRestPos = partTorsoRibcage.localPosition; _torsoRestRot = partTorsoRibcage.localRotation; }
            if (partCloak != null) { _cloakRestPos = partCloak.localPosition; _cloakRestRot = partCloak.localRotation; }
            if (partBeltAccessories != null) { _beltRestPos = partBeltAccessories.localPosition; _beltRestRot = partBeltAccessories.localRotation; }
            if (partPelvisRoot != null) { _pelvisRestPos = partPelvisRoot.localPosition; _pelvisRestRot = partPelvisRoot.localRotation; }
            if (partShinR != null) _shinRRestRot = partShinR.localRotation;
            if (partShinL != null) _shinLRestRot = partShinL.localRotation;
            if (partThighR != null) { _thighRRestPos = partThighR.localPosition; _thighRRestRot = partThighR.localRotation; }
            if (partThighL != null) { _thighLRestPos = partThighL.localPosition; _thighLRestRot = partThighL.localRotation; }
            if (partHead != null) _headRestRot = partHead.localRotation;
            if (partJaw != null) _jawRestRot = partJaw.localRotation;
            if (partArmR != null) _armRRestRot = partArmR.localRotation;
            if (partArmL != null) { _armLRestPos = partArmL.localPosition; _armLRestRot = partArmL.localRotation; }
            if (propFloatingCoin != null) { _coinRestPos = propFloatingCoin.localPosition; _coinRestRot = propFloatingCoin.localRotation; }

            if (partShinR != null && partThighR != null)
            {
                float kRy = partShinR.localPosition.y + _shinKneeOffsetR.x;
                float kRz = partShinR.localPosition.z + _shinKneeOffsetR.y;
                float dz = kRz - _thighRRestPos.z;
                float dyDown = Mathf.Max(0.05f, _thighRRestPos.y - kRy);
                _thighLenR = Mathf.Sqrt(dyDown * dyDown + dz * dz);
                _restThighPitchR = Mathf.Atan2(dz, dyDown);
            }
            if (partShinL != null && partThighL != null)
            {
                float kLy = partShinL.localPosition.y + _shinKneeOffsetL.x;
                float kLz = partShinL.localPosition.z + _shinKneeOffsetL.y;
                float dz = kLz - _thighLRestPos.z;
                float dyDown = Mathf.Max(0.05f, _thighLRestPos.y - kLy);
                _thighLenL = Mathf.Sqrt(dyDown * dyDown + dz * dz);
                _restThighPitchL = Mathf.Atan2(dz, dyDown);
            }
        }

        private void CacheEmissiveRuneRenderers()
        {
            _emissiveSlots.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Material[] mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;

                    bool isRuneMat = mat.name.Contains("MAT_Emissive_Rune") || mat.name.Contains("Emissive_Rune");
                    if (!isRuneMat) continue;

                    Color baseCol = mat.HasProperty(EmissionColorId) ? mat.GetColor(EmissionColorId) : fallbackRuneEmission;
                    if (baseCol.maxColorComponent < 0.05f) baseCol = fallbackRuneEmission;

                    _emissiveSlots.Add(new EmissiveSlot
                    {
                        Renderer = r,
                        MaterialIndex = m,
                        BaseEmission = baseCol
                    });
                }
            }
        }

        private void Update()
        {
            if (idleSpeed > 0.0001f)
            {
                _animClock += Time.deltaTime * idleSpeed;
                UpdateIdlePose(_animClock);
            }

            ApplyEmissionBlock(Time.time);
        }

        private void UpdateIdlePose(float t)
        {
            // 1. Living bent-knee breath cycle (ankle-up kinematics so boots remain grounded)
            float crouchPhase = 0.5f - 0.5f * Mathf.Cos(t * 1.45f);
            float breathSin = Mathf.Sin(t * 1.45f);
            float shinFlexR = crouchPhase * (kneeFlexDegrees * Mathf.Deg2Rad);
            float shinFlexL = crouchPhase * (kneeFlexDegrees * 0.94f * Mathf.Deg2Rad);
            float pelvisDY = 0f;
            float pelvisDZ = -crouchPhase * 0.004f;

            if (partShinR != null && partThighR != null && partShinL != null && partThighL != null)
            {
                partShinR.localRotation = _shinRRestRot * Quaternion.Euler(shinFlexR * Mathf.Rad2Deg, 0f, 0f);
                partShinL.localRotation = _shinLRestRot * Quaternion.Euler(shinFlexL * Mathf.Rad2Deg, 0f, 0f);

                float cR = Mathf.Cos(shinFlexR), sR = Mathf.Sin(shinFlexR);
                float kRy = partShinR.localPosition.y + (_shinKneeOffsetR.x * cR - _shinKneeOffsetR.y * sR);
                float kRz = partShinR.localPosition.z + (_shinKneeOffsetR.x * sR + _shinKneeOffsetR.y * cR);
                float reqDzR = kRz - (_thighRRestPos.z + pelvisDZ);
                float reqDyR = Mathf.Sqrt(Mathf.Max(0.01f, _thighLenR * _thighLenR - reqDzR * reqDzR));
                pelvisDY = (kRy + reqDyR) - _thighRRestPos.y;
                float newPitchR = Mathf.Atan2(reqDzR, reqDyR);
                partThighR.localPosition = _thighRRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partThighR.localRotation = _thighRRestRot * Quaternion.Euler(-(newPitchR - _restThighPitchR) * Mathf.Rad2Deg, 0f, 0f);

                float cL = Mathf.Cos(shinFlexL), sL = Mathf.Sin(shinFlexL);
                float kLy = partShinL.localPosition.y + (_shinKneeOffsetL.x * cL - _shinKneeOffsetL.y * sL);
                float kLz = partShinL.localPosition.z + (_shinKneeOffsetL.x * sL + _shinKneeOffsetL.y * cL);
                float reqDzL = kLz - (_thighLRestPos.z + pelvisDZ);
                float reqDyL = Mathf.Max(0.05f, (_thighLRestPos.y + pelvisDY) - kLy);
                float newPitchL = Mathf.Atan2(reqDzL, reqDyL);
                partThighL.localPosition = _thighLRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partThighL.localRotation = _thighLRestRot * Quaternion.Euler(-(newPitchL - _restThighPitchL) * Mathf.Rad2Deg, 0f, 0f);
            }

            if (partPelvisRoot != null)
            {
                partPelvisRoot.localPosition = _pelvisRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partPelvisRoot.localRotation = _pelvisRestRot * Quaternion.Euler(crouchPhase * 0.92f, 0f, 0f);
            }

            // 2. Ribcage, thick cloak & belt synchronized with crouched breath
            float torsoPitchDeg = crouchPhase * breathPitchDegrees;
            float torsoYawDeg = Mathf.Sin(t * 0.65f) * 1.05f;
            if (partTorsoRibcage != null)
            {
                partTorsoRibcage.localPosition = _torsoRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partTorsoRibcage.localRotation = _torsoRestRot * Quaternion.Euler(torsoPitchDeg, torsoYawDeg, 0f);
            }
            if (partCloak != null)
            {
                partCloak.localPosition = _cloakRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partCloak.localRotation = _cloakRestRot * Quaternion.Euler(crouchPhase * 0.68f, torsoYawDeg * 0.65f, 0f);
            }
            if (partBeltAccessories != null)
            {
                partBeltAccessories.localPosition = _beltRestPos + new Vector3(0f, pelvisDY, pelvisDZ);
                partBeltAccessories.localRotation = _beltRestRot * Quaternion.Euler(crouchPhase * 0.80f, torsoYawDeg * 0.70f, 0f);
            }
            if (partArmL != null)
            {
                partArmL.localPosition = _armLRestPos - new Vector3(0f, pelvisDY, pelvisDZ);
                partArmL.localRotation = _armLRestRot * Quaternion.Euler(-torsoPitchDeg * 0.65f, 0f, 0f);
            }

            // 3. Head curious shopkeeper glance & level gaze compensation
            if (partHead != null)
            {
                float yaw = Mathf.Sin(t * 0.78f) * headSwayDegrees;
                float pitch = Mathf.Sin(t * 1.56f + 0.5f) * (headSwayDegrees * 0.40f) - torsoPitchDeg * 0.55f;
                float roll = Mathf.Cos(t * 0.52f) * (headSwayDegrees * 0.38f);
                partHead.localRotation = _headRestRot * Quaternion.Euler(pitch, yaw, roll);
            }

            // 4. Keep jaw completely still at rest pose during idle
            if (partJaw != null)
            {
                partJaw.localRotation = _jawRestRot;
            }

            // 5. Right arm rubbing fingers / inviting customers
            if (partArmR != null)
            {
                float beckon = Mathf.Sin(t * 1.9f);
                float rub = Mathf.Cos(t * 3.8f) * 0.45f;
                partArmR.localRotation = _armRRestRot * Quaternion.Euler(
                    beckon * rightArmBeckonDegrees + breathSin * 0.55f,
                    rub * rightArmBeckonDegrees * 0.55f,
                    Mathf.Sin(t * 1.2f) * (rightArmBeckonDegrees * 0.32f)
                );
            }

            // 6. Optional hovering gold coin above right fingertips
            if (propFloatingCoin != null)
            {
                Vector3 cPos = _coinRestPos;
                cPos.y += Mathf.Sin(t * 2.8f) * coinBobAmplitude;
                propFloatingCoin.localPosition = cPos;
                propFloatingCoin.localRotation = _coinRestRot * Quaternion.Euler(0f, t * coinSpinDegPerSec, Mathf.Sin(t * 2.1f) * 6f);
            }
        }

        private void ApplyEmissionBlock(float t)
        {
            float pulse = 1f + soulPulseAmplitude * (0.68f * Mathf.Sin(t * soulPulseFrequency) + 0.32f * Mathf.Sin(t * soulPulseFrequency * 3.7f + 0.9f));
            float gain = emissionIntensity * Mathf.Max(0.05f, pulse);

            for (int i = 0; i < _emissiveSlots.Count; i++)
            {
                EmissiveSlot slot = _emissiveSlots[i];
                if (slot.Renderer == null || !slot.Renderer.gameObject.activeInHierarchy) continue;

                slot.Renderer.GetPropertyBlock(_mpb, slot.MaterialIndex);
                _mpb.SetColor(EmissionColorId, slot.BaseEmission * gain);
                slot.Renderer.SetPropertyBlock(_mpb, slot.MaterialIndex);
            }

            if (merchantLanternLight != null)
            {
                merchantLanternLight.intensity = baseLightIntensity * Mathf.Max(0.1f, pulse);
            }
        }

        /// <summary>
        /// Immediately snaps all animated merchant joints back to canonical Rest Pose.
        /// </summary>
        [ContextMenu("Reset To Rest Pose")]
        public void ResetToRestPose()
        {
            if (partTorsoRibcage != null) { partTorsoRibcage.localPosition = _torsoRestPos; partTorsoRibcage.localRotation = _torsoRestRot; }
            if (partCloak != null) { partCloak.localPosition = _cloakRestPos; partCloak.localRotation = _cloakRestRot; }
            if (partBeltAccessories != null) { partBeltAccessories.localPosition = _beltRestPos; partBeltAccessories.localRotation = _beltRestRot; }
            if (partPelvisRoot != null) { partPelvisRoot.localPosition = _pelvisRestPos; partPelvisRoot.localRotation = _pelvisRestRot; }
            if (partShinR != null) partShinR.localRotation = _shinRRestRot;
            if (partShinL != null) partShinL.localRotation = _shinLRestRot;
            if (partThighR != null) { partThighR.localPosition = _thighRRestPos; partThighR.localRotation = _thighRRestRot; }
            if (partThighL != null) { partThighL.localPosition = _thighLRestPos; partThighL.localRotation = _thighLRestRot; }
            if (partHead != null) partHead.localRotation = _headRestRot;
            if (partJaw != null) partJaw.localRotation = _jawRestRot;
            if (partArmR != null) partArmR.localRotation = _armRRestRot;
            if (partArmL != null) { partArmL.localPosition = _armLRestPos; partArmL.localRotation = _armLRestRot; }
            if (propFloatingCoin != null)
            {
                propFloatingCoin.localPosition = _coinRestPos;
                propFloatingCoin.localRotation = _coinRestRot;
            }
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == targetName) return child;
                Transform nested = FindDeepChild(child, targetName);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
