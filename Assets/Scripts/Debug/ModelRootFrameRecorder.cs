using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Records frame-by-frame root/model transform samples for movement jitter diagnostics.
/// </summary>
public class ModelRootFrameRecorder : MonoBehaviour
{
    [SerializeField]
    Transform modelRoot;
    [SerializeField]
    Transform playerModel;
    [SerializeField]
    Rigidbody targetRigidbody;
    [SerializeField]
    float durationSeconds = 10f;

    StreamWriter writer;
    string outputPath;
    float startedAt;
    int sampleIndex;
    int fixedUpdatesSinceLastRender;
    bool isRecording;
    bool hasLastRootPosition;
    bool hasLastModelPosition;
    Vector3 lastRootPosition;
    Vector3 lastModelPosition;

    public event Action<ModelRootFrameRecorder> Completed;

    public bool IsRecording => isRecording;
    public string OutputPath => outputPath;

    public void Configure(
        Transform root,
        Transform model,
        Rigidbody body,
        float duration,
        string path)
    {
        modelRoot = root;
        playerModel = model;
        targetRigidbody = body;
        durationSeconds = Mathf.Max(0.1f, duration);
        outputPath = path;
    }

    public void Begin()
    {
        if (modelRoot == null)
            modelRoot = transform;

        if (targetRigidbody == null && modelRoot != null)
            targetRigidbody = modelRoot.GetComponent<Rigidbody>();

        if (string.IsNullOrEmpty(outputPath))
            outputPath = CreateDefaultOutputPath();

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        writer = new StreamWriter(outputPath, false);
        WriteHeader();

        startedAt = Time.unscaledTime;
        sampleIndex = 0;
        fixedUpdatesSinceLastRender = 0;
        hasLastRootPosition = false;
        hasLastModelPosition = false;
        isRecording = true;
    }

    public void Stop()
    {
        if (!isRecording)
            return;

        isRecording = false;

        writer?.Flush();
        writer?.Dispose();
        writer = null;

        Completed?.Invoke(this);
    }

    private void OnDisable()
    {
        if (isRecording)
            Stop();
    }

    private void FixedUpdate()
    {
        if (isRecording)
            fixedUpdatesSinceLastRender++;
    }

    private void LateUpdate()
    {
        if (!isRecording || writer == null || modelRoot == null)
            return;

        WriteSample();

        if (Time.unscaledTime - startedAt >= durationSeconds)
            Stop();
    }

    private void WriteHeader()
    {
        writer.WriteLine(
            "sampleIndex,frameCount,fixedUpdatesSinceLastRender,time,unscaledTime,deltaTime,unscaledDeltaTime,"
            + "rootPosX,rootPosY,rootPosZ,rootYaw,rootDelta,rootHorizontalDelta,rootSpeed,"
            + "rbPosX,rbPosY,rbPosZ,rbVelX,rbVelY,rbVelZ,"
            + "modelPosX,modelPosY,modelPosZ,modelDelta,modelMinusRootDelta");
    }

    private void WriteSample()
    {
        Vector3 rootPosition = modelRoot.position;
        float rootYaw = modelRoot.eulerAngles.y;
        float rootDelta = hasLastRootPosition ? Vector3.Distance(rootPosition, lastRootPosition) : 0f;
        float rootHorizontalDelta = hasLastRootPosition
            ? Vector2.Distance(
                new Vector2(rootPosition.x, rootPosition.z),
                new Vector2(lastRootPosition.x, lastRootPosition.z))
            : 0f;
        float rootSpeed = Time.unscaledDeltaTime > 0f ? rootHorizontalDelta / Time.unscaledDeltaTime : 0f;

        Vector3 rbPosition = targetRigidbody != null ? targetRigidbody.position : Vector3.zero;
        Vector3 rbVelocity = targetRigidbody != null ? targetRigidbody.velocity : Vector3.zero;

        Vector3 modelPosition = playerModel != null ? playerModel.position : Vector3.zero;
        float modelDelta = playerModel != null && hasLastModelPosition
            ? Vector3.Distance(modelPosition, lastModelPosition)
            : 0f;
        float modelMinusRootDelta = playerModel != null
            ? Vector3.Distance(modelPosition, rootPosition)
            : 0f;

        writer.WriteLine(string.Join(",",
            sampleIndex.ToString(CultureInfo.InvariantCulture),
            Time.frameCount.ToString(CultureInfo.InvariantCulture),
            fixedUpdatesSinceLastRender.ToString(CultureInfo.InvariantCulture),
            Format(Time.time),
            Format(Time.unscaledTime),
            Format(Time.deltaTime),
            Format(Time.unscaledDeltaTime),
            Format(rootPosition.x),
            Format(rootPosition.y),
            Format(rootPosition.z),
            Format(rootYaw),
            Format(rootDelta),
            Format(rootHorizontalDelta),
            Format(rootSpeed),
            Format(rbPosition.x),
            Format(rbPosition.y),
            Format(rbPosition.z),
            Format(rbVelocity.x),
            Format(rbVelocity.y),
            Format(rbVelocity.z),
            Format(modelPosition.x),
            Format(modelPosition.y),
            Format(modelPosition.z),
            Format(modelDelta),
            Format(modelMinusRootDelta)));

        sampleIndex++;
        fixedUpdatesSinceLastRender = 0;
        lastRootPosition = rootPosition;
        lastModelPosition = modelPosition;
        hasLastRootPosition = true;
        hasLastModelPosition = playerModel != null;
    }

    private static string CreateDefaultOutputPath()
    {
        string fileName = $"model_root_frames_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(Application.dataPath, "..", "Library", "Diagnostics", fileName);
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
