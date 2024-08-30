using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DGP.ServiceLocator
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    public class ServiceLocatorDebuggerWindow : OdinEditorWindow 
    {
        [MenuItem("Tools/ServiceLocator Monitor")]
        private static void OpenWindow()
        {
            GetWindow<ServiceLocatorDebuggerWindow>().Show();
        }
        
        private void Update() {
            Repaint();
        }

        protected override void DrawEditors() {
            base.DrawEditors();
            
            if (!Application.isPlaying) {
                GUI.Label(new Rect(10, 10, position.width - 20, 20), "ServiceLocator Monitor is only active at runtime", EditorStyles.boldLabel);
                return;
            }
            
            foreach (var service in ServiceLocator.Instance.RegisteredServices) {
                if (service.Value!=null)
                    EditorGUILayout.LabelField(service.Key.Type.Name, new GUIStyle(EditorStyles.boldLabel));
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pending Queries");
            
            foreach (var query in ServiceLocator.Instance.PendingServiceQueries) {
                EditorGUILayout.LabelField(query.Address.Type.Name, query.SearchMode.ToString());
            }

        }

    }
#endif
}