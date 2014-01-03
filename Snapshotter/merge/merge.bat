@echo off
cd %cd%
ilrepack /target:exe /out:SnapshotBackup.exe ..\bin\Debug\SnapshotBackup.exe ..\bin\Debug\PowerArgs.dll
copy ..\bin\Debug\AWSSDK.DLL .
del *.pdb
