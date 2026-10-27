using System;
using Unity.AI.Navigation;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, Dead }

// Partage : etat du jeu (menu / en jeu / pause / mort) et lien entre la sauvegarde (P5) et les autres modules.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Joueur")]
    public PlayerController player;
    public PlayerStats stats;
    public Inventory inventory;
    public ThirdPersonCamera cam;
    public Transform spawnPoint;

    [Header("Monde")]
    public DayNightCycle dayNight;
    public BuildSystem build;
    public EnemySpawner spawner;
    public NavMeshSurface navMesh;

    [Tooltip("Sauvegarde automatique toutes les X secondes")]
    public float autoSaveInterval = 60f;

    public GameState State { get; private set; } = GameState.Menu;
    public bool IsPlaying => State == GameState.Playing;
    public event Action<GameState> StateChanged;

    float nextAutoSave;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (navMesh && navMesh.navMeshData == null) navMesh.BuildNavMesh();
    }

    void Start()
    {
        stats.Died += OnPlayerDied;
        SetState(GameState.Menu);
    }

    void Update()
    {
        if (State == GameState.Playing && Time.unscaledTime >= nextAutoSave)
        {
            nextAutoSave = Time.unscaledTime + autoSaveInterval;
            SaveSystem.Save(Capture());
        }
    }

    void SetState(GameState s)
    {
        State = s;
        Time.timeScale = s == GameState.Playing ? 1f : 0f;
        StateChanged?.Invoke(s);
    }

    public void NewGame()
    {
        ResetWorld();
        stats.ResetStats();
        inventory.Clear();
        player.Teleport(spawnPoint.position, spawnPoint.eulerAngles.y);
        cam.Yaw = spawnPoint.eulerAngles.y;
        dayNight.timeOfDay = 0.3f;
        dayNight.day = 1;
        SetState(GameState.Playing);
        SaveGame(false);
        UIRoot.Toast("Recolte du bois et de la pierre, puis construis un abri avant la nuit !", 4f);
    }

    public void ContinueGame()
    {
        var data = SaveSystem.Load();
        if (data == null)
        {
            NewGame();
            return;
        }
        Apply(data);
        SetState(GameState.Playing);
        UIRoot.Toast("Partie chargee - Jour " + data.day);
    }

    public void Pause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State == GameState.Paused) SetState(GameState.Playing);
    }

    public void SaveGame() => SaveGame(true);

    void SaveGame(bool notify)
    {
        if (stats.IsDead) return;
        SaveSystem.Save(Capture());
        nextAutoSave = Time.unscaledTime + autoSaveInterval;
        if (notify) UIRoot.Toast("Partie sauvegardee");
    }

    public void QuitToMenu()
    {
        SaveGame(false);
        build.CancelPlacement();
        SetState(GameState.Menu);
    }

    void OnPlayerDied()
    {
        build.CancelPlacement();
        SetState(GameState.Dead);
    }

    // Sur Android, l'application passe en pause quand on la quitte : on sauvegarde tout de suite.
    void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        if (State == GameState.Playing || State == GameState.Paused)
        {
            SaveGame(false);
            Pause();
        }
    }

    void OnApplicationQuit()
    {
        if (State == GameState.Playing || State == GameState.Paused) SaveGame(false);
    }

    void ResetWorld()
    {
        build.ClearAll();
        spawner.ClearAll();
        foreach (var node in ResourceNode.All) node.ResetNode();
    }

    SaveData Capture()
    {
        var d = new SaveData
        {
            playerPosition = player.transform.position,
            playerYaw = player.transform.eulerAngles.y,
            cameraYaw = cam.Yaw,
            cameraPitch = cam.Pitch,
            health = stats.Health,
            hunger = stats.Hunger,
            thirst = stats.Thirst,
            energy = stats.Energy,
            inventory = inventory.ToList(),
            equipped = inventory.Equipped,
            compassEquipped = inventory.CompassEquipped,
            timeOfDay = dayNight.timeOfDay,
            day = dayNight.day,
        };
        foreach (var p in build.placed)
            if (p) d.pieces.Add(new PieceSave { type = p.type, position = p.transform.position, yRotation = p.transform.eulerAngles.y });
        foreach (var n in ResourceNode.All)
            if (!n.IsAvailable) d.depletedNodes.Add(new NodeSave { id = n.id, remaining = n.RemainingRespawn });
        foreach (var e in EnemyAI.All)
            if (!e.IsDead) d.enemies.Add(new EnemySave { position = e.transform.position, health = e.Health });
        return d;
    }

    void Apply(SaveData d)
    {
        ResetWorld();
        player.Teleport(d.playerPosition, d.playerYaw);
        cam.Yaw = d.cameraYaw;
        cam.Pitch = d.cameraPitch;
        stats.Load(d.health, d.hunger, d.thirst, d.energy);
        inventory.Load(d.inventory, d.equipped, d.compassEquipped);
        dayNight.timeOfDay = d.timeOfDay;
        dayNight.day = Mathf.Max(1, d.day);

        foreach (var p in d.pieces) build.Place(p.type, p.position, p.yRotation);
        foreach (var saved in d.depletedNodes)
            foreach (var n in ResourceNode.All)
                if (n.id == saved.id) n.Deplete(saved.remaining);
        foreach (var e in d.enemies) spawner.Spawn(e.position, e.health);
    }
}
