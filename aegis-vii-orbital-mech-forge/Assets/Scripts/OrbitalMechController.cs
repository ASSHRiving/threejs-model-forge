using UnityEngine;

namespace ModelForge.AegisVII
{
    /// <summary>
    /// Runtime Unity URP controller for the AEGIS-VII Orbital Drop Mech GLB/FBX prefab.
    /// Drives clam-shell chest hatch deployment, reverse-cant drop-crouch kinematics,
    /// shoulder railgun pitch tracking and recoil kickback, real-time hydraulic piston LookAt
    /// alignment on all Part_Piston_* assemblies, and MaterialPropertyBlock HDR emission pulsing.
    /// </summary>
    public class OrbitalMechController : MonoBehaviour
    {
        [Header("Mech Articulation Controls")]
        [Range(0f, 1f)] public float chestHatchOpen;
        [Range(0f, 1f)] public float dropCrouch;
        [Range(-15f, 35f)] public float railgunPitchDeg;
        [Range(-35f, 35f)] public float torsoYawDeg;
        [Range(0f, 1f)] public float coreOverdrive = 0.65f;
        public bool enableIdleBreathing = true;

        [Header("Recoil & Kinematics Tuning")]
        [SerializeField] private float hatchSmoothSpeed = 6f;
        [SerializeField] private float crouchSmoothSpeed = 6f;
        [SerializeField] private float recoilDistance = 0.44f;
        [SerializeField] private float recoilSpringStiffness = 36f;
        [SerializeField] private float recoilDamping = 9.5f;

        [Header("Articulated Nodes (Auto-Resolved by Part_* Name if Null)")]
        [SerializeField] private Transform pelvis;
        [SerializeField] private Transform torsoYaw;
        [SerializeField] private Transform chestHatchUpper;
        [SerializeField] private Transform chestHatchLower;

        [SerializeField] private Transform leftRailgunTurret;
        [SerializeField] private Transform rightRailgunTurret;
        [SerializeField] private Transform leftRailgunBarrel;
        [SerializeField] private Transform rightRailgunBarrel;

        [SerializeField] private Transform leftHip;
        [SerializeField] private Transform leftKnee;
        [SerializeField] private Transform leftAnkle;
        [SerializeField] private Transform rightHip;
        [SerializeField] private Transform rightKnee;
        [SerializeField] private Transform rightAnkle;

        [SerializeField] private Transform thrusterNozzleL;
        [SerializeField] private Transform thrusterNozzleR;
        [SerializeField] private Transform airbrakeL;
        [SerializeField] private Transform airbrakeR;

        [Header("Hydraulic Piston Pairs (Auto-Resolved if Null)")]
        [SerializeField] private Transform pistonHipCylL, pistonHipRodL;
        [SerializeField] private Transform pistonHipCylR, pistonHipRodR;
        [SerializeField] private Transform pistonKneeCylL, pistonKneeRodL;
        [SerializeField] private Transform pistonKneeCylR, pistonKneeRodR;
        [SerializeField] private Transform pistonAnkleCylL, pistonAnkleRodL;
        [SerializeField] private Transform pistonAnkleCylR, pistonAnkleRodR;
        [SerializeField] private Transform pistonHatchCylL, pistonHatchRodL;
        [SerializeField] private Transform pistonHatchCylR, pistonHatchRodR;

