using UnityEngine;

namespace ModelForge.TritonIX
{
    /// <summary>
    /// Modulates URP Lit emissive intensity on floodlights, telemetry screens, and beacon strobes
    /// via MaterialPropertyBlock to keep SRP Batcher compatibility intact.
    /// </summary>
    public class SubmersibleBeaconPulse : MonoBehaviour
    {
        [SerializeField] private Renderer[] emissiveRenderers;
        [SerializeField] private Color baseEmissionColor = new Color(0.28f, 0.88f, 1.0f, 1.0f);
        [SerializeField] private float minIntensity = 1.6f;
        [SerializeField] private float maxIntensity = 2.9f;
        [SerializeField] private float pulseHz = 1.2f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (emissiveRenderers == null || emissiveRenderers.Length == 0)
            {
                emissiveRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Update()
        {
            if (emissiveRenderers == null || emissiveRenderers.Length == 0) return;
            float s = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, s);
            Color hdr = baseEmissionColor * Mathf.LinearToGammaSpace(intensity);

            for (int i = 0; i < emissiveRenderers.Length; i++)
            {
                Renderer r = emissiveRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, hdr);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
