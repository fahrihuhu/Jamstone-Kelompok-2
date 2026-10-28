using UnityEngine;
using UnityEngine.UI;

// Taruh di objek "CharacterStage" di dalam Canvas VN.
// Isi array Slots dengan 3 Image berurutan: Kiri, Tengah, Kanan.
// Sprite menetap di slotnya sampai ada perintah Hide / HideAll.
// Yang sedang bicara tampil terang, yang lain diredupkan.
public class CharacterStage : MonoBehaviour
{
    [Header("Slot Posisi")]
    [Tooltip("Urutan: 0 Left, 1 Center, 2 Right, 3 FarLeft, 4 FarRight")]
    public Image[] slots;

    [Header("Efek")]
    [Tooltip("Tingkat terang karakter yang tidak sedang bicara (1 = terang penuh)")]
    [Range(0f, 1f)] public float dimBrightness = 0.6f;

    [Tooltip("Lama transisi fade masuk/keluar dan terang/redup (detik)")]
    public float transitionDuration = 0.25f;

    private CharacterData[] occupants;   // siapa yang menempati tiap slot
    private float[] targetAlpha;
    private float[] targetBrightness;

    private void Awake()
    {
        int n = slots.Length;
        occupants = new CharacterData[n];
        targetAlpha = new float[n];
        targetBrightness = new float[n];

        for (int i = 0; i < n; i++)
        {
            targetBrightness[i] = 1f;
            if (slots[i] == null) continue;

            slots[i].preserveAspect = true;
            slots[i].raycastTarget = false;
            slots[i].color = new Color(1f, 1f, 1f, 0f); // mulai transparan
        }
    }

    // Transisi halus: warna bergerak pelan menuju target tiap frame
    private void Update()
    {
        float step = transitionDuration > 0f ? Time.deltaTime / transitionDuration : 1f;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            Color c = slots[i].color;
            float a = Mathf.MoveTowards(c.a, targetAlpha[i], step);
            float b = Mathf.MoveTowards(c.r, targetBrightness[i], step);
            slots[i].color = new Color(b, b, b, a);
        }
    }

    // ================= PERINTAH PANGGUNG =================

    public void ExecuteCommands(StageCommand[] commands)
    {
        if (commands == null) return;

        foreach (StageCommand cmd in commands)
        {
            switch (cmd.action)
            {
                case StageAction.Show:             Show(cmd); break;
                case StageAction.Hide:             Hide(cmd.character); break;
                case StageAction.ChangeExpression: ChangeExpression(cmd.character, cmd.expression); break;
                case StageAction.HideAll:          HideAll(); break;
            }
        }
    }

    private void Show(StageCommand cmd)
    {
        if (cmd.character == null)
        {
            Debug.LogWarning("Perintah Show tanpa karakter, dilewati.");
            return;
        }

        int slot = (int)cmd.position;
        if (slot >= slots.Length || slots[slot] == null)
        {
            Debug.LogWarning($"Slot untuk posisi {cmd.position} tidak tersedia di CharacterStage.");
            return;
        }

        Sprite sprite = cmd.character.GetExpression(cmd.expression);
        if (sprite == null) return; // peringatan sudah dikeluarkan oleh GetExpression

        // Kalau karakter ini sudah ada di slot lain, pindahkan (hilangkan dari slot lama)
        int existing = FindSlot(cmd.character);
        if (existing >= 0 && existing != slot)
        {
            occupants[existing] = null;
            targetAlpha[existing] = 0f;
        }

        occupants[slot] = cmd.character;
        slots[slot].sprite = sprite;
        targetAlpha[slot] = 1f;
    }

    private void Hide(CharacterData character)
    {
        int slot = FindSlot(character);
        if (slot < 0) return;

        occupants[slot] = null;
        targetAlpha[slot] = 0f; // sprite dibiarkan sampai fade selesai
    }

    private void ChangeExpression(CharacterData character, string expression)
    {
        int slot = FindSlot(character);
        if (slot < 0)
        {
            Debug.LogWarning("ChangeExpression: karakter belum ada di panggung.");
            return;
        }

        Sprite sprite = character.GetExpression(expression);
        if (sprite != null)
            slots[slot].sprite = sprite;
    }

    public void HideAll()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            occupants[i] = null;
            targetAlpha[i] = 0f;
        }
    }

    // ================= SOROTAN PEMBICARA =================

    // speaker = null (narasi): semua terang.
    // speaker terisi: hanya dia yang terang. Kalau dia tidak ada di panggung, semua redup.
    public void SetSpeaker(CharacterData speaker)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool bright = speaker == null || occupants[i] == speaker;
            targetBrightness[i] = bright ? 1f : dimBrightness;
        }
    }

    private int FindSlot(CharacterData character)
    {
        if (character == null) return -1;
        for (int i = 0; i < occupants.Length; i++)
        {
            if (occupants[i] == character) return i;
        }
        return -1;
    }
}
