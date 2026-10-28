using UnityEngine;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("Hidden Stats Tono (Skala 1 - 10)")]
    [Range(1, 10)] public int intelligence = 8;  
    [Range(1, 10)] public int courage = 2;       
    [Range(1, 10)] public int empathy = 6;       
    [Range(1, 10)] public int delinquency = 2;   
    [Range(1, 10)] public int reputation = 5;    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Fungsi universal untuk mengubah Stat (+1, -1, +2, -2)
    public void ModifyStat(string statName, int amount)
    {
        switch (statName.ToLower())
        {
            case "intelligence":
                intelligence = Mathf.Clamp(intelligence + amount, 1, 10);
                break;
            case "courage":
                courage = Mathf.Clamp(courage + amount, 1, 10);
                break;
            case "empathy":
                empathy = Mathf.Clamp(empathy + amount, 1, 10);
                break;
            case "delinquency":
                delinquency = Mathf.Clamp(delinquency + amount, 1, 10);
                break;
            case "reputation":
                reputation = Mathf.Clamp(reputation + amount, 1, 10);
                break;
            default:
                Debug.LogWarning($"Stat '{statName}' tidak ditemukan!");
                break;
        }

        Debug.Log($"[StateManager] {statName} modified by {amount}. Current Value: {GetStat(statName)}");
    }

    public int GetStat(string statName)
    {
        switch (statName.ToLower())
        {
            case "intelligence": return intelligence;
            case "courage": return courage;
            case "empathy": return empathy;
            case "delinquency": return delinquency;
            case "reputation": return reputation;
            default: return 0;
        }
    }
}