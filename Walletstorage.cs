using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

static class WalletStorage
{
    const string FilePath = "wallets.json";

    public static List<Wallet> Load()
    {
        if (!File.Exists(FilePath))
            return new List<Wallet>();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Wallet>>(json) ?? new List<Wallet>();
        }
        catch (Exception ex) when (ex is IOException || ex is JsonException)
        {
            Console.WriteLine("Could not load wallets. Starting with an empty list.");
            return new List<Wallet>();
        }
    }

    public static void Save(List<Wallet> wallets)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(wallets, options));
        }
        catch (IOException)
        {
            Console.WriteLine("Could not save wallets.");
        }
    }
}