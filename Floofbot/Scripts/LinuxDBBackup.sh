#! /bin/sh

BACKUP_PREFIX="dbBackup.db."

db_path=$1
output_path=$2
max_backups=$3

partial_path="$output_path/$BACKUP_PREFIX"

## Remove oldest max backup
rm -f "$output_path/$BACKUP_PREFIX$max_backups"

## Shift existing backups over by one
current_backup=`expr $max_backups - 1`
while [ "$current_backup" -gt 0 ]; do
    new_backup=`expr $current_backup + 1`
    mv "$partial_path$current_backup" "$partial_path$new_backup" 2>/dev/null
    current_backup=`expr $current_backup - 1`
done

## Copy database file
cp $db_path "$partial_path""1"

if [ $? -eq 0 ]; then
    echo "Backup successful"
    exit 0
else
    echo "Backup failed"
    exit 1
fi
