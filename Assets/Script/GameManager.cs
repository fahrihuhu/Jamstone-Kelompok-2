using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Dialogue Awal")]
    public DialogueData startingDialogue;

    [Header("UI - Dialogue Box")]
    public GameObject dialogueBox;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image characterSpriteUI;
    public Image backgroundUI;

    [Header("UI - Pilihan")]
    public GameObject choicePanel;
    public Button choiceButtonPrefab;

    // State saat ini
    private DialogueData currentDialogue;
    private int currentIndex;
    private bool isTyping;
    private List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    // Story flags: "ingatan" pilihan pemain, bertahan sepanjang playthrough (dan ikut ke-save)
    private Dictionary<string, string> storyFlags = new Dictionary<string, string>();

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

        nameText.text = line.GetDisplayName();
        nameText.color = line.character != null ? line.character.nameColor : Color.white;
        dialogueText.fontStyle = line.isThought ? FontStyles.Italic : FontStyles.Normal;

        if (line.character != null)
        {
            Sprite exp = line.character.GetExpression(line.expression);
            characterSpriteUI.enabled = exp != null;
            characterSpriteUI.sprite = exp;
        }
        else
        {
            characterSpriteUI.enabled = false;
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
            dialogueText.text = currentDialogue.lines[currentIndex].dialogueText;
            isTyping = false;
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
        // Kalau dialog ini punya lanjutan otomatis, langsung pindah ke sana
        if (currentDialogue != null && currentDialogue.nextDialogue != null)
        {
            StartDialogue(currentDialogue.nextDialogue, 0);
            return;
        }

        Debug.Log("Dialogue selesai dan tidak ada DialogueData berikutnya.");
        dialogueBox.SetActive(false);
    }

    // ================= STORY FLAGS =================

    public void SetFlag(string key, string value) => storyFlags[key] = value;

    public string GetFlag(string key) =>
        storyFlags.TryGetValue(key, out var value) ? value : "";

    // ================= SAVE / LOAD =================
    // Sengaja belum diimplementasi dulu - nanti tinggal aktifkan lagi
    // kalau SaveManager & SaveData sudah di-setup di project.
}