using System;
using System.Text.Json;

public class Whitelist
{
    private static readonly string filePath = "whitelist.json"; // Save list to JSON file
    public static HashSet<string> list = new HashSet<string>();

    // Load list from file on program startup
    public static void LoadFromFile()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            list = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
        }
    }

    // Save list to file when changed
    public static void SaveToFile()
    {
        string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    /*
    {
        "Chaos Orb",
        "Exalted Orb",
        "Mirror of Kalandra",
        "Mirror Shard",
        "Divine Orb",
        "Enlighten Support",
        "Orb of Alteration"
    };
    */
}