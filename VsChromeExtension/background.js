chrome.browserAction.onClicked.addListener(function(tab) {
	loading=true;
	badge("...","yellow","yellow.png");
	
	chrome.tabs.getAllInWindow(window.id,function(tabs){
		for(var j=0;j<tabs.length;j++){
			var tab = tabs[j];
			chrome.tabs.executeScript(tab.id, {code:"var element = document.getElementById('white-cover');element.outerHTML = '';delete element;"});
		}
	});
});
	
	var listener = "http://localhost:60024/";
	chrome.tabs.getCurrent(function(tab) {
		chrome.browserAction.setIcon({
			path: "icon.png",
			//tabId: tab.id
		});
	});
	
	var windowActions = {};
	
	
	function addUrlFunc(action,urlPart,callback){
		if(!windowActions[action])
			windowActions[action]={};
		windowActions[action][urlPart]=function(t){
			console.log("running: "+action+" - "+urlPart);
			callback(t);
		};
	}
	
	function _processUrl(action,tab){
		var url = tab.url;
		if (windowActions[action]){
			for(var urlPart in windowActions[action]){
				if (url.indexOf(urlPart)!=-1){
					windowActions[action][urlPart](tab);
				}
			}
		}
	}
	
	function processAction(action){
		chrome.windows.getAll(function(windows){
			for(var i =0; i<windows.length;i++){
				var window = windows[i];
				//if (window.state=="normal"){
				chrome.tabs.getAllInWindow(window.id,function(tabs){
					for(var j=0;j<tabs.length;j++){
						var tab = tabs[j];
						_processUrl(action,tab);
					}
				});
			}
		});
	}
	function showNotification(title,message,icon){
		if (typeof(icon)==="undefined")
			icon = "icon.png";
		if (typeof(message)==="undefined")
			message = "";
		if (typeof(title)==="undefined")
			title = "Notification!";
		chrome.notifications.create({title:title,message:message,iconUrl:icon,type:"basic"});
	}
	
	function pageLoaded(message,icon){
		if (loading){
			showNotification("Page Loaded",message,icon);
			processAction("pageLoaded");
		}
		loading=false;
	}
	
	var loading = false;
	
	function badge(text,color,icon){
		if (color=="yellow")
			color = "#FFBC00";
		if (color=="green")
			color = "#A5D82A";
		if (color=="red")
			color = "#B31515";
			
		chrome.browserAction.setBadgeText({text:text});
		chrome.browserAction.setBadgeBackgroundColor({color:color});
		chrome.browserAction.setIcon({path: icon});	
	}

	function processRequest(cmd){
		if (cmd.startsWith("dbStart")){
			loading=true;
			badge("db","yellow","yellow.png");
		}else if (cmd.startsWith("dbComplete")){
			loading=true;
			badge("db","green","yellow.png");
			//alert("VS Ready");
		}else if (cmd.startsWith("dbError")){
			loading=true;
			badge("db","red","green.png");
			showNotification("Database Error:",cmd,"red.png");
		}else if (cmd.startsWith("appStart")){
			loading=true;
			badge("app","yellow","yellow.png");	
		}else if (cmd.startsWith("appEnd")){
			loading=false;
			badge("app","green","gray.png");
		}else if (cmd.startsWith("pageLoad")){
			badge("","green","green.png");
			pageLoaded();
		}else if (cmd.startsWith("pageError")){
			badge("err","red","red.png");
			pageLoaded("Page error","red.png");
		}else if (cmd.startsWith("testDone")){
			badge("app","green","green.png");
			processAction("testDone");
		}else{
			badge("cmd?","red","icon.png");
			console.log("cmd not found: "+cmd);
		}
	}
	 
	errorCount=0;
	function getCommand(){

		var postData = { 
			"action": "getCommands" 
		};

		jQuery.ajax({
			url: listener,
			data:postData,
			success: function (json) { 
				if (errorCount>0){
					badge("","green","icon.png")
				}
				errorCount=0;
				if (json.commands && json.commands.length && json.status=="ok"){
					console.log(json);
					//var json = JSON.parse(response); 
					for (var i=0;i<json.commands.length;i++){
						processRequest(json.commands[i]);
					}
					$.ajax({url:listener,data: {"action":"clearCommands"}});
				}
				if (json.status=="error"){
					alert("Error:"+json.message);
				}
			},
			error: function(){
				errorCount++;
				if (errorCount==3){
					badge("","green","gray.png");
					showNotification("Lost connection","","gray.png");
				}
					//chrome.browserAction.setIcon({path: "icon.png"});
			},
			timeout: 10000,
		});
		
		setTimeout(getCommand, 100);
	}

	function send(data){

		var postData = {
			"action" : "send",
			"data": data
		};

		$.post(listener, postData);
	}


	setTimeout(getCommand, 100);
// });