using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Pengaturan Scene Game")]
    [Tooltip("Nama Scene tempat gameplay/dialog utama berjalan")]
    public string gameSceneName = "GameScene";

    [Header("Panel Pop-up (Opsional)")]
    public GameObject galleryPanel;
    public GameObject settingsPanel;
    public GameObject mailPanel;

    [Header("Tombol UI (Opsional jika mau di-disable)")]
    public Button continueButton;

    [Header("Popup Konfirmasi New Game")]
    [Tooltip("Panel Yes/No yang muncul kalau sudah ada save. Kosongkan untuk langsung mulai tanpa konfirmasi.")]
    public GameObject newGamePopup;

    [Header("Save")]
    [Tooltip("Hapus save lama saat New Game ditekan. Matikan kalau save lama mau dipertahankan sampai pemain menyimpan lagi.")]
    public bool hapusSaveSaatNewGame = true;

    private void Start()
    {
        if (newGamePopup != null) newGamePopup.SetActive(false);

        if (continueButton != null)
        {
            continueButton.interactable = SaveManager.HasSave();
        }
    }

    // Dipanggil dari tombol New Game
    public void OnNewGameClicked()
    {
        // Sudah ada save dan popup tersedia: minta konfirmasi dulu
        if (newGamePopup != null && SaveManager.HasSave())
        {
            newGamePopup.SetActive(true);
            return;
        }

        StartNewGame();
    }

    // Dipanggil dari tombol "Yes" di popup
    public void ConfirmNewGame()
    {
        StartNewGame();
    }

    // Dipanggil dari tombol "No" di popup
    public void CancelNewGame()
    {
        if (newGamePopup != null) newGamePopup.SetActive(false);
    }

    private void StartNewGame()
    {
        SaveManager.LoadRequested = false; // pastikan mulai dari awal, bukan memuat save
        if (hapusSaveSaatNewGame) SaveManager.DeleteSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnContinueClicked()
    {
        if (SaveManager.HasSave())
            SaveManager.ContinueGame(gameSceneName);
        else
            Debug.LogWarning("Tidak ada file simpanan untuk dilanjutkan.");
    }

    public void OnGalleryClicked()
    {
        if (galleryPanel != null)
            galleryPanel.SetActive(true);
        else
            Debug.Log("Membuka Gallery");
    }

    public void OnQuitClicked()
    {
        Debug.Log("Keluar dari Game.");
        Application.Quit();
    }

    public void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        else
            Debug.Log("Membuka Settings");
    }

    public void OnMailClicked()
    {
        if (mailPanel != null)
            mailPanel.SetActive(true);
        else
            Debug.Log("Membuka Mail/Message Panel...");
    }

    public void ClosePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }
}