using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MenuPopupManager : MonoBehaviour
{
    [Header("Popup Panels")]
    [SerializeField] private GameObject mainMenuPopup;
    [SerializeField] private GameObject quitPopup;
    [SerializeField] private GameObject savePopup;

    [Header("Pengaturan")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool pauseGameSaatPopup = true;
    [SerializeField] private float saveBerhasilDurasi = 0.5f;

    [Header("Save")]
    [Tooltip("Dipanggil saat tombol Save ditekan. Hubungkan ke fungsi save milik SaveManager di sini kalau sudah ada.")]
    [SerializeField] private UnityEvent onSaveRequested;

    private Coroutine saveRoutine;

    private void Start()
    {
        // Pastikan semua popup tertutup saat game dimulai
        if (mainMenuPopup != null) mainMenuPopup.SetActive(false);
        if (quitPopup != null) quitPopup.SetActive(false);
        if (savePopup != null) savePopup.SetActive(false);
    }

    // ===== MAIN MENU =====

    public void OpenMainMenuPopup()
    {
        mainMenuPopup.SetActive(true);
        PauseGame(true);
    }

    public void ConfirmMainMenu()
    {
        Time.timeScale = 1f; // Wajib dikembalikan sebelum pindah scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void CancelMainMenu()
    {
        mainMenuPopup.SetActive(false);
        PauseGame(false);
    }

    // ===== SAVE =====

    // Dipanggil dari tombol "Save"
    public void OnSaveButtonClicked()
    {
        PerformSave();

        // Kalau diklik berulang, ulangi hitungan dari awal
        if (saveRoutine != null) StopCoroutine(saveRoutine);
        saveRoutine = StartCoroutine(ShowSavePopup());
    }

    private void PerformSave()
    {
        // Hubungkan ke SaveManager.SaveGame() lewat Inspector di field "On Save Requested"
        onSaveRequested?.Invoke();
    }

    private IEnumerator ShowSavePopup()
    {
        savePopup.SetActive(true);

        // Realtime supaya tetap jalan walau Time.timeScale = 0 (game sedang di-pause)
        yield return new WaitForSecondsRealtime(saveBerhasilDurasi);

        savePopup.SetActive(false);
        saveRoutine = null;
    }

    // ===== QUIT =====

    public void OpenQuitPopup()
    {
        quitPopup.SetActive(true);
        PauseGame(true);
    }

    public void ConfirmQuit()
    {
        Debug.Log("Keluar dari game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CancelQuit()
    {
        quitPopup.SetActive(false);
        PauseGame(false);
    }

    // ===== HELPER =====

    private void PauseGame(bool pause)
    {
        if (pauseGameSaatPopup)
        {
            Time.timeScale = pause ? 0f : 1f;
        }
    }
}