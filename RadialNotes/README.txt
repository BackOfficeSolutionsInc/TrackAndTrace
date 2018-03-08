===========IMPORTANT NOTES===========
1. THESE FILES CONTAIN SENSITIVE DATA.
	- .ebextensions/03_settings_file.config 
		+ IAM keys for uploading to the notes server space
	- SSL.7z contains encrypted ssl certificates. 
2. Server cannot scale up. A local cache on etherpad-lite is used to store edits before saving to db
3. Helpful Logs are stored at 
	- /var/radial/start.log
	- /var/log/eb-activity.log
	- /var/log/nodejs/
	- /var/log/nginx/
4. The AWS File Upload extension is in the "custom" folder.
5. Etherpad lite requires node 4.8.7
6. Do not do a log pull. It doesn't work.
7. Do not Rebuild Environment. It fails because security groups cannot be deleted.

 
===========INSTALL NEW SERVER===========
 1. Create a zip of the contents of this directory (not the parent directory! Zip needs to be a set of files, not one directory) 
 2. Upload using Elastic Beanstalk (increment version number)
 3. Deploy to Notes-env-2
 4. (optional) view deployment with "tail -f /var/radial/start.log"
 
 
===========RESTART MANUALLY===========
 sudo killall tt-server.sh
 #crontab should run after 10 seconds. If not run:
 #/var/radial/install/tt-server.sh
 
 
===========HOW IT WORKS===========
1. Files are installed with the "file" ebextension
	- the server is setup under a temporary directory /var/radial/install/
	- the settings file is copied into this temp folder
2. The Post deploy hook file is run. 
	- must be run in post deploy otherwise the folder will get overwritten by elb.
	- /opt/elasticbeanstalk/hooks/appdeploy/post/99_restart_delayed_job.sh
		+ all operations are running in the temp folder
	- etherpad-lite is deleted, and redownloaded
		+ /var/radial/current/install/download.sh
	- All plugins are installed
		+ /var/radial/current/install/fileUploadAWS.sh
		+ /var/radial/current/install/additionalPlugins.sh
	- Dependency script is run
		+ found in /etherpad-lite/bin/installDependencies.sh
	- files are copied from the temp directory into the live directory
	- The live directory is /var/app/current
3. A crontab is installed that runs every 10 seconds.
	- The crontab starts "runner.sh" which checks if the server is running
	- When the server is not running, tt-server.sh is started
		+ tt-server.sh changes directories to the etherpad-lite folder
		+ tt-server.sh starts the node server under port 8081 (which is required for the nginx proxy server)
4. ELB automatically starts "node app.js:"
	- This is a phantum server, it runs on port 9001, it exists because etherpad has a non-typical start process
	- Extensive attempts were made to get ELB to play nice, it could not be accomplished.


 
===========ELB Settings===========

Add the servers security group to the database security group.

Software
	- Proxy Server:	Nginx
	- Node version:	4.8.7	
	- Node command:	<empty>
	
Capacity
	- Environment:	Load Balanced
	- Instances: 	min 1, max 1
	
	
Security:
	- Service role: aws-elasticbeanstalk-service-role
	- EC2 key pair: radial-east
	- IAM instance profile: aws-elasticbeanstalk-ec2-role
	

 