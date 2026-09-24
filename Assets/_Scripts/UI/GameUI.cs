using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Runtime UI Toolkit screens. All actions go through the gameplay state machine.</summary>
public sealed class GameUI : MonoBehaviour
{
    private GameManager manager;
    private GameUISkin skin;
    private PanelSettings panel;
    private VisualElement stage;
    private readonly VisualElement[] letterbox = new VisualElement[4];
    private Label timer, timerHint;
    private readonly Label[] moveDigits = new Label[5];
    private VisualElement timerFill;
    private int page;
    private int lastWidth, lastHeight;
    public VisualElement Root => stage;
    public const int PageSize = 12;

    public void Initialize(GameManager game)
    {
        manager = game;
        skin = Resources.Load<GameUISkin>("GameUI/Skin");
        if (skin == null) { Debug.LogError("Game UI skin missing. Run Tools > Game UI > Configure UI assets."); return; }
        panel = Instantiate(Resources.Load<PanelSettings>("GameUI/Panel"));
        panel.scaleMode = PanelScaleMode.ConstantPixelSize;
        panel.sortingOrder = 100;
        var document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        var root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.styleSheets.Add(Resources.Load<StyleSheet>("GameUI/GameUI"));
        for (int i = 0; i < letterbox.Length; i++)
        {
            letterbox[i] = Box(root, 0, 0, 0, 0, "hud-bar");
            letterbox[i].pickingMode = PickingMode.Ignore;
        }
        stage = new VisualElement { name = "game-ui" };
        stage.AddToClassList("stage");
        stage.style.unityFontDefinition = FontDefinition.FromFont(skin.font);
        root.Add(stage);
        manager.StateChanged += Render;
        Resize();
    }

    private void Update()
    {
        if (stage == null) return;
        if (Screen.width != lastWidth || Screen.height != lastHeight) Resize();
        if (timer == null) return;
        UpdateHud();
    }

    private void UpdateHud()
    {
        float elapsed = manager.ElapsedSeconds;
        int rating = GameManager.StarsForTime(elapsed);
        float remaining = Mathf.Max(0, (rating == 3 ? 30 : 60) - elapsed);
        timer.text = rating == 1 ? FormatTime(elapsed) : FormatTime(Mathf.Ceil(remaining));
        timerHint.text = rating == 3 ? "三星倒计时" : rating == 2 ? "二星倒计时" : "已用时间 · 完成即得一星";
        timerFill.style.width = Length.Percent(rating == 1 ? 100 : remaining / 30f * 100);
        timerFill.style.backgroundColor = rating == 3 ? new Color(0.49f, 0.83f, 0.78f) : new Color(1, 0.72f, 0.28f);
        string digits = Mathf.Min(99999, manager.MoveCount).ToString("D5");
        for (int i = 0; i < moveDigits.Length; i++) if (moveDigits[i] != null) moveDigits[i].text = digits[i].ToString();
    }

    public static string FormatTime(float seconds)
    {
        int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (whole / 60).ToString("D2") + ":" + (whole % 60).ToString("D2");
    }

    private void Resize()
    {
        lastWidth = Screen.width; lastHeight = Screen.height;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
        panel.scale = Mathf.Max(0.1f, scale);
        float width = Screen.width / panel.scale, height = Screen.height / panel.scale;
        float x = Mathf.Max(0, (width - 1280) / 2), y = Mathf.Max(0, (height - 800) / 2);
        stage.style.left = x; stage.style.top = y;
        Position(letterbox[0], 0, 0, width, y);
        Position(letterbox[1], 0, y + 800, width, y);
        Position(letterbox[2], 0, y, x, 800);
        Position(letterbox[3], x + 1280, y, x, 800);
        SetCamera();
    }

    private void SetCamera()
    {
        if (Camera.main == null) return;
        bool gameplay = manager.gameState == GameState.Play || manager.gameState == GameState.Paused
            || manager.gameState == GameState.Win || manager.gameState == GameState.Complete;
        // Fit the map inside a dedicated rectangle, away from the HUD and controls.
        float scale = panel.scale;
        float x = (Screen.width - 1280 * scale) / 2;
        float y = (Screen.height - 800 * scale) / 2;
        Camera.main.rect = gameplay ? new Rect((x + 36 * scale) / Screen.width,
            (y + 92 * scale) / Screen.height, 1208 * scale / Screen.width, 578 * scale / Screen.height) : new Rect(0, 0, 1, 1);
    }

    private void Render()
    {
        if (stage == null) return;
        stage.Clear(); timer = null;
        stage.EnableInClassList("menu", manager.gameState == GameState.Home || manager.gameState == GameState.LevelSelect);
        stage.pickingMode = PickingMode.Ignore;
        SetCamera();
        if (manager.gameState == GameState.Home) Home();
        else if (manager.gameState == GameState.LevelSelect) Select();
        else
        {
            Hud();
            if (manager.gameState == GameState.Win || manager.gameState == GameState.Complete) Results();
            else if (manager.gameState == GameState.Paused) PausePanel();
        }
    }

