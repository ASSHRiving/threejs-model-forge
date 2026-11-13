using UnityEngine;

namespace ModelForge.WraithIX
{
    /// <summary>
    /// Controls vector thrust nozzle deflection, flight control surfaces (canards, ailerons,
    /// rudders, stabilators), canopy articulation, airbrake deployment, and afterburner
    /// MaterialPropertyBlock emission on the Wraith-IX Stealth Interceptor GLB/FBX prefab.
    /// </summary>
    public class StealthInterceptorController : MonoBehaviour
    {
        [Header("Flight Control Inputs (-1 to +1)")]
        [Range(-1f, 1f)] public float pitchInput;
        [Range(-1f, 1f)] public float rollInput;
        [Range(-1f, 1f)] public float yawInput;
        [Range(0f, 1f)] public float afterburnerThrust = 0.65f;
        [Range(0f, 1f)] public float canopyOpen;
        [Range(0f, 1f)] public float airbrakeDeploy;

        [Header("Articulated Nodes (Auto-Resolved if Null)")]
        [SerializeField] private Transform canopyAssembly;
        [SerializeField] private Transform airbrakePetal;
        [SerializeField] private Transform canardLeft;
        [SerializeField] private Transform canardRight;
        [SerializeField] private Transform aileronLeft;
        [SerializeField] private Transform aileronRight;
        [SerializeField] private Transform leFlapLeft;
        [SerializeField] private Transform leFlapRight;
        [SerializeField] private Transform rudderLeft;
        [SerializeField] private Transform rudderRight;
        [SerializeField] private Transform stabilatorLeft;
        [SerializeField] private Transform stabilatorRight;
        [SerializeField] private Transform nozzleLeft;
        [SerializeField] private Transform nozzleRight;
        [SerializeField] private Renderer[] afterburnerRenderers;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private Quaternion _canopyRest, _airbrakeRest;
        private Quaternion _canardLRest, _canardRRest;
        private Quaternion _aileronLRest, _aileronRRest;
        private Quaternion _flapLRest, _flapRRest;
        private Quaternion _rudderLRest, _rudderRRest;
        private Quaternion _stabLRest, _stabRRest;
        private Quaternion _nozzleLRest, _nozzleRRest;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            if (canopyAssembly == null) canopyAssembly = FindDeepChild(transform, "Part_Canopy_Assembly");
            if (airbrakePetal == null) airbrakePetal = FindDeepChild(transform, "Part_Airbrake");
            if (canardLeft == null) canardLeft = FindDeepChild(transform, "Part_Canard_L");
            if (canardRight == null) canardRight = FindDeepChild(transform, "Part_Canard_R");
            if (aileronLeft == null) aileronLeft = FindDeepChild(transform, "Part_Aileron_L");
            if (aileronRight == null) aileronRight = FindDeepChild(transform, "Part_Aileron_R");
            if (leFlapLeft == null) leFlapLeft = FindDeepChild(transform, "Part_LE_Flap_L");
            if (leFlapRight == null) leFlapRight = FindDeepChild(transform, "Part_LE_Flap_R");
            if (rudderLeft == null) rudderLeft = FindDeepChild(transform, "Part_Rudder_L");
            if (rudderRight == null) rudderRight = FindDeepChild(transform, "Part_Rudder_R");
            if (stabilatorLeft == null) stabilatorLeft = FindDeepChild(transform, "Part_Stabilator_L");
            if (stabilatorRight == null) stabilatorRight = FindDeepChild(transform, "Part_Stabilator_R");
            if (nozzleLeft == null) nozzleLeft = FindDeepChild(transform, "Part_Nozzle_L");
            if (nozzleRight == null) nozzleRight = FindDeepChild(transform, "Part_Nozzle_R");

