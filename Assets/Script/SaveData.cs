using System;
using System.Collections.Generic;

// Isi file save. JsonUtility tidak bisa menyimpan Dictionary,
// jadi story flags disimpan sebagai dua list yang urutannya berpasangan.
[Serializable]
public class SaveData
{
    public string sceneName;    // scene Unity tempat save dibuat (dimuat ulang saat Continue)
    public string dialogueId;   // nama aset DialogueData yang sedang jalan
    public int lineIndex;       // baris keberapa di dialog itu

    public List<string> flagKeys = new List<string>();
    public List<string> flagValues = new List<string>();

    public string savedAt;      // waktu save, untuk info/debug
}