using System.Collections.Generic;
using UnityEngine;

public enum ChaseActorType
{
    Player,    // terus berlari, arahnya diatur input
    Chaser,    // mengikuti pemain lewat jalur terpendek
    Wanderer   // bergerak acak di tiap persimpangan
}

// Satu aktor di maze. Bergerak dari sel ke sel dengan halus.
// Dibuat dan diatur oleh ChaseMinigame, jadi tidak perlu ditempel manual.
public class ChaseActor : MonoBehaviour
{
    private ChaseMinigame game;
    private ChaseMaze maze;
    private ChaseActorType type;
    private float speed;

    private Vector2Int currentCell;     // sel terakhir yang dicapai
    private Vector2Int targetCell;      // sel yang sedang dituju
    private Vector2Int direction;       // arah gerak terakhir
    private Vector2Int desiredDirection; // arah yang diminta pemain (hanya untuk Player)
    private bool isMoving;

    // Sel yang paling dekat dengan posisi aktor saat ini
    public Vector2Int Cell
    {
        get { return maze != null ? maze.WorldToCell(transform.position) : Vector2Int.zero; }
    }

    public void Init(ChaseMinigame owner, ChaseMaze mazeData, Vector2Int startCell, ChaseActorType actorType, float moveSpeed)
    {
        game = owner;
        maze = mazeData;
        type = actorType;
        speed = moveSpeed;

        currentCell = startCell;
        targetCell = startCell;
        direction = Vector2Int.zero;
        desiredDirection = Vector2Int.zero;
        isMoving = false;

        transform.position = maze.CellToWorld(startCell);
    }

    // Dipanggil saat pemain menekan arah. Arah disimpan dan dipakai di persimpangan berikutnya
    // yang memungkinkan. Kalau berlawanan dengan arah sekarang, putar balik langsung.
    public void Steer(Vector2Int newDirection)
    {
        desiredDirection = newDirection;

        if (isMoving && newDirection == -direction)
        {
            Vector2Int previous = currentCell;
            currentCell = targetCell;
            targetCell = previous;
            direction = newDirection;
        }
    }

    private void Update()
    {
        if (game == null || !game.IsRunning) return;

        // speed dalam "sel per detik", diubah ke satuan dunia game sesuai ukuran sel
        float remaining = speed * maze.CellSize * Time.deltaTime;

        // Sisa jarak dipakai ke sel berikutnya supaya tidak ada jeda di tiap sel
        int safety = 4;
        while (remaining > 0f && safety-- > 0)
        {
            if (!isMoving)
            {
                Vector2Int next = ChooseDirection();
                if (next == Vector2Int.zero) break; // tidak bisa bergerak: diam

                direction = next;
                targetCell = currentCell + direction;
                isMoving = true;
            }

            Vector3 targetPosition = maze.CellToWorld(targetCell);
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (remaining >= distance)
            {
                transform.position = targetPosition;
                remaining -= distance;
                currentCell = targetCell;
                isMoving = false;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, remaining);
                remaining = 0f;
            }
        }
    }

    // ================= MEMILIH ARAH DI TIAP SEL =================

    private Vector2Int ChooseDirection()
    {
        switch (type)
        {
            case ChaseActorType.Player: return PlayerDirection();
            case ChaseActorType.Chaser: return ChaserDirection();
            default:                    return WandererDirection();
        }
    }

    private bool CanMove(Vector2Int from, Vector2Int dir)
    {
        return !maze.IsWall(from + dir);
    }

    // Pemain: belok ke arah yang diminta kalau jalannya terbuka, kalau tidak lanjut lurus.
    // Kalau buntu, berhenti sampai ada arah baru yang valid.
    private Vector2Int PlayerDirection()
    {
        if (desiredDirection != Vector2Int.zero && CanMove(currentCell, desiredDirection))
            return desiredDirection;

        if (direction != Vector2Int.zero && CanMove(currentCell, direction))
            return direction;

        return Vector2Int.zero;
    }

    // Pengejar utama: selalu ambil langkah pertama dari jalur terpendek menuju pemain.
    private Vector2Int ChaserDirection()
    {
        if (game.Player == null) return Vector2Int.zero;
        return maze.FirstStepTowards(currentCell, game.Player.Cell);
    }

    // Pengejar acak: pilih arah acak di tiap sel, tidak berbalik arah kecuali di jalan buntu.
    private Vector2Int WandererDirection()
    {
        var options = new List<Vector2Int>();
        foreach (Vector2Int d in ChaseMaze.Directions)
        {
            if (d == -direction) continue;
            if (CanMove(currentCell, d)) options.Add(d);
        }

        if (options.Count > 0)
            return options[Random.Range(0, options.Count)];

        // Jalan buntu: satu-satunya jalan adalah balik
        if (CanMove(currentCell, -direction)) return -direction;
        return Vector2Int.zero;
    }
}
