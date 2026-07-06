using System.Collections;
using UnityEngine;

namespace Linxium.SuperShader {
    internal sealed class PostEffectCoroutineRunner : MonoBehaviour {
        static PostEffectCoroutineRunner instance;

        public static Coroutine Run(IEnumerator routine) {
            EnsureInstance();
            return instance.StartCoroutine(routine);
        }

        public static void Stop(Coroutine routine) {
            if (instance != null && routine != null) {
                instance.StopCoroutine(routine);
            }
        }

        static void EnsureInstance() {
            if (instance != null) {
                return;
            }

            var go = new GameObject("[Linxium.SuperShader] PostEffectRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PostEffectCoroutineRunner>();
        }
    }
}
