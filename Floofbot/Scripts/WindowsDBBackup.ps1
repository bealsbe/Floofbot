$dbPath = $args[0]
$outputPath = $args[1]
$maxNumberBackups = $args[2]

# delete oldest backup
$oldestBackup = $outputPath + "dbBackup.db." + $maxNumberBackups
if (Test-Path $oldestBackup){
    Remove-Item $oldestBackup
}

# rename old backups
For ($i = $maxNumberBackups - 1; $i -gt 0; $i--){
    $oldBackup = $outputPath + "dbBackup.db." + $i
    if (Test-Path $oldBackup) # if old backup file exists
    {
        $newIndex = $i + 1;
        $newFile = $outputPath + "dbBackup.db." + $newIndex
        Rename-Item $oldBackup -NewName $newFile
    }
}

$backupPath = $outputPath + "dbBackup.db.1"
Copy-Item $dbPath -Destination $backupPath -force

if ($?) # Copy success
{
    Write-Host "Backup Successful"
}
else
{
    Write-Host "Backup-Failed"
}