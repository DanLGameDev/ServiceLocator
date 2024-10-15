using System;
using UnityEditor;

namespace DGP.ServiceLocator
{
    public abstract class Singleton<T> : IDisposable where T : Singleton<T>, new()
    {
        private static Lazy<T> _instance = new Lazy<T>(() => new T());

        public static T Instance => _instance.Value;
        public static bool IsInitialized => _instance.IsValueCreated;

        private static void DestroyInstance() {
            _instance?.Value.Dispose();
            _instance = new Lazy<T>(() => new T());
        }

        protected Singleton() { }
        
        #if UNITY_EDITOR
        static Singleton() {
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }
        
        private static void PlayModeStateChange(PlayModeStateChange mode) {
            if (mode == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                DestroyInstance();
        }
        #endif
        
        #region IDisposable
        protected virtual void Dispose(bool disposing) {
            //noop
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}