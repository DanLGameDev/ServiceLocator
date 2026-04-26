using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DGP.ServiceLocator.Editor
{
    public class ServiceLocatorDebugWindow : EditorWindow
    {
        [MenuItem("DGP/Service Locator Debugger")]
        private static void OpenWindow()
        {
            var window = GetWindow<ServiceLocatorDebugWindow>("Service Locator Debugger");
            window.minSize = new Vector2(400f, 300f);
            window.Show();
        }

        // ── Flash ─────────────────────────────────────────────────────────────
        private readonly Dictionary<Type, double> _flashTimestamps = new();
        private HashSet<Type> _knownServiceTypes = new();
        private const double FlashDuration = 0.6;
        private static readonly Color FlashColor = new Color(1f, 0.85f, 0f, 1f);
        private static readonly Color NormalBg   = new Color(0.20f, 0.20f, 0.20f, 1f);
        private static readonly Color PendingBg  = new Color(0.22f, 0.18f, 0.18f, 1f);

        // ── Panels ────────────────────────────────────────────────────────────
        private Vector2 _servicesScrollPos;
        private Vector2 _queriesScrollPos;
        private float _splitterFraction = 0.65f;
        private bool _draggingSplitter;

        // ── Reflection cache (static: survives window close/reopen) ───────────
        private static FieldInfo _pendingQueriesField;
        private static FieldInfo _queriesListField;
        private static FieldInfo _callbackField;

        // ── GUIStyles (lazy: must not init before first OnGUI) ────────────────
        private GUIStyle _serviceNameStyle;
        private GUIStyle _instanceTypeStyle;
        private GUIStyle _searchModeStyle;
        private GUIStyle _sectionHeaderStyle;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            ServiceLocator.Instance.OnServicesListChanged += HandleServicesChanged;
            EditorApplication.update += OnEditorUpdate;
            _knownServiceTypes = new HashSet<Type>(ServiceLocator.Instance.RegisteredServices.Keys);
        }

        private void OnDisable()
        {
            ServiceLocator.Instance.OnServicesListChanged -= HandleServicesChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void HandleServicesChanged()
        {
            double now = EditorApplication.timeSinceStartup;
            foreach (var key in ServiceLocator.Instance.RegisteredServices.Keys)
            {
                if (!_knownServiceTypes.Contains(key))
                    _flashTimestamps[key] = now;
            }
            _knownServiceTypes = new HashSet<Type>(ServiceLocator.Instance.RegisteredServices.Keys);
            Repaint();
        }

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            foreach (var kvp in _flashTimestamps)
            {
                if (now - kvp.Value < FlashDuration)
                {
                    Repaint();
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GUI
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            float toolbarH  = EditorGUIUtility.singleLineHeight + 6f;
            float available = position.height - toolbarH;
            const float splitterH = 4f;

            float servicesH = Mathf.Round(available * _splitterFraction);
            float queriesH  = available - servicesH - splitterH;

            DrawServicesPanel(new Rect(0f, toolbarH, position.width, servicesH));
            DrawSplitter(new Rect(0f, toolbarH + servicesH, position.width, splitterH));
            DrawPendingQueriesPanel(new Rect(0f, toolbarH + servicesH + splitterH, position.width, queriesH));
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Service Locator Debugger", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Clear Services",
                        "Clear all registered services and pending queries?", "Clear", "Cancel"))
                    ServiceLocator.ClearServices();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServicesPanel(Rect rect)
        {
            var services = new List<KeyValuePair<Type, object>>(ServiceLocator.Instance.RegisteredServices);

            GUILayout.BeginArea(rect);
            EditorGUI.DrawRect(new Rect(0f, 0f, rect.width, EditorGUIUtility.singleLineHeight + 4f),
                new Color(0.15f, 0.15f, 0.15f, 1f));
            GUILayout.Label($"  Registered Services  ({services.Count})", _sectionHeaderStyle);

            _servicesScrollPos = EditorGUILayout.BeginScrollView(_servicesScrollPos);

            if (services.Count == 0)
                EditorGUILayout.LabelField("No services registered.", EditorStyles.centeredGreyMiniLabel);
            else
            {
                double now = EditorApplication.timeSinceStartup;
                foreach (var kvp in services)
                    DrawServiceRow(kvp.Key, kvp.Value, now);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawServiceRow(Type serviceType, object instance, double now)
        {
            float flashT = 0f;
            if (_flashTimestamps.TryGetValue(serviceType, out double ts))
                flashT = Mathf.Clamp01(1f - (float)((now - ts) / FlashDuration));

            Color rowBg = Color.Lerp(NormalBg, FlashColor, Mathf.SmoothStep(0f, 1f, flashT));

            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rowRect, rowBg);

            Color prevContent = GUI.contentColor;
            GUI.contentColor = flashT > 0.05f ? Color.black : Color.white;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(serviceType.Name, _serviceNameStyle, GUILayout.ExpandWidth(true));

            Type instanceType = instance?.GetType();
            if (instanceType != null && instanceType != serviceType)
                GUILayout.Label($"({instanceType.Name})", _instanceTypeStyle, GUILayout.Width(140f));

            GUI.contentColor = prevContent;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1f);
        }

        private void DrawSplitter(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                _draggingSplitter = true;
                e.Use();
            }
            if (_draggingSplitter)
            {
                if (e.type == EventType.MouseDrag)
                {
                    float toolbarH  = EditorGUIUtility.singleLineHeight + 6f;
                    float available = position.height - toolbarH;
                    _splitterFraction = Mathf.Clamp((e.mousePosition.y - toolbarH) / available, 0.2f, 0.8f);
                    Repaint();
                    e.Use();
                }
                if (e.type == EventType.MouseUp)
                {
                    _draggingSplitter = false;
                    e.Use();
                }
            }
        }

        private void DrawPendingQueriesPanel(Rect rect)
        {
            var queries = new List<ServiceQuery>(GetPendingQueries());

            GUILayout.BeginArea(rect);
            EditorGUI.DrawRect(new Rect(0f, 0f, rect.width, EditorGUIUtility.singleLineHeight + 4f),
                new Color(0.15f, 0.15f, 0.15f, 1f));
            GUILayout.Label($"  Pending Queries  ({queries.Count})", _sectionHeaderStyle);

            _queriesScrollPos = EditorGUILayout.BeginScrollView(_queriesScrollPos);

            if (queries.Count == 0)
                EditorGUILayout.LabelField("No pending queries.", EditorStyles.centeredGreyMiniLabel);
            else
                foreach (var query in queries)
                    DrawQueryRow(query);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawQueryRow(ServiceQuery query)
        {
            Rect rowRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(rowRect, PendingBg);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(query.SearchedType.Name, _serviceNameStyle, GUILayout.ExpandWidth(true));

            var (targetType, methodName) = GetCallbackInfo(query);
            GUILayout.Label($"{targetType}.{methodName}", _instanceTypeStyle, GUILayout.Width(160f));
            GUILayout.Label(query.SearchMode.ToString(), _searchModeStyle, GUILayout.Width(80f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Reflection helpers
        // ─────────────────────────────────────────────────────────────────────

        private static List<ServiceQuery> GetPendingQueries()
        {
            if (_pendingQueriesField == null)
                _pendingQueriesField = typeof(ServiceContainer).GetField("_pendingQueries",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_pendingQueriesField == null) return new List<ServiceQuery>();

            var queryList = _pendingQueriesField.GetValue(ServiceLocator.Instance);
            if (queryList == null) return new List<ServiceQuery>();

            if (_queriesListField == null)
                _queriesListField = queryList.GetType().GetField("_queries",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_queriesListField == null) return new List<ServiceQuery>();

            return _queriesListField.GetValue(queryList) as List<ServiceQuery> ?? new List<ServiceQuery>();
        }

        private static (string targetType, string method) GetCallbackInfo(ServiceQuery query)
        {
            if (_callbackField == null)
                _callbackField = typeof(ServiceQuery).GetField("_callback",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_callbackField?.GetValue(query) is Delegate del)
            {
                string target = del.Target?.GetType().Name
                             ?? del.Method.DeclaringType?.Name
                             ?? "static";
                string method = del.Method.Name;
                var match = Regex.Match(method, @"<(.+?)>");
                if (match.Success) method = match.Groups[1].Value;
                return (target, method);
            }

            return ("<unknown>", "<unknown>");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Style init (lazy — GUIStyle can't be created before first OnGUI)
        // ─────────────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_serviceNameStyle != null) return;

            _serviceNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
            };

            _instanceTypeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal    = { textColor = new Color(0.50f, 0.75f, 0.50f, 1f) }
            };

            _searchModeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal    = { textColor = new Color(0.50f, 0.65f, 0.85f, 1f) }
            };

            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f, 1f) }
            };
        }
    }
}
