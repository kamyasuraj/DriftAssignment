using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DriftAssignment.UI
{
    /// One-minute stress-test stat recorder for the Phase 9 optimization pass.
    /// Attach to any scene GameObject, enter Play, drive for the configured
    /// window (default 60s) and the script writes an aggregated JSON report
    /// (avg / min / max / p50 / p95 / p99) to Doc/stress-capture/<label>_<ts>.json.
    ///
    /// Set _label = "before" for the baseline pass and _label = "after" for
    /// the post-optimization pass — the filenames make the diff obvious.
    ///
    /// Runtime hotkeys:
    ///   F9  — start (or restart) a capture immediately
    ///   F10 — stop the current capture early and write the report
    ///
    /// Uses ProfilerRecorder (Unity 2020.2+) so it's zero-editor-window,
    /// works in a headless build too.
    [DisallowMultipleComponent]
    public class StressTestCapture : MonoBehaviour
    {
        [Header("Capture settings")]
        [Tooltip("Label prefixed to the output filename — use 'before' / 'after' for the optimization diff.")]
        [SerializeField] private string _label = "before";
        [SerializeField] private float _durationSeconds = 60f;
        [SerializeField] private bool _startAutomaticallyOnPlay = true;
        [Tooltip("Seconds to wait after scene load before starting — gives the driver time to focus the window and grab controls.")]
        [SerializeField] private float _startDelaySeconds = 3f;
        [SerializeField] private bool _showOverlay = true;

        // Render category
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;
        private ProfilerRecorder _renderThreadNs;
        // Internal (thread timing)
        private ProfilerRecorder _mainThreadNs;
        // Memory
        private ProfilerRecorder _gcAllocInFrame;
        private ProfilerRecorder _systemMemory;
        // Physics
        private ProfilerRecorder _physicsSimulateNs;

        private readonly List<Sample> _samples = new();
        private bool _capturing;
        private bool _startPending;
        private float _captureStartTime;
        private float _captureEndTime;
        private float _pendingStartTime;
        private string _lastReportPath;

        private struct Sample
        {
            public float Fps;
            public long Batches;
            public long SetPass;
            public long DrawCalls;
            public long Triangles;
            public long Vertices;
            public double MainThreadMs;
            public double RenderThreadMs;
            public double PhysicsMs;
            public long GcAllocBytes;
            public long SystemMemoryBytes;
        }

        private void OnEnable()
        {
            _batches         = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass         = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _drawCalls       = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _triangles       = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _vertices        = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            _renderThreadNs  = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 15);
            _mainThreadNs    = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            _gcAllocInFrame  = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _systemMemory    = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _physicsSimulateNs = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Physics.Simulate", 15);

            if (_startAutomaticallyOnPlay)
            {
                _startPending = true;
                _pendingStartTime = Time.realtimeSinceStartup + _startDelaySeconds;
            }
        }

        private void OnDisable()
        {
            if (_capturing) WriteReport(interrupted: true);
            DisposeRecorder(ref _batches);
            DisposeRecorder(ref _setPass);
            DisposeRecorder(ref _drawCalls);
            DisposeRecorder(ref _triangles);
            DisposeRecorder(ref _vertices);
            DisposeRecorder(ref _renderThreadNs);
            DisposeRecorder(ref _mainThreadNs);
            DisposeRecorder(ref _gcAllocInFrame);
            DisposeRecorder(ref _systemMemory);
            DisposeRecorder(ref _physicsSimulateNs);
        }

        private static void DisposeRecorder(ref ProfilerRecorder r)
        {
            if (r.Valid) r.Dispose();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f9Key.wasPressedThisFrame) StartCapture();
                if (kb.f10Key.wasPressedThisFrame) StopCaptureEarly();
            }

            if (_startPending && Time.realtimeSinceStartup >= _pendingStartTime)
            {
                _startPending = false;
                StartCapture();
            }

            if (!_capturing) return;

            SampleFrame();

            if (Time.realtimeSinceStartup >= _captureEndTime)
            {
                _capturing = false;
                WriteReport(interrupted: false);
            }
        }

        private void SampleFrame()
        {
            _samples.Add(new Sample
            {
                Fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f),
                Batches = _batches.LastValue,
                SetPass = _setPass.LastValue,
                DrawCalls = _drawCalls.LastValue,
                Triangles = _triangles.LastValue,
                Vertices = _vertices.LastValue,
                MainThreadMs = _mainThreadNs.LastValue * 1e-6,
                RenderThreadMs = _renderThreadNs.LastValue * 1e-6,
                PhysicsMs = _physicsSimulateNs.LastValue * 1e-6,
                GcAllocBytes = _gcAllocInFrame.LastValue,
                SystemMemoryBytes = _systemMemory.LastValue
            });
        }

        public void StartCapture()
        {
            _samples.Clear();
            _captureStartTime = Time.realtimeSinceStartup;
            _captureEndTime = _captureStartTime + _durationSeconds;
            _capturing = true;
            _startPending = false;
            Debug.Log($"[StressTestCapture] START label='{_label}' duration={_durationSeconds}s");
        }

        public void StopCaptureEarly()
        {
            if (!_capturing) return;
            _capturing = false;
            WriteReport(interrupted: true);
        }

        private void WriteReport(bool interrupted)
        {
            if (_samples.Count == 0)
            {
                Debug.LogWarning("[StressTestCapture] no samples captured — nothing written");
                return;
            }

            var json = BuildReport(interrupted);
            var dir = ResolveOutputDirectory();
            Directory.CreateDirectory(dir);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = string.IsNullOrWhiteSpace(_label) ? $"capture_{stamp}.json" : $"{_label}_{stamp}.json";
            var path = Path.Combine(dir, fileName);
            File.WriteAllText(path, json);
            _lastReportPath = path;
            Debug.Log($"[StressTestCapture] WROTE {path}  samples={_samples.Count} interrupted={interrupted}");
        }

        private static string ResolveOutputDirectory()
        {
#if UNITY_EDITOR
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, "Doc", "stress-capture");
#else
            return Path.Combine(Application.persistentDataPath, "stress-capture");
