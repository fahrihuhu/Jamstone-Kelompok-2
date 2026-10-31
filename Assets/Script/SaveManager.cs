using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // Diset true oleh tombol Continue di main menu, dibaca GameManager saat scene VN mulai
    public static bool LoadRequested = false;

    [Header("Referensi")]
    [SerializeField] private GameManager gameManager;

    [Header("Daftar Semua Dialogue")]
    [Tooltip("Masukkan SEMUA DialogueData di project. Dipakai untuk mencari dialog berdasarkan nama saat load.")]
    [SerializeField] private DialogueData[] allDialogues;

    private const string FileName = "save.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    public static bool HasSave() => File.Exists(SavePath);

    private void Awake()
    {
        Instance = this;
    }

    // ===== SAVE =====

    // Hubungkan ke tombol Save (lewat On Save Requested di MenuPopupManager)
    public void SaveGame()
    {
        if (gameManager == null || !gameManager.CanSave)
        {
            Debug.LogWarning("SaveManager: tidak bisa save sekarang (tidak ada dialog, minigame, atau sedang transisi).");
            return;
        }

        SaveData data = new SaveData
        {
            sceneName = gameObject.scene.name,
            dialogueId = gameManager.CurrentDialogue.name,
            lineIndex = gameManager.CurrentLineIndex,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (var pair in gameManager.GetFlagsCopy())
        {
            data.flagKeys.Add(pair.Key);
            data.flagValues.Add(pair.Value);
        }

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"Game tersimpan: {data.dialogueId} baris {data.lineIndex} -> {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError("Gagal menyimpan game: " + e.Message);
        }
    }

    // ===== LOAD =====

    // Return true kalau berhasil dimuat
    public bool LoadGame()
    {
        if (!HasSave())
        {
            Debug.LogWarning("SaveManager: file save tidak ditemukan.");
            return false;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError("Gagal membaca save: " + e.Message);
            return false;
        }

        DialogueData dialogue = FindDialogue(data.dialogueId);
        if (dialogue == null)
        {
            Debug.LogError($"SaveManager: dialog '{data.dialogueId}' tidak ada di daftar All Dialogues.");
            return false;
        }

        var flags = new Dictionary<string, string>();
        int count = Mathf.Min(data.flagKeys.Count, data.flagValues.Count);
        for (int i = 0; i < count; i++)
            flags[data.flagKeys[i]] = data.flagValues[i];

        gameManager.RestoreState(dialogue, data.lineIndex, flags);
        return true;
    }

    private DialogueData FindDialogue(string id)
    {
        foreach (var d in allDialogues)
            if (d != null && d.name == id) return d;
        return null;
    }

    public static void DeleteSave()
    {
        if (HasSave()) File.Delete(SavePath);
    }

    // ===== CONTINUE (dipanggil dari main menu) =====

    // fallbackSceneName dipakai hanya kalau save lama belum mencatat nama scene
    public static void ContinueGame(string fallbackSceneName)
    {
        if (!HasSave()) return;

        string targetScene = fallbackSceneName;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data != null && !string.IsNullOrEmpty(data.sceneName))
                targetScene = data.sceneName;
        }
        catch (Exception e)
        {
            Debug.LogError("Gagal membaca save: " + e.Message);
            return;
        }

        LoadRequested = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }
}