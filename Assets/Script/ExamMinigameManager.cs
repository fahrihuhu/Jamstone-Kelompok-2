using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ExamType
{
    UH,  // Ulangan Harian (10 Soal, 60 Detik)
    UTS, // Ujian Tengah Semester (15 Soal, 75 Detik)
    UAS  // Ujian Akhir Semester (20 Soal, 90 Detik)
}

[System.Serializable]
public class ExamQuestion
{
    [TextArea(2, 4)]
    public string questionText;
    public string[] options = new string[4]; // 4 Pilihan Jawaban (A, B, C, D)
    public int correctAnswerIndex;           // Index jawaban benar (0=A, 1=B, 2=C, 3=D)
}

public class ExamMinigameManager : MonoBehaviour
{
    [Header("Pengaturan Tipe Ujian")]
    public ExamType examType = ExamType.UH;

    [Header("Bank Soal Berdasarkan Tingkat Kesulitan")]
    [Tooltip("Daftar soal kategori SUSAH (Digunakan jika Intelligence Tono 1 - 4)")]
    public ExamQuestion[] hardQuestions;

    [Tooltip("Daftar soal kategori SEDANG (Digunakan jika Intelligence Tono 5 - 7)")]
    public ExamQuestion[] mediumQuestions;

    [Tooltip("Daftar soal kategori MUDAH (Digunakan jika Intelligence Tono 8 - 10)")]
    public ExamQuestion[] easyQuestions;

    [Header("Pengaturan Nilai")]
    [Tooltip("Nilai minimal untuk lulus. Nilai SAMA DENGAN angka ini sudah dihitung lulus")]
    public float passingScore = 80f;

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI questionContentText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Result UI (Opsional)")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultStatusText;
    public TextMeshProUGUI finalScoreText;

    private ExamQuestion[] activeQuestions; // Array soal yang akhirnya dipakai
    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;
    private float totalTime;
    private float timeRemaining;
    private bool isGameActive = false;

    private void Awake()
    {
        // 1. Atur batas waktu berdasarkan Tipe Ujian
        switch (examType)
        {
            case ExamType.UH:
                totalTime = 60f;
                break;
            case ExamType.UTS:
                totalTime = 75f;
                break;
            case ExamType.UAS:
                totalTime = 90f;
                break;
        }

        // 2. Tentukan Bank Soal berdasarkan Intelligence Tono dari StateManager
        SelectQuestionBankByIntelligence();
    }

    private void SelectQuestionBankByIntelligence()
    {
        int intel = 8; // Default fallback jika StateManager belum terhubung (tes langsung dari scene ujian)

        if (StateManager.Instance != null)
        {
            intel = StateManager.Instance.intelligence;
        }

        // Penentuan Kategori Soal berdasarkan Range Intelligence
        if (intel <= 4)
        {
            activeQuestions = hardQuestions;
            Debug.Log($"[Exam] Intelligence Tono ({intel}): Menggunakan Soal SUSAH.");
        }
        else if (intel <= 7)
        {
            activeQuestions = mediumQuestions;
            Debug.Log($"[Exam] Intelligence Tono ({intel}): Menggunakan Soal SEDANG.");
        }
        else
        {
            activeQuestions = easyQuestions;
            Debug.Log($"[Exam] Intelligence Tono ({intel}): Menggunakan Soal MUDAH.");
        }

        // Fallback: kalau bank untuk tingkat ini kosong, pakai bank lain yang terisi
        // supaya pemain tidak otomatis gagal dan kena pengurangan stat.
        if (activeQuestions == null || activeQuestions.Length == 0)
        {
            Debug.LogWarning("[Exam] Bank soal untuk tingkat ini kosong, memakai bank lain yang terisi.");

            if (mediumQuestions != null && mediumQuestions.Length > 0) activeQuestions = mediumQuestions;
            else if (easyQuestions != null && easyQuestions.Length > 0) activeQuestions = easyQuestions;
            else if (hardQuestions != null && hardQuestions.Length > 0) activeQuestions = hardQuestions;
            else Debug.LogError("[Exam] Semua bank soal kosong! Isi minimal satu bank di Inspector.");
        }
    }

    private void Start()
    {
        timeRemaining = totalTime;
        currentQuestionIndex = 0;
        correctAnswersCount = 0;
        isGameActive = true;

        if (resultPanel != null) resultPanel.SetActive(false);
        ShowQuestion();
    }

    private void Update()
    {
        if (!isGameActive) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            timeRemaining = 0;
            UpdateTimerUI();
            EndExam();
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ShowQuestion()
    {
        if (activeQuestions == null || activeQuestions.Length == 0 || currentQuestionIndex >= activeQuestions.Length)
        {
            EndExam();
            return;
        }

        ExamQuestion q = activeQuestions[currentQuestionIndex];
        if (questionNumberText != null)
            questionNumberText.text = $"Soal No. {currentQuestionIndex + 1} / {activeQuestions.Length}";

        if (questionContentText != null)
            questionContentText.text = q.questionText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = $"{(char)('A' + i)}. {q.options[i]}";

                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnAnswerSelected(int selectedIndex)
    {
        if (!isGameActive) return;
        if (activeQuestions == null || currentQuestionIndex >= activeQuestions.Length) return;

        if (selectedIndex == activeQuestions[currentQuestionIndex].correctAnswerIndex)
            correctAnswersCount++;

        currentQuestionIndex++;
        ShowQuestion();
    }

    private void EndExam()
    {
        if (!isGameActive) return; // cegah terpanggil dua kali
        isGameActive = false;

        float finalScore = (activeQuestions != null && activeQuestions.Length > 0)
            ? ((float)correctAnswersCount / activeQuestions.Length) * 100f
            : 0f;

        // Nilai sama dengan passingScore dihitung lulus
        bool isPassed = finalScore >= passingScore;

        StartCoroutine(FinishSequence(isPassed, finalScore));
    }

    private IEnumerator FinishSequence(bool isPassed, float finalScore)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultStatusText != null)
                resultStatusText.text = isPassed ? "LULUS UJIAN!" : "NILAI BIASA / GAGAL";
            if (finalScoreText != null)
                finalScoreText.text = $"Nilai Akhir: {finalScore:F0}";

            yield return new WaitForSeconds(2.0f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Update Stat Tono
        if (StateManager.Instance != null)
        {
            if (isPassed)
            {
                StateManager.Instance.ModifyStat("intelligence", 2);
                StateManager.Instance.ModifyStat("reputation", 1);
            }
            else
            {
                StateManager.Instance.ModifyStat("intelligence", -2);
                StateManager.Instance.ModifyStat("delinquency", 1);
                StateManager.Instance.ModifyStat("empathy", 1);
            }
        }

        if (GameManager.Instance != null)
        {
            // Hasil dibaca dialog lewat flag minigame_result = "lulus" / "gagal"
            GameManager.Instance.ReportMinigameResult(isPassed, "lulus", "gagal");
        }
        else
        {
            // Dites langsung dari scene ujian
            Debug.Log($"[Exam] Hasil ujian: {(isPassed ? "lulus" : "gagal")} (nilai {finalScore:F0}). GameManager tidak ditemukan.");
        }
    }
}