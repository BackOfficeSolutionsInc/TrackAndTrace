Update files in the "install" directory

Run CompressAndUpload.bat to upload the files to S3. 
 - Any existing install.zip will be date-tagged and moved to the _Old directory

New servers will pull the "install" directory from S3 and execute the code on install.

log.txt contains a log of the CompressAndUpload program


CLOUDWATCH NOTE: You must cycle out all instances if you update the cloudwatch settings.