using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Taruh di GameObject "IntroSequence" di IntroScene.
// Urutan: layar hitam -> teks fade in/out satu per satu -> gambar gerbang fade in
//         -> (opsional) layar fade ke hitam -> pindah ke VNScene.
public class IntroSequence : MonoBehaviour
{
    [Header("Referensi UI")]
    public TextMeshProUGUI introText;
    public Image gateImage;
    public Image fadeOverlay;

    [Header("Audio")]
    [Tooltip("Semua AudioSource di sini diputar bersamaan saat intro mulai (misal: klakson + langkah sepatu) dan ikut mengecil saat layar menggelap")]
    public AudioSource[] ambientSources;
    [Tooltip("Lama suara mengecil sampai hilang. Dimulai bersamaan dengan gambar gerbang fade in. Samakan dengan Gate Fade In kalau mau berakhir bersamaan.")]
    public float audioFadeOutDuration = 3f;

    [Header("Teks Intro")]
    [TextArea(2, 4)]
    public string[] introLines =
    {
        "SMA Garuda.",
        "Sekolah ini tidak pernah ada di dalam daftar sekolah impian siapa pun.",
        "\u2026",
        "Setidaknya, bukan untuk anak baik-baik seperti Tono."
    };

    [Header("Durasi (detik)")]
    [Tooltip("Jeda layar hitam sebelum teks pertama muncul")]
    public float startDelay = 1f;
    public float textFadeIn = 1f;
    public float textHold = 1f;
    public float textFadeOut = 1f;
    [Tooltip("Jeda antar teks")]
    public float gapBetweenTexts = 0.5f;

    [Space]
    public float gateFadeIn = 3f;
    [Tooltip("Lama gambar gerbang ditahan setelah fade in selesai, sebelum pindah scene")]
    public float gateHold = 1.5f;
    [Tooltip("Lama layar menggelap sebelum pindah scene. Isi 0 kalau gambar gerbang harus langsung tersambung ke VNScene tanpa menggelap.")]
    public float fadeOutDuration = 0f;

    [Header("Scene Tujuan")]
    [Tooltip("Harus sama persis dengan nama scene, dan sudah ada di Build Settings")]
    public string nextSceneName = "VNScene";

    private void Start()
    {
        // Pastikan kondisi awal: semua transparan, hanya layar hitam yang terlihat
        SetAlpha(introText, 0f);
        SetAlpha(gateImage, 0f);
        SetAlpha(fadeOverlay, 0f);

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        foreach (AudioSource source in ambientSources)
        {
            if (source != null)
                source.Play();
        }

        yield return new WaitForSeconds(startDelay);

        // Teks muncul satu per satu: fade in -> tahan -> fade out
        foreach (string line in introLines)
        {
            introText.text = line;
            yield return Fade(introText, 0f, 1f, textFadeIn);
            yield return new WaitForSeconds(textHold);
            yield return Fade(introText, 1f, 0f, textFadeOut);
            yield return new WaitForSeconds(gapBetweenTexts);
        }

        // Gambar gerbang muncul perlahan, suara mengecil bersamaan
        StartCoroutine(FadeOutAudio(audioFadeOutDuration));
        yield return Fade(gateImage, 0f, 1f, gateFadeIn);
        yield return new WaitForSeconds(gateHold);

        // Layar menggelap, lalu pindah scene
        yield return FadeOutAndLoad();
    }

    // Semua suara mengecil sampai hilang, masing-masing dari volumenya sendiri, lalu dihentikan
    private IEnumerator FadeOutAudio(float duration)
    {
        float[] startVolumes = new float[ambientSources.Length];
        for (int i = 0; i < ambientSources.Length; i++)
            startVolumes[i] = ambientSources[i] != null ? ambientSources[i].volume : 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            for (int i = 0; i < ambientSources.Length; i++)
            {
                if (ambientSources[i] != null)
                    ambientSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, progress);
            }
            yield return null;
        }

        foreach (AudioSource source in ambientSources)
        {
            if (source != null)
                source.Stop();
        }
    }

    private IEnumerator FadeOutAndLoad()
    {
        float t = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            SetAlpha(fadeOverlay, Mathf.Clamp01(t / fadeOutDuration));
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // Fade alpha untuk Image maupun TextMeshPro (keduanya turunan Graphic)
    private IEnumerator Fade(Graphic target, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(target, to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(target, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetAlpha(target, to);
    }

    private void SetAlpha(Graphic target, float alpha)
    {
        if (target == null) return;
        Color c = target.color;
        c.a = alpha;
        target.color = c;
    }
}