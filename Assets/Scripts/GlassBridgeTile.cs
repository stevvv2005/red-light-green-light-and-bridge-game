using UnityEngine;

public class GlassBridgeTile : MonoBehaviour
{
    private const float HoverScaleMultiplier = 1.06f;

    private Renderer cachedRenderer;
    private Material runtimeMaterial;
    private Vector3 baseScale;
    private Color idleTint;
    private Color highlightTint;
    private Color safeTint;
    private Color failTint;

    public int StepIndex { get; private set; }
    public bool IsSafe { get; private set; }
    public bool IsBroken { get; private set; }

    public void Configure(int stepIndex, bool isSafe, Color idleColor, Color safeColor, Color failColor)
    {
        StepIndex = stepIndex;
        IsSafe = isSafe;
        idleTint = idleColor;
        highlightTint = Color.Lerp(idleColor, Color.white, 0.35f);
        safeTint = safeColor;
        failTint = failColor;

        cachedRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale;

        Shader standardShader = Shader.Find("Standard");
        runtimeMaterial = standardShader != null ? new Material(standardShader) : new Material(cachedRenderer.sharedMaterial);
        runtimeMaterial.color = idleTint;
        runtimeMaterial.SetFloat("_Metallic", 0.08f);
        runtimeMaterial.SetFloat("_Glossiness", 0.88f);
        cachedRenderer.material = runtimeMaterial;
        SetEmission(idleTint * 0.1f);
    }

    public void SetHovered(bool hovered)
    {
        if (IsBroken || runtimeMaterial == null)
            return;

        transform.localScale = hovered ? baseScale * HoverScaleMultiplier : baseScale;
        Color targetColor = hovered ? highlightTint : idleTint;
        runtimeMaterial.color = targetColor;
        SetEmission(targetColor * (hovered ? 0.2f : 0.1f));
    }

    public void RevealSafe()
    {
        if (runtimeMaterial == null)
            return;

        transform.localScale = baseScale;
        runtimeMaterial.color = safeTint;
        SetEmission(safeTint * 0.22f);
    }

    public void BreakTile(float sideForce)
    {
        if (IsBroken)
            return;

        IsBroken = true;
        transform.localScale = baseScale;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.color = failTint;
            SetEmission(failTint * 0.26f);
        }

        Collider tileCollider = GetComponent<Collider>();

        if (tileCollider != null)
            tileCollider.enabled = false;

        Rigidbody tileBody = GetComponent<Rigidbody>();

        if (tileBody == null)
            tileBody = gameObject.AddComponent<Rigidbody>();

        tileBody.mass = 4f;
        tileBody.linearDamping = 0.18f;
        tileBody.angularDamping = 0.05f;
        tileBody.interpolation = RigidbodyInterpolation.Interpolate;
        tileBody.AddForce(new Vector3(sideForce, -3.2f, 0f), ForceMode.Impulse);
        tileBody.AddTorque(new Vector3(sideForce * 2f, sideForce * 3f, -sideForce * 1.2f), ForceMode.Impulse);
    }

    private void SetEmission(Color emissionColor)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.EnableKeyword("_EMISSION");
        runtimeMaterial.SetColor("_EmissionColor", emissionColor);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Object.Destroy(runtimeMaterial);
    }
}
