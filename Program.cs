using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Timers;
using Timer = System.Timers.Timer;

class FolderSynchronizer
{
    private static string sourceFolder;
    private static string replicaFolder;
    private static string logFilePath;
    private static int syncIntervalSeconds;
    private static Timer syncTimer;

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSynchronizer <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalSeconds>");
            return;
        }

        sourceFolder = args[0];
        replicaFolder = args[1];
        logFilePath = args[2];

        if (!int.TryParse(args[3], out syncIntervalSeconds) || syncIntervalSeconds <= 0)
        {
            Console.WriteLine("Wrong  interval.Positive number (seconds).");
            return;
        }

        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine("Error: Source folder doesn't exist.");
            return;
        }

        Directory.CreateDirectory(replicaFolder);
        string? logDir = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Synchronize();

        syncTimer = new Timer(syncIntervalSeconds * 1000);
        syncTimer.Elapsed += (sender, e) => Synchronize();
        syncTimer.Start();

        Console.WriteLine($"Sync started. Source: {sourceFolder} → Replica: {replicaFolder} every {syncIntervalSeconds} seconds.");
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
        syncTimer.Stop();
    }

    private static void Synchronize()
    {
        try
        {
            SyncDirectory(sourceFolder, replicaFolder);
            CleanupReplica(sourceFolder, replicaFolder);
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
    }

    private static void SyncDirectory(string source, string replica)
    {
        Directory.CreateDirectory(replica);

        foreach (string sourceFilePath in Directory.GetFiles(source))
        {
            string fileName = Path.GetFileName(sourceFilePath);
            string replicaFilePath = Path.Combine(replica, fileName);

            if (!File.Exists(replicaFilePath) || !FilesAreEqual(sourceFilePath, replicaFilePath))
            {
                File.Copy(sourceFilePath, replicaFilePath, true);
                Log($"Copied: {sourceFilePath} → {replicaFilePath}");
            }
        }

        foreach (string sourceSubDir in Directory.GetDirectories(source))
        {
            string dirName = Path.GetFileName(sourceSubDir);
            string replicaSubDir = Path.Combine(replica, dirName);
            SyncDirectory(sourceSubDir, replicaSubDir);
        }
    }

    private static void CleanupReplica(string source, string replica)
    {
        foreach (string replicaFilePath in Directory.GetFiles(replica))
        {
            string fileName = Path.GetFileName(replicaFilePath);
            string sourceFilePath = Path.Combine(source, fileName);

            if (!File.Exists(sourceFilePath))
            {
                File.Delete(replicaFilePath);
                Log($"Deleted: {replicaFilePath}");
            }
        }

        foreach (string replicaSubDir in Directory.GetDirectories(replica))
        {
            string dirName = Path.GetFileName(replicaSubDir);
            string sourceSubDir = Path.Combine(source, dirName);

            if (!Directory.Exists(sourceSubDir))
            {
                Directory.Delete(replicaSubDir, true);
                Log($"Deleted folder: {replicaSubDir}");
            }
            else
            {
                CleanupReplica(sourceSubDir, replicaSubDir);
            }
        }
    }

    private static bool FilesAreEqual(string filePath1, string filePath2)
    {
        using var md5 = MD5.Create();
        using var fs1 = File.OpenRead(filePath1);
        using var fs2 = File.OpenRead(filePath2);

        byte[] hash1 = md5.ComputeHash(fs1);
        byte[] hash2 = md5.ComputeHash(fs2);

        return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
    }

    private static void Log(string message)
    {
        string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        Console.WriteLine(entry);
        File.AppendAllText(logFilePath, entry + Environment.NewLine);
    }
}