#endif
        }

        private string BuildReport(bool interrupted)
        {
            var n = _samples.Count;
            var fps = new float[n];
            var mainMs = new double[n];
            var renderMs = new double[n];
            var physMs = new double[n];
            var batches = new long[n];
            var setPass = new long[n];
            var drawCalls = new long[n];
            var tris = new long[n];
            var verts = new long[n];
            var gc = new long[n];
            var mem = new long[n];

            for (int i = 0; i < n; i++)
            {
                var s = _samples[i];
                fps[i] = s.Fps;
                mainMs[i] = s.MainThreadMs;
                renderMs[i] = s.RenderThreadMs;
                physMs[i] = s.PhysicsMs;
                batches[i] = s.Batches;
                setPass[i] = s.SetPass;
                drawCalls[i] = s.DrawCalls;
                tris[i] = s.Triangles;
                verts[i] = s.Vertices;
                gc[i] = s.GcAllocBytes;
                mem[i] = s.SystemMemoryBytes;
            }

            var ci = CultureInfo.InvariantCulture;
            var actualDur = (Time.realtimeSinceStartup - _captureStartTime).ToString("0.00", ci);
            var durTarget = _durationSeconds.ToString("0.00", ci);

            var sb = new StringBuilder(4096);
            sb.AppendLine("{");
            sb.AppendLine($"  \"label\": \"{Escape(_label)}\",");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:s}\",");
            sb.AppendLine($"  \"unity_version\": \"{Application.unityVersion}\",");
            sb.AppendLine($"  \"platform\": \"{Application.platform}\",");
            sb.AppendLine($"  \"quality_level\": \"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\",");
            sb.AppendLine($"  \"screen\": \"{Screen.width}x{Screen.height}\",");
            sb.AppendLine($"  \"duration_target_s\": {durTarget},");
            sb.AppendLine($"  \"duration_actual_s\": {actualDur},");
            sb.AppendLine($"  \"sample_count\": {n},");
            sb.AppendLine($"  \"interrupted\": {(interrupted ? "true" : "false")},");
            sb.AppendLine("  \"metrics\": {");
            sb.AppendLine("    \"fps\":                     " + StatsF(fps) + ",");
            sb.AppendLine("    \"main_thread_ms\":          " + StatsD(mainMs) + ",");
            sb.AppendLine("    \"render_thread_ms\":        " + StatsD(renderMs) + ",");
            sb.AppendLine("    \"physics_simulate_ms\":     " + StatsD(physMs) + ",");
            sb.AppendLine("    \"batches\":                 " + StatsL(batches) + ",");
            sb.AppendLine("    \"set_pass_calls\":          " + StatsL(setPass) + ",");
            sb.AppendLine("    \"draw_calls\":              " + StatsL(drawCalls) + ",");
            sb.AppendLine("    \"triangles\":               " + StatsL(tris) + ",");
            sb.AppendLine("    \"vertices\":                " + StatsL(verts) + ",");
            sb.AppendLine("    \"gc_alloc_bytes_per_frame\":" + StatsL(gc) + ",");
            sb.AppendLine("    \"system_used_memory_bytes\":" + StatsL(mem));
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string StatsF(float[] arr)
        {
            Array.Sort(arr);
            var ci = CultureInfo.InvariantCulture;
            double sum = 0;
            for (int i = 0; i < arr.Length; i++) sum += arr[i];
            var avg = sum / arr.Length;
            return $"{{ \"avg\": {avg.ToString("0.00", ci)}, \"min\": {arr[0].ToString("0.00", ci)}, \"max\": {arr[arr.Length - 1].ToString("0.00", ci)}, \"p50\": {PctF(arr, 0.50).ToString("0.00", ci)}, \"p95\": {PctF(arr, 0.95).ToString("0.00", ci)}, \"p99\": {PctF(arr, 0.99).ToString("0.00", ci)} }}";
        }

        private static string StatsD(double[] arr)
        {
            Array.Sort(arr);
            var ci = CultureInfo.InvariantCulture;
            double sum = 0;
            for (int i = 0; i < arr.Length; i++) sum += arr[i];
            var avg = sum / arr.Length;
            return $"{{ \"avg\": {avg.ToString("0.000", ci)}, \"min\": {arr[0].ToString("0.000", ci)}, \"max\": {arr[arr.Length - 1].ToString("0.000", ci)}, \"p50\": {PctD(arr, 0.50).ToString("0.000", ci)}, \"p95\": {PctD(arr, 0.95).ToString("0.000", ci)}, \"p99\": {PctD(arr, 0.99).ToString("0.000", ci)} }}";
        }

        private static string StatsL(long[] arr)
        {
            Array.Sort(arr);
            var ci = CultureInfo.InvariantCulture;
            double sum = 0;
            for (int i = 0; i < arr.Length; i++) sum += arr[i];
            var avg = sum / arr.Length;
            return $"{{ \"avg\": {avg.ToString("0", ci)}, \"min\": {arr[0]}, \"max\": {arr[arr.Length - 1]}, \"p50\": {PctL(arr, 0.50)}, \"p95\": {PctL(arr, 0.95)}, \"p99\": {PctL(arr, 0.99)} }}";
        }

        private static float PctF(float[] sorted, double p)
        {
            var idx = Mathf.Clamp(Mathf.RoundToInt((float)p * (sorted.Length - 1)), 0, sorted.Length - 1);
            return sorted[idx];
        }

        private static double PctD(double[] sorted, double p)
        {
            var idx = Mathf.Clamp(Mathf.RoundToInt((float)p * (sorted.Length - 1)), 0, sorted.Length - 1);
            return sorted[idx];
        }

        private static long PctL(long[] sorted, double p)
        {
            var idx = Mathf.Clamp(Mathf.RoundToInt((float)p * (sorted.Length - 1)), 0, sorted.Length - 1);
            return sorted[idx];
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;
            var w = 380;
            var h = 100;
            var rect = new Rect(Screen.width - w - 12, 12, w, h);
            var boxTex = MakeSolidTex(new Color(0f, 0f, 0f, 0.75f));
            var boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = boxTex;
            GUI.Box(rect, GUIContent.none, boxStyle);

            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12));
            if (_capturing)
            {
                var elapsed = Time.realtimeSinceStartup - _captureStartTime;
                var remaining = Mathf.Max(0f, _captureEndTime - Time.realtimeSinceStartup);
                GUILayout.Label($"<color=#ff5050>● REC</color>  <color=#ffff90>{_label.ToUpper()}</color>  {elapsed:0.0}s / {_durationSeconds:0}s", style);
                GUILayout.Label($"samples: {_samples.Count}   remaining: {remaining:0.0}s", style);
                GUILayout.Label("F10 stop early", style);
            }
            else if (_startPending)
            {
                var wait = Mathf.Max(0f, _pendingStartTime - Time.realtimeSinceStartup);
                GUILayout.Label($"<color=#88ff88>arming</color>  {wait:0.0}s → capture '{_label}'", style);
                GUILayout.Label($"window {_durationSeconds:0}s   F9 start now", style);
            }
            else
            {
                GUILayout.Label($"StressTestCapture idle  <color=#88ff88>[{_label}]</color>", style);
                GUILayout.Label($"F9 start  ({_durationSeconds:0}s window)", style);
                if (!string.IsNullOrEmpty(_lastReportPath))
                    GUILayout.Label($"last: {Path.GetFileName(_lastReportPath)}", style);
            }
            GUILayout.EndArea();
        }

        private static Texture2D _sSolid;

        private static Texture2D MakeSolidTex(Color c)
        {
            if (_sSolid != null) return _sSolid;
            _sSolid = new Texture2D(1, 1);
            _sSolid.SetPixel(0, 0, c);
            _sSolid.Apply();
            return _sSolid;
        }
    }
}