            if (canopyAssembly) _canopyRest = canopyAssembly.localRotation;
            if (airbrakePetal) _airbrakeRest = airbrakePetal.localRotation;
            if (canardLeft) _canardLRest = canardLeft.localRotation;
            if (canardRight) _canardRRest = canardRight.localRotation;
            if (aileronLeft) _aileronLRest = aileronLeft.localRotation;
            if (aileronRight) _aileronRRest = aileronRight.localRotation;
            if (leFlapLeft) _flapLRest = leFlapLeft.localRotation;
            if (leFlapRight) _flapRRest = leFlapRight.localRotation;
            if (rudderLeft) _rudderLRest = rudderLeft.localRotation;
            if (rudderRight) _rudderRRest = rudderRight.localRotation;
            if (stabilatorLeft) _stabLRest = stabilatorLeft.localRotation;
            if (stabilatorRight) _stabRRest = stabilatorRight.localRotation;
            if (nozzleLeft) _nozzleLRest = nozzleLeft.localRotation;
            if (nozzleRight) _nozzleRRest = nozzleRight.localRotation;
        }

        private void Update()
        {
            if (canopyAssembly) canopyAssembly.localRotation = _canopyRest * Quaternion.Euler(-canopyOpen * 36f, 0f, 0f);
            if (airbrakePetal) airbrakePetal.localRotation = _airbrakeRest * Quaternion.Euler(-airbrakeDeploy * 42f, 0f, 0f);

            float pitchDeg = pitchInput * 18f;
            float rollDeg = rollInput * 20f;
            float yawDeg = yawInput * 16f;

            if (canardLeft) canardLeft.localRotation = _canardLRest * Quaternion.Euler(pitchDeg * 0.9f, 0f, 0f);
            if (canardRight) canardRight.localRotation = _canardRRest * Quaternion.Euler(pitchDeg * 0.9f, 0f, 0f);

            if (aileronLeft) aileronLeft.localRotation = _aileronLRest * Quaternion.Euler(rollDeg, 0f, 0f);
            if (aileronRight) aileronRight.localRotation = _aileronRRest * Quaternion.Euler(-rollDeg, 0f, 0f);

            if (leFlapLeft) leFlapLeft.localRotation = _flapLRest * Quaternion.Euler(-Mathf.Max(0f, pitchInput) * 12f, 0f, 0f);
            if (leFlapRight) leFlapRight.localRotation = _flapRRest * Quaternion.Euler(-Mathf.Max(0f, pitchInput) * 12f, 0f, 0f);

            if (rudderLeft) rudderLeft.localRotation = _rudderLRest * Quaternion.Euler(0f, yawDeg, 0f);
            if (rudderRight) rudderRight.localRotation = _rudderRRest * Quaternion.Euler(0f, yawDeg, 0f);

            if (stabilatorLeft) stabilatorLeft.localRotation = _stabLRest * Quaternion.Euler(-pitchDeg + rollDeg * 0.4f, 0f, 0f);
            if (stabilatorRight) stabilatorRight.localRotation = _stabRRest * Quaternion.Euler(-pitchDeg - rollDeg * 0.4f, 0f, 0f);

            if (nozzleLeft) nozzleLeft.localRotation = _nozzleLRest * Quaternion.Euler(pitchDeg * 0.85f, yawDeg * 0.35f, 0f);
            if (nozzleRight) nozzleRight.localRotation = _nozzleRRest * Quaternion.Euler(pitchDeg * 0.85f, yawDeg * 0.35f, 0f);

            if (afterburnerRenderers != null && afterburnerRenderers.Length > 0)
            {
                float intensity = Mathf.Lerp(0.4f, 4.2f, afterburnerThrust);
                Color hdr = new Color(0.22f, 0.78f, 1.0f, 1f) * Mathf.LinearToGammaSpace(intensity);
                for (int i = 0; i < afterburnerRenderers.Length; i++)
                {
                    Renderer r = afterburnerRenderers[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionColorId, hdr);
                    r.SetPropertyBlock(_mpb);
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
