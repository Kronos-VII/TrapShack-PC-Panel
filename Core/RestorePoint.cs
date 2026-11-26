namespace TrapShack.Core;

public static class RestorePoint
{
    /// <summary>Creates a Windows System Restore point so a full rollback is always possible.</summary>
    public static (bool Ok, string Message) Create(string description = "TrapShack - before changes")
    {
        // Windows normally refuses a second restore point within 24 hours. The frequency limit is lifted
        // for this one call and then put back exactly as it was.
        var script = @"
$ErrorActionPreference = 'Stop'
$k = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore'
$old = $null
try {
  $old = (Get-ItemProperty -Path $k -Name SystemRestorePointCreationFrequency -ErrorAction SilentlyContinue).SystemRestorePointCreationFrequency
  Set-ItemProperty -Path $k -Name SystemRestorePointCreationFrequency -Value 0 -Type DWord
  Checkpoint-Computer -Description '" + description.Replace("'", "''") + @"' -RestorePointType MODIFY_SETTINGS
} catch {
  [Console]::Error.WriteLine($_.Exception.Message)
  exit 1
} finally {
  if ($null -eq $old) { Remove-ItemProperty -Path $k -Name SystemRestorePointCreationFrequency -ErrorAction SilentlyContinue }
  else { Set-ItemProperty -Path $k -Name SystemRestorePointCreationFrequency -Value $old -Type DWord }
}
";
        var r = Shell.PowerShell(script, 300_000);
        if (r.Ok) { Log.Info("Restore point created"); return (true, "Restore point created."); }

        var msg = (r.Error + r.Output).Trim();
        Log.Error("Restore point failed: " + msg);
        return (false, string.IsNullOrWhiteSpace(msg) ? "Windows didn't create a restore point." : msg);
    }
}

/// <summary>One System Restore point.</summary>
public sealed record RestorePointInfo(uint Sequence, string Description, DateTime Created, string Kind);

/// <summary>Lists, creates and deletes System Restore points, and turns System Restore on for the Windows drive.</summary>
public static class RestorePoints
{
    const string SrKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore";

    /// <summary>True when System Restore is on (Windows sets RPSessionInterval to 1 when it is enabled for a drive).</summary>
    public static bool IsEnabled()
    {
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(SrKey);
            return k?.GetValue("RPSessionInterval") is int v && v == 1;
        }
        catch { return false; }
    }

    public static (bool Ok, string Message) Enable()
    {
        var r = Shell.PowerShell("Enable-ComputerRestore -Drive ($env:SystemDrive + '\\')", 120_000);
        return r.Ok ? (true, "System Restore is on for the Windows drive.") : (false, (r.Error + r.Output).Trim());
    }

    public static List<RestorePointInfo> List()
    {
        var r = Shell.PowerShell(
            "$p = @(Get-ComputerRestorePoint -ErrorAction SilentlyContinue | ForEach-Object { [pscustomobject]@{ " +
            "Seq = [uint32]$_.SequenceNumber; Desc = [string]$_.Description; Type = [int]$_.RestorePointType; " +
            "Time = $_.ConvertToDateTime($_.CreationTime).ToString('o') } }); ConvertTo-Json -InputObject $p -Compress", 60_000);
        var list = new List<RestorePointInfo>();
        try
        {
            var text = r.Output.Trim();
            if (text.Length == 0) return list;
            using var doc = System.Text.Json.JsonDocument.Parse(text);
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var type = e.GetProperty("Type").GetInt32();
                list.Add(new RestorePointInfo(
                    e.GetProperty("Seq").GetUInt32(),
                    e.GetProperty("Desc").GetString() ?? "",
                    DateTime.TryParse(e.GetProperty("Time").GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t : DateTime.MinValue,
                    type switch
                    {
                        0 => "App installed", 1 => "App removed", 10 => "Driver installed", 12 => "Settings changed",
                        13 => "Cancelled", _ => "Restore point",
                    }));
            }
        }
        catch (Exception ex) { Log.Error("Reading restore points failed: " + ex.Message); }
        return list.OrderByDescending(p => p.Created).ToList();
    }

    /// <summary>Deletes one restore point with Windows' own System Restore API.</summary>
    public static (bool Ok, string Message) Delete(uint sequence)
    {
        try
        {
            var code = SRRemoveRestorePoint(sequence);
            if (code == 0) { Log.Info($"Restore point {sequence} deleted"); return (true, "Restore point deleted."); }
            return (false, $"Windows didn't delete it (error {code}).");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    [System.Runtime.InteropServices.DllImport("srclient.dll")]
    static extern int SRRemoveRestorePoint(uint index);
}
