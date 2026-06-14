using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DebugFpsDisplay : MonoBehaviour
{
    [SerializeField] private Text fpsText;
    [SerializeField] private float refreshInterval = 0.25f;

    private int updateFrameCount;
    private int fixedFrameCount;
    private float elapsedTime;

    private void Update()
    {
        updateFrameCount++;
        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime < refreshInterval)
            return;

        float updateFps = updateFrameCount / elapsedTime;
        float fixedFps = fixedFrameCount / elapsedTime;
        fpsText.text = $"Update FPS: {updateFps:0}\nFixed FPS: {fixedFps:0}";

        updateFrameCount = 0;
        fixedFrameCount = 0;
        elapsedTime = 0f;
    }

    private void FixedUpdate()
    {
        fixedFrameCount++;
    }
}
