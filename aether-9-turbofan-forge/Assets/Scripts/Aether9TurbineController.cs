using UnityEngine;

namespace ModelForge.Aether9
{
    /// <summary>
    /// Drives intake spinner, counter-rotating compressor stages, and 16-petal
    /// thrust-vectoring nozzle constriction on the AETHER-9 Vectored Plasma-Turbofan.
    /// Attach to the root SciFi_Plasma_Turbine_Engine GameObject.
    /// </summary>
    public class Aether9TurbineController : MonoBehaviour
    {
        [Range(0f, 1f)] public float throttle = 0.68f;
        public Transform spinnerCone;
        public Transform primaryFanRotor;
        public Transform compressorStage02;
        public Transform exhaustNozzleGroup;

        private readonly Transform[] _petals = new Transform[16];

        private void Start()
        {
            if (!spinnerCone) spinnerCone = FindChild(transform, "Part_Intake_Spinner_Cone");
            if (!primaryFanRotor) primaryFanRotor = FindChild(transform, "Part_Primary_Fan_Rotor");
            if (!compressorStage02) compressorStage02 = FindChild(transform, "Part_Compressor_Stage_02");
            if (!exhaustNozzleGroup) exhaustNozzleGroup = FindChild(transform, "Part_Exhaust_Nozzle_Petals");

            for (int i = 0; i < 16; i++)
                _petals[i] = FindChild(transform, $"Part_Nozzle_Petal_{i + 1:00}");
        }

        private void Update()
        {
            float rpmSpeed = Mathf.Lerp(180f, 1650f, throttle) * Time.deltaTime;
            if (spinnerCone) spinnerCone.Rotate(0f, 0f, rpmSpeed, Space.Self);
            if (primaryFanRotor) primaryFanRotor.Rotate(0f, 0f, rpmSpeed, Space.Self);
            if (compressorStage02) compressorStage02.Rotate(0f, 0f, -rpmSpeed * 1.35f, Space.Self);

            float constrictDeg = Mathf.Lerp(-4f, 16f, throttle);
            for (int i = 0; i < _petals.Length; i++)
            {
                if (_petals[i])
                    _petals[i].localRotation = Quaternion.Euler(constrictDeg, 0f, 0f);
            }
        }

        private static Transform FindChild(Transform p, string n)
        {
            foreach (Transform c in p)
            {
                if (c.name == n) return c;
                var r = FindChild(c, n);
                if (r) return r;
            }
            return null;
        }
    }
}
