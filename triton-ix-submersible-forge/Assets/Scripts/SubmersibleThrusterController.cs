using UnityEngine;

namespace ModelForge.TritonIX
{
    /// <summary>
    /// Controls thruster rotor spin, acoustic scanner sweep, and subtle manipulator
    /// idle articulation on the Triton-IX Deep-Sea Submersible GLB/FBX prefab.
    /// Attach to the root Triton_IX_Submersible_Rig GameObject.
    /// </summary>
    public class SubmersibleThrusterController : MonoBehaviour
    {
        [Header("Thruster Rotors (Auto-Resolved if Null)")]
        [SerializeField] private Transform propellerLeft;
        [SerializeField] private Transform propellerRight;
        [SerializeField] private Transform propellerStern;
        [SerializeField] private Transform radarBeacon;

        [Header("Manipulator Arms")]
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;

        [Header("Speeds")]
        [SerializeField] private float sideThrusterRpm = 420f;
        [SerializeField] private float sternThrusterRpm = 310f;
        [SerializeField] private float radarSweepDegPerSec = 55f;
        [SerializeField] private float armIdleAmplitudeDeg = 2.5f;
        [SerializeField] private float armIdleFrequency = 0.65f;

        private Quaternion _armLeftRest;
        private Quaternion _armRightRest;
        private bool _hasArmRest;

        private void Awake()
        {
            if (propellerLeft == null) propellerLeft = FindDeepChild(transform, "Part_Propeller_L");
            if (propellerRight == null) propellerRight = FindDeepChild(transform, "Part_Propeller_R");
            if (propellerStern == null) propellerStern = FindDeepChild(transform, "Part_Propeller_Stern");
            if (radarBeacon == null) radarBeacon = FindDeepChild(transform, "Part_Radar_Beacon");
            if (armLeft == null) armLeft = FindDeepChild(transform, "Part_Arm_L");
            if (armRight == null) armRight = FindDeepChild(transform, "Part_Arm_R");

            if (armLeft != null && armRight != null)
            {
                _armLeftRest = armLeft.localRotation;
                _armRightRest = armRight.localRotation;
                _hasArmRest = true;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float sideDeg = sideThrusterRpm * 6f * dt;
            float sternDeg = sternThrusterRpm * 6f * dt;

            if (propellerLeft != null) propellerLeft.Rotate(0f, 0f, sideDeg, Space.Self);
            if (propellerRight != null) propellerRight.Rotate(0f, 0f, -sideDeg, Space.Self);
            if (propellerStern != null) propellerStern.Rotate(0f, 0f, sternDeg, Space.Self);
            if (radarBeacon != null) radarBeacon.Rotate(0f, radarSweepDegPerSec * dt, 0f, Space.Self);

            if (_hasArmRest)
            {
                float wave = Mathf.Sin(Time.time * armIdleFrequency * Mathf.PI * 2f) * armIdleAmplitudeDeg;
                armLeft.localRotation = _armLeftRest * Quaternion.Euler(wave, 0f, 0f);
                armRight.localRotation = _armRightRest * Quaternion.Euler(-wave, 0f, 0f);
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