        [Header("HDR Emission Renderers")]
        [SerializeField] private Renderer[] coilAndCoreRenderers;
        [SerializeField] private Renderer[] thrusterRenderers;
        [SerializeField] private Color coilEmissionColor = new Color(0.13f, 0.83f, 0.93f, 1f);
        [SerializeField] private Color thrusterEmissionColor = new Color(0.22f, 0.74f, 0.97f, 1f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private const float L1 = 1.66f;
        private const float L2 = 1.62f;
        private const float AnkleY = 0.36f;
        private const float AnkleZ = -0.15f;
        private const float A1RestDeg = 26f;
        private const float A1CrouchDeg = 56f;
        private const float A2RestDeg = -32f;
        private const float A2CrouchDeg = -60f;

        private float _currentHatch;
        private float _currentCrouch;
        private float _recoilL, _recoilVelL;
        private float _recoilR, _recoilVelR;
        private float _flashPulse;
        private float _restHipY, _restHipZ;

        private Vector3 _pelvisRestPos;
        private Quaternion _torsoRestRot, _hatchUpperRestRot, _hatchLowerRestRot;
        private Quaternion _turretLRestRot, _turretRRestRot;
        private Vector3 _barrelLRestPos, _barrelRRestPos;
        private Quaternion _hipLRestRot, _kneeLRestRot, _ankleLRestRot;
        private Quaternion _hipRRestRot, _kneeRRestRot, _ankleRRestRot;
        private Quaternion _nozzleLRestRot, _nozzleRRestRot;
        private Quaternion _airbrakeLRestRot, _airbrakeRRestRot;

        private Transform _talonF1L, _talonF2L, _talonRL;
        private Transform _talonF1R, _talonF2R, _talonRR;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            ResolveHierarchy();
            CacheRestTransforms();
            ComputeHipPlanarPos(0f, out _restHipY, out _restHipZ);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time;

            _currentHatch = Mathf.MoveTowards(_currentHatch, Mathf.Clamp01(chestHatchOpen), hatchSmoothSpeed * dt);
            _currentCrouch = Mathf.MoveTowards(_currentCrouch, Mathf.Clamp01(dropCrouch), crouchSmoothSpeed * dt);

            float idleBreath = enableIdleBreathing ? Mathf.Sin(t * 1.9f) : 0f;
            float idleScan = enableIdleBreathing ? Mathf.Sin(t * 0.72f) : 0f;
            float c01 = Mathf.Clamp01(_currentCrouch + idleBreath * 0.014f);

            // 1. Reverse-Cant Drop-Crouch Leg Kinematics (feet planted cleanly at y = 0)
            ComputeHipPlanarPos(c01, out float hipY, out float hipZ);
            if (pelvis)
            {
                pelvis.localPosition = _pelvisRestPos + new Vector3(0f, hipY - _restHipY, hipZ - _restHipZ);
            }

            float a1 = Mathf.Lerp(A1RestDeg, A1CrouchDeg, c01);
            float a2 = Mathf.Lerp(A2RestDeg, A2CrouchDeg, c01);
            float dA1 = a1 - A1RestDeg;
            float dA2 = a2 - A2RestDeg;

            if (leftHip) leftHip.localRotation = _hipLRestRot * Quaternion.Euler(-dA1, 0f, 0f);
            if (leftKnee) leftKnee.localRotation = _kneeLRestRot * Quaternion.Euler(dA1 - dA2, 0f, 0f);
            if (leftAnkle) leftAnkle.localRotation = _ankleLRestRot * Quaternion.Euler(dA2, 0f, 0f);

            if (rightHip) rightHip.localRotation = _hipRRestRot * Quaternion.Euler(-dA1, 0f, 0f);
            if (rightKnee) rightKnee.localRotation = _kneeRRestRot * Quaternion.Euler(dA1 - dA2, 0f, 0f);
            if (rightAnkle) rightAnkle.localRotation = _ankleRRestRot * Quaternion.Euler(dA2, 0f, 0f);

            float talonDeg = c01 * 16f;
            if (_talonF1L) _talonF1L.localRotation = Quaternion.Euler(talonDeg, -12.6f, 0f);
            if (_talonF2L) _talonF2L.localRotation = Quaternion.Euler(talonDeg, 12.6f, 0f);
            if (_talonRL) _talonRL.localRotation = Quaternion.Euler(-talonDeg, 0f, 0f);
            if (_talonF1R) _talonF1R.localRotation = Quaternion.Euler(talonDeg, -12.6f, 0f);
            if (_talonF2R) _talonF2R.localRotation = Quaternion.Euler(talonDeg, 12.6f, 0f);
            if (_talonRR) _talonRR.localRotation = Quaternion.Euler(-talonDeg, 0f, 0f);

            // 2. Torso Yaw Scanning & Chest Cockpit Clam-Shell Hatches
            if (torsoYaw)
            {
                float scanY = torsoYawDeg + idleScan * 6.5f;
                float leanX = c01 * 5f + idleBreath * 0.8f;
                torsoYaw.localRotation = _torsoRestRot * Quaternion.Euler(leanX, scanY, 0f);
            }

            if (chestHatchUpper) chestHatchUpper.localRotation = _hatchUpperRestRot * Quaternion.Euler(-_currentHatch * 74f, 0f, 0f);
            if (chestHatchLower) chestHatchLower.localRotation = _hatchLowerRestRot * Quaternion.Euler(_currentHatch * 62f, 0f, 0f);

            // 3. Shoulder Railgun Elevation Tracking & Damped Spring Recoil
            float clampedPitch = Mathf.Clamp(railgunPitchDeg, -15f, 35f);
            if (leftRailgunTurret) leftRailgunTurret.localRotation = _turretLRestRot * Quaternion.Euler(-clampedPitch, 0f, 0f);
            if (rightRailgunTurret) rightRailgunTurret.localRotation = _turretRRestRot * Quaternion.Euler(-clampedPitch, 0f, 0f);

            StepRecoilSpring(ref _recoilL, ref _recoilVelL, dt);
            StepRecoilSpring(ref _recoilR, ref _recoilVelR, dt);

            if (leftRailgunBarrel) leftRailgunBarrel.localPosition = _barrelLRestPos + new Vector3(0f, 0f, -_recoilL);
            if (rightRailgunBarrel) rightRailgunBarrel.localPosition = _barrelRRestPos + new Vector3(0f, 0f, -_recoilR);

            // 4. Dorsal Thruster Gimbal & Deployable Heat-Shield Airbrakes
            float nozzleExtraDeg = (coreOverdrive * 0.16f + c01 * 0.12f) * Mathf.Rad2Deg;
            if (thrusterNozzleL) thrusterNozzleL.localRotation = _nozzleLRestRot * Quaternion.Euler(nozzleExtraDeg, 0f, 0f);
            if (thrusterNozzleR) thrusterNozzleR.localRotation = _nozzleRRestRot * Quaternion.Euler(nozzleExtraDeg, 0f, 0f);

            float brakeDeg = c01 * 34f + coreOverdrive * 18f;
            if (airbrakeL) airbrakeL.localRotation = _airbrakeLRestRot * Quaternion.Euler(-brakeDeg, 0f, -brakeDeg * 0.55f);
            if (airbrakeR) airbrakeR.localRotation = _airbrakeRRestRot * Quaternion.Euler(-brakeDeg, 0f, brakeDeg * 0.55f);

            // 5. MaterialPropertyBlock HDR Emission Pulsing (Zero Material Instancing)
            _flashPulse = Mathf.Max(0f, _flashPulse - dt * 4.2f);
            float pulseWave = enableIdleBreathing ? (0.88f + 0.12f * Mathf.Sin(t * 4.2f)) : 1f;
            float coilIntensity = Mathf.Lerp(0.35f, 3.8f, coreOverdrive) * pulseWave + _flashPulse * 3.2f;
            float thrusterIntensity = Mathf.Lerp(0.45f, 4.4f, coreOverdrive) * pulseWave;

            ApplyEmission(coilAndCoreRenderers, coilEmissionColor, coilIntensity);
            ApplyEmission(thrusterRenderers, thrusterEmissionColor, thrusterIntensity);
        }

