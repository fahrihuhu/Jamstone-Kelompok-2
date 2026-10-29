using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Minigame kejar-kejaran. Taruh di satu GameObject kosong di PacmanScene.
//
// Dua cara menyusun maze:
//  1. Tilemap (disarankan): isi Wall Tilemap, dan tandai posisi awal dengan objek kosong.
//  2. Layout teks: biarkan Wall Tilemap kosong, maze dibangun dari kotak berwarna (untuk tes cepat).
//
// Aturan: pemain terus berlari, hanya mengatur belokan.
// Bertahan selama "Survive Duration" = lolos. Tersentuh pengejar = tertangkap.
public class ChaseMinigame : MonoBehaviour
{
    [Header("Aturan")]
    [Tooltip("Pemain lolos kalau bertahan selama ini (detik)")]
    public float surviveDuration = 60f;
    [Tooltip("Jeda 'Bersiap...' sebelum permainan dimulai")]
    public float readyDelay = 1.5f;
    [Tooltip("Jeda menampilkan hasil sebelum kembali ke cerita")]
    public float resultDelay = 1.5f;
    [Tooltip("Jarak (dalam hitungan sel) pemain dan pengejar yang dianggap tertangkap")]
    public float catchDistance = 0.6f;

    [Header("Kecepatan (sel per detik)")]
    public float playerSpeed = 4f;
    [Tooltip("Pengejar yang mengikuti pemain. Buat sedikit lebih lambat dari pemain supaya masih bisa lolos")]
    public float chaserSpeed = 3.4f;
    [Tooltip("Dua pengejar yang bergerak acak")]
    public float wandererSpeed = 3f;

    [Header("Maze dari Tilemap (disarankan)")]
    [Tooltip("Sel yang punya tile di Tilemap ini dianggap TEMBOK. Kalau kosong, maze dibaca dari Layout teks di bawah.")]
    public Tilemap wallTilemap;
    [Tooltip("Opsional. Kalau diisi, hanya sel yang punya tile lantai (dan bukan tembok) yang bisa dilewati. Kalau kosong, semua sel tanpa tembok di dalam batas Wall Tilemap bisa dilewati.")]
    public Tilemap floorTilemap;
    [Tooltip("Objek kosong yang ditaruh di sel awal Tono")]
    public Transform playerStart;
    [Tooltip("Objek kosong di sel awal pengejar utama (Rico)")]
    public Transform chaserStart;
    [Tooltip("Objek kosong di sel awal tiap pengejar acak (anak buah). Boleh lebih dari dua.")]
    public Transform[] wandererStarts;

    [Header("Maze dari Layout Teks (dipakai kalau Wall Tilemap kosong)")]
    [Tooltip("# = tembok, . = jalan, P = awal pemain, C = awal pengejar utama, R = awal pengejar acak (boleh lebih dari satu). Baris pertama = bagian atas layar.")]
    public string[] layout = new string[]
    {
        "###################",
        "#........C........#",
        "#.#.#.#.#.#.#.#.#.#",
        "#.................#",
        "#.#.#.#.#.#.#.#.#.#",
        "#R...............R#",
        "#.#.#.#.#.#.#.#.#.#",
        "#.................#",
        "#.#.#.#.#.#.#.#.#.#",
        "#.................#",
        "#.#.#.#.#.#.#.#.#.#",
        "#........P........#",
        "###################",
    };

    [Header("Tampilan")]
    [Tooltip("Kosongkan: kamera dibuat otomatis. Kalau diisi, hapus AudioListener di kamera itu.")]
    public Camera minigameCamera;
    public float cameraMargin = 1f;
    public Color backgroundColor = new Color(0.03f, 0.03f, 0.05f);
    [Header("Warna placeholder")]
    public Color floorColor = new Color(0.10f, 0.10f, 0.13f);
    public Color wallColor = new Color(0.20f, 0.25f, 0.60f);
    public Color playerColor = Color.yellow;
    public Color chaserColor = Color.red;
    public Color wandererColor = new Color(1f, 0.6f, 0.1f);

    // Dibaca oleh ChaseActor
    public ChaseMaze Maze { get; private set; }
    public ChaseActor Player { get; private set; }
    public bool IsRunning { get; private set; }

    private readonly List<ChaseActor> enemies = new List<ChaseActor>();
    private Sprite squareSprite;
    private float timeLeft;
    private bool finished;
    private bool won;

