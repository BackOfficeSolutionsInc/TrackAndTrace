var path = require('path')
  , express = require('ep_etherpad-lite/node_modules/express')
  , eejs = require("ep_etherpad-lite/node/eejs")
  , controller = require("./controllers/fileUpload");

var settings = require('ep_etherpad-lite/node/utils/Settings');
var pluginSettings = settings.ep_fileupload_aws || {};
var bucket = pluginSettings.bucket;//'bucket';
var base_key = pluginSettings.base_key||"";//'folder/subfolder/';
var urlBase = "https://s3.amazonaws.com/";
  
  
exports.expressConfigure = function(hook_name, args, cb) {
}

exports.expressServer = function (hook_name, args, cb) {
  args.app.post('/fileUpload', controller.onRequest);
  args.app.get('/up/:filename(*)', function(req, res) { 
    var url = req.params.filename.replace(/\.\./g, '').split("?")[0];
    //var filePath = path.normalize(path.join(__dirname, "upload", url));
	res.writeHead(301,  {Location: urlBase+bucket+"/"+base_key+url});
	res.end();
    //res.sendfile(filePath, { maxAge: exports.maxAge });
  });
}

exports.eejsBlock_editbarMenuLeft = function (hook_name, args, cb) {
    args.content = args.content + eejs.require("ep_fileupload_aws/templates/fileUploadEditbarButtons.ejs", {}, module);
  return cb();
}

exports.eejsBlock_scripts = function (hook_name, args, cb) {
  args.content = args.content + eejs.require("ep_fileupload_aws/templates/fileUploadScripts.ejs", {}, module);
  return cb();
}

exports.eejsBlock_styles = function (hook_name, args, cb) {
  args.content = args.content + eejs.require("ep_fileupload_aws/templates/fileUploadStyles.ejs", {}, module);
  return cb();
}
