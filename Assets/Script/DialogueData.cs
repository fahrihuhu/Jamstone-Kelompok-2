using UnityEngine;

// ScriptableObject untuk menyimpan satu rangkaian dialog (misalnya 1 chapter/scene).
// Klik kanan di Project window -> Create -> VisualNovel -> Dialogue
[CreateAssetMenu(fileName = "NewDialogue", menuName = "VisualNovel/Dialogue")]
public class DialogueData : ScriptableObject
{
    [Header("Info Scene")]
    public string sceneId;
    [TextArea] public string sceneDescription;

    [Header("Isi Dialog")]
    public DialogueLine[] lines;

    [Header("Transisi Scene")]
    [Tooltip("Centang jika perpindahan ke scene/DialogueData ini menggunakan efek Fade (layar hitam)")]
    public bool fadeTransition;

    [Header("Lanjutan Otomatis (opsional)")]
    [Tooltip("Kalau semua baris sudah habis, otomatis lanjut ke dialog ini. Kosongkan kalau scene ini berakhir lewat pilihan, atau memang akhir cerita.")]
    public DialogueData nextDialogue;

    [Tooltip("Dicek berurutan dari atas. Yang pertama cocok dengan story flag akan dipakai. Kalau tidak ada yang cocok, dipakai Next Dialogue di atas.")]
    public ConditionalNext[] conditionalNexts;
}

[System.Serializable]
public class ConditionalNext
{
    public string conditionKey;
    public string conditionValue;
    public DialogueData nextDialogue;
}

public enum StageAction
{
    Show,              // Munculkan karakter di slot posisi tertentu
    Hide,              // Hilangkan satu karakter dari layar
    ChangeExpression,  // Ganti ekspresi karakter yang sudah ada di layar
    HideAll            // Kosongkan seluruh panggung (misal pindah lokasi)
}

public enum StagePosition
{
    FarLeft = 0,
    Left = 1,
    Center = 2,
    Right = 3,
    FarRight = 4
}

// Satu perintah panggung. Dijalankan saat baris dialog yang memilikinya tampil.
[System.Serializable]
public class StageCommand
{
    public StageAction action;

    [Tooltip("Karakter yang dituju. Tidak dipakai untuk HideAll.")]
    public CharacterData character;

    [Tooltip("Hanya dipakai untuk Show")]
    public StagePosition position = StagePosition.Center;

    [Tooltip("Nama ekspresi. Dipakai untuk Show dan ChangeExpression. Harus cocok dengan salah satu di CharacterData.")]
    public string expression = "Normal";
}

[System.Serializable]
public class DialogueLine
{
    [Header("Siapa yang bicara")]
    public CharacterData character;

    [Tooltip("Kosongkan untuk pakai nama asli character. Isi misalnya '???' kalau identitas belum terungkap")]
    public string speakerNameOverride = "";

    [Header("Isi Dialog")]
    [TextArea(2, 5)]
    public string dialogueText;
    

    [Tooltip("Centang kalau ini inner thought/monolog batin (biasanya ditulis dalam kurung), untuk ditampilkan beda gaya di UI")]
    public bool isThought = false;

    [Tooltip("Nama ekspresi yang dipakai, harus cocok dengan salah satu di CharacterData")]
    public string expression = "Normal";

    [Header("Visual Tambahan (opsional)")]
    public Sprite backgroundOverride;

    [Header("Efek Suara (opsional)")]
    [Tooltip("SFX yang diputar sekali saat baris ini tampil, misal: Brukk")]
    public AudioClip sfxClip;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Kondisi Tampil (opsional)")]
    [Tooltip("Isi kalau baris ini cuma boleh muncul kalau story flag tertentu punya nilai tertentu. Kosongkan kalau baris selalu tampil.")]
    public string conditionKey = "";
    public string conditionValue = "";

    [Header("Minigame (opsional)")]
    [Tooltip("Isi dengan nama scene Unity minigame. Saat baris ini tercapai, minigame langsung dibuka (teks baris ini tidak ditampilkan). Scene harus ada di Build Settings.")]
    public string minigameSceneName = "";

    [Header("Perintah Panggung (opsional)")]
    [Tooltip("Muncul, hilang, atau ganti ekspresi karakter di layar. Dijalankan berurutan saat baris ini tampil.")]
    public StageCommand[] stageCommands;

    [Header("Percabangan (opsional)")]
    [Tooltip("Isi kalau baris ini adalah titik pilihan pemain")]
    public DialogueChoice[] choices;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(speakerNameOverride))
            return speakerNameOverride;
        if (character != null)
            return character.characterName;
        return "";
    }
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;

    [Tooltip("Dialogue Data tujuan kalau pilihan ini diambil (untuk branching antar scene)")]
    public DialogueData nextDialogue;

    [Tooltip("Atau index baris tujuan di dalam scene yang sama (isi -1 kalau pakai nextDialogue)")]
    public int nextLineIndex = -1;

    [Header("Story Flag (opsional)")]
    [Tooltip("Isi kalau pilihan ini perlu 'diingat' untuk mempengaruhi dialog di scene lain nanti, misal key='rico_result', value='damai'")]
    public string setFlagKey = "";
    public string setFlagValue = "";
}