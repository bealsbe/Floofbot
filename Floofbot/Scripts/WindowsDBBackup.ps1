$dbPath = $args[0]
$backupPath = $args[1] + "dbBackup.db." + $args[2]

Copy-Item $dbPath -Destination $backupPath -force

if ($?) # Copy success
{
    Write-Host "Backup Successful"
}
else
{
    Write-Host "Backup-Failed"
}