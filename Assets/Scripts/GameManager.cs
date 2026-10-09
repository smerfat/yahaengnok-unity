using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState { Title, Play, Dead, End }
    public enum Element { Fire, Water, Lightning, Earth, Wind }

    private const float GROUND_Y = 0f;
    private const float WORLD_WIDTH = 4200f;

    private GameState state = GameState.Title;
    private float time;
    private float msgTimer;
    private string msg = "";
    private bool drawing;

    private Player player;
    private List<Enemy> enemies = new List<Enemy>();
    private Boss boss;
    private List<Projectile> projectiles = new List<Projectile>();
    private List<Effect> effects = new List<Effect>();
    private List<Platform> platforms = new List<Platform>();
    private List<Wall> walls = new List<Wall>();
    private List<Pickup> pickups = new List<Pickup>();

    private List<Vector2> talismanPath = new List<Vector2>();
    private LineRenderer talismanLine;
    private bool yang, yin, hongwol, lockArea, contract;
    private int choice;
    private int selectedOption;
    private int kills, xp;

    private Dictionary<string, bool> flags = new Dictionary<string, bool>();
    private string resultText = "";
    private bool dialogActive;
    private Dialog dialog;

    // UI References
    private Image hpBarFill;
    private Image bossFill;
    private Text dialogText;
    private Image dialogPanel;
    private Text messageText;
    private Text hpText;
    private Button[] elementButtons = new Button[5];
    private Image[] elementImages = new Image[5];
    private Image bossBarPanel;
    private Text bossNameText;
    private Image skyImage;
    private Image moonImage;

    // Sprite References (Assign in Inspector)
    public Sprite playerSprite;
    public Sprite[] goblinSprites = new Sprite[9];
    public Sprite bossSprite;
    public Sprite pickupSprite;

    private readonly Color[] elementColors =
    {
        new Color(1f, 0.42f, 0.17f), // Fire
        new Color(0.23f, 0.65f, 1f), // Water
        new Color(1f, 0.9f, 0.3f), // Lightning
        new Color(0.7f, 0.52f, 0.25f), // Earth
        new Color(0.62f, 1f, 0.75f) // Wind
    };

    private void Start()
    {
        CreateCamera();
        CreateTalismanLine();
        InitializeUI();
        ResetGame();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        time += dt;

        if (state == GameState.Title)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
            return;
        }

        if (state == GameState.Dead || state == GameState.End)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
            return;
        }

        if (dialogActive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                AdvanceDialog();
            return;
        }

        if (choice == 1)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { selectedOption = 1; choice = 2; note("우클릭을 누른 채 부적을 크게 그려 봉인하라"); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { selectedOption = 2; choice = 2; note("우클릭을 누른 채 부적을 크게 그려 봉인하라"); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { selectedOption = 3; choice = 2; note("우클릭을 누른 채 부적을 크게 그려 봉인하라"); }
            return;
        }

        HandleInput();
        HandleTalismanInput();

        if (state == GameState.Play)
        {
            UpdatePlayer(dt);
            UpdateEnemies(dt);
            UpdateBoss(dt);
            UpdateProjectiles(dt);
            UpdateEffects(dt);
            UpdatePickups();
            UpdateHUDElements();

            if (player.hp <= 0f)
            {
                state = GameState.Dead;
            }
        }

        if (msgTimer > 0f)
            msgTimer -= dt;
    }

    private void StartGame()
    {
        ResetGame();
        state = GameState.Play;
        Say(new[]
        {
            "",
            "대한민국 어느 산골 마을, 월운촌. 이안(17)은 방학을 맞아 할머니의 집을 정리하러 내려왔다.",
            "",
            "마을 사람들은 말한다. \"해가 지면 절대 밖으로 나가지 마.\"",
            "",
            "딸랑… 딸랑… 숲속에서 누군가 부른다.",
            "홍련: \"이번에는… 늦지 않았구나.\"",
            "홍련: \"이안. 이번에는 반드시 기억해.\" — 홍련이 음양검을 건넨다.",
            "이안: 무슨 말이지…? (영안이 열려 요괴가 보이기 시작한다)"
        });
    }

    private void ResetGame()
    {
        // Clean up old objects
        foreach (var e in enemies) if (e.go) Destroy(e.go);
        foreach (var p in projectiles) if (p.go) Destroy(p.go);
        foreach (var fx in effects) if (fx.go) Destroy(fx.go);
        foreach (var p in platforms) if (p.go) Destroy(p.go);
        foreach (var w in walls) if (w.go) Destroy(w.go);
        foreach (var p in pickups) if (p.go) Destroy(p.go);
        if (boss != null && boss.go) Destroy(boss.go);
        if (player != null && player.go) Destroy(player.go);

        enemies.Clear();
        projectiles.Clear();
        effects.Clear();
        platforms.Clear();
        walls.Clear();
        pickups.Clear();

        player = new Player();
        player.go = CreateSprite("Player", new Vector3(1.5f, 1.2f, 0f), new Vector3(0.7f, 0.7f, 1f), new Color(1f, 1f, 1f), playerSprite);
        player.width = 0.7f;
        player.height = 1.8f;
        player.x = 1.5f;
        player.y = 0.9f;
        player.vx = 0f;
        player.vy = 0f;
        player.hp = 100f;
        player.face = 1f;
        player.atk = 0f;
        player.inv = 0f;
        player.onGround = true;
        player.cd = 0f;
        player.selected = 0;
        player.xp = 0;
        player.potion = 2;
        player.food = 2;

        // enemies
        for (int i = 0; i < 9; i++)
        {
            var e = new Enemy();
            Sprite gobSprite = (i < goblinSprites.Length && goblinSprites[i] != null) ? goblinSprites[i] : null;
            e.go = CreateSprite("Goblin_" + i, new Vector3(9f + i * 3.5f, 1.2f, 0f), new Vector3(0.6f, 0.6f, 1f), Color.white, gobSprite);
            e.x = 9f + i * 3.5f;
            e.y = 0.6f;
            e.width = 0.7f;
            e.height = 1.3f;
            e.hp = 30f;
            e.dir = (i % 2 == 0) ? -1f : 1f;
            e.t = Random.Range(0f, 3f);
            e.hit = 0f;
            e.stun = 0f;
            enemies.Add(e);
        }

        // boss
        boss = new Boss();
        boss.go = CreateSprite("Boss", new Vector3(42f, 2.6f, 0f), new Vector3(1.2f, 1.2f, 1f), Color.white, bossSprite);
        boss.x = 42f;
        boss.y = 1.0f;
        boss.width = 1.7f;
        boss.height = 2.2f;
        boss.hp = 320f;
        boss.maxHp = 320f;
        boss.timer = 0f;
        boss.dir = -1f;
        boss.hit = 0f;
        boss.stun = 0f;
        boss.groggy = false;
        boss.active = false;

        // platforms
        platforms.Add(CreatePlatform(7f, 1.1f, 4.0f));
        platforms.Add(CreatePlatform(14f, 0.9f, 3.2f));
        platforms.Add(CreatePlatform(22f, 1.2f, 4.0f));
        platforms.Add(CreatePlatform(30f, 0.9f, 3.2f));
        platforms.Add(CreatePlatform(38f, 0.9f, 3.0f));
        platforms.Add(CreatePlatform(11.5f, 2.4f, 2.3f, true));
        platforms.Add(CreatePlatform(24.5f, 2.2f, 2.2f, true));
        platforms.Add(CreatePlatform(38.5f, 2.3f, 2.2f, true));

        // pickups (potion crystals)
        pickups.Add(CreatePickup(6.5f, 2.1f));
        pickups.Add(CreatePickup(12.5f, 2.6f));
        pickups.Add(CreatePickup(25.3f, 2.5f));
        pickups.Add(CreatePickup(34.5f, 2.2f));

        // flags
        flags.Clear();
        drawing = false;
        talismanPath.Clear();
        choice = 0;
        selectedOption = 0;
        contract = false;
        kills = 0;
        xp = 0;
        resultText = "";
        msgTimer = 0f;
        msg = "";
        dialogActive = false;
        dialog = null;
        yang = false;
        yin = false;
        hongwol = false;
        lockArea = false;
        SetTalismanVisible(false);

        // camera
        Camera.main.transform.position = new Vector3(0f, 2.2f, -10f);
    }

    private GameObject CreateSprite(string name, Vector3 pos, Vector3 scale, Color color, Sprite sprite = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = pos;
        obj.transform.localScale = scale;

        SpriteRenderer rend = obj.AddComponent<SpriteRenderer>();
        rend.color = color;
        if (sprite != null)
            rend.sprite = sprite;
        else
            rend.color = new Color(color.r, color.g, color.b, 0.5f); // Default cube color if no sprite

        return obj;
    }

    private GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        var rend = obj.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Standard"));
        rend.material.color = color;
        return obj;
    }

    private Platform CreatePlatform(float x, float y, float width, bool floating = false)
    {
        var p = new Platform();
        p.go = CreateCube("Platform", new Vector3(x, y, 0f), new Vector3(width, 0.35f, 0.5f), new Color(0.22f, 0.17f, 0.15f));
        p.x = x;
        p.y = y;
        p.width = width;
        p.height = 0.35f;
        p.floating = floating;
        return p;
    }

    private Pickup CreatePickup(float x, float y)
    {
        var p = new Pickup();
        p.go = CreateSprite("Pickup", new Vector3(x, y, 0f), new Vector3(0.3f, 0.3f, 1f), new Color(0.98f, 0.83f, 0.3f), pickupSprite);
        p.x = x;
        p.y = y;
        p.collected = false;
        return p;
    }

    private void InitializeUI()
    {
        // Find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas를 찾을 수 없습니다! Canvas를 먼저 만드세요.");
            return;
        }

        // HP Bar
        Transform hpPanel = canvas.transform.Find("HPBarPanel");
        if (hpPanel != null)
        {
            hpBarFill = hpPanel.Find("HPBarFill").GetComponent<Image>();
            hpText = hpPanel.Find("HPText").GetComponent<Text>();
        }

        // Dialog
        Transform dialogPanelTrans = canvas.transform.Find("DialogPanel");
        if (dialogPanelTrans != null)
        {
            dialogPanel = dialogPanelTrans.GetComponent<Image>();
            dialogText = dialogPanelTrans.Find("DialogText").GetComponent<Text>();
            dialogPanel.gameObject.SetActive(false);
        }

        // Message
        messageText = canvas.transform.Find("MessageText").GetComponent<Text>();
        if (messageText != null)
            messageText.text = "";

        // Elements
        Transform elementsPanel = canvas.transform.Find("ElementsPanel");
        if (elementsPanel != null)
        {
            string[] elementNames = { "FireBtn", "WaterBtn", "LightningBtn", "EarthBtn", "WindBtn" };
            for (int i = 0; i < 5; i++)
            {
                Transform btnTrans = elementsPanel.Find(elementNames[i]);
                if (btnTrans != null)
                {
                    elementButtons[i] = btnTrans.GetComponent<Button>();
                    elementImages[i] = btnTrans.GetComponent<Image>();
                }
            }
        }

        // Boss Bar
        Transform bossPanel = canvas.transform.Find("BossBarPanel");
        if (bossPanel != null)
        {
            bossBarPanel = bossPanel.GetComponent<Image>();
            bossNameText = bossPanel.Find("BossNameText").GetComponent<Text>();
            bossFill = bossPanel.Find("BossHPBG/BossHPFill").GetComponent<Image>();
            bossBarPanel.gameObject.SetActive(false);
        }

        // Sky
        skyImage = canvas.transform.Find("Sky").GetComponent<Image>();
        moonImage = canvas.transform.Find("Moon").GetComponent<Image>();
    }

    private void HandleInput()
    {
        float dir = 0f;
        if (Input.GetKey(KeyCode.D)) dir = 1f;
        if (Input.GetKey(KeyCode.A)) dir = -1f;

        player.vx = dir * 7f;
        if (dir != 0f) player.face = dir;

        if (Input.GetKeyDown(KeyCode.Space) && player.onGround)
        {
            player.vy = 10f;
            player.onGround = false;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) player.selected = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) player.selected = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) player.selected = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) player.selected = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) player.selected = 4;

        if (Input.GetKeyDown(KeyCode.E) && player.potion > 0 && player.hp < 100f)
        {
            player.potion--;
            player.hp = Mathf.Min(100f, player.hp + 35f);
        }

        if (Input.GetKeyDown(KeyCode.Z) && player.food > 0 && player.hp < 100f)
        {
            player.food--;
            player.hp = Mathf.Min(100f, player.hp + 15f);
        }

        if (Input.GetKeyDown(KeyCode.F) && player.hp > 15f)
        {
            yang = !yang;
            yin = false;
        }

        if (Input.GetKeyDown(KeyCode.G) && player.hp > 15f)
        {
            yin = !yin;
            yang = false;
        }

        if (Input.GetMouseButtonDown(0) && !drawing)
        {
            if (player.atk <= 0f)
            {
                player.atk = 0.25f;
                Slash();
            }
        }
    }

    private void UpdatePlayer(float dt)
    {
        player.inv = Mathf.Max(0f, player.inv - dt);
        player.atk = Mathf.Max(0f, player.atk - dt);
        player.cd = Mathf.Max(0f, player.cd - dt);

        if (yang || yin)
        {
            player.hp -= 3f * dt;
            if (player.hp <= 10f) { yang = false; yin = false; }
        }

        // gravity
        player.vy -= 30f * dt;
        player.x += player.vx * dt;
        player.y += player.vy * dt;

        player.onGround = false;

        // ground collision
        if (player.y <= GROUND_Y)
        {
            player.y = GROUND_Y;
            player.vy = 0f;
            player.onGround = true;
        }

        // platform collision
        foreach (var p in platforms)
        {
            if (!p.floating && !yang) continue;

            float left = player.x - player.width / 2f;
            float right = player.x + player.width / 2f;
            float bottom = player.y - player.height / 2f;
            float top = player.y + player.height / 2f;

            float platLeft = p.x - p.width / 2f;
            float platRight = p.x + p.width / 2f;
            float platTop = p.y + p.height / 2f;

            if (player.vy <= 0f &&
                right > platLeft &&
                left < platRight &&
                bottom <= platTop &&
                bottom >= platTop - 0.6f)
            {
                player.y = p.y + p.height / 2f + player.height / 2f;
                player.vy = 0f;
                player.onGround = true;
            }
        }

        // world clamp
        player.x = Mathf.Clamp(player.x, lockArea ? 38f : 0f, WORLD_WIDTH - player.width);

        // events
        if (!flags.ContainsKey("a") && player.x > 3.5f)
        {
            flags["a"] = true;
            Say(new[] {
                "무녀: \"홀로 신사를 지키는 자다. 그 영안… 흥미롭구나. 부적용지와 특수 붓을 주마.\"",
                "무녀: \"우클릭을 누르면 시간이 느려진다. 그 사이 부적을 그려라. 1~5로 오행을 고르고, 양둔(F)은 어둠을 물리지만 체력을 먹는다.\""
            });
        }

        if (!flags.ContainsKey("b") && player.x > 28f)
        {
            flags["b"] = true;
            hongwol = true;
            Say(new[] {
                "",
                "붉은 달이 떠오른다. 도깨비들이 흉폭해진다.",
                "이안: …이제 공격해 오는군. 검이 통한다!"
            });
        }

        if (!flags.ContainsKey("c") && player.x > 42f)
        {
            flags["c"] = true;
            lockArea = true;
            boss.active = true;
            Say(new[] {
                "도깨비들의 왕: \"또 너냐.\"",
                "도깨비들의 왕: \"이번에는 기억하지 못하는구나.\""
            });
        }

        // visual sync
        if (player.go != null)
        {
            player.go.transform.position = new Vector3(player.x, player.y + player.height / 2f, 0f);

            SpriteRenderer rend = player.go.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                if (player.inv > 0f && Mathf.FloorToInt(time * 20f) % 2 == 0)
                {
                    rend.color = new Color(1f, 1f, 1f, 0.4f);
                }
                else
                {
                    rend.color = Color.white;
                }

                // Flip sprite based on direction
                rend.flipX = (player.face < 0);
            }
        }

        Camera.main.transform.position = new Vector3(Mathf.Clamp(player.x, 0f, WORLD_WIDTH - 18f), 2.3f, -10f);
    }

    private void Slash()
    {
        float damage = contract ? 20f : 12f;
        float startX = player.face > 0f ? player.x + player.width / 2f : player.x - player.width / 2f;

        foreach (var e in GetTargets())
        {
            if (startX > e.x - e.width / 2f && startX < e.x + e.width / 2f + 0.8f)
            {
                if (player.y + 0.5f > e.y - e.height / 2f && player.y - 0.5f < e.y + e.height / 2f)
                    Hurt(e, damage, player.face);
            }
        }

        AddEffect(new Vector3(player.x + player.face * 0.8f, player.y + 0.2f, 0f), Color.white, 0.13f, 8);
    }

    private void Hurt(Enemy e, float dmg, float dir)
    {
        if (e.groggy) return;

        e.hp -= dmg;
        e.hit = 0.15f;
        e.x += dir * (e.isBoss ? 0.06f : 0.2f);

        AddEffect(new Vector3(e.x, e.y + 0.2f, 0f), new Color(1f, 0.3f, 0.3f), 0.18f, 12);

        if (e.hp <= 0f)
        {
            if (e.isBoss)
            {
                e.hp = 0f;
                e.groggy = true;
                projectiles.Clear();
                Say(new[]
                {
                    "",
                    "도깨비들의 왕이 무릎을 꿇었다. 그로기 상태!",
                    "",
                    "부적으로 처리하라 — 1 퇴마 / 2 계약 / 3 소멸"
                });
                choice = 1;
            }
            else
            {
                e.dead = true;
                player.xp += 20;
                kills++;
            }
        }
    }

    private List<Enemy> GetTargets()
    {
        var list = new List<Enemy>();
        foreach (var e in enemies) if (!e.dead) list.Add(e);

        if (boss != null && boss.active && !boss.groggy)
            list.Add(boss);

        return list;
    }

    private void UpdateEnemies(float dt)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e.dead)
            {
                Destroy(e.go);
                enemies.RemoveAt(i);
                continue;
            }

            e.hit = Mathf.Max(0f, e.hit - dt);
            if (e.stun > 0f)
            {
                e.stun = Mathf.Max(0f, e.stun - dt);
                continue;
            }

            e.t += dt;
            float dist = player.x - e.x;

            if (hongwol && Mathf.Abs(dist) < (yin ? 1.5f : 5.5f))
            {
                e.dir = Mathf.Sign(dist) == 0 ? 1 : Mathf.Sign(dist);
                e.x += e.dir * 1.8f * dt;
            }
            else
            {
                if (e.t > 2.5f)
                {
                    e.t = 0f;
                    e.dir *= -1f;
                }
                e.x += e.dir * (hongwol ? 0.9f : 0.6f) * dt;
            }

            if (hongwol && Collides(player, e))
            {
                DamagePlayer(8f, e);
            }

            if (e.go != null)
            {
                e.go.transform.position = new Vector3(e.x, e.y + e.height / 2f, 0f);

                SpriteRenderer rend = e.go.GetComponent<SpriteRenderer>();
                if (rend != null)
                {
                    rend.flipX = (e.dir < 0);
                }
            }
        }
    }

    private void UpdateBoss(float dt)
    {
        if (boss == null || !boss.active || boss.groggy) return;

        boss.hit = Mathf.Max(0f, boss.hit - dt);

        if (boss.stun > 0f)
        {
            boss.stun = Mathf.Max(0f, boss.stun - dt);
            return;
        }

        boss.timer += dt;
        float dist = player.x - boss.x;

        if (boss.timer < 2.4f)
        {
            boss.dir = Mathf.Sign(dist) == 0 ? 1 : Mathf.Sign(dist);
            boss.x += boss.dir * 0.9f * dt;
        }
        else if (boss.timer < 2.8f)
        {
            // idle
        }
        else if (boss.timer < 3.5f)
        {
            boss.x += boss.dir * 6.4f * dt;
        }
        else
        {
            boss.timer = 0f;
        }

        boss.x = Mathf.Clamp(boss.x, 38f, WORLD_WIDTH - boss.width);

        if (Collides(player, boss))
            DamagePlayer(20f, boss);

        if (boss.go != null)
        {
            boss.go.transform.position = new Vector3(boss.x, boss.y + boss.height / 2f, 0f);

            SpriteRenderer rend = boss.go.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                rend.flipX = (boss.dir < 0);
            }
        }
    }

    private void DamagePlayer(float amount, Enemy attacker)
    {
        if (player.inv > 0f || drawing) return;

        player.hp -= amount;
        player.inv = 0.9f;
        player.vy = 8f;
        player.x += player.x < attacker.x ? -0.8f : 0.8f;

        AddEffect(new Vector3(player.x, player.y + 0.3f, 0f), new Color(0.8f, 0.1f, 0.1f), 0.2f, 10);
    }

    private void UpdateProjectiles(float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            var p = projectiles[i];
            p.life -= dt;

            if (p.type == 0)
            {
                p.x += p.vx * dt;
                p.y += p.vy * dt;

                if (p.y <= GROUND_Y)
                {
                    p.life = 0f;
                    Explode(p.x, GROUND_Y);
                }
                else
                {
                    foreach (var e in GetTargets())
                    {
                        if (p.x > e.x - e.width / 2f && p.x < e.x + e.width / 2f &&
                            p.y > e.y - e.height / 2f && p.y < e.y + e.height / 2f)
                        {
                            p.life = 0f;
                            Explode(p.x, p.y);
                            break;
                        }
                    }
                }
            }

            if (p.type == 1)
            {
                p.x += p.dir * 6.5f * dt;

                foreach (var e in GetTargets())
                {
                    if (Mathf.Abs((e.x + e.width / 2f) - p.x) < 0.8f)
                    {
                        e.x += p.dir * (e.isBoss ? 0.75f : 3.2f);
                        if (Random.value < 0.12f)
                            Hurt(e, 3f, 0f);
                    }
                }
            }

            if (p.type == 2 && p.life <= 0f)
            {
                var tx = p.target != null ? p.target.x : p.x;
                AddEffect(new Vector3(tx, 2f, 0f), new Color(1f, 0.9f, 0.3f), 0.35f, 18);

                foreach (var e in GetTargets())
                {
                    if (Mathf.Abs(e.x - tx) < 1.2f)
                        Hurt(e, 60f, 0f);
                }
            }

            if (p.go != null)
                p.go.transform.position = new Vector3(p.x, p.y, 0f);

            if (p.life <= 0f)
            {
                if (p.go) Destroy(p.go);
                projectiles.RemoveAt(i);
            }
        }
    }

    private void Explode(float x, float y)
    {
        AddEffect(new Vector3(x, y, 0f), new Color(1f, 0.55f, 0.2f), 0.7f, 18);

        foreach (var e in GetTargets())
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(e.x, e.y));
            if (dist < 1.8f)
                Hurt(e, 40f, e.x > x ? 1f : -1f);
        }
    }

    private void UpdateEffects(float dt)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var fx = effects[i];
            fx.life -= dt;

            if (fx.go != null)
                fx.go.transform.position += new Vector3(fx.vx * dt, fx.vy * dt, 0f);

            if (fx.life <= 0f)
            {
                Destroy(fx.go);
                effects.RemoveAt(i);
            }
        }
    }

    private void UpdatePickups()
    {
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            var p = pickups[i];
            if (p.collected)
            {
                Destroy(p.go);
                pickups.RemoveAt(i);
                continue;
            }

            if (Mathf.Abs(player.x - p.x) < 0.7f && Mathf.Abs(player.y - p.y) < 0.8f)
            {
                p.collected = true;
                player.hp = Mathf.Min(100f, player.hp + 30f);
                player.potion++;
                note("영력의 구슬을 얻었다 (양둔으로 드러난 숨은 길)");
            }

            if (p.go != null)
            {
                p.go.transform.position = new Vector3(p.x, p.y + Mathf.Sin(time * 4f + i) * 0.15f, 0f);
            }
        }
    }

    private void UpdateHUDElements()
    {
        // HP Bar
        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = Mathf.Clamp01(player.hp / 100f);

            if (player.hp > 50f)
                hpBarFill.color = new Color(0.8f, 0.2f, 0.2f);
            else if (player.hp > 25f)
                hpBarFill.color = new Color(1f, 0.7f, 0.2f);
            else
                hpBarFill.color = new Color(1f, 0.2f, 0.2f);
        }

        if (hpText != null)
        {
            hpText.text = "체력 " + Mathf.Ceil(player.hp);
        }

        // Dialog
        if (dialogActive && dialog != null)
        {
            if (dialogPanel != null)
                dialogPanel.gameObject.SetActive(true);
            if (dialogText != null)
                dialogText.text = dialog.lines[dialog.index];
        }
        else
        {
            if (dialogPanel != null)
                dialogPanel.gameObject.SetActive(false);
        }

        // Message
        if (messageText != null)
        {
            messageText.text = msgTimer > 0f ? msg : "";
        }

        // Boss Bar
        if (boss != null && boss.active && !boss.groggy)
        {
            if (bossBarPanel != null)
                bossBarPanel.gameObject.SetActive(true);
            if (bossFill != null)
                bossFill.fillAmount = Mathf.Clamp01(boss.hp / boss.maxHp);
        }
        else
        {
            if (bossBarPanel != null)
                bossBarPanel.gameObject.SetActive(false);
        }

        // Element selection
        for (int i = 0; i < 5; i++)
        {
            if (elementImages[i] != null)
            {
                if (i == player.selected)
                {
                    elementImages[i].color = new Color(elementColors[i].r * 1.2f, elementColors[i].g * 1.2f, elementColors[i].b * 1.2f, 1f);
                }
                else
                {
                    elementImages[i].color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
                }
            }
        }
    }

    private void HandleTalismanInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            drawing = true;
            talismanPath.Clear();
            AddMousePoint();
            SetTalismanVisible(true);
        }

        if (Input.GetMouseButton(1) && drawing)
        {
            AddMousePoint();
        }

        if (Input.GetMouseButtonUp(1) && drawing)
        {
            drawing = false;
            FinishTalisman();
            SetTalismanVisible(false);
        }
    }

    private void AddMousePoint()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        talismanPath.Add(new Vector2(p.x, p.y));
        UpdateTalismanLine();
    }

    private void UpdateTalismanLine()
    {
        if (talismanLine == null) return;
        talismanLine.positionCount = talismanPath.Count;
        for (int i = 0; i < talismanPath.Count; i++)
            talismanLine.SetPosition(i, new Vector3(talismanPath[i].x, talismanPath[i].y, 0f));
    }

    private void FinishTalisman()
    {
        float len = 0f;
        for (int i = 1; i < talismanPath.Count; i++)
            len += Vector2.Distance(talismanPath[i - 1], talismanPath[i]);

        if (choice == 2)
        {
            if (len >= 4f)
                ResolveBossChoice();
            else
                note("부적이 미완성이다. 더 크게 그려라");
            talismanPath.Clear();
            return;
        }

        if (len >= 1.8f && player.cd <= 0f)
        {
            CastTalisman();
        }
        else if (len >= 1.8f)
        {
            note("부적이 아직 식지 않았다");
        }

        talismanPath.Clear();
    }

    private void CastTalisman()
    {
        Vector2 start = talismanPath[0];
        Vector2 end = talismanPath[talismanPath.Count - 1];
        Vector2 delta = end - start;
        float mag = Mathf.Max(0.1f, delta.magnitude);
        Vector2 dir = delta / mag;

        float cx = player.x;
        float cy = player.y + 0.3f;
        int index = player.selected;

        if (index == 0)
        {
            var p = new Projectile();
            p.type = 0;
            p.x = cx;
            p.y = cy;
            p.vx = dir.x * 8.5f;
            p.vy = dir.y * 8.5f;
            p.life = 1.8f;
            p.go = CreateCube("FireOrb", new Vector3(cx, cy, 0f), new Vector3(0.25f, 0.25f, 0.25f), Color.red);
            projectiles.Add(p);
        }

        if (index == 1)
        {
            var p = new Projectile();
            p.type = 1;
            p.x = cx;
            p.y = player.y - 0.4f;
            p.dir = player.face;
            p.life = 1.2f;
            p.go = CreateCube("WaterSlash", new Vector3(cx, player.y - 0.4f, 0f), new Vector3(1.5f, 0.25f, 0.25f), new Color(0.2f, 0.6f, 1f));
            projectiles.Add(p);
        }

        if (index == 2)
        {
            var target = GetClosestEnemy();
            var p = new Projectile();
            p.type = 2;
            p.x = target != null ? target.x : cx + player.face * 4f;
            p.y = 2.5f;
            p.life = 0.5f;
            p.target = target;
            p.go = CreateCube("Lightning", new Vector3(p.x, p.y, 0f), new Vector3(0.2f, 0.2f, 0.2f), new Color(1f, 0.95f, 0.2f));
            projectiles.Add(p);
        }

        if (index == 3)
        {
            float wallX = cx + player.face * 1.2f;
            walls.Add(CreateWall(wallX, 0.8f));
        }

        if (index == 4)
        {
            foreach (var e in GetTargets())
            {
                if (Vector2.Distance(new Vector2(e.x, e.y), new Vector2(cx, cy)) < 4f)
                    e.stun = e.isBoss ? 1.5f : 2.5f;
            }
        }

        player.cd = 0.8f;
    }

    private Enemy GetClosestEnemy()
    {
        Enemy best = null;
        float bestDist = float.MaxValue;

        foreach (var e in GetTargets())
        {
            float d = Mathf.Abs(e.x - player.x);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    private Wall CreateWall(float x, float width)
    {
        var w = new Wall();
        w.go = CreateCube("Wall", new Vector3(x, 1.2f, 0f), new Vector3(width, 2.5f, 0.5f), new Color(0.68f, 0.5f, 0.36f));
        w.x = x;
        w.width = width;
        return w;
    }

    private void ResolveBossChoice()
    {
        choice = 3;

        switch (selectedOption)
        {
            case 1:
                resultText = "퇴마 — 도깨비들의 왕은 며칠 뒤 다시 깨어난다. (난이도는 더욱 쉬워진다)";
                break;
            case 2:
                contract = true;
                resultText = "계약 — 괴력의 일부를 얻고, 도깨비 왕의 그림자가 곁을 따른다. (검 공격력 상승)";
                break;
            case 3:
                player.xp += 500;
                resultText = "소멸 — 이 세계에 다시 소환되지 않는다. 경험치 +500";
                break;
            default:
                resultText = "퇴마 — 도깨비들의 왕은 며칠 뒤 다시 깨어난다. (난이도는 더욱 쉬워진다)";
                selectedOption = 1;
                break;
        }

        boss.active = false;
        boss.groggy = true;

        Say(new[]
        {
            "",
            resultText,
            "도깨비: \"또 너에게 빚을 지는구나.\"",
            "",
            "보스를 쓰러뜨리자 기억이 흘러든다. 자신의 기억이 아니다. 500년 전, 한 음양사의 기억."
        });

        state = GameState.End;
    }

    private bool Collides(Player a, Enemy b)
    {
        float left1 = a.x - a.width / 2f;
        float right1 = a.x + a.width / 2f;
        float bottom1 = a.y - a.height / 2f;
        float top1 = a.y + a.height / 2f;

        float left2 = b.x - b.width / 2f;
        float right2 = b.x + b.width / 2f;
        float bottom2 = b.y - b.height / 2f;
        float top2 = b.y + b.height / 2f;

        return !(left1 > right2 || right1 < left2 || top1 < bottom2 || bottom1 > top2);
    }

    private void AddEffect(Vector3 pos, Color color, float life, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            var rend = go.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = color;

            Effect fx = new Effect();
            fx.go = go;
            fx.life = life;
            fx.vx = Random.Range(-2.5f, 2.5f);
            fx.vy = Random.Range(-2.2f, 2.2f);
            effects.Add(fx);
        }
    }

    private void CreateCamera()
    {
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 7f;
        Camera.main.transform.position = new Vector3(0f, 2.2f, -10f);
    }

    private void CreateTalismanLine()
    {
        var go = new GameObject("TalismanLine");
        talismanLine = go.AddComponent<LineRenderer>();
        talismanLine.material = new Material(Shader.Find("Sprites/Default"));
        talismanLine.startWidth = 0.12f;
        talismanLine.endWidth = 0.12f;
        talismanLine.startColor = new Color(1f, 0.8f, 0.3f, 0.95f);
        talismanLine.endColor = new Color(1f, 0.8f, 0.3f, 0.95f);
        talismanLine.positionCount = 0;
        talismanLine.enabled = false;
    }

    private void SetTalismanVisible(bool visible)
    {
        if (talismanLine == null) return;
        talismanLine.enabled = visible;
        if (!visible) talismanLine.positionCount = 0;
    }

    private void Say(string[] lines)
    {
        dialog = new Dialog();
        dialog.lines = lines;
        dialog.index = 0;
        dialogActive = true;
    }

    private void AdvanceDialog()
    {
        if (dialog == null) return;
        dialog.index++;
        if (dialog.index >= dialog.lines.Length)
        {
            dialog = null;
            dialogActive = false;
        }
    }

    private void note(string s)
    {
        msg = s;
        msgTimer = 2.5f;
    }

    // ---------- Classes ----------

    private class Player
    {
        public GameObject go;
        public float x, y;
        public float width = 0.7f;
        public float height = 1.8f;
        public float vx, vy;
        public float hp = 100f;
        public float face = 1f;
        public float atk;
        public float inv;
        public bool onGround;
        public float cd;
        public int selected;
        public int xp;
        public int potion, food;
    }

    private class Enemy
    {
        public GameObject go;
        public float x, y;
        public float width, height;
        public float hp;
        public float dir;
        public float t;
        public float hit;
        public float stun;
        public bool dead;
        public bool groggy;
        public bool isBoss;
    }

    private class Boss : Enemy
    {
        public float timer;
        public float maxHp;
        public bool active;
    }

    private class Projectile
    {
        public GameObject go;
        public int type;
        public float x, y;
        public float vx, vy;
        public float dir;
        public float life;
        public Enemy target;
    }

    private class Effect
    {
        public GameObject go;
        public float life;
        public float vx, vy;
    }

    private class Platform
    {
        public GameObject go;
        public float x, y;
        public float width, height;
        public bool floating;
    }

    private class Wall
    {
        public GameObject go;
        public float x;
        public float width;
    }

    private class Pickup
    {
        public GameObject go;
        public float x, y;
        public bool collected;
    }

    private class Dialog
    {
        public string[] lines;
        public int index;
    }
}
