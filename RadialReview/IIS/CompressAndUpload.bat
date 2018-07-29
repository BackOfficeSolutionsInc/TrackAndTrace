@echo off
echo( >> log.txt
echo '======================%date:/=-% %time::=-%======================' >> log.txt
move install.zip ./_Old/"%date:/=-% %time::=-% install.zip" >> log.txt 2>>&1

for /d %%X in (./install) do (for /d %%a in (%%X) do ( "C:\Program Files\7-Zip\7z.exe" a -tzip "install.zip" ".\%%a\" )) >> log.txt 2>>&1
aws s3 cp install.zip s3://Radial/Installer/ --region us-east-1 --grants read=uri=http://acs.amazonaws.com/groups/global/AllUsers full=emailaddress=clay.upton@mytractiontools.com >> log.txt 2>>&1

@echo on
