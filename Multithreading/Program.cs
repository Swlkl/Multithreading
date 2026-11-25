using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Commande
{
    public int Id { get; set; }
    public string Client { get; set; }
    public decimal Montant { get; set; }
    public bool Traitee { get; set; }
}

class Prog
{
    static string connectionString =
        "Server=localhost;Port=3306;Database=gestion_commandes;Uid=root;Pwd=;";

    static List<Commande> GetCommandes()
    {
        List<Commande> commandes = new List<Commande>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT Id, Client, Montant, Traitee FROM Commande WHERE Traitee = 0";

            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    commandes.Add(new Commande
                    {
                        Id = reader.GetInt32("Id"),
                        Client = reader.GetString("Client"),
                        Montant = reader.GetDecimal("Montant"),
                        Traitee = reader.GetBoolean("Traitee")
                    });
                }
            }
        }

        return commandes;
    }

    static void TraiterCommande(Commande c)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Début commande {c.Id} (thread {Thread.CurrentThread.ManagedThreadId})");

        Thread.Sleep(10000); 

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            string sql = "UPDATE Commande SET Montant = Montant + 1, Traitee = 1 WHERE Id = @id";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", c.Id);
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Fin commande {c.Id}");
    }

    static void TraiterToutesLesCommandes(List<Commande> commandes)
    {
        foreach (Commande c in commandes)
        {
            TraiterCommande(c);
        }
    }
    static async Task TraiterCommandeAsync(Commande c)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Début commande {c.Id}");

        await Task.Delay(10000); 

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            string sql = "UPDATE Commande SET Montant = Montant + 1, Traitee = 1 WHERE Id = @id";
            using (MySqlCommand cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", c.Id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Fin commande {c.Id}");
    }

    static async Task TraiterToutesLesCommandesAsync(List<Commande> commandes)
    {
        List<Task> taches = commandes.Select(c => TraiterCommandeAsync(c)).ToList();

        await Task.WhenAll(taches);
    }

    static async Task Main()
    {
        List<Commande> commandes = GetCommandes();
        Console.WriteLine($"{commandes.Count} commande(s) à traiter");

        Stopwatch sw = Stopwatch.StartNew();
        await TraiterToutesLesCommandesAsync(commandes);
        sw.Stop();

        Console.WriteLine($"Durée totale : {sw.Elapsed.TotalSeconds:F1} s");
    }

    static void TraiterAvecParallelForEach(List<Commande> commandes)
    {
        Parallel.ForEach(
            commandes,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            c => TraiterCommande(c));
    }

    static SemaphoreSlim limiteur = new SemaphoreSlim(3); 

    static async Task TraiterAvecLimiteAsync(Commande c)
    {
        await limiteur.WaitAsync();
        try
        {
            await TraiterCommandeAsync(c);
        }
        finally
        {
            limiteur.Release();
        }
    }
}
