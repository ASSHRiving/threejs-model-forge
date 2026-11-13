using UnityEngine;

namespace ModelForge.Aero900X
{
    /// <summary>
    /// Unity URP runtime flight-systems controller for the AERO-900X Widebody Commercial Airliner.
    /// Drives sequenced retractable tricycle landing gear and bay doors, slotted Fowler flaps, slats,
    /// spoilers, primary flight control surfaces, high-bypass turbofan rotors and thrust reversers,
    /// cutaway upper fuselage shell visibility, and MaterialPropertyBlock aviation lighting.
    /// </summary>
    public class AirlinerFlightSystemsController : MonoBehaviour
    {
        [Header("Flight & Ground Systems")]
        [Range(0f, 1f)] public float landingGearDeploy = 1f;
        [Range(0f, 1f)] public float flapsSlatsDeploy = 0f;
        [Range(0f, 1f)] public float engineThrottle = 0.45f;
        [Range(0f, 1f)] public float thrustReverserDeploy = 0f;
        [Range(0f, 1f)] public float cabinCutaway = 0f;
        public bool aviationLightsEnabled = true;

        [Header("Primary Aerodynamic Control Inputs (-1 to +1)")]
        [Range(-1f, 1f)] public float pitchInput = 0f;
        [Range(-1f, 1f)] public float rollInput = 0f;
        [Range(-1f, 1f)] public float yawInput = 0f;

        [Header("Landing Gear & Bay Door Nodes (Auto-Resolved)")]
        [SerializeField] private Transform noseGear;
        [SerializeField] private Transform mainGearLeft;
        [SerializeField] private Transform mainGearRight;
        [SerializeField] private Transform gearDoorNoseLeft;
        [SerializeField] private Transform gearDoorNoseRight;
        [SerializeField] private Transform gearDoorMainLeft;
        [SerializeField] private Transform gearDoorMainRight;
        [SerializeField] private Transform gearDoorStrutLeft;
        [SerializeField] private Transform gearDoorStrutRight;

        [Header("High-Lift & Control Surface Nodes (Auto-Resolved)")]
        [SerializeField] private Transform flapLeft;
        [SerializeField] private Transform flapRight;
        [SerializeField] private Transform slatLeft;
        [SerializeField] private Transform slatRight;
        [SerializeField] private Transform spoilerLeft;
        [SerializeField] private Transform spoilerRight;
        [SerializeField] private Transform aileronLeft;
        [SerializeField] private Transform aileronRight;
        [SerializeField] private Transform elevatorLeft;
        [SerializeField] private Transform elevatorRight;
        [SerializeField] private Transform rudder;

        [Header("Turbofan Propulsion & Interior Cutaway Nodes")]
        [SerializeField] private Transform fanBladesLeft;
        [SerializeField] private Transform fanBladesRight;
        [SerializeField] private Transform thrustReverserLeft;
        [SerializeField] private Transform thrustReverserRight;
        [SerializeField] private Transform fuselageUpperShell;

        [Header("Emissive Lighting Renderers (Optional Auto-Scan)")]
        [SerializeField] private Renderer[] strobeRenderers;
        [SerializeField] private Renderer[] beaconRenderers;
        [SerializeField] private Renderer[] cabinWindowRenderers;
        [SerializeField] private Renderer[] landingLightRenderers;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private Quaternion _noseGearRest, _mainGearLRest, _mainGearRRest;
        private Quaternion _doorNoseLRest, _doorNoseRRest, _doorMainLRest, _doorMainRRest, _doorStrutLRest, _doorStrutRRest;
        private Vector3 _flapLPosRest, _flapRPosRest, _slatLPosRest, _slatRPosRest;
        private Quaternion _flapLRotRest, _flapRRotRest, _slatLRotRest, _slatRRotRest;
        private Quaternion _spoilerLRest, _spoilerRRest, _aileronLRest, _aileronRRest;
        private Quaternion _elevatorLRest, _elevatorRRest, _rudderRest;
        private Vector3 _revLPosRest, _revRPosRest;