    private void Home()
    {
        Text(stage, "LOVE QiE  /  同行计划", 70, 40, 600, 32, "eyebrow");
        Art(stage, skin.success, 50, 170, 710, 452);
        Text(stage, "每一段旅程，都需要默契。", 90, 637, 650, 38, "muted");
        Text(stage, "双车同行", 795, 190, 425, 85, "title");
        Text(stage, "同步出发，一起到站", 799, 295, 420, 40, "subtitle");
        Text(stage, "控制两辆小车，穿过城市街道。\n让它们同时抵达终点，收集通关星星。", 800, 351, 410, 70, "body");
        var start = Button(stage, "", 925, 440, 150, 140, () => manager.ShowLevelSelect(), "art-button", "start-game");
        start.style.backgroundImage = skin.play;
        Button(stage, "开始游戏", 822, 593, 365, 64, () => manager.ShowLevelSelect(), "primary", "start-label");
        Text(stage, "99 个关卡  /  3 星挑战", 833, 683, 345, 30, "muted center");
        Footer();
    }

    private void Select()
    {
        Text(stage, "选择关卡", 66, 43, 570, 62, "heading");
        int total = 0;
        for (int i = 1; i <= GameManager.MaxScene; i++) total += manager.BestStars(i);
        Text(stage, "自由选择路线 · 最佳成绩自动保存", 70, 113, 700, 32, "muted");
        Art(stage, skin.star, 906, 59, 35, 35);
        Text(stage, total + " / 297", 952, 58, 160, 35, "subtitle");
        IconButton(stage, skin.home, 1150, 40, 68, () => manager.ShowHome(), "返回首页", "select-home");
        int first = page * PageSize + 1;
        for (int i = 0; i < PageSize && first + i <= GameManager.MaxScene; i++)
        {
            int level = first + i;
            int best = manager.BestStars(level);
            float x = 85 + (i % 6) * 188, y = 179 + (i / 6) * 238;
            var card = Button(stage, "", x, y, 169, 216, () => manager.LoadLevel(level), "level-card", "level-" + level);
            card.tooltip = "第 " + level + " 关 · " + (best == 0 ? "尚未通关" : "最佳 " + best + " 星");
            Art(card, skin.levels[(level - 1) % skin.levels.Length], 20, 0, 129, 164);
            // The supplied train icons include baked numbers/stars; replace these with live values.
            var number = Text(card, level.ToString("D2"), 55, 45, 62, 62, "level-number");
            number.pickingMode = PickingMode.Ignore;
            var score = Box(card, 32, 109, 108, 42, "score-plaque");
            Stars(score, best, 6, 6, 28, 7);
            Text(card, best == 0 ? "第 " + level.ToString("D2") + " 关" : "已完成 · " + best + " 星", 0, 173, 169, 30, "card-caption");
        }
        var prev = Button(stage, "上一页", 390, 678, 145, 48, () => { page--; Render(); }, "secondary", "previous-page");
        prev.SetEnabled(page > 0);
        Text(stage, (page + 1) + " / " + Mathf.CeilToInt(GameManager.MaxScene / (float)PageSize), 555, 683, 170, 40, "subtitle center");
        var next = Button(stage, "下一页", 745, 678, 145, 48, () => { page++; Render(); }, "secondary", "next-page");
        next.SetEnabled(first + PageSize <= GameManager.MaxScene);
        Footer();
    }