    private void Start()
    {
        if (wallTilemap != null)
        {
            Maze = new ChaseMaze(wallTilemap, floorTilemap, playerStart, chaserStart, wandererStarts);
        }
        else
        {
            Maze = new ChaseMaze(layout);
            BuildTextVisuals(); // Tilemap sudah punya visual sendiri, layout teks belum
        }

        timeLeft = surviveDuration;

        SetupCamera();
        SpawnActors();

        StartCoroutine(BeginRoutine());
    }

    private IEnumerator BeginRoutine()
    {
        yield return new WaitForSecondsRealtime(readyDelay);
        IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning) return;

        // Input hanya mengubah arah yang diinginkan, pemain tetap terus berlari
        Vector2Int input = ReadDirection();
        if (input != Vector2Int.zero)
            Player.Steer(input);

        // Tertangkap? (jarak dihitung dalam satuan sel supaya tidak tergantung ukuran Grid)
        float catchWorldDistance = catchDistance * Maze.CellSize;
        foreach (ChaseActor enemy in enemies)
        {
            float distance = Vector3.Distance(enemy.transform.position, Player.transform.position);
            if (distance < catchWorldDistance)
            {
                Finish(false);
                return;
            }
        }

        // Waktu habis = lolos
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
            Finish(true);
    }

    // ================= HASIL =================

    private void Finish(bool playerWon)
    {
        IsRunning = false;
        finished = true;
        won = playerWon;
        StartCoroutine(FinishRoutine(playerWon));
    }

    private IEnumerator FinishRoutine(bool playerWon)
    {
        yield return new WaitForSecondsRealtime(resultDelay);

        if (GameManager.Instance != null)
        {
            // Dijalankan dari VNScene: laporkan hasil, GameManager yang menutup scene ini
            GameManager.Instance.ReportMinigameResult(playerWon);
        }
        else
        {
            // Dites langsung dari PacmanScene: muat ulang supaya bisa dicoba lagi
            Debug.Log($"Hasil minigame: {(playerWon ? "lolos" : "tertangkap")} (GameManager tidak ditemukan, memuat ulang scene)");
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }

    // ================= INPUT =================

    private Vector2Int ReadDirection()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2Int.zero;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) return Vector2Int.up;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return Vector2Int.down;
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return Vector2Int.left;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return Vector2Int.right;
#else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return Vector2Int.right;
#endif
        return Vector2Int.zero;
    }

    // ================= MEMBANGUN SCENE =================

    // Hanya untuk mode layout teks (mode Tilemap memakai visual Tilemap sendiri)
    private void BuildTextVisuals()
    {
        MakeSprite("Floor", floorColor,
            Vector3.zero, new Vector3(Maze.width, Maze.height, 1f), 0);

        for (int x = 0; x < Maze.width; x++)
        {
            for (int y = 0; y < Maze.height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (Maze.IsWall(cell))
                    MakeSprite("Wall", wallColor, Maze.CellToWorld(cell), Vector3.one * 0.96f, 1);
            }
        }
    }

    private void SetupCamera()
    {
        if (minigameCamera == null)
        {
            GameObject camObject = new GameObject("MinigameCamera");
            camObject.transform.SetParent(transform, false);
            minigameCamera = camObject.AddComponent<Camera>();
        }

        minigameCamera.orthographic = true;
        minigameCamera.clearFlags = CameraClearFlags.SolidColor;
        minigameCamera.backgroundColor = backgroundColor;
        minigameCamera.depth = 10; // di atas kamera VNScene

        // Pusatkan kamera dan atur zoom supaya seluruh maze terlihat di layar apa pun
        Bounds bounds = Maze.WorldBounds;
        minigameCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);

        float aspect = Mathf.Max(0.1f, minigameCamera.aspect);
        float halfHeight = bounds.extents.y + cameraMargin;
        float halfWidth = bounds.extents.x + cameraMargin;
        minigameCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
    }

    private void SpawnActors()
    {
        Player = SpawnActor("Player (Tono)", Maze.playerStart, ChaseActorType.Player, playerSpeed, playerColor);

        enemies.Add(SpawnActor("Chaser (Rico)", Maze.chaserStart, ChaseActorType.Chaser, chaserSpeed, chaserColor));

        foreach (Vector2Int start in Maze.wandererStarts)
            enemies.Add(SpawnActor("Wanderer (anak buah)", start, ChaseActorType.Wanderer, wandererSpeed, wandererColor));
    }

    private ChaseActor SpawnActor(string actorName, Vector2Int cell, ChaseActorType type, float speed, Color color)
    {
        Vector3 scale = Vector3.one * 0.7f * Maze.CellSize;
        GameObject go = MakeSprite(actorName, color, Maze.CellToWorld(cell), scale, 2);
        ChaseActor actor = go.AddComponent<ChaseActor>();
        actor.Init(this, Maze, cell, type, speed);
        return actor;
    }

    // Semua objek dijadikan anak dari manager ini supaya ikut hilang saat scene minigame
    // ditutup (objek baru yang tidak punya induk masuk ke scene VN, bukan scene ini).
    private GameObject MakeSprite(string objectName, Color color, Vector3 position, Vector3 scale, int sortingOrder)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private Sprite GetSquareSprite()
    {
        if (squareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width); // pixels-per-unit = lebar tekstur, jadi ukuran sprite = 1 unit
        }
        return squareSprite;
    }

    // ================= TAMPILAN SEMENTARA (timer & hasil) =================
    // Pakai IMGUI supaya tidak perlu Canvas/EventSystem di scene ini.
    // Ganti dengan UI asli nanti.

    private void OnGUI()
    {
        int size = Mathf.RoundToInt(Screen.height * 0.06f);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        style.normal.textColor = Color.white;

        string text;
        if (finished)
            text = won ? "LOLOS!" : "TERTANGKAP!";
        else if (!IsRunning)
            text = "Bersiap...";
        else
            text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)).ToString();

        GUI.Label(new Rect(0, 10, Screen.width, size * 2), text, style);

        GUIStyle hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.03f),
            alignment = TextAnchor.LowerCenter
        };
        hint.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
        GUI.Label(new Rect(0, Screen.height - size * 1.5f, Screen.width, size * 1.4f),
            "Panah / WASD untuk belok", hint);
    }
}

