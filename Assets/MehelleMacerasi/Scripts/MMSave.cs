using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Oyunun yaddaşı: bütün vəziyyət bir JSON faylında saxlanılır (persistentDataPath).
[System.Serializable]
public class MMSaveData
{
    public int Version = 2;
    public int Coins, Hearts, Wishes, LettersFound;
    public float PlayTime, TimeOfDay, SpeedBoostT;
    public bool CatQuestStarted, CatFound, CatReturned;
    public bool LetterQuestStarted, LettersReturned;
    public bool TeaQuestStarted, HasTea, TeaDelivered;
    public bool HasCharm, HasSneakers;
    public float PlayerX, PlayerY;
    public int CatMode;
    public float CatX, CatY;
    public List<string> Collected = new List<string>();
    public List<string> Inventory = new List<string>();
    public int Water, Selected;
    public int Day = 1, Season, SeasonDay = 1;
    public float TimeSaved;
    public List<string> Plots = new List<string>();
    public List<int> Snowmen = new List<int>();
}

public static class MMSave
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "mehelle_save.json");

    public static bool Exists => File.Exists(FilePath);

    public static void Write(MMSaveData data)
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
        catch (System.Exception e) { Debug.LogWarning("[MM] Save failed: " + e.Message); }
    }

    public static MMSaveData Read()
    {
        try
        {
            if (!Exists) return null;
            return JsonUtility.FromJson<MMSaveData>(File.ReadAllText(FilePath));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MM] Load failed: " + e.Message);
            return null;
        }
    }

    public static void Clear()
    {
        try { if (Exists) File.Delete(FilePath); }
        catch (System.Exception e) { Debug.LogWarning("[MM] Could not clear save: " + e.Message); }
    }
}
