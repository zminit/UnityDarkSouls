using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CFSM;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ModelRootJitterDiagnosticRunner
{
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    const float DefaultDurationSeconds = 10f;
    const string RunningKey = "ModelRootJitterDiagnostic.Running";
    const string CommandLineKey = "ModelRootJitterDiagnostic.CommandLine";
    const string RunIdKey = "ModelRootJitterDiagnostic.RunId";
    const string CsvPathKey = "ModelRootJitterDiagnostic.CsvPath";
    const string ErrorKey = "ModelRootJitterDiagnostic.Error";
    const int TargetFrameRate = 60;

    static ModelRootJitterDiagnosticRunner()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/Diagnostics/Run ModelRoot Jitter Diagnostic")]
    public static void RunFromMenu()
    {
        StartRun(false);
    }

    public static void RunFromCommandLine()
    {
        StartRun(true);
    }

    static void StartRun(bool commandLine)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("ModelRoot jitter diagnostic is already entering or running PlayMode.");
            return;
        }

        if (!commandLine && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string runId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string csvPath = Path.Combine(GetDiagnosticsDirectory(), $"modelroot_jitter_{runId}.csv");

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(CommandLineKey, commandLine);
        SessionState.SetString(RunIdKey, runId);
        SessionState.SetString(CsvPathKey, csvPath);
        SessionState.SetString(ErrorKey, string.Empty);

        if (SceneManager.GetActiveScene().path != MainScenePath)
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        Debug.Log($"Starting ModelRoot jitter diagnostic. Output: {csvPath}");
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
            SetupRuntimeCapture();
        else if (state == PlayModeStateChange.EnteredEditMode)
            FinishRun();
    }

    static void SetupRuntimeCapture()
    {
        try
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            GameObject rootObject = GameObject.Find("ModelRoot");
            if (rootObject == null)
            {
                PlayerManager playerManager = UnityEngine.Object.FindObjectOfType<PlayerManager>();
                if (playerManager != null)
                    rootObject = playerManager.gameObject;
            }

            if (rootObject == null)
                throw new InvalidOperationException("Could not find ModelRoot or PlayerManager in the active scene.");

            CharacterFSM fsm = rootObject.GetComponent<CharacterFSM>();
            Rigidbody body = rootObject.GetComponent<Rigidbody>();
            Transform playerModel = FindChildByName(rootObject.transform, "PlayerModel");
            if (playerModel == null)
            {
                Animator animator = rootObject.GetComponentInChildren<Animator>();
                if (animator != null)
                    playerModel = animator.transform;
            }

            PlayerInputAdapter inputAdapter = rootObject.GetComponent<PlayerInputAdapter>();
            if (inputAdapter != null)
                inputAdapter.enabled = false;

            ModelRootAutoMoveDriver driver = rootObject.AddComponent<ModelRootAutoMoveDriver>();
            driver.Configure(fsm, MoveMode.Run, Vector2.up);

            ModelRootFrameRecorder recorder = rootObject.AddComponent<ModelRootFrameRecorder>();
            recorder.Configure(
                rootObject.transform,
                playerModel,
                body,
                DefaultDurationSeconds,
                SessionState.GetString(CsvPathKey, string.Empty));
            recorder.Completed += OnRecorderCompleted;
            recorder.Begin();
        }
        catch (Exception ex)
        {
            SessionState.SetString(ErrorKey, ex.ToString());
            Debug.LogError(ex);
            EditorApplication.delayCall += StopPlayMode;
        }
    }

    static void OnRecorderCompleted(ModelRootFrameRecorder recorder)
    {
        if (recorder != null)
            SessionState.SetString(CsvPathKey, recorder.OutputPath);

        EditorApplication.delayCall += StopPlayMode;
    }

    static void StopPlayMode()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
    }

    static void FinishRun()
    {
        string error = SessionState.GetString(ErrorKey, string.Empty);
        string csvPath = SessionState.GetString(CsvPathKey, string.Empty);
        bool commandLine = SessionState.GetBool(CommandLineKey, false);

        SessionState.SetBool(RunningKey, false);
        SessionState.SetBool(CommandLineKey, false);
        SessionState.SetString(ErrorKey, string.Empty);

        int exitCode = 0;
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"ModelRoot jitter diagnostic failed:\n{error}");
            exitCode = 1;
        }
        else
        {
            try
            {
                DiagnosticSummary summary = AnalyzeCsv(csvPath);
                WriteSummaryFiles(summary);
                Debug.Log(summary.ToConsoleString());
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                exitCode = 1;
            }
        }

        if (commandLine)
            EditorApplication.Exit(exitCode);
    }

    static DiagnosticSummary AnalyzeCsv(string csvPath)
    {
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            throw new FileNotFoundException("ModelRoot jitter CSV was not found.", csvPath);

        List<FrameSample> samples = new List<FrameSample>();
        string[] lines = File.ReadAllLines(csvPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            samples.Add(FrameSample.Parse(lines[i]));
        }

        if (samples.Count < 2)
            throw new InvalidOperationException("Not enough samples to analyze ModelRoot jitter.");

        List<FrameSample> movementSamples = samples.Skip(1).ToList();
        List<float> renderDeltaMs = movementSamples.Select(s => s.unscaledDeltaTime * 1000f).ToList();
        List<float> rootHorizontalDelta = movementSamples.Select(s => s.rootHorizontalDelta).ToList();
        List<float> rootSpeed = movementSamples.Select(s => s.rootSpeed).ToList();
        List<float> modelDelta = movementSamples.Select(s => s.modelDelta).ToList();
        List<float> modelRootDeltaDiff = movementSamples
            .Select(s => Mathf.Abs(s.modelDelta - s.rootDelta))
            .ToList();

        float medianRootDelta = Median(rootHorizontalDelta);
        int zeroStepFrames = medianRootDelta > 0.000001f
            ? rootHorizontalDelta.Count(v => v < medianRootDelta * 0.2f)
            : 0;
        int doubleStepFrames = medianRootDelta > 0.000001f
            ? rootHorizontalDelta.Count(v => v > medianRootDelta * 1.8f)
            : 0;
        int jitterFrames = medianRootDelta > 0.000001f
            ? rootHorizontalDelta.Count(v => Mathf.Abs(v - medianRootDelta) / medianRootDelta > 0.35f)
            : 0;

        return new DiagnosticSummary
        {
            csvPath = csvPath,
            summaryPath = Path.ChangeExtension(csvPath, ".summary.txt"),
            jsonPath = Path.ChangeExtension(csvPath, ".summary.json"),
            sampleCount = samples.Count,
            renderDeltaMsAvg = Average(renderDeltaMs),
            renderDeltaMsP95 = Percentile(renderDeltaMs, 0.95f),
            renderDeltaMsMax = Max(renderDeltaMs),
            rootDeltaAvg = Average(rootHorizontalDelta),
            rootDeltaP95 = Percentile(rootHorizontalDelta, 0.95f),
            rootDeltaMax = Max(rootHorizontalDelta),
            rootSpeedAvg = Average(rootSpeed),
            rootSpeedP95 = Percentile(rootSpeed, 0.95f),
            rootSpeedMax = Max(rootSpeed),
            medianRootDelta = medianRootDelta,
            zeroStepFrames = zeroStepFrames,
            doubleStepFrames = doubleStepFrames,
            jitterFrames = jitterFrames,
            fixedUpdateZeroFrames = movementSamples.Count(s => s.fixedUpdatesSinceLastRender == 0),
            fixedUpdateOneFrames = movementSamples.Count(s => s.fixedUpdatesSinceLastRender == 1),
            fixedUpdateTwoPlusFrames = movementSamples.Count(s => s.fixedUpdatesSinceLastRender >= 2),
            modelDeltaAvg = Average(modelDelta),
            modelDeltaP95 = Percentile(modelDelta, 0.95f),
            modelDeltaMinusRootDeltaAvg = Average(modelRootDeltaDiff),
            modelDeltaMinusRootDeltaP95 = Percentile(modelRootDeltaDiff, 0.95f)
        };
    }

    static void WriteSummaryFiles(DiagnosticSummary summary)
    {
        File.WriteAllText(summary.summaryPath, summary.ToTextString());
        File.WriteAllText(summary.jsonPath, summary.ToJsonString());
    }

    static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    static string GetDiagnosticsDirectory()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string directory = Path.Combine(projectRoot, "Library", "Diagnostics");
        Directory.CreateDirectory(directory);
        return directory;
    }

    static float Average(List<float> values)
    {
        return values.Count == 0 ? 0f : values.Sum() / values.Count;
    }

    static float Max(List<float> values)
    {
        return values.Count == 0 ? 0f : values.Max();
    }

    static float Median(List<float> values)
    {
        return Percentile(values, 0.5f);
    }

    static float Percentile(List<float> values, float percentile)
    {
        if (values.Count == 0)
            return 0f;

        List<float> sorted = values.OrderBy(v => v).ToList();
        int index = Mathf.Clamp(
            Mathf.RoundToInt((sorted.Count - 1) * Mathf.Clamp01(percentile)),
            0,
            sorted.Count - 1);
        return sorted[index];
    }

    struct FrameSample
    {
        public int fixedUpdatesSinceLastRender;
        public float unscaledDeltaTime;
        public float rootDelta;
        public float rootHorizontalDelta;
        public float rootSpeed;
        public float modelDelta;

        public static FrameSample Parse(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 25)
                throw new FormatException($"Invalid ModelRoot frame sample: {line}");

            return new FrameSample
            {
                fixedUpdatesSinceLastRender = int.Parse(parts[2], CultureInfo.InvariantCulture),
                unscaledDeltaTime = ParseFloat(parts[6]),
                rootDelta = ParseFloat(parts[11]),
                rootHorizontalDelta = ParseFloat(parts[12]),
                rootSpeed = ParseFloat(parts[13]),
                modelDelta = ParseFloat(parts[23])
            };
        }
    }

    class DiagnosticSummary
    {
        public string csvPath;
        public string summaryPath;
        public string jsonPath;
        public int sampleCount;
        public float renderDeltaMsAvg;
        public float renderDeltaMsP95;
        public float renderDeltaMsMax;
        public float rootDeltaAvg;
        public float rootDeltaP95;
        public float rootDeltaMax;
        public float rootSpeedAvg;
        public float rootSpeedP95;
        public float rootSpeedMax;
        public float medianRootDelta;
        public int zeroStepFrames;
        public int doubleStepFrames;
        public int jitterFrames;
        public int fixedUpdateZeroFrames;
        public int fixedUpdateOneFrames;
        public int fixedUpdateTwoPlusFrames;
        public float modelDeltaAvg;
        public float modelDeltaP95;
        public float modelDeltaMinusRootDeltaAvg;
        public float modelDeltaMinusRootDeltaP95;

        public string ToConsoleString()
        {
            return "ModelRoot jitter diagnostic complete.\n"
                + $"CSV: {csvPath}\n"
                + $"Summary: {summaryPath}\n"
                + $"Samples: {sampleCount}\n"
                + $"Render ms avg/p95/max: {Format(renderDeltaMsAvg)} / {Format(renderDeltaMsP95)} / {Format(renderDeltaMsMax)}\n"
                + $"Root delta avg/p95/max: {Format(rootDeltaAvg)} / {Format(rootDeltaP95)} / {Format(rootDeltaMax)}\n"
                + $"Root speed avg/p95/max: {Format(rootSpeedAvg)} / {Format(rootSpeedP95)} / {Format(rootSpeedMax)}\n"
                + $"Fixed updates per render frame 0/1/2+: {fixedUpdateZeroFrames} / {fixedUpdateOneFrames} / {fixedUpdateTwoPlusFrames}\n"
                + $"Zero/double/jitter frames: {zeroStepFrames} / {doubleStepFrames} / {jitterFrames}\n"
                + $"Model delta avg/p95: {Format(modelDeltaAvg)} / {Format(modelDeltaP95)}\n"
                + $"Abs(modelDelta-rootDelta) avg/p95: {Format(modelDeltaMinusRootDeltaAvg)} / {Format(modelDeltaMinusRootDeltaP95)}";
        }

        public string ToTextString()
        {
            return ToConsoleString() + "\n";
        }

        public string ToJsonString()
        {
            return "{\n"
                + $"  \"csvPath\": \"{Escape(csvPath)}\",\n"
                + $"  \"sampleCount\": {sampleCount},\n"
                + $"  \"renderDeltaMs_avg\": {Format(renderDeltaMsAvg)},\n"
                + $"  \"renderDeltaMs_p95\": {Format(renderDeltaMsP95)},\n"
                + $"  \"renderDeltaMs_max\": {Format(renderDeltaMsMax)},\n"
                + $"  \"rootDelta_avg\": {Format(rootDeltaAvg)},\n"
                + $"  \"rootDelta_p95\": {Format(rootDeltaP95)},\n"
                + $"  \"rootDelta_max\": {Format(rootDeltaMax)},\n"
                + $"  \"rootSpeed_avg\": {Format(rootSpeedAvg)},\n"
                + $"  \"rootSpeed_p95\": {Format(rootSpeedP95)},\n"
                + $"  \"rootSpeed_max\": {Format(rootSpeedMax)},\n"
                + $"  \"medianRootDelta\": {Format(medianRootDelta)},\n"
                + $"  \"zeroStepFrames\": {zeroStepFrames},\n"
                + $"  \"doubleStepFrames\": {doubleStepFrames},\n"
                + $"  \"jitterFrames\": {jitterFrames},\n"
                + $"  \"fixedUpdateZeroFrames\": {fixedUpdateZeroFrames},\n"
                + $"  \"fixedUpdateOneFrames\": {fixedUpdateOneFrames},\n"
                + $"  \"fixedUpdateTwoPlusFrames\": {fixedUpdateTwoPlusFrames},\n"
                + $"  \"modelDelta_avg\": {Format(modelDeltaAvg)},\n"
                + $"  \"modelDelta_p95\": {Format(modelDeltaP95)},\n"
                + $"  \"modelDeltaMinusRootDelta_avg\": {Format(modelDeltaMinusRootDeltaAvg)},\n"
                + $"  \"modelDeltaMinusRootDelta_p95\": {Format(modelDeltaMinusRootDeltaP95)}\n"
                + "}\n";
        }
    }

    static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    static string Escape(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