// Data maze: tembok, posisi awal, dan pencarian jalur untuk pengejar.
// Bisa dibangun dari layout teks atau dari Tilemap.
// Koordinat sel: x ke kanan, y ke ATAS.
public class ChaseMaze
{
    public static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public Vector2Int playerStart;
    public Vector2Int chaserStart;
    public readonly List<Vector2Int> wandererStarts = new List<Vector2Int>();

    // Ukuran satu sel di dunia game (1 untuk layout teks, mengikuti Grid untuk Tilemap)
    public float CellSize { get; private set; }
    // Batas maze di dunia game, dipakai untuk mengatur kamera
    public Bounds WorldBounds { get; private set; }

    // Khusus mode layout teks
    public int width;
    public int height;
    private bool[,] wall;

    // Khusus mode Tilemap
    private bool usesTilemap;
    private Tilemap wallMap;
    private Tilemap floorMap;
    private BoundsInt tileBounds;

    // ---------- Dari layout teks ----------
    public ChaseMaze(string[] layout)
    {
        usesTilemap = false;
        CellSize = 1f;

        height = layout.Length;
        width = 0;
        foreach (string row in layout)
            width = Mathf.Max(width, row.Length);

        wall = new bool[width, height];

        bool hasPlayer = false;
        bool hasChaser = false;
        Vector2Int firstFloor = new Vector2Int(-1, -1);

        for (int r = 0; r < height; r++)
        {
            int y = height - 1 - r; // baris pertama teks = bagian atas
            for (int x = 0; x < width; x++)
            {
                char ch = x < layout[r].Length ? layout[r][x] : '#';
                wall[x, y] = (ch == '#');
                if (ch == '#') continue;

                Vector2Int cell = new Vector2Int(x, y);
                if (firstFloor.x < 0) firstFloor = cell;

                if (ch == 'P') { playerStart = cell; hasPlayer = true; }
                else if (ch == 'C') { chaserStart = cell; hasChaser = true; }
                else if (ch == 'R') { wandererStarts.Add(cell); }
            }
        }

        if (!hasPlayer)
        {
            Debug.LogWarning("Layout maze tidak punya 'P' (awal pemain), memakai sel kosong pertama.");
            playerStart = firstFloor;
        }
        if (!hasChaser)
        {
            Debug.LogWarning("Layout maze tidak punya 'C' (pengejar utama), memakai sel kosong pertama.");
            chaserStart = firstFloor;
        }

        WorldBounds = new Bounds(Vector3.zero, new Vector3(width, height, 0f));
    }

