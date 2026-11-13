using UnityEngine;

namespace ModelForge.AstralChronos
{
    /// <summary>
    /// Pulses URP Lit emission intensity via MaterialPropertyBlock without
    /// instantiating per-renderer materials, keeping SRP Batcher / draw calls optimal.
    /// </summary>
    public class ArcaneEmissionPulse : MonoBehaviour
    {
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color baseEmissionColor = new Color(0.18f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private float minIntensity = 1.4f;
        [SerializeField] private float maxIntensity = 3.2f;
        [SerializeField] private float pulseFrequency = 1.8f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Update()
        {
            if (targetRenderers == null || targetRenderers.Length == 0) return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);
            Color hdrColor = baseEmissionColor * Mathf.LinearToGammaSpace(intensity);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer r = targetRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColorId, hdrColor);
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
