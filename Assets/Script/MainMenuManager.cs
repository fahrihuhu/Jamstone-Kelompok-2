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

    private void Start()
    {
        if (continueButton != null)
        {
            bool hasSaveData = PlayerPrefs.HasKey("SavedScene");
            continueButton.interactable = hasSaveData;
        }
    }

    public void OnNewGameClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnContinueClicked()
    {
        if (PlayerPrefs.HasKey("SavedScene"))
        {
            string savedScene = PlayerPrefs.GetString("SavedScene");
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            Debug.LogWarning("Tidak ada file simpanan untuk dilanjutkan.");
        }
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