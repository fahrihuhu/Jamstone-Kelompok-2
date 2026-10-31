using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Dialogue Awal")]
    public DialogueData startingDialogue;

    [Header("UI - Dialogue Box")]
    public GameObject dialogueBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public CharacterStage stage;
    public Image backgroundUI;

    [Header("UI - Next Indicator")]
    [Tooltip("Sprite segitiga kebalik, muncul saat teks sudah selesai dimuat")]
    public GameObject nextIndicator;

    [Header("Audio")]
    [Tooltip("AudioSource untuk efek suara per baris (SFX)")]
    public AudioSource sfxSource;

    [Header("History")]
    [Tooltip("Boleh dikosongkan kalau belum memakai fitur history")]
    public DialogueHistory history;

    [Header("UI - Pilihan")]
    public GameObject choicePanel;
    public Button choiceButtonPrefab;

    [Header("Teks")]
    [Tooltip("Jeda per huruf (detik). Set 0 supaya teks langsung muncul (berguna untuk tes)")]
    public float typeDelay = 0.02f;

    [Header("Transisi Antar Scene (Fade)")]
    [Tooltip("CanvasGroup pada Image hitam penuh layar di dalam Vn Canvas. Kosongkan untuk mematikan fade")]
    public CanvasGroup fadeOverlay;
    [Tooltip("Durasi fade out, dan juga fade in (detik)")]
    public float fadeDuration = 0.5f;
    [Tooltip("Lama layar tetap gelap sebelum scene berikutnya muncul (detik)")]
    public float blackHoldDuration = 0.3f;

    [Header("Minigame")]
    [Tooltip("Root Canvas VN, disembunyikan selama minigame berjalan")]
    public GameObject vnCanvas;

    // Akses cepat dari scene minigame: GameManager.Instance.ReportMinigameResult(...)
    public static GameManager Instance { get; private set; }
    private string minigameResult = null;
    private bool inMinigame = false;
    private bool isTransitioning = false;
    private Coroutine choicesCoroutine;

    // State saat ini
    private DialogueData currentDialogue;
    private int currentIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;
    private string currentFullText = "";
    private bool currentHasChoices;
    private List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    // Story flags: "ingatan" pilihan pemain, bertahan sepanjang playthrough (dan ikut ke-save)
    private Dictionary<string, string> storyFlags = new Dictionary<string, string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Datang dari tombol Continue: muat save, bukan mulai dari awal
        if (SaveManager.LoadRequested)
        {
            SaveManager.LoadRequested = false;
            if (SaveManager.Instance != null && SaveManager.Instance.LoadGame())
                return;
        }

        if (startingDialogue != null)
            StartDialogue(startingDialogue, 0);
    }

    public void StartDialogue(DialogueData data, int startIndex)
    {
        currentDialogue = data;
        currentIndex = startIndex;
        dialogueBox.SetActive(true);
        ShowLine();
    }

    private void ShowLine()
    {
        ClearChoices();
        SetIndicator(false);

        if (currentDialogue == null || currentIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentIndex];

        // Cek kondisi: kalau baris ini punya syarat flag dan tidak terpenuhi, skip ke baris berikutnya
        if (!string.IsNullOrEmpty(line.conditionKey))
        {
            string currentValue = GetFlag(line.conditionKey);
            if (currentValue != line.conditionValue)
            {
                currentIndex++;
                ShowLine();
                return;
            }
        }

        // Baris pemicu minigame: buka minigame, teks baris ini tidak ditampilkan
        if (!string.IsNullOrEmpty(line.minigameSceneName))
        {
            StopTextRoutines();
            StartCoroutine(RunMinigame(line.minigameSceneName));
            return;
        }

        nameText.text = line.GetDisplayName();
        nameText.color = line.character != null ? line.character.nameColor : Color.white;
        dialogueText.fontStyle = line.isThought ? FontStyles.Italic : FontStyles.Normal;

        // Panggung: jalankan perintah (muncul/hilang/ganti ekspresi), lalu sorot yang bicara
        if (stage != null)
        {
            stage.ExecuteCommands(line.stageCommands);
            stage.SetSpeaker(line.character);
        }

        if (line.backgroundOverride != null)
            backgroundUI.sprite = line.backgroundOverride;

        // Efek suara per baris (misal: Brukk)
        if (line.sfxClip != null && sfxSource != null)
            sfxSource.PlayOneShot(line.sfxClip, line.sfxVolume);

        // Catat ke history (baris yang dilewati karena syarat flag tidak sampai ke sini)
        if (history != null)
            history.AddLine(line.GetDisplayName(), line.dialogueText, line.isThought);

        bool hasChoices = line.choices != null && line.choices.Length > 0;

        StopTextRoutines();
        currentFullText = line.dialogueText;
        currentHasChoices = hasChoices;
        typingCoroutine = StartCoroutine(TypeText(line.dialogueText, hasChoices));

        if (hasChoices)
            choicesCoroutine = StartCoroutine(ShowChoicesAfterTyping(line.choices));
    }

    private IEnumerator TypeText(string text, bool hasChoices)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            if (typeDelay > 0f)
                yield return new WaitForSeconds(typeDelay);
        }
        isTyping = false;
        typingCoroutine = null;

        // Teks sudah utuh: tampilkan indikator (kecuali baris ini punya pilihan)
        if (!hasChoices)
            SetIndicator(true);
    }

    private IEnumerator ShowChoicesAfterTyping(DialogueChoice[] choices)
    {
        while (isTyping) yield return null;
        ShowChoices(choices);
    }

    public void OnAdvanceClicked()
    {
        // Selama minigame berjalan, klik dari VN diabaikan
        if (inMinigame || isTransitioning) return;

        // Tidak ada dialog yang sedang berjalan
        if (currentDialogue == null || currentIndex >= currentDialogue.lines.Length) return;

        // Teks masih diketik: klik langsung menampilkan teks penuh
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            dialogueText.text = currentFullText;
            isTyping = false;
            if (!currentHasChoices) SetIndicator(true);
            return;
        }

        // Baris dengan pilihan: pemain harus memilih lewat tombol, bukan next
        if (currentDialogue.lines[currentIndex].choices != null &&
            currentDialogue.lines[currentIndex].choices.Length > 0)
            return;

        SetIndicator(false); // Sembunyikan lagi setelah diklik
        currentIndex++;
        ShowLine();
    }

    private void ShowChoices(DialogueChoice[] choices)
    {
        choicePanel.SetActive(true);
        foreach (var choice in choices)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicePanel.transform);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.choiceText;
            btn.onClick.AddListener(() => OnChoiceSelected(choice));
            spawnedChoiceButtons.Add(btn.gameObject);
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        ClearChoices();

        if (history != null)
            history.AddChoice(choice.choiceText);

        // Simpan flag kalau pilihan ini perlu "diingat" untuk scene lain
        if (!string.IsNullOrEmpty(choice.setFlagKey))
            SetFlag(choice.setFlagKey, choice.setFlagValue);

        if (choice.nextDialogue != null)
        {
            int startAt = choice.nextLineIndex >= 0 ? choice.nextLineIndex : 0;
            GoToDialogue(choice.nextDialogue, startAt);
        }
        else if (choice.nextLineIndex >= 0)
        {
            currentIndex = choice.nextLineIndex;
            ShowLine();
        }
    }

    private void ClearChoices()
    {
        foreach (var btn in spawnedChoiceButtons)
            Destroy(btn);
        spawnedChoiceButtons.Clear();
        choicePanel.SetActive(false);
    }

    private void SetIndicator(bool show)
    {
        if (nextIndicator != null)
            nextIndicator.SetActive(show);
    }

    private void EndDialogue()
    {
        if (currentDialogue != null)
        {
            // 1. Cek lanjutan bersyarat dulu (berdasarkan story flag)
            if (currentDialogue.conditionalNexts != null)
            {
                foreach (var cn in currentDialogue.conditionalNexts)
                {
                    if (cn.nextDialogue != null && GetFlag(cn.conditionKey) == cn.conditionValue)
                    {
                        GoToDialogue(cn.nextDialogue, 0);
                        return;
                    }
                }
            }

            // 2. Kalau tidak ada yang cocok, pakai lanjutan biasa
            if (currentDialogue.nextDialogue != null)
            {
                GoToDialogue(currentDialogue.nextDialogue, 0);
                return;
            }
        }

        Debug.Log("Dialogue selesai dan tidak ada DialogueData berikutnya.");
        SetIndicator(false);
        dialogueBox.SetActive(false);
    }

    // ================= TRANSISI ANTAR SCENE =================

    // Hentikan hanya coroutine teks dan pilihan. StopAllCoroutines() tidak dipakai
    // karena akan ikut mematikan fade dan minigame yang sedang berjalan.
    private void StopTextRoutines()
    {
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        if (choicesCoroutine != null) { StopCoroutine(choicesCoroutine); choicesCoroutine = null; }
        isTyping = false;
    }

    // Pindah ke DialogueData lain. Fade hanya dipakai kalau DialogueData tujuan
    // mencentang "Fade Transition" (artinya itu scene baru, bukan lanjutan 1 -> 1A).
    private void GoToDialogue(DialogueData next, int startAt)
    {
        if (next.fadeTransition && fadeOverlay != null)
            StartCoroutine(TransitionToDialogue(next, startAt));
        else
            StartDialogue(next, startAt);
    }

    private IEnumerator TransitionToDialogue(DialogueData next, int startAt)
    {
        isTransitioning = true;

        yield return Fade(0f, 1f);                              // fade out ke hitam
        yield return new WaitForSecondsRealtime(blackHoldDuration);

        StartDialogue(next, startAt);                           // ganti karakter/background saat layar gelap

        yield return Fade(1f, 0f);                              // fade in
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeOverlay.blocksRaycasts = true; // klik tidak tembus selama fade
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadeOverlay.alpha = to;
        fadeOverlay.blocksRaycasts = to > 0f;
    }

    // ================= MINIGAME =================

    private IEnumerator RunMinigame(string sceneName)
    {
        inMinigame = true;
        minigameResult = null;
        if (vnCanvas != null) vnCanvas.SetActive(false);

        // Buka scene minigame di atas scene VN (VN tetap hidup, state aman)
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Tunggu sampai minigame melapor hasilnya
        while (minigameResult == null)
            yield return null;

        // Hasil disimpan sebagai flag, dipakai untuk percabangan dialog
        SetFlag("minigame_result", minigameResult);

        yield return SceneManager.UnloadSceneAsync(sceneName);

        if (vnCanvas != null) vnCanvas.SetActive(true);
        inMinigame = false;

        // Lanjut ke baris setelah titik minigame
        currentIndex++;
        ShowLine();
    }

    // Dipanggil oleh script di scene minigame saat permainan selesai.
    // Nilai default untuk minigame kejar-kejaran; minigame lain bisa memakai nilai sendiri,
    // misalnya ReportMinigameResult(lulus, "lulus", "gagal").
    public void ReportMinigameResult(bool won, string winValue = "lolos", string loseValue = "tertangkap")
    {
        minigameResult = won ? winValue : loseValue;
    }

    // ================= STORY FLAGS =================

    public void SetFlag(string key, string value) => storyFlags[key] = value;

    public string GetFlag(string key) =>
        storyFlags.TryGetValue(key, out var value) ? value : "";

    // ================= SAVE / LOAD =================

    // Data yang dibaca SaveManager saat menyimpan
    public DialogueData CurrentDialogue => currentDialogue;
    public int CurrentLineIndex => currentIndex;
    public Dictionary<string, string> GetFlagsCopy() => new Dictionary<string, string>(storyFlags);

    // Save hanya boleh saat dialog berjalan normal (bukan saat minigame atau fade)
    public bool CanSave => currentDialogue != null && !inMinigame && !isTransitioning;

    // Dipanggil SaveManager saat memuat save
    public void RestoreState(DialogueData data, int lineIndex, Dictionary<string, string> flags)
    {
        StopTextRoutines();
        ClearChoices();

        storyFlags = flags != null
            ? new Dictionary<string, string>(flags)
            : new Dictionary<string, string>();

        if (data.lines == null || data.lines.Length == 0)
        {
            StartDialogue(data, 0);
            return;
        }

        lineIndex = Mathf.Clamp(lineIndex, 0, data.lines.Length - 1);

        // Putar ulang perintah panggung & background dari baris 0 sampai sebelum baris tujuan,
        // supaya karakter dan latar di layar sama seperti saat di-save
        for (int i = 0; i < lineIndex; i++)
        {
            DialogueLine l = data.lines[i];
            if (!string.IsNullOrEmpty(l.conditionKey) && GetFlag(l.conditionKey) != l.conditionValue) continue;
            if (!string.IsNullOrEmpty(l.minigameSceneName)) continue;

            if (stage != null) stage.ExecuteCommands(l.stageCommands);
            if (l.backgroundOverride != null) backgroundUI.sprite = l.backgroundOverride;
        }

        StartDialogue(data, lineIndex);
    }
}