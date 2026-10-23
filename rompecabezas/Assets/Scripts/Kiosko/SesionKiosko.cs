using System;
using UnityEngine;

namespace Kiosko
{
    public class SesionKiosko : MonoBehaviour
    {
        public static int Usuario { get; private set; }
        public static bool HayUsuario => Usuario > 0;

        public static event Action<int> UsuarioCambio;

        [Tooltip("Usuario de prueba al correr en el Editor (1 = sofia en los datos de prueba)")]
        [SerializeField] private int usuarioEditor = 1;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (Application.isEditor)
                Usuario = usuarioEditor;
            else
                Usuario = LeerUsuarioDeUrl(Application.absoluteURL);
        }

        public void SetUsuario(string id)
        {
            if (int.TryParse(id, out int valor) && valor > 0)
            {
                Usuario = valor;
                UsuarioCambio?.Invoke(valor);
            }
            else
            {
                Debug.LogWarning($"[SesionKiosko] Usuario inválido: '{id}'");
            }
        }

        private static int LeerUsuarioDeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return 0;
            int q = url.IndexOf('?');
            if (q < 0) return 0;

            foreach (string par in url.Substring(q + 1).Split('&'))
            {
                string[] kv = par.Split('=');
                if (kv.Length == 2 && kv[0] == "usuario" && int.TryParse(kv[1], out int id))
                    return id;
            }
            return 0;
        }
    }
}
