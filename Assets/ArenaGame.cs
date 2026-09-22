using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ArenaGame : MonoBehaviour
{
    const float MapW = 913f, MapH = 1723f;
    const float MatchLength = 180f;
    static readonly string[] HeroNames = { "BRUTUS", "LYRA", "NIX", "SOL" };
    static readonly string[] HeroFiles = { "heroes_brutus", "heroes_lyra", "heroes_nix", "heroes_sol" };
    static readonly Color Gold = new Color(.99f, .76f, .33f);
    static readonly Color Blue = new Color(.25f, .75f, 1f);
    static readonly Color Red = new Color(1f, .36f, .35f);
    static readonly Color Ink = new Color(.055f, .10f, .16f);

    class Actor
    {
        public Vector2 p;
        public int team;
        public float hp, maxHp, speed, damage, range, attackAt;
        public string sprite;
        public bool hero;
    }

    class Fort
    {
        public Vector2 p;
        public int team;
        public float hp, maxHp, attackAt;
        public bool core;
    }

    readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
    readonly List<Actor> actors = new List<Actor>();
    readonly List<Fort> forts = new List<Fort>();
    Texture2D pixel, circle;
    GUIStyle titleStyle, labelStyle, smallStyle, buttonStyle, controlStyle, centerStyle;
    Rect mapRect, stickRect;
    Actor player, enemyHero;
    int screen = 0, selectedHero = 0, result = 0;
    float matchTime, nextWave, nextEnemySpawn, dashReady, skillReady, attackReady, noticeUntil;
    string notice = "";
    Vector2 stick;
    bool stickHeld, paused;
    int waveCount;

    void Awake()
    {
        Application.targetFrameRate = 60;
        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
            circle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31f - d)));
        }
        circle.Apply();
        string[] names = {
            "map", "heroes_brutus", "heroes_lyra", "heroes_nix", "heroes_sol",
            "structures_tower_blue", "structures_tower_red", "structures_base_blue",
            "structures_base_red", "dragon_dragon", "skills_aa",
            "skills_investida_brutus", "skills_escudo_bumerangue"
        };
        foreach (string n in names) art[n] = Resources.Load<Texture2D>("Art/" + n);
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-mvp-capture") StartCoroutine(CaptureScreens(args[i + 1]));
    }

    IEnumerator CaptureScreens(string folder)
    {
        Directory.CreateDirectory(folder);
        yield return new WaitForSeconds(1f);
        yield return CaptureFrame(Path.Combine(folder, "menu.png"));
        yield return new WaitForSeconds(1f);
        screen = 1;
        yield return new WaitForSeconds(.5f);
        yield return CaptureFrame(Path.Combine(folder, "selecao.png"));
        yield return new WaitForSeconds(1f);
        StartMatch();
        yield return new WaitForSeconds(1.5f);
        yield return CaptureFrame(Path.Combine(folder, "partida.png"));
        float before = forts[3].hp;
        player.p = forts[3].p + new Vector2(0, 75);
        BasicAttack();
        Debug.Log("MVP_COMBAT_TEST=" + (forts[3].hp < before ? "Passed" : "Failed"));
        forts[1].hp = 0;
        End(1);
        yield return new WaitForSeconds(.5f);
        yield return CaptureFrame(Path.Combine(folder, "vitoria.png"));
        yield return new WaitForSeconds(1f);
        Application.Quit();
    }

    IEnumerator CaptureFrame(string path)
    {
        yield return new WaitForEndOfFrame();
        var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Destroy(image);
    }

    void StartMatch()
    {
        actors.Clear();
        forts.Clear();
        matchTime = MatchLength;
        nextWave = 2f;
        nextEnemySpawn = 0f;
        dashReady = skillReady = attackReady = 0;
        waveCount = 0;
        result = 0;
        paused = false;
        player = SpawnHero(0, selectedHero, new Vector2(205, 1350));
        enemyHero = SpawnHero(1, 0, new Vector2(710, 340));
        forts.Add(new Fort { p = new Vector2(455, 1505), team = 0, hp = 1800, maxHp = 1800, core = true });
        forts.Add(new Fort { p = new Vector2(455, 200), team = 1, hp = 1800, maxHp = 1800, core = true });
        foreach (float x in new[] { 205f, 705f })
        {
            forts.Add(new Fort { p = new Vector2(x, 1420), team = 0, hp = 720, maxHp = 720 });
            forts.Add(new Fort { p = new Vector2(x, 280), team = 1, hp = 720, maxHp = 720 });
        }
        screen = 2;
        Say("Destrua o núcleo vermelho!");
    }

    Actor SpawnHero(int team, int type, Vector2 p)
    {
        var a = new Actor {
            p = p, team = team, hero = true, maxHp = type == 0 ? 780 : 600,
            speed = type == 2 ? 170 : 145, damage = type == 1 ? 85 : 75,
            range = type == 1 || type == 3 ? 155 : 95, sprite = HeroFiles[type]
        };
        a.hp = a.maxHp;
        actors.Add(a);
        return a;
    }

    void SpawnWave()
    {
        waveCount++;
        foreach (int team in new[] { 0, 1 })
        foreach (float lane in new[] { 205f, 705f })
        for (int i = 0; i < 2; i++)
        {
            var a = new Actor {
                p = new Vector2(lane + (i == 0 ? -16 : 16), team == 0 ? 1280 + i * 38 : 425 - i * 38),
                team = team, maxHp = 150, hp = 150, speed = 56, damage = 24, range = 65,
                sprite = team == 0 ? "heroes_brutus" : "heroes_nix"
            };
            actors.Add(a);
        }
        Say("Onda " + waveCount);
    }

    void Update()
    {
        if (screen != 2 || paused) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        matchTime -= dt;
        ReadMovement();
        if (player != null && player.hp > 0)
        {
            Vector2 next = player.p + stick * player.speed * dt;
            if (Walkable(next)) player.p = next;
            if (Input.GetKeyDown(KeyCode.Space)) BasicAttack();
            if (Input.GetKeyDown(KeyCode.Q)) Dash();
            if (Input.GetKeyDown(KeyCode.E)) Skill();
        }
        else if (player != null && matchTime < nextEnemySpawn)
        {
            player.hp = player.maxHp;
            player.p = new Vector2(205, 1350);
            Say("Você voltou à arena");
        }
        if (Input.GetKeyDown(KeyCode.Escape)) paused = !paused;
        nextWave -= dt;
        if (nextWave <= 0) { SpawnWave(); nextWave = 11; }
        UpdateActors(dt);
        UpdateForts();
        actors.RemoveAll(a => a.hp <= 0 && !a.hero);
        if (enemyHero != null && enemyHero.hp <= 0)
        {
            actors.Remove(enemyHero);
            enemyHero = null;
            nextEnemySpawn = matchTime - 12;
        }
        else if (enemyHero == null && matchTime <= nextEnemySpawn)
            enemyHero = SpawnHero(1, 0, new Vector2(710, 340));
        if (forts[0].hp <= 0) End(2);
        else if (forts[1].hp <= 0) End(1);
        else if (matchTime <= 0) End(forts[1].hp < forts[0].hp ? 1 : 2);
    }

    bool Walkable(Vector2 p)
    {
        if (p.x < 105 || p.x > 807 || p.y < 95 || p.y > 1620) return false;
        if (p.y > 640 && p.y < 1080 && p.x > 260 && p.x < 650) return false;
        return true;
    }

    void ReadMovement()
    {
        stick = Vector2.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) stick.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) stick.x += 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) stick.y -= 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) stick.y += 1;
        stick = Vector2.ClampMagnitude(stick, 1);
        stickHeld = false;
        foreach (Touch t in Input.touches)
        {
            Vector2 q = new Vector2(t.position.x, Screen.height - t.position.y);
            if (stickRect.Contains(q) && t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled)
            {
                stick = Vector2.ClampMagnitude((q - stickRect.center) / (stickRect.width * .33f), 1);
                stickHeld = true;
                break;
            }
        }
        if (!stickHeld && Input.GetMouseButton(0))
        {
            Vector2 q = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (stickRect.Contains(q))
            {
                stick = Vector2.ClampMagnitude((q - stickRect.center) / (stickRect.width * .33f), 1);
                stickHeld = true;
            }
        }
    }

    void UpdateActors(float dt)
    {
        foreach (Actor a in actors)
        {
            if (a.hp <= 0 || a == player) continue;
            Actor target = ClosestEnemy(a.p, a.team, 145);
            Fort tower = ClosestFort(a.p, a.team, 110);
            if (target != null && Vector2.Distance(a.p, target.p) <= a.range)
            {
                if (Time.time >= a.attackAt) { target.hp -= a.damage; a.attackAt = Time.time + (a.hero ? 1.1f : 1.5f); }
                continue;
            }
            if (tower != null && Vector2.Distance(a.p, tower.p) <= a.range)
            {
                if (Time.time >= a.attackAt) { tower.hp -= a.damage; a.attackAt = Time.time + (a.hero ? 1.1f : 1.5f); }
                continue;
            }
            Vector2 goal = a.hero && player.hp > 0 && Vector2.Distance(a.p, player.p) < 280
                ? player.p : new Vector2(a.p.x < 450 ? 205 : 705, a.team == 0 ? 220 : 1500);
            Vector2 next = a.p + (goal - a.p).normalized * a.speed * dt;
            if (Walkable(next)) a.p = next;
        }
        if (player.hp <= 0 && nextEnemySpawn >= matchTime) nextEnemySpawn = matchTime - 7;
    }

    void UpdateForts()
    {
        foreach (Fort f in forts)
        {
            if (f.hp <= 0 || Time.time < f.attackAt) continue;
            Actor target = ClosestEnemy(f.p, f.team, f.core ? 135 : 165);
            if (target != null)
            {
                target.hp -= f.core ? 42 : 55;
                f.attackAt = Time.time + 1.35f;
            }
        }
    }

    Actor ClosestEnemy(Vector2 p, int team, float radius)
    {
        Actor best = null;
        float d = radius;
        foreach (Actor a in actors)
        {
            if (a.team == team || a.hp <= 0) continue;
            float n = Vector2.Distance(p, a.p);
            if (n < d) { best = a; d = n; }
        }
        return best;
    }

    Fort ClosestFort(Vector2 p, int team, float radius)
    {
        Fort best = null;
        float d = radius;
        foreach (Fort f in forts)
        {
            if (f.team == team || f.hp <= 0) continue;
            float n = Vector2.Distance(p, f.p);
            if (n < d) { best = f; d = n; }
        }
        return best;
    }

    void BasicAttack()
    {
        if (player.hp <= 0 || Time.time < attackReady) return;
        attackReady = Time.time + .55f;
        Actor target = ClosestEnemy(player.p, 0, player.range + 35);
        if (target != null) { target.hp -= player.damage; Say("Acerto!"); return; }
        Fort fort = ClosestFort(player.p, 0, player.range + 45);
        if (fort != null) { fort.hp -= player.damage; Say("Torre atingida!"); }
    }

    void Dash()
    {
        if (player.hp <= 0 || Time.time < dashReady) return;
        dashReady = Time.time + 7;
        Vector2 dir = stick.sqrMagnitude > .05f ? stick.normalized : Vector2.up;
        Vector2 dest = player.p;
        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = dest + dir * 22;
            if (!Walkable(candidate)) break;
            dest = candidate;
        }
        player.p = dest;
        foreach (Actor a in actors)
            if (a.team == 1 && a.hp > 0 && Vector2.Distance(a.p, player.p) < 115) a.hp -= 95;
        Say("Investida!");
    }

    void Skill()
    {
        if (player.hp <= 0 || Time.time < skillReady) return;
        skillReady = Time.time + 10;
        if (selectedHero == 3)
        {
            player.hp = Mathf.Min(player.maxHp, player.hp + 210);
            foreach (Actor a in actors) if (a.team == 0 && Vector2.Distance(a.p, player.p) < 150)
                a.hp = Mathf.Min(a.maxHp, a.hp + 100);
            Say("Cura radiante!");
            return;
        }
        float radius = selectedHero == 1 ? 230 : 170;
        foreach (Actor a in actors) if (a.team == 1 && a.hp > 0 && Vector2.Distance(a.p, player.p) < radius)
            a.hp -= selectedHero == 1 ? 145 : 170;
        foreach (Fort f in forts) if (f.team == 1 && f.hp > 0 && Vector2.Distance(f.p, player.p) < radius)
            f.hp -= 95;
        Say(selectedHero == 0 ? "Escudo retornante!" : selectedHero == 1 ? "Chuva de flechas!" : "Passo sombrio!");
    }

    void End(int winner) { result = winner; screen = 3; }
    void Say(string message) { notice = message; noticeUntil = Time.time + 2; }

    void OnGUI()
    {
        MakeStyles();
        if (screen == 0) DrawHome();
        else if (screen == 1) DrawSelection();
        else if (screen == 2) DrawMatch();
        else DrawResult();
    }

    void MakeStyles()
    {
        int unit = Mathf.RoundToInt(Mathf.Min(Screen.width / 540f, Screen.height / 960f) * 22);
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(25, unit * 2), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(16, unit), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
        centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter };
        smallStyle = new GUIStyle(labelStyle) { fontSize = Mathf.Max(12, unit * 3 / 4) };
        buttonStyle = new GUIStyle(centerStyle) { fontSize = Mathf.Max(16, unit), wordWrap = true };
        controlStyle = new GUIStyle(centerStyle) { fontSize = Mathf.Max(13, unit * 2 / 3), wordWrap = false };
    }

    void Fill(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, pixel); GUI.color = Color.white; }
    void Circle(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, circle); GUI.color = Color.white; }
    void Tex(string name, Rect r, Color tint)
    {
        if (!art.TryGetValue(name, out Texture2D t) || t == null) return;
        GUI.color = tint;
        GUI.DrawTexture(r, t, ScaleMode.ScaleToFit, true);
        GUI.color = Color.white;
    }
    bool Button(Rect r, string text, Color bg)
    {
        Fill(r, bg);
        Fill(new Rect(r.x, r.y, r.width, 2), Gold);
        GUI.Label(r, text, buttonStyle);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
    void DrawHome()
    {
        Rect full = new Rect(0, 0, Screen.width, Screen.height);
        Tex("map", full, Color.white);
        Fill(full, new Color(.03f, .08f, .13f, .72f));
        float w = Screen.width, h = Screen.height;
        GUI.Label(new Rect(w * .05f, h * .09f, w * .9f, h * .12f), "ARENA\nFRENÉTICA", titleStyle);
        GUI.Label(new Rect(w * .1f, h * .25f, w * .8f, h * .07f), "Heróis • torres • partidas rápidas", centerStyle);
        Tex("heroes_brutus", new Rect(w * .22f, h * .34f, w * .56f, h * .26f), Color.white);
        if (Button(new Rect(w * .12f, h * .69f, w * .76f, h * .075f), "JOGAR", new Color(.14f, .45f, .66f))) screen = 1;
        GUI.Label(new Rect(w * .1f, h * .81f, w * .8f, h * .08f), "Versão inicial • partida contra bot", centerStyle);
    }
    void DrawSelection()
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), Ink);
        float w = Screen.width, h = Screen.height;
        GUI.Label(new Rect(w * .05f, h * .045f, w * .9f, h * .09f), "ESCOLHA SEU HERÓI", titleStyle);
        float cardW = w * .42f, cardH = h * .28f;
        for (int i = 0; i < 4; i++)
        {
            float x = w * (.06f + (i % 2) * .46f), y = h * (.16f + (i / 2) * .31f);
            Rect card = new Rect(x, y, cardW, cardH);
            Fill(card, i == selectedHero ? new Color(.14f, .37f, .55f) : new Color(.11f, .19f, .28f));
            Tex(HeroFiles[i], new Rect(x + cardW * .1f, y + 8, cardW * .8f, cardH * .73f), Color.white);
            GUI.Label(new Rect(x, y + cardH * .77f, cardW, cardH * .18f), HeroNames[i], centerStyle);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none)) selectedHero = i;
        }
        if (Button(new Rect(w * .12f, h * .82f, w * .76f, h * .075f), "ENTRAR NA ARENA", new Color(.14f, .45f, .66f))) StartMatch();
        if (GUI.Button(new Rect(w * .06f, h * .92f, w * .3f, h * .055f), "Voltar", buttonStyle)) screen = 0;
    }
    void DrawMatch()
    {
        float w = Screen.width, h = Screen.height;
        float header = h * .075f, footer = h * .15f;
        float mapH = h - header - footer;
        float mapW = Mathf.Min(w, mapH * MapW / MapH);
        mapRect = new Rect((w - mapW) / 2, header, mapW, mapH);
        Fill(new Rect(0, 0, w, h), Ink);
        Tex("map", mapRect, Color.white);
        foreach (Fort f in forts) DrawFort(f);
        foreach (Actor a in actors) if (a.hp > 0) DrawActor(a);
        Fill(new Rect(0, 0, w, header), new Color(.04f, .10f, .15f, .96f));
        GUI.Label(new Rect(w * .04f, 0, w * .5f, header), "ARENA FRENÉTICA", labelStyle);
        GUI.Label(new Rect(w * .59f, 0, w * .23f, header), Mathf.FloorToInt(Mathf.Max(0, matchTime) / 60) + ":" + Mathf.FloorToInt(Mathf.Max(0, matchTime) % 60).ToString("00"), centerStyle);
        if (GUI.Button(new Rect(w * .88f, 0, w * .1f, header), "Ⅱ", buttonStyle)) paused = true;
        if (Time.time < noticeUntil)
        {
            Rect nr = new Rect(w * .2f, header + 8, w * .6f, h * .045f);
            Fill(nr, new Color(.06f, .12f, .18f, .78f));
            GUI.Label(nr, notice, centerStyle);
        }
        Fill(new Rect(0, h - footer, w, footer), new Color(.04f, .10f, .16f, .97f));
        float s = Mathf.Min(footer * .76f, w * .22f);
        stickRect = new Rect(w * .045f, h - footer + (footer - s) / 2, s, s);
        Circle(stickRect, new Color(.12f, .27f, .38f));
        Rect knob = new Rect(stickRect.center.x - s * .17f + stick.x * s * .27f, stickRect.center.y - s * .17f + stick.y * s * .27f, s * .34f, s * .34f);
        Circle(knob, Blue);
        float bw = w * .20f, bh = footer * .60f, by = h - footer + (footer - bh) / 2;
        if (ControlButton(new Rect(w * .36f, by, bw, bh), Time.time < dashReady ? Mathf.CeilToInt(dashReady - Time.time).ToString() : "AVANÇO", new Color(.12f, .34f, .45f))) Dash();
        if (ControlButton(new Rect(w * .58f, by, bw, bh), Time.time < skillReady ? Mathf.CeilToInt(skillReady - Time.time).ToString() : "PODER", new Color(.22f, .29f, .49f))) Skill();
        if (ControlButton(new Rect(w * .80f, by, bw * .87f, bh), "ATAQUE", new Color(.58f, .30f, .20f))) BasicAttack();
        if (paused) DrawPause();
    }
    bool ControlButton(Rect r, string text, Color bg)
    {
        Fill(r, bg);
        Fill(new Rect(r.x, r.y, r.width, 2), Gold);
        GUI.Label(r, text, controlStyle);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }
    Rect MapBox(Vector2 p, float width, float height)
    {
        float scale = mapRect.height / MapH;
        return new Rect(mapRect.x + (p.x - width * .5f) * scale, mapRect.y + (p.y - height) * scale, width * scale, height * scale);
    }
    void DrawFort(Fort f)
    {
        if (f.hp <= 0) return;
        Tex(f.core ? (f.team == 0 ? "structures_base_blue" : "structures_base_red")
                : (f.team == 0 ? "structures_tower_blue" : "structures_tower_red"),
            MapBox(f.p, f.core ? 185 : 112, f.core ? 170 : 132), Color.white);
        DrawBar(f.p + new Vector2(0, f.core ? -150 : -112), 105, f.hp / f.maxHp, f.team == 0 ? Blue : Red);
    }
    void DrawActor(Actor a)
    {
        float size = a.hero ? 112 : 58;
        Tex(a.sprite, MapBox(a.p, size, size), a.team == 1 ? new Color(1f, .70f, .66f) : Color.white);
        DrawBar(a.p + new Vector2(0, -size), a.hero ? 75 : 42, a.hp / a.maxHp, a.team == 0 ? Blue : Red);
        if (a == player)
        {
            Rect r = MapBox(a.p + new Vector2(0, 20), 96, 12);
            Fill(new Rect(r.x, r.y, r.width, 3), Gold);
        }
    }
    void DrawBar(Vector2 p, float width, float ratio, Color color)
    {
        Rect r = MapBox(p, width, 5);
        Fill(r, new Color(.07f, .08f, .10f));
        Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(ratio), r.height), color);
    }
    void DrawPause()
    {
        float w = Screen.width, h = Screen.height;
        Fill(new Rect(0, 0, w, h), new Color(.02f, .06f, .10f, .88f));
        GUI.Label(new Rect(w * .1f, h * .25f, w * .8f, h * .12f), "PAUSA", titleStyle);
        if (Button(new Rect(w * .15f, h * .45f, w * .7f, h * .07f), "CONTINUAR", new Color(.14f, .45f, .66f))) paused = false;
        if (Button(new Rect(w * .15f, h * .56f, w * .7f, h * .07f), "SAIR PARA O MENU", new Color(.30f, .20f, .24f))) { paused = false; screen = 0; }
    }
    void DrawResult()
    {
        float w = Screen.width, h = Screen.height;
        Tex("map", new Rect(0, 0, w, h), Color.white);
        Fill(new Rect(0, 0, w, h), new Color(.03f, .08f, .13f, .83f));
        GUI.Label(new Rect(w * .06f, h * .25f, w * .88f, h * .15f), result == 1 ? "VITÓRIA!" : "DERROTA", titleStyle);
        GUI.Label(new Rect(w * .12f, h * .42f, w * .76f, h * .08f), result == 1 ? "A arena é sua." : "Tente outra estratégia.", centerStyle);
        if (Button(new Rect(w * .12f, h * .62f, w * .76f, h * .08f), "JOGAR DE NOVO", new Color(.14f, .45f, .66f))) StartMatch();
        if (Button(new Rect(w * .12f, h * .73f, w * .76f, h * .07f), "MENU", new Color(.15f, .25f, .35f))) screen = 0;
    }
}