    private void Hud()
    {
        var bar = Box(stage, 0, 0, 1280, 126, "hud-bar");
        Text(bar, "同行 / " + manager.CurrentScene.ToString("D2"), 40, 25, 245, 40, "subtitle");
        Text(bar, "第 " + manager.CurrentScene + " 关，共 99 关", 42, 72, 220, 30, "muted");
        Art(bar, skin.time, 353, 7, 230, 115);
        timer = Text(bar, "00:30", 454, 39, 104, 47, "timer");
        timerHint = Text(bar, "三星倒计时", 614, 21, 300, 32, "subtitle");
        var track = Box(bar, 616, 65, 224, 8, "timer-track");
        timerFill = Box(track, 0, 0, 224, 8, "timer-fill");
        Text(bar, "30 秒三星 / 60 秒二星 / 完成一星", 614, 87, 400, 25, "tiny");
        IconButton(bar, skin.home, 1080, 25, 65, () => manager.ShowLevelSelect(), "关卡选择", "hud-levels");
        IconButton(bar, skin.pause, 1171, 26, 63, () => manager.Pause(), "暂停", "pause-game");
        var bottom = Box(stage, 0, 708, 1280, 92, "hud-bar");
        Art(bottom, skin.steps, 26, 4, 190, 85);
        for (int i = 0; i < moveDigits.Length; i++)
            moveDigits[i] = Text(bottom, "0", 99 + i * 20.4f, 40, 17, 23, "steps");
        Text(bottom, "WASD / 方向键移动 · 左右镜像 · R 重开", 244, 14, 615, 28, "muted");
        Text(bottom, "两辆小车同时到达顶部终点即可过关", 244, 48, 565, 26, "tiny");
        Button(bottom, "重开", 797, 23, 90, 48, () => manager.LoadLevel(manager.CurrentScene), "secondary", "restart-level");
        string[] captions = { "←", "↑", "↓", "→" };
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.up, Vector2Int.down, Vector2Int.right };
        for (int i = 0; i < 4; i++)
        {
            Vector2Int direction = directions[i];
            Button(bottom, captions[i], 924 + i * 76, 23, 62, 48, () => manager.TryMove(direction), "direction", "move-" + i);
        }
        UpdateHud();
    }

    private void Results()
    {
        var overlay = Box(stage, 0, 0, 1280, 800, "overlay");
        var modal = Box(overlay, 362, 51, 556, 698, "modal");
        modal.name = "results-dialog";
        Art(modal, skin.success, 19, 14, 518, 330);
        Text(modal, manager.gameState == GameState.Complete ? "全程通关！" : "顺利到站！", 24, 335, 508, 62, "heading center");
        Stars(modal, manager.EarnedStars, 148, 405, 76, 16);
        Text(modal, "第 " + manager.CurrentScene.ToString("D2") + " 关  ·  用时 " + manager.ElapsedSeconds.ToString("F1") + " 秒  ·  " + manager.MoveCount + " 步", 20, 496, 516, 34, "body center");
        Text(modal, "≤ 30 秒三星    ≤ 60 秒二星    超过 60 秒一星", 20, 537, 516, 28, "tiny center");
        Button(modal, manager.gameState == GameState.Complete ? "返回关卡" : "下一关", 59, 583, 438, 56,
            () => { if (manager.gameState == GameState.Complete) manager.ShowLevelSelect(); else manager.NextLevel(); }, "primary", "next-level");
        Button(modal, "再挑战一次", 69, 648, 190, 36, () => manager.LoadLevel(manager.CurrentScene), "text-button", "retry-level");
        Button(modal, "关卡选择", 295, 648, 190, 36, () => manager.ShowLevelSelect(), "text-button", "results-levels");
    }

    private void PausePanel()
    {
        var overlay = Box(stage, 0, 0, 1280, 800, "overlay");
        var modal = Box(overlay, 420, 164, 440, 472, "modal");
        modal.name = "pause-dialog";
        Art(modal, skin.pause, 177, 30, 86, 86);
        Text(modal, "休息一下", 20, 140, 400, 60, "heading center");
        Text(modal, "计时已暂停，旅程等你继续", 20, 215, 400, 38, "muted center");
        Button(modal, "继续游戏", 55, 286, 330, 58, () => manager.Resume(), "primary", "resume-game");
        Button(modal, "关卡选择", 55, 366, 330, 52, () => manager.ShowLevelSelect(), "secondary", "pause-levels");
    }

    private void Footer() => Text(stage, "30 秒内 ★★★     60 秒内 ★★     完成关卡 ★", 70, 754, 1140, 26, "tiny center");

    private void Stars(VisualElement parent, int count, float x, float y, float size, float gap)
    {
        for (int i = 0; i < 3; i++)
        {
            var star = Art(parent, skin.star, x + i * (size + gap), y, size, size);
            star.name = "rating-star-" + (i + 1);
            if (i >= count) star.style.unityBackgroundImageTintColor = new Color(0.23f, 0.28f, 0.34f);
        }
    }

    private void IconButton(VisualElement parent, Texture2D texture, float x, float y, float size, Action action, string tooltip, string name)
    {
        var button = Button(parent, "", x, y, size, size, action, "art-button", name);
        button.style.backgroundImage = texture;
        button.tooltip = tooltip;
    }
    private static void Position(VisualElement element, float x, float y, float width, float height)
    {
        element.style.position = UnityEngine.UIElements.Position.Absolute;
        element.style.left = x; element.style.top = y;
        element.style.width = width; element.style.height = height;
    }
    private static void Classes(VisualElement element, string classes)
    {
        foreach (string value in classes.Split(' ')) element.AddToClassList(value);
    }
    private static VisualElement Box(VisualElement parent, float x, float y, float width, float height, string classes)
    {
        var element = new VisualElement(); Position(element, x, y, width, height); Classes(element, classes); parent.Add(element); return element;
    }
    private static Label Text(VisualElement parent, string text, float x, float y, float width, float height, string classes)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        Position(label, x, y, width, height); Classes(label, classes); parent.Add(label); return label;
    }
    private static VisualElement Art(VisualElement parent, Texture2D texture, float x, float y, float width, float height)
    {
        var element = Box(parent, x, y, width, height, "art");
        element.style.backgroundImage = texture;
        element.pickingMode = PickingMode.Ignore;
        return element;
    }
    private static Button Button(VisualElement parent, string caption, float x, float y, float width, float height, Action action, string classes, string name)
    {
        var button = new Button(action) { text = caption, name = name };
        Position(button, x, y, width, height); Classes(button, classes); parent.Add(button); return button;
    }
    private void OnDestroy()
    {
        if (manager != null) manager.StateChanged -= Render;
        if (panel != null) Destroy(panel);
        if (Camera.main != null) Camera.main.rect = new Rect(0, 0, 1, 1);
    }
}