        private void LateUpdate()
        {
            // Real-time hydraulic piston LookAt alignment after all leg/hatch rotations settle
            AlignPistonPair(pistonHipCylL, pistonHipRodL);
            AlignPistonPair(pistonHipCylR, pistonHipRodR);
            AlignPistonPair(pistonKneeCylL, pistonKneeRodL);
            AlignPistonPair(pistonKneeCylR, pistonKneeRodR);
            AlignPistonPair(pistonAnkleCylL, pistonAnkleRodL);
            AlignPistonPair(pistonAnkleCylR, pistonAnkleRodR);
            AlignPistonPair(pistonHatchCylL, pistonHatchRodL);
            AlignPistonPair(pistonHatchCylR, pistonHatchRodR);
        }

        /// <summary>
        /// Triggers electromagnetic railgun recoil kickback and HDR coil flash pulse.
        /// </summary>
        public void TriggerRailgunFire(bool fireLeft = true, bool fireRight = true)
        {
            if (fireLeft)
            {
                _recoilL = recoilDistance;
                _recoilVelL = 0f;
            }
            if (fireRight)
            {
                _recoilR = recoilDistance;
                _recoilVelR = 0f;
            }
            _flashPulse = 1f;
        }

        private void StepRecoilSpring(ref float offset, ref float velocity, float dt)
        {
            if (offset <= 0.0005f && Mathf.Abs(velocity) <= 0.0005f)
            {
                offset = 0f;
                velocity = 0f;
                return;
            }
            float accel = -recoilSpringStiffness * offset - recoilDamping * velocity;
            velocity += accel * dt;
            offset = Mathf.Max(0f, offset + velocity * dt);
        }

