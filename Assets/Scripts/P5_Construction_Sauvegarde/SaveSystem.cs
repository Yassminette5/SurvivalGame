using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PieceSave
{
    public BuildPieceType type;
    public Vector3 position;
    public float yRotation;
}

[Serializable]
public class NodeSave
{
    public int id;
    public float remaining;
}

[Serializable]
public class EnemySave
{
    public Vector3 position;
    public float health;
}

// Tout ce qui est ecrit dans le fichier de sauvegarde.
[Serializable]
public class SaveData
{
    public string savedAt;
    public Vector3 playerPosition;
    public float playerYaw;
    public float cameraYaw;
    public float cameraPitch;

    public float health, hunger, thirst, energy;
    public List<ItemStack> inventory = new List<ItemStack>();
    public ItemType equipped;
    public bool compassEquipped;

    public float timeOfDay;
    public int day;

    public List<PieceSave> pieces = new List<PieceSave>();
    public List<NodeSave> depletedNodes = new List<NodeSave>();
    public List<EnemySave> enemies = new List<EnemySave>();
}

// P5 - Sauvegarde en JSON dans le stockage de l'application.
public static class SaveSystem
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "survie_save.json");

    public static bool HasSave => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        data.savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        string tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
        if (File.Exists(FilePath)) File.Delete(FilePath);
        File.Move(tmp, FilePath);
    }

    public static SaveData Load()
    {
        if (!HasSave) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Sauvegarde illisible : " + e.Message);
            return null;
        }
    }

    public static string SavedAt()
    {
        var d = Load();
        return d != null ? d.savedAt : "";
    }

    public static void Delete()
    {
        if (HasSave) File.Delete(FilePath);
    }
}
