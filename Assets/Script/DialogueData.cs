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

    [Header("Lanjutan Otomatis (opsional)")]
    [Tooltip("Kalau semua baris sudah habis, otomatis lanjut ke dialog ini. Kosongkan kalau scene ini berakhir lewat pilihan, atau memang akhir cerita.")]
    public DialogueData nextDialogue;
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

    [Header("Kondisi Tampil (opsional)")]
    [Tooltip("Isi kalau baris ini cuma boleh muncul kalau story flag tertentu punya nilai tertentu. Kosongkan kalau baris selalu tampil.")]
    public string conditionKey = "";
    public string conditionValue = "";

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