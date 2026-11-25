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
