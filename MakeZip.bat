@echo off
setlocal
set oldFileNames=bin\Release\WrapMlirText.pikensoft.*.zip
set  newFileName=bin\Release\WrapMlirText.pikensoft.%date%.zip

echo Deleting any old files: %oldFileNames%
del %oldFileNames% 2> nul
echo Compressing new file: %newFileName%
set command=\programs\file\7-Zip\7zG.exe a -tzip -mcu=on -mx=9 %newFileName% ./ReadMe.md ./WrapMlirText.png ./WrapMlirTextCli.png ./License.txt .\bin\Release\WrapMlirText.exe .\bin\Release\WrapMlirTextCli.exe
echo %command%
%command%