    // ---------- Dari Tilemap ----------
    public ChaseMaze(Tilemap walls, Tilemap floor, Transform playerMarker, Transform chaserMarker, Transform[] wandererMarkers)
    {
        usesTilemap = true;
        wallMap = walls;
        floorMap = floor;

        // Rapatkan batas ke tile yang benar-benar terpakai
        wallMap.CompressBounds();
        tileBounds = wallMap.cellBounds;

        Bounds world = TilemapWorldBounds(wallMap);
        if (floorMap != null)
        {
            floorMap.CompressBounds();
            world.Encapsulate(TilemapWorldBounds(floorMap));
        }
        WorldBounds = world;

        // Ukuran sel di dunia game = Cell Size pada Grid x skala objek Grid
        GridLayout grid = wallMap.layoutGrid;
        CellSize = grid != null ? grid.cellSize.x * grid.transform.lossyScale.x : 1f;
        if (CellSize <= 0f) CellSize = 1f;

        playerStart = StartFromMarker(playerMarker, "Player Start");
        chaserStart = StartFromMarker(chaserMarker, "Chaser Start");

        if (wandererMarkers != null)
        {
            foreach (Transform marker in wandererMarkers)
            {
                if (marker != null)
                    wandererStarts.Add(WorldToCell(marker.position));
            }
        }
    }

    private static Bounds TilemapWorldBounds(Tilemap map)
    {
        BoundsInt cellBounds = map.cellBounds;
        Vector3 min = map.CellToWorld(cellBounds.min);
        Vector3 max = map.CellToWorld(cellBounds.max);

        Bounds bounds = new Bounds(min, Vector3.zero);
        bounds.Encapsulate(max);
        return bounds;
    }

    private Vector2Int StartFromMarker(Transform marker, string label)
    {
        if (marker == null)
        {
            Debug.LogWarning($"{label} belum diisi, memakai sel kosong pertama.");
            return FindFirstWalkable();
        }

        Vector2Int cell = WorldToCell(marker.position);
        if (IsWall(cell))
            Debug.LogWarning($"{label} berada di sel tembok atau di luar area jalan. Geser objeknya ke sel yang bisa dilewati.");
        return cell;
    }

    private Vector2Int FindFirstWalkable()
    {
        for (int y = tileBounds.yMin; y < tileBounds.yMax; y++)
        {
            for (int x = tileBounds.xMin; x < tileBounds.xMax; x++)
            {
                if (!IsWall(new Vector2Int(x, y)))
                    return new Vector2Int(x, y);
            }
        }
        return Vector2Int.zero;
    }

    // ---------- Pertanyaan dasar ----------

    // True kalau sel tidak bisa dilewati (tembok, atau di luar area jalan)
    public bool IsWall(Vector2Int cell)
    {
        if (!usesTilemap)
        {
            return cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height || wall[cell.x, cell.y];
        }

        Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0);

        if (wallMap.HasTile(tilePos)) return true;
        if (floorMap != null) return !floorMap.HasTile(tilePos);

        // Tanpa Floor Tilemap: sel di luar batas Wall Tilemap dianggap tidak bisa dilewati
        return cell.x < tileBounds.xMin || cell.x >= tileBounds.xMax
            || cell.y < tileBounds.yMin || cell.y >= tileBounds.yMax;
    }

    // Titik tengah sel di dunia game
    public Vector3 CellToWorld(Vector2Int cell)
    {
        if (!usesTilemap)
            return new Vector3(cell.x - (width - 1) / 2f, cell.y - (height - 1) / 2f, 0f);

        Vector3 position = wallMap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        position.z = 0f;
        return position;
    }

    public Vector2Int WorldToCell(Vector3 position)
    {
        if (!usesTilemap)
        {
            return new Vector2Int(
                Mathf.RoundToInt(position.x + (width - 1) / 2f),
                Mathf.RoundToInt(position.y + (height - 1) / 2f));
        }

        Vector3Int cell = wallMap.WorldToCell(position);
        return new Vector2Int(cell.x, cell.y);
    }

    // Langkah pertama pada jalur terpendek dari start ke goal (BFS).
    // Mengembalikan Vector2Int.zero kalau sudah sampai atau tidak ada jalur.
    public Vector2Int FirstStepTowards(Vector2Int start, Vector2Int goal)
    {
        if (start == goal) return Vector2Int.zero;

        var firstStep = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();

        foreach (Vector2Int d in Directions)
        {
            Vector2Int n = start + d;
            if (IsWall(n)) continue;
            firstStep[n] = d;
            if (n == goal) return d;
            queue.Enqueue(n);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int d in Directions)
            {
                Vector2Int n = current + d;
                if (IsWall(n) || n == start || firstStep.ContainsKey(n)) continue;

                firstStep[n] = firstStep[current];
                if (n == goal) return firstStep[n];
                queue.Enqueue(n);
            }
        }

        return Vector2Int.zero;
    }
}
