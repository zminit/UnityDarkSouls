using UnityEngine;

/// <summary>
/// Applies a predictable runtime frame-rate cap for movement and camera jitter tests.
/// </summary>
public class GameFrameRateSettings : MonoBehaviour
{
    [SerializeField]
    bool applyOnAwake = true;

    [SerializeField]
    bool disableVSync = true;

    [SerializeField]
    int targetFrameRate = 60;

    public int TargetFrameRate => targetFrameRate;
    public bool DisableVSync => disableVSync;

    private void Awake()
    {
        if (applyOnAwake)
            Apply();
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Max(-1, targetFrameRate);
    }

    [ContextMenu("Apply Frame Rate Settings")]
    public void Apply()
    {
        if (disableVSync)
            QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = targetFrameRate;
    }
}
