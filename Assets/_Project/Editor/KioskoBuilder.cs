#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KioskoBuilder
{
    private const string Base = "Assets/_Project/Scenes/";
    private const string Salida = "C:/Users/rouch/";

    private struct Juego
    {
        public string nombre;
        public string[] escenas;
    }

    private static readonly Juego[] Juegos =
    {
        new Juego { nombre = "OrbitPayment", escenas = new[] { Base + "OrbitPayment/MainMenuOP.unity", Base + "OrbitPayment/Level1.unity", Base + "OrbitPayment/Level2.unity", Base + "OrbitPayment/Level3.unity" } },
        new Juego { nombre = "BrewStack", escenas = new[] { Base + "BrewStack/BS_Scenes/BS_Menu.unity", Base + "BrewStack/BS_Scenes/BS_Game.unity" } },
        new Juego { nombre = "AtlasExpress", escenas = new[] { Base + "AtlasExpress/AE_Scenes/AE_Menu.unity", Base + "AtlasExpress/AE_Scenes/AE_Instrucciones.unity", Base + "AtlasExpress/AE_Scenes/AE_Jugar.unity", Base + "AtlasExpress/AE_Scenes/AE_Pantalla_Victoria.unity", Base + "AtlasExpress/AE_Scenes/AE_Pantalla_Derrota.unity" } },
        new Juego { nombre = "SpaceDefender", escenas = new[] { Base + "SpaceDefender/SD_Scenes/SD_MainMenu.unity", Base + "SpaceDefender/SD_Scenes/SD_Instrucciones.unity", Base + "SpaceDefender/SD_Scenes/SD_Gameplay.unity", Base + "SpaceDefender/SD_Scenes/SD_Aftermath.unity" } },
        new Juego { nombre = "HeadBreaker", escenas = new[] { Base + "Headbreaker/HB_Scenes/HB_Game.unity" } }
    };

    [MenuItem("Kiosko/Build WebGL/Orbit Payment")]
    public static void ConstruirOrbit() { Construir(0); }

    [MenuItem("Kiosko/Build WebGL/BrewStack")]
    public static void ConstruirBrew() { Construir(1); }

    [MenuItem("Kiosko/Build WebGL/Atlas Express")]
    public static void ConstruirAtlas() { Construir(2); }

    [MenuItem("Kiosko/Build WebGL/Space Defender")]
    public static void ConstruirSpace() { Construir(3); }

    [MenuItem("Kiosko/Build WebGL/Head Breaker")]
    public static void ConstruirHead() { Construir(4); }

    [MenuItem("Kiosko/Build WebGL/Los 5 juegos")]
    public static void ConstruirTodos()
    {
        for (int i = 0; i < Juegos.Length; i++)
            if (!Construir(i)) break;
    }

    [MenuItem("Kiosko/Escenas en Build Settings/Orbit Payment")]
    public static void EscenasOrbit() { Cargar(0); }

    [MenuItem("Kiosko/Escenas en Build Settings/BrewStack")]
    public static void EscenasBrew() { Cargar(1); }

    [MenuItem("Kiosko/Escenas en Build Settings/Atlas Express")]
    public static void EscenasAtlas() { Cargar(2); }

    [MenuItem("Kiosko/Escenas en Build Settings/Space Defender")]
    public static void EscenasSpace() { Cargar(3); }

    [MenuItem("Kiosko/Escenas en Build Settings/Head Breaker")]
    public static void EscenasHead() { Cargar(4); }

    private static void Cargar(int indice)
    {
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();
        foreach (string ruta in Juegos[indice].escenas)
            lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    private static bool Construir(int indice)
    {
        Juego juego = Juegos[indice];
        foreach (string ruta in juego.escenas)
        {
            if (!File.Exists(ruta))
            {
                Debug.LogError("[KioskoBuilder] Falta la escena " + ruta);
                return false;
            }
        }

        Cargar(indice);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = false;

        string carpeta = Salida + juego.nombre;
        Directory.CreateDirectory(carpeta);

        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = juego.escenas,
            locationPathName = carpeta,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        bool bien = reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        Debug.Log("[KioskoBuilder] " + juego.nombre + ": " + reporte.summary.result + " -> " + carpeta);
        return bien;
    }
}
#endif
