using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string saveFilePath = Application.persistentDataPath + "/savegame.json";
    
    public static void Save(SaveData dataToSave)
    {
        string json = JsonUtility.ToJson(dataToSave, true);

        File.WriteAllText(saveFilePath, json);

        Debug.Log("Гра успішно збережена за шляхом: " + saveFilePath);
    }
    
    public static SaveData Load()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            
            SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Гра успішно завантажена!");
            return loadedData;
        }
        else
        {
            Debug.LogWarning("Файл збереження не знайдено. Створюємо новий профіль.");
            return null;
        }
    }
}
