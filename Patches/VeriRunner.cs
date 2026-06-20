using System;
using System.Collections;
using UnityEngine;

namespace CruiserLoader.Patches
{
    public class VeriRunner : MonoBehaviour
    {
        private static VeriRunner? _instance;
        public static VeriRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("VeriRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VeriRunner>();
                }
                return _instance;
            }
        }

        public void WaitThen(float seconds, Action onTimeout)
        {
            StartCoroutine(WaitCoroutine(seconds, onTimeout));
        }

        private IEnumerator WaitCoroutine(float seconds, Action onTimeout)
        {
            yield return new WaitForSecondsRealtime(seconds);
            try
            {
                onTimeout?.Invoke();
            }
            catch (Exception ex)
            {
                CruiserLoader.Log.LogError($"[CL] Verification runner callback error: {ex.Message}");
            }
        }
    }
}