        private float _fanAngleL;
        private float _fanAngleR;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (noseGear == null) noseGear = FindDeepChild(transform, "Part_NoseGear");
            if (mainGearLeft == null) mainGearLeft = FindDeepChild(transform, "Part_MainGear_L");
            if (mainGearRight == null) mainGearRight = FindDeepChild(transform, "Part_MainGear_R");
            if (gearDoorNoseLeft == null) gearDoorNoseLeft = FindDeepChild(transform, "Part_GearDoor_Nose_L");
            if (gearDoorNoseRight == null) gearDoorNoseRight = FindDeepChild(transform, "Part_GearDoor_Nose_R");
            if (gearDoorMainLeft == null) gearDoorMainLeft = FindDeepChild(transform, "Part_GearDoor_Main_L");
            if (gearDoorMainRight == null) gearDoorMainRight = FindDeepChild(transform, "Part_GearDoor_Main_R");
            if (gearDoorStrutLeft == null) gearDoorStrutLeft = FindDeepChild(transform, "Part_GearDoor_Strut_L");
            if (gearDoorStrutRight == null) gearDoorStrutRight = FindDeepChild(transform, "Part_GearDoor_Strut_R");

            if (flapLeft == null) flapLeft = FindDeepChild(transform, "Part_Flap_L");
            if (flapRight == null) flapRight = FindDeepChild(transform, "Part_Flap_R");
            if (slatLeft == null) slatLeft = FindDeepChild(transform, "Part_Slat_L");
            if (slatRight == null) slatRight = FindDeepChild(transform, "Part_Slat_R");
            if (spoilerLeft == null) spoilerLeft = FindDeepChild(transform, "Part_Spoiler_L");
            if (spoilerRight == null) spoilerRight = FindDeepChild(transform, "Part_Spoiler_R");
            if (aileronLeft == null) aileronLeft = FindDeepChild(transform, "Part_Aileron_L");
            if (aileronRight == null) aileronRight = FindDeepChild(transform, "Part_Aileron_R");
            if (elevatorLeft == null) elevatorLeft = FindDeepChild(transform, "Part_Elevator_L");
            if (elevatorRight == null) elevatorRight = FindDeepChild(transform, "Part_Elevator_R");
            if (rudder == null) rudder = FindDeepChild(transform, "Part_Rudder");

            if (fanBladesLeft == null) fanBladesLeft = FindDeepChild(transform, "Part_FanBlades_L");
            if (fanBladesRight == null) fanBladesRight = FindDeepChild(transform, "Part_FanBlades_R");
            if (thrustReverserLeft == null) thrustReverserLeft = FindDeepChild(transform, "Part_ThrustReverser_L");
            if (thrustReverserRight == null) thrustReverserRight = FindDeepChild(transform, "Part_ThrustReverser_R");
            if (fuselageUpperShell == null) fuselageUpperShell = FindDeepChild(transform, "Part_Fuselage_UpperShell");

            CacheRestTransforms();
            AutoBindLightingRenderers();
        }

        private void CacheRestTransforms()
        {
            if (noseGear) _noseGearRest = noseGear.localRotation;
            if (mainGearLeft) _mainGearLRest = mainGearLeft.localRotation;
            if (mainGearRight) _mainGearRRest = mainGearRight.localRotation;

            if (gearDoorNoseLeft) _doorNoseLRest = gearDoorNoseLeft.localRotation;
            if (gearDoorNoseRight) _doorNoseRRest = gearDoorNoseRight.localRotation;
            if (gearDoorMainLeft) _doorMainLRest = gearDoorMainLeft.localRotation;
            if (gearDoorMainRight) _doorMainRRest = gearDoorMainRight.localRotation;
            if (gearDoorStrutLeft) _doorStrutLRest = gearDoorStrutLeft.localRotation;
            if (gearDoorStrutRight) _doorStrutRRest = gearDoorStrutRight.localRotation;

            if (flapLeft) { _flapLPosRest = flapLeft.localPosition; _flapLRotRest = flapLeft.localRotation; }
            if (flapRight) { _flapRPosRest = flapRight.localPosition; _flapRRotRest = flapRight.localRotation; }
            if (slatLeft) { _slatLPosRest = slatLeft.localPosition; _slatLRotRest = slatLeft.localRotation; }
            if (slatRight) { _slatRPosRest = slatRight.localPosition; _slatRRotRest = slatRight.localRotation; }
            if (spoilerLeft) _spoilerLRest = spoilerLeft.localRotation;
            if (spoilerRight) _spoilerRRest = spoilerRight.localRotation;
            if (aileronLeft) _aileronLRest = aileronLeft.localRotation;
            if (aileronRight) _aileronRRest = aileronRight.localRotation;
            if (elevatorLeft) _elevatorLRest = elevatorLeft.localRotation;
            if (elevatorRight) _elevatorRRest = elevatorRight.localRotation;
            if (rudder) _rudderRest = rudder.localRotation;

            if (thrustReverserLeft) _revLPosRest = thrustReverserLeft.localPosition;
            if (thrustReverserRight) _revRPosRest = thrustReverserRight.localPosition;
        }

