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

    [Header("UI - Pilihan")]
    public GameObject choicePanel;
    public Button choiceButtonPrefab;

    [Header("Minigame")]
    [Tooltip("Root Canvas VN, disembunyikan selama minigame berjalan")]
    public GameObject vnCanvas;

    // Akses cepat dari scene minigame: GameManager.Instance.ReportMinigameResult(...)
    public static GameManager Instance { get; private set; }
    private string minigameResult = null;

    // State saat ini
    private DialogueData currentDialogue;
    private int currentIndex;
    private bool isTyping;
    private List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    // Story flags: "ingatan" pilihan pemain, bertahan sepanjang playthrough (dan ikut ke-save)
    private Dictionary<string, string> storyFlags = new Dictionary<string, string>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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
            StopAllCoroutines();
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

        StopAllCoroutines();
        StartCoroutine(TypeText(line.dialogueText));

        if (line.choices != null && line.choices.Length > 0)
            StartCoroutine(ShowChoicesAfterTyping(line.choices));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f);
        }
        isTyping = false;
    }

    private IEnumerator ShowChoicesAfterTyping(DialogueChoice[] choices)
    {
        while (isTyping) yield return null;
        ShowChoices(choices);
    }

    public void OnAdvanceClicked()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            DialogueLine typingLine = currentDialogue.lines[currentIndex];
            dialogueText.text = typingLine.dialogueText;
            isTyping = false;

            // Coroutine pilihan ikut berhenti oleh StopAllCoroutines, jadi munculkan langsung
            if (typingLine.choices != null && typingLine.choices.Length > 0)
                ShowChoices(typingLine.choices);
            return;
        }

        if (currentDialogue.lines[currentIndex].choices != null &&
            currentDialogue.lines[currentIndex].choices.Length > 0)
            return;

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

        // Simpan flag kalau pilihan ini perlu "diingat" untuk scene lain
        if (!string.IsNullOrEmpty(choice.setFlagKey))
            SetFlag(choice.setFlagKey, choice.setFlagValue);

        if (choice.nextDialogue != null)
        {
            int startAt = choice.nextLineIndex >= 0 ? choice.nextLineIndex : 0;
            StartDialogue(choice.nextDialogue, startAt);
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
                        StartDialogue(cn.nextDialogue, 0);
                        return;
                    }
                }
            }

            // 2. Kalau tidak ada yang cocok, pakai lanjutan biasa
            if (currentDialogue.nextDialogue != null)
            {
                StartDialogue(currentDialogue.nextDialogue, 0);
                return;
            }
        }

        Debug.Log("Dialogue selesai dan tidak ada DialogueData berikutnya.");
        dialogueBox.SetActive(false);
    }

    // ================= MINIGAME =================

    private IEnumerator RunMinigame(string sceneName)
    {
        minigameResult = null;
        if (vnCanvas != null) vnCanvas.SetActive(false);

        // Buka scene minigame di atas scene VN (VN tetap hidup, state aman)
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Tunggu sampai minigame melapor hasilnya
        while (minigameResult == null)
            yield return null;

        SetFlag("minigame_result", minigameResult);

        yield return SceneManager.UnloadSceneAsync(sceneName);

        if (vnCanvas != null) vnCanvas.SetActive(true);

        // Lanjut ke baris setelah titik minigame
        currentIndex++;
        ShowLine();
    }

    // Dipanggil oleh script di scene minigame saat permainan selesai
    public void ReportMinigameResult(bool won)
    {
        minigameResult = won ? "lolos" : "tertangkap";
    }

    // ================= STORY FLAGS =================

    public void SetFlag(string key, string value) => storyFlags[key] = value;

    public string GetFlag(string key) =>
        storyFlags.TryGetValue(key, out var value) ? value : "";

    // ================= SAVE / LOAD =================
    // Sengaja belum diimplementasi dulu - nanti tinggal aktifkan lagi
    // kalau SaveManager & SaveData sudah di-setup di project.
}