        private void AlignPistonPair(Transform cyl, Transform rod)
        {
            if (!cyl || !rod) return;
            Vector3 upRef = torsoYaw ? torsoYaw.up : transform.up;
            cyl.LookAt(rod.position, upRef);
            rod.LookAt(cyl.position, upRef);
        }

        private void ApplyEmission(Renderer[] renderers, Color baseColor, float intensity)
        {
            if (renderers == null || renderers.Length == 0) return;
            Color hdr = baseColor * Mathf.LinearToGammaSpace(intensity);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (!r) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, hdr);
                r.SetPropertyBlock(_mpb);
            }
        }

        private static void ComputeHipPlanarPos(float crouch01, out float hipY, out float hipZ)
        {
            float r1 = Mathf.Lerp(A1RestDeg, A1CrouchDeg, crouch01) * Mathf.Deg2Rad;
            float r2 = Mathf.Lerp(A2RestDeg, A2CrouchDeg, crouch01) * Mathf.Deg2Rad;

            float kneeY = AnkleY + L2 * Mathf.Cos(r2);
            float kneeZ = AnkleZ - L2 * Mathf.Sin(r2);
            hipY = kneeY + L1 * Mathf.Cos(r1);
            hipZ = kneeZ - L1 * Mathf.Sin(r1);
        }

        private void ResolveHierarchy()
        {
            if (!pelvis) pelvis = FindDeepChild(transform, "Part_Pelvis");
            if (!torsoYaw) torsoYaw = FindDeepChild(transform, "Part_Torso_Yaw");
            if (!chestHatchUpper) chestHatchUpper = FindDeepChild(transform, "Part_ChestHatch_Upper");
            if (!chestHatchLower) chestHatchLower = FindDeepChild(transform, "Part_ChestHatch_Lower");

            if (!leftRailgunTurret) leftRailgunTurret = FindDeepChild(transform, "Part_LeftRailgun_Turret");
            if (!rightRailgunTurret) rightRailgunTurret = FindDeepChild(transform, "Part_RightRailgun_Turret");
            if (!leftRailgunBarrel) leftRailgunBarrel = FindDeepChild(transform, "Part_LeftRailgun_Barrel");
            if (!rightRailgunBarrel) rightRailgunBarrel = FindDeepChild(transform, "Part_RightRailgun_Barrel");

            if (!leftHip) leftHip = FindDeepChild(transform, "Part_LeftHip");
            if (!leftKnee) leftKnee = FindDeepChild(transform, "Part_LeftKnee");
            if (!leftAnkle) leftAnkle = FindDeepChild(transform, "Part_LeftAnkle");
            if (!rightHip) rightHip = FindDeepChild(transform, "Part_RightHip");
            if (!rightKnee) rightKnee = FindDeepChild(transform, "Part_RightKnee");
            if (!rightAnkle) rightAnkle = FindDeepChild(transform, "Part_RightAnkle");

            if (!thrusterNozzleL) thrusterNozzleL = FindDeepChild(transform, "Part_ThrusterNozzle_L");
            if (!thrusterNozzleR) thrusterNozzleR = FindDeepChild(transform, "Part_ThrusterNozzle_R");
            if (!airbrakeL) airbrakeL = FindDeepChild(transform, "Part_Airbrake_L");
            if (!airbrakeR) airbrakeR = FindDeepChild(transform, "Part_Airbrake_R");

            if (!pistonHipCylL) pistonHipCylL = FindDeepChild(transform, "Part_Piston_Hip_Cyl_L");
            if (!pistonHipRodL) pistonHipRodL = FindDeepChild(transform, "Part_Piston_Hip_Rod_L");
            if (!pistonHipCylR) pistonHipCylR = FindDeepChild(transform, "Part_Piston_Hip_Cyl_R");
            if (!pistonHipRodR) pistonHipRodR = FindDeepChild(transform, "Part_Piston_Hip_Rod_R");

            if (!pistonKneeCylL) pistonKneeCylL = FindDeepChild(transform, "Part_Piston_Knee_Cyl_L");
            if (!pistonKneeRodL) pistonKneeRodL = FindDeepChild(transform, "Part_Piston_Knee_Rod_L");
            if (!pistonKneeCylR) pistonKneeCylR = FindDeepChild(transform, "Part_Piston_Knee_Cyl_R");
            if (!pistonKneeRodR) pistonKneeRodR = FindDeepChild(transform, "Part_Piston_Knee_Rod_R");

            if (!pistonAnkleCylL) pistonAnkleCylL = FindDeepChild(transform, "Part_Piston_Ankle_Cyl_L");
            if (!pistonAnkleRodL) pistonAnkleRodL = FindDeepChild(transform, "Part_Piston_Ankle_Rod_L");
            if (!pistonAnkleCylR) pistonAnkleCylR = FindDeepChild(transform, "Part_Piston_Ankle_Cyl_R");
            if (!pistonAnkleRodR) pistonAnkleRodR = FindDeepChild(transform, "Part_Piston_Ankle_Rod_R");

            if (!pistonHatchCylL) pistonHatchCylL = FindDeepChild(transform, "Part_Piston_Hatch_Cyl_L");
            if (!pistonHatchRodL) pistonHatchRodL = FindDeepChild(transform, "Part_Piston_Hatch_Rod_L");
            if (!pistonHatchCylR) pistonHatchCylR = FindDeepChild(transform, "Part_Piston_Hatch_Cyl_R");
            if (!pistonHatchRodR) pistonHatchRodR = FindDeepChild(transform, "Part_Piston_Hatch_Rod_R");

            _talonF1L = FindDeepChild(transform, "Part_Talon_F1_L");
            _talonF2L = FindDeepChild(transform, "Part_Talon_F2_L");
            _talonRL = FindDeepChild(transform, "Part_Talon_R_L");
            _talonF1R = FindDeepChild(transform, "Part_Talon_F1_R");
            _talonF2R = FindDeepChild(transform, "Part_Talon_F2_R");
            _talonRR = FindDeepChild(transform, "Part_Talon_R_R");
        }

        private void CacheRestTransforms()
        {
            if (pelvis) _pelvisRestPos = pelvis.localPosition;
            if (torsoYaw) _torsoRestRot = torsoYaw.localRotation;
            if (chestHatchUpper) _hatchUpperRestRot = chestHatchUpper.localRotation;
            if (chestHatchLower) _hatchLowerRestRot = chestHatchLower.localRotation;
            if (leftRailgunTurret) _turretLRestRot = leftRailgunTurret.localRotation;
            if (rightRailgunTurret) _turretRRestRot = rightRailgunTurret.localRotation;
            if (leftRailgunBarrel) _barrelLRestPos = leftRailgunBarrel.localPosition;
            if (rightRailgunBarrel) _barrelRRestPos = rightRailgunBarrel.localPosition;
            if (leftHip) _hipLRestRot = leftHip.localRotation;
            if (leftKnee) _kneeLRestRot = leftKnee.localRotation;
            if (leftAnkle) _ankleLRestRot = leftAnkle.localRotation;
            if (rightHip) _hipRRestRot = rightHip.localRotation;
            if (rightKnee) _kneeRRestRot = rightKnee.localRotation;
            if (rightAnkle) _ankleRRestRot = rightAnkle.localRotation;
            if (thrusterNozzleL) _nozzleLRestRot = thrusterNozzleL.localRotation;
            if (thrusterNozzleR) _nozzleRRestRot = thrusterNozzleR.localRotation;
            if (airbrakeL) _airbrakeLRestRot = airbrakeL.localRotation;
            if (airbrakeR) _airbrakeRRestRot = airbrakeR.localRotation;
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
