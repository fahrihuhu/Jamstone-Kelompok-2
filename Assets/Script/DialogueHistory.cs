using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Satu entri riwayat. Hanya berisi tipe sederhana supaya mudah ikut disimpan ke save nanti.
[System.Serializable]
public class HistoryEntry
{
    public string speaker;   // kosong = narasi
    public string text;
    public bool isThought;
    public bool isChoice;    // true = catatan pilihan yang diambil pemain
}

// Taruh di GameObject kosong sendiri (misal "DialogueHistory") yang SELALU aktif,
// jangan di dalam HistoryPanel (panel itu dimatikan saat tidak dipakai).
public class DialogueHistory : MonoBehaviour
{
    [Header("UI")]
    public GameObject historyPanel;
    public ScrollRect scrollRect;
    [Tooltip("Content di dalam ScrollView (tempat entri dibuat)")]
    public Transform contentParent;
    [Tooltip("Prefab berisi satu TextMeshProUGUI untuk satu entri")]
    public TextMeshProUGUI entryPrefab;

    [Header("Sembunyikan Saat History Terbuka")]
    [Tooltip("UI yang disembunyikan selama panel history terbuka, lalu ditampilkan lagi saat ditutup. Contoh: DialogueBox, HistoryButton, ChoicePanel.")]
    public GameObject[] hideWhileOpen;

    [Header("Pengaturan")]
    [Tooltip("Entri terlama dibuang kalau jumlahnya melebihi batas ini")]
    public int maxEntries = 100;

    private readonly List<HistoryEntry> entries = new List<HistoryEntry>();

    // Untuk keperluan save/load nanti
    public List<HistoryEntry> Entries => entries;
    

    // ================= PENCATATAN (dipanggil GameManager) =================

    public void AddLine(string speaker, string text, bool isThought)
    {
        if (string.IsNullOrEmpty(text)) return;

        Add(new HistoryEntry { speaker = speaker, text = text, isThought = isThought });
    }

    public void AddChoice(string choiceText)
    {
        Add(new HistoryEntry { text = choiceText, isChoice = true });
    }

    public void Clear()
    {
        entries.Clear();
    }

    private void Add(HistoryEntry entry)
    {
        entries.Add(entry);
        if (entries.Count > maxEntries)
            entries.RemoveAt(0);
    }

    // ================= PANEL (dihubungkan ke tombol) =================

    public void Open()
    {
        
        SetOtherUIVisible(false);
        historyPanel.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        
        historyPanel.SetActive(false);
        SetOtherUIVisible(true);
    }

    // Disembunyikan lewat CanvasGroup (bukan SetActive), supaya tidak bentrok dengan
    // GameManager yang juga mengatur aktif/nonaktif objek seperti ChoicePanel.
    private void SetOtherUIVisible(bool visible)
    {
        if (hideWhileOpen == null) return;

        foreach (GameObject target in hideWhileOpen)
        {
            if (target == null) continue;

            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();

            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }

    private void Rebuild()
    {
        // Hapus entri lama. Dinonaktifkan dulu supaya layout langsung mengabaikannya.
        foreach (Transform child in contentParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (HistoryEntry e in entries)
        {
            TextMeshProUGUI label = Instantiate(entryPrefab, contentParent);
            label.text = Format(e);
        }

        StartCoroutine(ScrollToBottom());
    }

    private string Format(HistoryEntry e)
    {
        if (e.isChoice)
            return $"<color=#FFD966>\u25B6 Kamu memilih: {e.text}</color>";

        string body = e.isThought ? $"<i>{e.text}</i>" : e.text;

        // Narasi (tanpa nama pembicara) ditampilkan tanpa judul
        if (string.IsNullOrEmpty(e.speaker))
            return $"<color=#BBBBBB>{body}</color>";

        return $"<b>{e.speaker}</b>\n{body}";
    }

    // Layout baru selesai dihitung setelah satu frame, baru bisa scroll ke entri terbaru
    private IEnumerator ScrollToBottom()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentParent);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}