using System.Collections;
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
    public string[] options = new string[4];
    public int correctAnswerIndex;
}

public class ExamMinigameManager : MonoBehaviour
{
    [Header("Pengaturan Tipe Ujian")]
    public ExamType examType = ExamType.UH;

    [Header("Bank Soal Berdasarkan Tingkat Kesulitan")]
    public ExamQuestion[] hardQuestions;
    public ExamQuestion[] mediumQuestions;
    public ExamQuestion[] easyQuestions;

    [Header("Pengaturan Nilai")]
    public float passingScore = 80f;

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI questionContentText;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("UI Tutorial (Pop-up Awal)")]
    public GameObject tutorialPanel;
    public Button startExamButton;

    [Header("Result UI (Opsional)")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultStatusText;
    public TextMeshProUGUI finalScoreText;

    private ExamQuestion[] activeQuestions;
    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;
    private float totalTime;
    private float timeRemaining;
    private bool isGameActive = false;

    private void Awake()
    {
        switch (examType)
        {
            case ExamType.UH: totalTime = 60f; break;
            case ExamType.UTS: totalTime = 75f; break;
            case ExamType.UAS: totalTime = 90f; break;
        }

        SelectQuestionBankByIntelligence();
    }

    private void SelectQuestionBankByIntelligence()
    {
        int intel = 8;
        if (StateManager.Instance != null)
        {
            intel = StateManager.Instance.intelligence;
        }

        if (intel <= 4) activeQuestions = hardQuestions;
        else if (intel <= 7) activeQuestions = mediumQuestions;
        else activeQuestions = easyQuestions;
    }

    private void Start()
    {
        timeRemaining = totalTime;
        currentQuestionIndex = 0;
        correctAnswersCount = 0;
        isGameActive = false; // Belum mulai sebelum tombol Start diklik

        if (resultPanel != null) resultPanel.SetActive(false);

        // Setup Pop-up Tutorial Awal
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            if (startExamButton != null)
            {
                startExamButton.onClick.RemoveAllListeners();
                startExamButton.onClick.AddListener(StartExam);
            }
        }
        else
        {
            // Jika tutorialPanel tidak diisi, langsung mulai game
            StartExam();
        }
    }

    // Dipanggil saat tombol "MULAI UJIAN" diklik
    public void StartExam()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        isGameActive = true;
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

        if (selectedIndex == activeQuestions[currentQuestionIndex].correctAnswerIndex)
            correctAnswersCount++;

        currentQuestionIndex++;
        ShowQuestion();
    }

    private void EndExam()
    {
        isGameActive = false;

        float finalScore = (activeQuestions != null && activeQuestions.Length > 0)
            ? ((float)correctAnswersCount / activeQuestions.Length) * 100f
            : 0f;

        bool isPassed = finalScore > passingScore;

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
            GameManager.Instance.ReportMinigameResult(isPassed);
        }
    }
}