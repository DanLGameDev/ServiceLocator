using DGP.ServiceLocator.Injectable;
using UnityEditor;

namespace DGP.ServiceLocator.Editor
{
    public class ServiceLocatorEditorUtil
    {
        
        [InitializeOnLoadMethod]
        private static void Initialize() {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        private static void PlayModeStateChange(PlayModeStateChange obj) {
            if (obj == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                ServiceLocator.ClearServices();
            }
        }
    }
}