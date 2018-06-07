/**
 * Copyright 2009, 2011 RedHog, Egil Möller <egil.moller@piratpartiet.se>
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS-IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

var crypto = require('crypto');
var fs = require('fs');
var path = require('path');
var formidable = require('formidable');
var eejs = require("ep_etherpad-lite/node/eejs");
var AWS = require('aws-sdk');
const uuidv4 = require('uuid/v4');

var settings = require('ep_etherpad-lite/node/utils/Settings');
var pluginSettings = settings.ep_fileupload_aws || {};
var S3_ACCESS_KEY = pluginSettings.S3_ACCESS_KEY;
var S3_SECRET_KEY = pluginSettings.S3_SECRET_KEY;
var bucket = pluginSettings.bucket;
var base_key = pluginSettings.base_key || "";
var storageClass = pluginSettings.storage_class ||"REDUCED_REDUNDANCY";
var urlBase = "https://s3.amazonaws.com/";


var s3 = new AWS.S3({
    accessKeyId: S3_ACCESS_KEY,//config.get('S3_ACCESS_KEY'),
    secretAccessKey: S3_SECRET_KEY,//config.get('S3_SECRET_KEY'),
    apiVersion: '2006-03-01'
});
var s3Stream = require('s3-upload-stream')(s3);



var hashFile = function (file, hash, digest, cb) {
  if (digest === undefined) digest = 'hex';
  if (hash === undefined) hash = 'md5';
 
  var state = crypto.createHash(hash);

  var stream = fs.createReadStream(file, {});
  stream.on("data", function(data){
    state.update(data);
  });
  stream.on("error", function(err){
    cb(err, null);
  });
  stream.on("close", function(){
    cb(null, state.digest(digest));
  });
}

exports.onRequest = function (req, res) {
	var form = new formidable.IncomingForm();
	form.on('progress', function(bytesReceived, bytesExpected) {
		//console.log('onprogress', parseInt( 100 * bytesReceived / bytesExpected ), '%');
	});
	
	form.on('error', function(err) {
		console.log('err',err);
	});
	
	// This 'end' is for the client to finish uploading
	// upload.on('uploaded') is when the uploading is
	// done on AWS S3
	form.on('end', function() {
		console.log('ended!!!!', arguments);
	});
	
	form.on('aborted', function() {
		console.log('aborted', arguments);
	});
	
    var extension = "";//form.uploadfile.name.split('.').pop();
	var key = "err";//base_key+uuidv4()+"."+extension;
	var contentType = undefined;
	
	form.onPart = function(part) {
		console.log('part',part);
		// part looks like this
		//    {
		//        readable: true,
		//        headers:
		//        {
		//            'content-disposition': 'form-data; name="upload"; filename="00video38.mp4"',
		//            'content-type': 'video/mp4'
		//        },
		//        name: 'upload',
		//        filename: '00video38.mp4',
		//        mime: 'video/mp4',
		//        transferEncoding: 'binary',
		//        transferBuffer: ''
		//    }
		extension = part.filename.split('.').pop();
		key = base_key+uuidv4()+"."+extension;
		contentType = part.mime;
		
		var start = new Date().getTime();
		var uploadData = {
			"Bucket": bucket,
			"Key": key,
			"StorageClass": storageClass,
			"ACL": "public-read"
		};
		if (contentType){
			uploadData.ContentType = contentType;
		}
		
		
		var upload = s3Stream.upload(uploadData);

		// Optional configuration
		//upload.maxPartSize(20971520); // 20 MB
		upload.concurrentParts(5);

		// Handle errors.
		upload.on('error', function (error) {
			console.error('errr',error);
		});
		upload.on('part', function (details) {
			console.log('part',details);
		});
		upload.on('uploaded', function (details) {
			var end = new Date().getTime();
			console.log('it took',end-start);
			console.log('uploaded',details);
		});

		// Maybe you could add compress like
		// part.pipe(compress).pipe(upload)
		part.pipe(upload);
	};

	form.parse(req, function(err, fields, files) {
		// var regex = /^(https?:\/\/[^\/]+)\//;
		// var urlBase = 'http://' + req.headers.host;
		// var matches = regex.exec(req.headers['referer']);
		// if(typeof req.headers['referer'] != "undefined" && typeof matches[1] != "undefined"){
		  // urlBase = matches[1];
		// }
		res.send(eejs.require("ep_fileupload_aws/templates/fileUploaded.ejs", {upload: urlBase + /*bucket +*/ "up/" + key}, module));

	});
	return;
}
  
  
  /*form.uploadDir = path.normalize(path.join(__dirname, "..", "upload"));
  form.parse(req, function(err, fields, files) {
    if (err) throw err;

    var tmp = files.uploadfile.path;
    var extension = files.uploadfile.name.split('.').pop();

    hashFile(tmp, undefined, undefined, function (err, hash) {
      var name = hash + "." + extension;
      var perm = path.normalize(path.join(__dirname, "..", "upload", name));
      fs.rename(tmp, perm, function(err) {
        fs.unlink(tmp, function() {
          if (err) throw err;
            var regex = /^(https?:\/\/[^\/]+)\//;
            var urlBase = 'http://' + req.headers.host;
            var matches = regex.exec(req.headers['referer']);
            if(typeof req.headers['referer'] != "undefined" && typeof matches[1] != "undefined"){
              urlBase = matches[1];
            }
            res.send(eejs.require("ep_fileupload_aws/templates/fileUploaded.ejs", {upload: urlBase + "/up/" + name}, module));
        });
      });
    });
  });*/

