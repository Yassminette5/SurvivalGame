using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using UnityEngine.UI;

// P5 - Menu principal, pause, ecran de mort, et affichage du jour / de l'heure.
public class MenuUI : MonoBehaviour
{
    RectTransform mainMenu;
    RectTransform pauseMenu;
    RectTransform deathMenu;
    Text clock;

    GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        var menus = UIRoot.Layer(UILayer.Menus);

        UIRoot.AddTopButton("II", gm.Pause, 110f);

        // Sous les barres de vie (en haut au centre, le texte passait sous les boutons sur un ecran 16:9).
        clock = UIFactory.MakeText(UIRoot.Layer(UILayer.Hud), "", 28, TextAnchor.UpperLeft);
        UIFactory.Place(clock.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -320f), new Vector2(700f, 120f));

        mainMenu = MakeFullScreen(menus, "MainMenu");
        pauseMenu = MakeFullScreen(menus, "PauseMenu");
        deathMenu = MakeFullScreen(menus, "DeathMenu");

        gm.StateChanged += OnStateChanged;
        OnStateChanged(gm.State);
    }

    void OnDestroy()
    {
        if (gm) gm.StateChanged -= OnStateChanged;
    }

    static RectTransform MakeFullScreen(RectTransform parent, string name)
    {
        var bg = UIFactory.MakeImage(parent, name, new Color(0f, 0f, 0f, 0.7f), true);
        UIFactory.Stretch(bg.rectTransform);
        var panel = UIFactory.MakePanel(bg.transform, "Panel", new Vector2(900f, 600f));
        panel.sizeDelta = new Vector2(900f, panel.sizeDelta.y);
        return panel;
    }

    static void Show(RectTransform panel, bool show) => panel.parent.gameObject.SetActive(show);

    void OnStateChanged(GameState state)
    {
        bool playing = state == GameState.Playing;
        UIRoot.Layer(UILayer.Controls).gameObject.SetActive(playing);
        UIRoot.Layer(UILayer.Hud).gameObject.SetActive(playing);
        UIRoot.Layer(UILayer.Panels).gameObject.SetActive(playing);

        Show(mainMenu, state == GameState.Menu);
        Show(pauseMenu, state == GameState.Paused);
        Show(deathMenu, state == GameState.Dead);

        if (state == GameState.Menu) BuildMain();
        if (state == GameState.Paused) BuildPause();
        if (state == GameState.Dead) BuildDeath();
    }

    void BuildMain()
    {
        UIFactory.ClearChildren(mainMenu);
        UIFactory.AddRowText(mainMenu, "SURVIE", 80, 120f, TextAnchor.MiddleCenter);
        UIFactory.AddRowButton(mainMenu, "Nouvelle partie", gm.NewGame, 100f, UIFactory.AccentColor);
        if (SaveSystem.HasSave)
            UIFactory.AddRowButton(mainMenu, "Continuer  (" + SaveSystem.SavedAt() + ")", gm.ContinueGame, 100f);
        UIFactory.AddRowText(mainMenu, SensorReport(), 24, 200f);
    }

    void BuildPause()
    {
        UIFactory.ClearChildren(pauseMenu);
        UIFactory.AddRowText(pauseMenu, "Pause", 60, 90f, TextAnchor.MiddleCenter);
        UIFactory.AddRowButton(pauseMenu, "Reprendre", gm.Resume, 90f, UIFactory.AccentColor);
        UIFactory.AddRowButton(pauseMenu, "Sauvegarder", gm.SaveGame, 90f);

        var gyro = GyroCameraInput.Instance;
        if (gyro)
        {
            UIFactory.AddRowButton(pauseMenu, "Gyroscope : " + (gyro.useGyro ? "ON" : "OFF"), () =>
            {
                gyro.useGyro = !gyro.useGyro;
                BuildPause();
            }, 80f);
            UIFactory.AddRowButton(pauseMenu, "Inverser gyroscope (gauche/droite)", () =>
            {
                gyro.invertYaw = !gyro.invertYaw;
                UIRoot.Toast("Gyroscope inverse : " + (gyro.invertYaw ? "oui" : "non"));
            }, 80f);
        }
        UIFactory.AddRowButton(pauseMenu, "Sauvegarder et quitter", gm.QuitToMenu, 90f);
    }

    void BuildDeath()
    {
        UIFactory.ClearChildren(deathMenu);
        UIFactory.AddRowText(deathMenu, "Tu es mort...", 64, 100f, TextAnchor.MiddleCenter);
        if (SaveSystem.HasSave)
            UIFactory.AddRowButton(deathMenu, "Recharger la sauvegarde", gm.ContinueGame, 100f, UIFactory.AccentColor);
        UIFactory.AddRowButton(deathMenu, "Nouvelle partie", gm.NewGame, 100f);
    }

    static string SensorReport()
    {
        string Line(string name, bool ok) => (ok ? "[OK] " : "[--] ") + name;
        return "Capteurs detectes :\n"
            + Line("Gyroscope (P1)", SensorUtil.IsOn(Gyroscope.current)) + "     "
            + Line("Proximite (P2)", SensorUtil.IsOn(ProximitySensor.current)) + "\n"
            + Line("Magnetometre (P3)", SensorUtil.IsOn(MagneticFieldSensor.current)) + "     "
            + Line("Accelerometre (P4)", SensorUtil.IsOn(Accelerometer.current)) + "\n"
            + Line("Lumiere (P5)", SensorUtil.IsOn(LightSensor.current)) + "     "
            + Line("Podometre (bonus)", SensorUtil.IsOn(StepCounter.current));
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame)
        {
            if (gm.State == GameState.Playing) gm.Pause();
            else if (gm.State == GameState.Paused) gm.Resume();
        }

        var cycle = DayNightCycle.Instance;
        if (gm.State != GameState.Playing || !cycle) return;

        string text = "Jour " + cycle.day + "  -  " + cycle.ClockText() + (cycle.IsNight ? "  (nuit)" : "");
        var light = AmbientLightSensor.Instance;
        if (light && light.Lux >= 0f) text += "\nLumiere reelle : " + Mathf.RoundToInt(light.Lux) + " lux" + (light.IsDarkRoom ? " - piece sombre" : "");
        var steps = Pedometer.Instance;
        if (steps && steps.SessionSteps > 0) text += "\nPas reels : " + steps.SessionSteps;
        clock.text = text;
    }
}
