#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

// Test automatique du jeu en mode Play (Editor seulement, jamais dans le build Android).
// Ne fait rien sauf si la variable d'environnement SURVIE_PLAYTEST vaut 1 (voir PlaytestRunner).
// Joue une partie : deplacement, recolte, craft, construction, ennemi, nuit, sauvegarde, mort.
public class PlaytestDriver : MonoBehaviour
{
    readonly StringBuilder report = new StringBuilder();
    readonly List<string> errors = new List<string>();
    int failures;
    string outDir;
    string savePath, saveBackup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Environment.GetEnvironmentVariable("SURVIE_PLAYTEST") != "1") return;
        Debug.Log("[Playtest] demarrage du scenario");
        new GameObject("Playtest").AddComponent<PlaytestDriver>();
    }

    void Awake()
    {
        Application.logMessageReceived += OnLog;
        outDir = Environment.GetEnvironmentVariable("SURVIE_PLAYTEST_OUT");
        if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Application.dataPath, "../Logs/Playtest");
        Directory.CreateDirectory(outDir);

        // Ne pas ecraser la vraie sauvegarde de l'Editor.
        savePath = Path.Combine(Application.persistentDataPath, "survie_save.json");
        saveBackup = savePath + ".playtest-backup";
        if (File.Exists(savePath)) File.Copy(savePath, saveBackup, true);
        if (File.Exists(savePath)) File.Delete(savePath);

        StartCoroutine(RunAll());
    }

    void OnLog(string message, string stack, LogType type)
    {
        // L'indexeur de recherche de l'Editor plante parfois au demarrage en mode batch : erreur de Unity, pas du jeu.
        if (stack.Contains("UnityEditor.Search.")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(type + ": " + message + "\n" + stack);
    }

    void Check(string name, bool ok, string detail = "")
    {
        if (!ok) failures++;
        string line = (ok ? "[OK]   " : "[FAIL] ") + name + (detail.Length > 0 ? "  -> " + detail : "");
        report.AppendLine(line);
        Debug.Log("[Playtest] " + line);
    }

    IEnumerator RunAll()
    {
        var timeout = Time.realtimeSinceStartup + 240f;
        var steps = Scenario();
        while (true)
        {
            object current;
            try
            {
                if (!steps.MoveNext()) break;
                current = steps.Current;
            }
            catch (Exception e)
            {
                Check("Scenario sans exception", false, e.ToString());
                break;
            }
            if (Time.realtimeSinceStartup > timeout)
            {
                Check("Scenario fini avant 240 s", false);
                break;
            }
            yield return current;
        }
        Finish();
    }

    static WaitForSecondsRealtime Wait(float s) => new WaitForSecondsRealtime(s);

    IEnumerator Scenario()
    {
        yield return Wait(1f);
        var gm = GameManager.Instance;
        Check("GameManager present", gm != null);
        if (gm == null) yield break;

        var player = gm.player;
        var actions = player.GetComponent<PlayerActions>();
        var inv = gm.inventory;
        var stats = gm.stats;
        var build = gm.build;

        // --- Menu principal
        Check("Demarre sur le menu", gm.State == GameState.Menu, gm.State.ToString());
        Check("Temps arrete dans le menu", Time.timeScale == 0f);
        Check("Menu principal visible", Find("MainMenu") && Find("MainMenu").activeInHierarchy);
        yield return Capture("01_menu");

        // --- Nouvelle partie
        gm.NewGame();
        yield return Wait(1.5f);
        Check("Nouvelle partie -> en jeu", gm.IsPlaying);
        Check("Joueur au sol", player.transform.position.y > -0.5f && player.transform.position.y < 1f, player.transform.position.ToString());
        Check("Ressources dans la scene", ResourceNode.All.Count > 100, ResourceNode.All.Count + " noeuds");
        Check("NavMesh present", NavMesh.SamplePosition(Vector3.zero + Vector3.forward * 5f, out _, 5f, NavMesh.AllAreas));
        yield return Capture("02_en_jeu");

        // --- Deplacement (joystick)
        Vector3 start = player.transform.position;
        InputHub.Instance.joystick = new Vector2(0f, 1f);
        yield return Wait(1.5f);
        InputHub.Instance.joystick = Vector2.zero;
        float moved = Vector3.Distance(Flat(start), Flat(player.transform.position));
        Check("Le joystick fait avancer le joueur", moved > 2f, moved.ToString("0.0") + " m");

        // --- Sprint consomme de l'energie
        float energyBefore = stats.Energy;
        InputHub.Instance.joystick = new Vector2(1f, 0f);
        InputHub.Instance.sprintHeld = true;
        yield return Wait(1f);
        InputHub.Instance.joystick = Vector2.zero;
        InputHub.Instance.sprintHeld = false;
        Check("Le sprint consomme de l'energie", stats.Energy < energyBefore, energyBefore.ToString("0") + " -> " + stats.Energy.ToString("0"));

        // --- Recolte
        yield return Harvest(ResourceKind.Tree, ItemType.Wood, 1, "Arbre -> bois (mains nues)");
        yield return Harvest(ResourceKind.Rock, ItemType.Stone, 1, "Rocher -> pierre (mains nues)");
        yield return Harvest(ResourceKind.Bush, ItemType.Berry, 2, "Buisson -> baies");

        // --- Boire
        stats.Drink(-60f);
        float thirst = stats.Thirst;
        var lake = FindAnyObjectByType<WaterSource>();
        Check("Lac present", lake != null);
        if (lake)
        {
            var box = lake.GetComponent<Collider>().bounds;
            MoveTo(new Vector3(box.center.x + box.extents.x - 0.5f, 0.05f, box.center.z), box.center);
            yield return Wait(0.6f);
            actions.DoAction();
            yield return Wait(0.2f);
            Check("Boire au lac remonte la soif", stats.Thirst > thirst + 1f, thirst.ToString("0") + " -> " + stats.Thirst.ToString("0"));
        }

        // --- Manger
        stats.Eat(-40f);
        float hunger = stats.Hunger;
        var invUI = FindAnyObjectByType<InventoryUI>();
        invUI.Open();
        yield return null;
        Check("Le sac s'ouvre", invUI.IsOpen);
        yield return Capture("03_sac");
        typeof(InventoryUI).GetMethod("Eat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(invUI, new object[] { ItemType.Berry });
        Check("Manger des baies remonte la faim", stats.Hunger > hunger, hunger.ToString("0") + " -> " + stats.Hunger.ToString("0"));
        invUI.Close();

        // --- Craft
        inv.Add(ItemType.Wood, 30);
        inv.Add(ItemType.Stone, 30);
        var craftUI = FindAnyObjectByType<CraftingUI>();
        craftUI.Toggle();
        yield return null;
        yield return Capture("04_craft");
        craftUI.Toggle();
        foreach (var r in ItemDB.Recipes)
            Check("Fabriquer " + ItemDB.Name(r.result), inv.Craft(r) && inv.Count(r.result) == 1);
        Check("Boussole equipee", inv.CompassEquipped);
        inv.Equip(ItemType.Axe);
        Check("Hache en main", inv.Equipped == ItemType.Axe);
        yield return Harvest(ResourceKind.Tree, ItemType.Wood, 3, "Arbre avec hache -> 3 bois");
        inv.Equip(ItemType.Pickaxe);
        yield return Harvest(ResourceKind.Rock, ItemType.Stone, 3, "Rocher avec pioche -> 3 pierres");

        // --- Construction (zone degagee pres du point de depart)
        MoveTo(new Vector3(-30f, 0.05f, -30f), new Vector3(-30f, 0f, -20f));
        yield return Wait(0.5f);
        int piecesBefore = build.placed.Count;
        Vector3 roofPos = Vector3.zero;
        foreach (BuildPieceType t in new[] { BuildPieceType.Floor, BuildPieceType.Roof, BuildPieceType.Wall, BuildPieceType.Campfire })
        {
            if (t == BuildPieceType.Wall) build.Rotate();
            build.BeginPlacement(t);
            yield return Wait(0.2f);
            if (t == BuildPieceType.Roof) yield return Capture("05_fantome_toit");
            bool ok = build.ConfirmPlacement();
            Check("Poser " + BuildPiece.Name(t), ok);
            if (ok && t == BuildPieceType.Roof) roofPos = build.placed[build.placed.Count - 1].transform.position;
            build.CancelPlacement();
            if (t == BuildPieceType.Roof) MoveTo(new Vector3(-30f, 0.05f, -34f), new Vector3(-30f, 0f, -20f));
            yield return null;
        }
        Check("4 pieces posees", build.placed.Count == piecesBefore + 4, build.placed.Count.ToString());
        Check("Pas deux toits au meme endroit", !Place(BuildPieceType.Roof, roofPos));

        MoveTo(roofPos + Vector3.up * 0.05f, roofPos + Vector3.forward);
        yield return Wait(0.6f);
        Check("Sous le toit = a l'abri", stats.Sheltered);
        yield return CaptureFrom("06_abri", roofPos + new Vector3(8f, 6f, -8f), roofPos);

        MoveTo(roofPos + new Vector3(20f, 0.05f, 20f), roofPos);
        yield return Wait(0.6f);
        Check("Loin du toit = plus a l'abri", !stats.Sheltered);

        // --- Boussole
        var compassUI = FindAnyObjectByType<CompassUI>();
        Check("Boussole affichee", Find("Compass") && Find("Compass").activeInHierarchy);
        Check("Boussole trouve l'abri", build.TryGetNearestShelter(player.transform.position, out Vector3 shelter) && Vector3.Distance(Flat(shelter), Flat(roofPos)) < 0.1f);

        // --- Ennemi : poursuite, degats, combat
        Vector3 arena = new Vector3(40f, 0.05f, -40f);
        MoveTo(arena, arena + Vector3.forward);
        yield return Wait(0.5f);
        gm.spawner.ClearAll();
        yield return null;
        stats.Load(100f, 100f, 100f, 100f);
        NavMesh.SamplePosition(arena + Vector3.forward * 7f, out NavMeshHit hit, 5f, NavMesh.AllAreas);
        var enemy = gm.spawner.Spawn(hit.position, 60f);
        Check("Ennemi pose sur le NavMesh", enemy && enemy.GetComponent<NavMeshAgent>().isOnNavMesh);
        float health = stats.Health;
        float t0 = Time.realtimeSinceStartup;
        while (stats.Health >= health && Time.realtimeSinceStartup - t0 < 8f) yield return null;
        Check("L'ennemi poursuit et frappe le joueur", stats.Health < health,
            "vie " + health.ToString("0") + " -> " + stats.Health.ToString("0") + " en " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");
        yield return CaptureFrom("07_ennemi", player.transform.position + new Vector3(4f, 3f, -4f), player.transform.position);

        inv.Equip(ItemType.Sword);
        int meat = inv.Count(ItemType.Meat);
        t0 = Time.realtimeSinceStartup;
        while (enemy && !enemy.IsDead && Time.realtimeSinceStartup - t0 < 8f)
        {
            stats.AddEnergy(100f);
            stats.Heal(100f);
            actions.DoAction();
            yield return Wait(0.5f);
        }
        Check("Tuer l'ennemi a l'epee", enemy == null || enemy.IsDead);
        Check("L'ennemi donne de la viande", inv.Count(ItemType.Meat) == meat + 2);
        yield return Wait(1.6f);
        Check("Le corps disparait", enemy == null);

        // --- Ennemi ne frappe pas a l'abri
        MoveTo(roofPos + Vector3.up * 0.05f, roofPos + Vector3.forward);
        yield return Wait(0.5f);
        NavMesh.SamplePosition(roofPos + Vector3.right * 2.2f, out hit, 3f, NavMesh.AllAreas);
        var e2 = gm.spawner.Spawn(hit.position, 60f);
        health = stats.Health;
        yield return Wait(3f);
        Check("A l'abri, l'ennemi n'attaque pas", stats.Health >= health, health.ToString("0") + " -> " + stats.Health.ToString("0"));
        gm.spawner.ClearAll();
        yield return null;

        // --- Nuit par le capteur de lumiere (simulation)
        var light = AmbientLightSensor.Instance;
        typeof(AmbientLightSensor).GetField("simulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(light, 1);
        yield return Wait(2f);
        Check("Capteur : piece sombre detectee", light.IsDarkRoom, light.Lux + " lux");
        t0 = Time.realtimeSinceStartup;
        while (!gm.dayNight.IsNight && Time.realtimeSinceStartup - t0 < 15f) yield return null;
        Check("Piece sombre -> la nuit tombe", gm.dayNight.IsNight, "heure " + gm.dayNight.ClockText() + " en " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");
        yield return Wait(3f);
        Check("La nuit reste tant que la piece est sombre", gm.dayNight.IsNight, gm.dayNight.ClockText());
        MoveTo(arena, arena + Vector3.forward);
        yield return Wait(0.3f);
        yield return Capture("08_nuit");
        typeof(AmbientLightSensor).GetField("simulation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(light, 0);

        // --- Spawner automatique
        gm.spawner.ClearAll();
        t0 = Time.realtimeSinceStartup;
        while (EnemyAI.All.Count == 0 && Time.realtimeSinceStartup - t0 < 12f) yield return null;
        Check("Le spawner cree des ennemis", EnemyAI.All.Count > 0, EnemyAI.All.Count + " ennemi(s)");

        // --- Podometre (simulation)
        stats.Eat(-50f);
        hunger = stats.Hunger;
        typeof(Pedometer).GetMethod("AddSteps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(Pedometer.Instance, new object[] { 10 });
        Check("10 pas reels remontent la faim", stats.Hunger > hunger + 1f, hunger.ToString("0.0") + " -> " + stats.Hunger.ToString("0.0"));

        // --- Pause
        gm.Pause();
        yield return null;
        Check("Pause arrete le temps", Time.timeScale == 0f && Find("PauseMenu") && Find("PauseMenu").activeInHierarchy);
        yield return Capture("09_pause");
        gm.Resume();
        yield return null;
        Check("Reprendre", gm.IsPlaying && Time.timeScale == 1f);

        // --- Sauvegarde et chargement
        stats.Load(77f, 66f, 55f, 44f);
        int wood = inv.Count(ItemType.Wood);
        int pieces = build.placed.Count;
        Vector3 pos = player.transform.position;
        int day = gm.dayNight.day;
        gm.SaveGame();
        Check("Fichier de sauvegarde ecrit", SaveSystem.HasSave);

        gm.QuitToMenu();
        yield return null;
        Check("Quitter -> menu", gm.State == GameState.Menu);
        yield return Capture("10_menu_continuer");

        // Continuer depuis le menu : on retrouve la partie sauvegardee
        gm.ContinueGame();
        yield return Wait(0.5f);
        Check("Charger : en jeu", gm.IsPlaying);
        Check("Charger : inventaire", inv.Count(ItemType.Wood) == wood, inv.Count(ItemType.Wood) + " / " + wood);
        Check("Charger : constructions", build.placed.Count == pieces, build.placed.Count + " / " + pieces);
        Check("Charger : position", Vector3.Distance(Flat(player.transform.position), Flat(pos)) < 1f, player.transform.position + " / " + pos);
        Check("Charger : vie", Mathf.Abs(stats.Health - 77f) < 2f, stats.Health.ToString("0.0"));
        Check("Charger : jour", gm.dayNight.day == day);

        gm.NewGame();
        yield return Wait(0.3f);
        Check("Nouvelle partie remet a zero", inv.Count(ItemType.Wood) == 0 && build.placed.Count == 0);

        // --- Mort
        stats.Damage(1000f);
        yield return null;
        Check("Mort -> ecran de mort", gm.State == GameState.Dead && Find("DeathMenu") && Find("DeathMenu").activeInHierarchy);
        yield return Capture("11_mort");
        gm.NewGame();
        yield return Wait(0.5f);
        Check("Rejouer apres la mort", gm.IsPlaying && stats.Health == stats.maxHealth);

        // Laisse tourner quelques secondes pour attraper les erreurs dans Update
        yield return Wait(3f);
    }

    IEnumerator Harvest(ResourceKind kind, ItemType item, int expected, string label)
    {
        var player = GameManager.Instance.player;
        var inv = GameManager.Instance.inventory;
        var stats = GameManager.Instance.stats;

        ResourceNode best = null;
        float bestDist = float.MaxValue;
        foreach (var n in ResourceNode.All)
        {
            if (n.kind != kind || !n.IsAvailable) continue;
            float d = Vector3.Distance(n.transform.position, player.transform.position);
            if (d < bestDist) { best = n; bestDist = d; }
        }
        if (!best)
        {
            Check(label, false, "aucune ressource trouvee");
            yield break;
        }

        Vector3 dir = Flat(player.transform.position - best.transform.position);
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
        MoveTo(best.transform.position + dir.normalized * 1.4f + Vector3.up * 0.05f, best.transform.position);
        yield return Wait(0.6f);

        stats.AddEnergy(100f);
        int before = inv.Count(item);
        player.GetComponent<PlayerActions>().DoAction();
        yield return Wait(0.5f);
        int got = inv.Count(item) - before;
        Check(label, got == expected, "+" + got + " (attendu +" + expected + ")");
    }

    void MoveTo(Vector3 position, Vector3 lookAt)
    {
        var gm = GameManager.Instance;
        Vector3 to = Flat(lookAt - position);
        float yaw = to.sqrMagnitude > 0.01f ? Quaternion.LookRotation(to).eulerAngles.y : 0f;
        gm.player.Teleport(position, yaw);
        gm.cam.Yaw = yaw;
    }

    bool Place(BuildPieceType type, Vector3 at)
    {
        // Verifie la detection "deja occupe" via la methode privee IsOccupied.
        var m = typeof(BuildSystem).GetMethod("IsOccupied", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return !(bool)m.Invoke(GameManager.Instance.build, new object[] { type, at });
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    static GameObject Find(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.name == name && t.gameObject.scene.IsValid()) return t.gameObject;
        return null;
    }

    IEnumerator Capture(string name) => CaptureFrom(name, null, Vector3.zero);

    // Rendu de la camera du jeu + UI dans un PNG (le mode batch n'a pas de Game View).
    IEnumerator CaptureFrom(string name, Vector3? camPos, Vector3 lookAt)
    {
        yield return null; // WaitForEndOfFrame ne se declenche pas en mode batch
        var cam = Camera.main;
        if (!cam) yield break;

        Vector3 oldPos = cam.transform.position;
        Quaternion oldRot = cam.transform.rotation;
        if (camPos.HasValue) cam.transform.SetPositionAndRotation(camPos.Value, Quaternion.LookRotation(lookAt + Vector3.up - camPos.Value));

        var canvas = FindAnyObjectByType<UIRoot>().GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = cam.nearClipPlane + 0.05f;
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(1280, 720, 24);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = old;
        Destroy(rt);
        Destroy(tex);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cam.transform.SetPositionAndRotation(oldPos, oldRot);
    }

    void Finish()
    {
        Check("Aucune erreur dans la console", errors.Count == 0, errors.Count + " erreur(s)");
        var text = new StringBuilder();
        text.AppendLine(failures == 0 ? "RESULTAT : TOUT EST OK" : "RESULTAT : " + failures + " ECHEC(S)");
        text.Append(report);
        if (errors.Count > 0)
        {
            text.AppendLine().AppendLine("=== Erreurs console ===");
            var seen = new HashSet<string>();
            foreach (var e in errors) if (seen.Add(e)) text.AppendLine(e);
        }
        File.WriteAllText(Path.Combine(outDir, "rapport.txt"), text.ToString());
        Debug.Log("PLAYTEST\n" + text);

        if (File.Exists(saveBackup)) File.Copy(saveBackup, savePath, true);
        else if (File.Exists(savePath)) File.Delete(savePath);
        if (File.Exists(saveBackup)) File.Delete(saveBackup);

        UnityEditor.EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
#endif