        private void AutoBindLightingRenderers()
        {
            if (strobeRenderers == null || strobeRenderers.Length == 0)
            {
                strobeRenderers = FindRenderersByPrefix("Strobe_");
            }
            if (beaconRenderers == null || beaconRenderers.Length == 0)
            {
                beaconRenderers = FindRenderersByPrefix("Beacon_");
            }
            if (cabinWindowRenderers == null || cabinWindowRenderers.Length == 0)
            {
                cabinWindowRenderers = FindRenderersByPrefix("Cabin_Windows_Glass");
            }
            if (landingLightRenderers == null || landingLightRenderers.Length == 0)
            {
                landingLightRenderers = FindRenderersByPrefix("LandingLight_");
            }
        }

        private void Update()
        {
            UpdateLandingGearSequence();
            UpdateAerodynamicSurfaces();
            UpdatePropulsionSystems();
            UpdateCutawayShell();
            UpdateAviationLighting();
        }

        private void UpdateLandingGearSequence()
        {
            float doorPhase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(landingGearDeploy / 0.36f));
            float gearPhase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((landingGearDeploy - 0.22f) / 0.78f));

            if (noseGear) noseGear.localRotation = _noseGearRest * Quaternion.Euler(-gearPhase * 90f, 0f, 0f);
            if (mainGearLeft) mainGearLeft.localRotation = _mainGearLRest * Quaternion.Euler(0f, 0f, gearPhase * 90f);
            if (mainGearRight) mainGearRight.localRotation = _mainGearRRest * Quaternion.Euler(0f, 0f, -gearPhase * 90f);

            if (gearDoorNoseLeft) gearDoorNoseLeft.localRotation = _doorNoseLRest * Quaternion.Euler(0f, 0f, doorPhase * 84f);
            if (gearDoorNoseRight) gearDoorNoseRight.localRotation = _doorNoseRRest * Quaternion.Euler(0f, 0f, -doorPhase * 84f);
            if (gearDoorMainLeft) gearDoorMainLeft.localRotation = _doorMainLRest * Quaternion.Euler(0f, 0f, -doorPhase * 86f);
            if (gearDoorMainRight) gearDoorMainRight.localRotation = _doorMainRRest * Quaternion.Euler(0f, 0f, doorPhase * 86f);
            if (gearDoorStrutLeft) gearDoorStrutLeft.localRotation = _doorStrutLRest * Quaternion.Euler(0f, 0f, gearPhase * 90f);
            if (gearDoorStrutRight) gearDoorStrutRight.localRotation = _doorStrutRRest * Quaternion.Euler(0f, 0f, -gearPhase * 90f);
        }

        private void UpdateAerodynamicSurfaces()
        {
            if (flapLeft)
            {
                flapLeft.localPosition = _flapLPosRest + new Vector3(0f, -flapsSlatsDeploy * 0.28f, -flapsSlatsDeploy * 0.72f);
                flapLeft.localRotation = _flapLRotRest * Quaternion.Euler(-flapsSlatsDeploy * 34f, 0f, 0f);
            }
            if (flapRight)
            {
                flapRight.localPosition = _flapRPosRest + new Vector3(0f, -flapsSlatsDeploy * 0.28f, -flapsSlatsDeploy * 0.72f);
                flapRight.localRotation = _flapRRotRest * Quaternion.Euler(-flapsSlatsDeploy * 34f, 0f, 0f);
            }

            if (slatLeft)
            {
                slatLeft.localPosition = _slatLPosRest + new Vector3(0f, -flapsSlatsDeploy * 0.16f, flapsSlatsDeploy * 0.42f);
                slatLeft.localRotation = _slatLRotRest * Quaternion.Euler(flapsSlatsDeploy * 22f, 0f, 0f);
            }
            if (slatRight)
            {
                slatRight.localPosition = _slatRPosRest + new Vector3(0f, -flapsSlatsDeploy * 0.16f, flapsSlatsDeploy * 0.42f);
                slatRight.localRotation = _slatRRotRest * Quaternion.Euler(flapsSlatsDeploy * 22f, 0f, 0f);
            }

            float spoilerPhase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((flapsSlatsDeploy - 0.45f) / 0.55f));
            if (spoilerLeft) spoilerLeft.localRotation = _spoilerLRest * Quaternion.Euler(spoilerPhase * 42f, 0f, 0f);
            if (spoilerRight) spoilerRight.localRotation = _spoilerRRest * Quaternion.Euler(spoilerPhase * 42f, 0f, 0f);

            float rollDeg = rollInput * 20f;
            float pitchDeg = pitchInput * 18f;
            float yawDeg = yawInput * 22f;

            if (aileronLeft) aileronLeft.localRotation = _aileronLRest * Quaternion.Euler(-rollDeg, 0f, 0f);
            if (aileronRight) aileronRight.localRotation = _aileronRRest * Quaternion.Euler(rollDeg, 0f, 0f);
            if (elevatorLeft) elevatorLeft.localRotation = _elevatorLRest * Quaternion.Euler(pitchDeg, 0f, 0f);
            if (elevatorRight) elevatorRight.localRotation = _elevatorRRest * Quaternion.Euler(pitchDeg, 0f, 0f);
            if (rudder) rudder.localRotation = _rudderRest * Quaternion.Euler(0f, yawDeg, 0f);
        }

        private void UpdatePropulsionSystems()
        {
            float rpmDegPerSec = Mathf.Lerp(0f, 1080f, engineThrottle);
            _fanAngleL = (_fanAngleL + rpmDegPerSec * Time.deltaTime) % 360f;
            _fanAngleR = (_fanAngleR - rpmDegPerSec * Time.deltaTime) % 360f;

            if (fanBladesLeft) fanBladesLeft.localRotation = Quaternion.Euler(0f, 0f, _fanAngleL);
            if (fanBladesRight) fanBladesRight.localRotation = Quaternion.Euler(0f, 0f, _fanAngleR);

            if (thrustReverserLeft) thrustReverserLeft.localPosition = _revLPosRest + new Vector3(0f, 0f, -thrustReverserDeploy * 0.82f);
            if (thrustReverserRight) thrustReverserRight.localPosition = _revRPosRest + new Vector3(0f, 0f, -thrustReverserDeploy * 0.82f);
        }

        private void UpdateCutawayShell()
        {
            if (fuselageUpperShell == null) return;
            bool visible = cabinCutaway < 0.88f;
            if (fuselageUpperShell.gameObject.activeSelf != visible)
            {
                fuselageUpperShell.gameObject.SetActive(visible);
            }
        }

        private void UpdateAviationLighting()
        {
            float t = Time.time;
            float strobeCycle = t % 1.4f;
            bool doubleFlash = (strobeCycle < 0.08f) || (strobeCycle > 0.18f && strobeCycle < 0.26f);
            float strobeInt = aviationLightsEnabled ? (doubleFlash ? 4.8f : 0.2f) : 0f;

            float beaconWave = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 3.8f)), 4f);
            float beaconInt = aviationLightsEnabled ? Mathf.Lerp(0.25f, 4.2f, beaconWave) : 0f;

            float cabinInt = aviationLightsEnabled ? 0.9f : 0f;
            float landingInt = aviationLightsEnabled ? (landingGearDeploy > 0.25f ? 3.2f : 0.35f) : 0f;

            ApplyEmissionGroup(strobeRenderers, new Color(0.88f, 0.96f, 1f, 1f), strobeInt);
            ApplyEmissionGroup(beaconRenderers, new Color(1f, 0.14f, 0.12f, 1f), beaconInt);
            ApplyEmissionGroup(cabinWindowRenderers, new Color(1f, 0.85f, 0.65f, 1f), cabinInt);
            ApplyEmissionGroup(landingLightRenderers, new Color(1f, 0.96f, 0.82f, 1f), landingInt);
        }

        private void ApplyEmissionGroup(Renderer[] renderers, Color baseCol, float intensity)
        {
            if (renderers == null || renderers.Length == 0) return;
            Color hdr = baseCol * Mathf.LinearToGammaSpace(intensity);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, hdr);
                r.SetPropertyBlock(_mpb);
            }
        }

        private Renderer[] FindRenderersByPrefix(string prefix)
        {
            var all = GetComponentsInChildren<Renderer>(true);
            var matched = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    matched.Add(all[i]);
                }
            }
            return matched.ToArray();
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
