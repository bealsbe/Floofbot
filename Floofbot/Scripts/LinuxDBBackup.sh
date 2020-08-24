#!/bin/bash

if cp $1 $2dbBackup.db.$3 ; then
	echo "Backup Success"
else
	echo "Backup Fail"
fi
