using System.Runtime.InteropServices;
using UnityEngine;

namespace Kiosko
{
    public static class KioskoWeb
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void KioskoAvisarPagina(string tipo, string datos);
#endif

        public static void Avisar(string tipo, string datosJson = "{}")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            KioskoAvisarPagina(tipo, datosJson);
#else
            Debug.Log($"[KioskoWeb] {tipo} {datosJson}");
#endif
        }
    }
}
