using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Test automatique : ouvre la scene et lance le mode Play (le scenario est dans PlaytestDriver).
// Ligne de commande : Unity -batchmode -projectPath . -executeMethod PlaytestRunner.Run
// avec la variable d'environnement SURVIE_PLAYTEST=1.
public static class PlaytestRunner
{
    static double deadline;
    static double nextLog;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Survie.unity");
        deadline = EditorApplication.timeSinceStartup + 300.0;
        EditorApplication.update += Watchdog;
        EditorApplication.isPlaying = true;
    }

    static void Watchdog()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now >= nextLog)
        {
            nextLog = now + 5.0;
            Debug.Log("[Playtest] isPlaying=" + EditorApplication.isPlaying + " frame=" + Time.frameCount + " t=" + Time.realtimeSinceStartup.ToString("0"));
        }
        if (now > deadline)
        {
            Debug.LogError("[Playtest] Delai depasse, arret.");
            EditorApplication.Exit(2);
        }
    }
}
