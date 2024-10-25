using UnityEditor;
using UnityEngine;

namespace DGP.ServiceLocator.Editor
{
    public class ServiceLocatorDebuggerWindow : EditorWindow 
    {
        [MenuItem("Tools/ServiceLocator Monitor")]
        private static void OpenWindow()
        {
            GetWindow<ServiceLocatorDebuggerWindow>("ServiceLocator Monitor").Show();
        }
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= Repaint;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.update += Repaint;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    EditorApplication.update -= Repaint;
                    break;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("ServiceLocator Monitor is only active at runtime", EditorStyles.boldLabel);
                return;
            }
            
            EditorGUILayout.LabelField("Registered Services", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var service in ServiceLocator.Container.RegisteredServices)
            {
                if (service.Value != null) {
                    EditorGUILayout.LabelField($"{service.Key.Name}");
                }
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pending Queries", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var query in ServiceLocator.Container.PendingServiceQueries)
            {
                EditorGUILayout.LabelField($"{query.SearchedType.Name} - {query.SearchMode})");
            }
            EditorGUI.indentLevel--;
        }
    }
}