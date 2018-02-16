// this is the background code...

// listen for our browerAction to be clicked
// chrome.browserAction.onClicked.addListener(function (tab) {
	// // for the current tab, inject the "inject.js" file & execute it
	
// });
chrome.tabs.onUpdated.addListener(function (tabId , info) {
  chrome.browserAction.setIcon({path:"gray.png"});
  if (info.status === 'complete') {
	//console.log("completed update");
	chrome.tabs.query({'active': true, 'lastFocusedWindow': true}, function (tabs) {
		if (tabs.length>0){
			var config = {url: tabs[0].url};
			//console.log("setting config.",config);	  
			try{
				chrome.tabs.executeScript(tabId,{
					code: "var config = "+JSON.stringify(config)+";"			
				},function() {
					try{	
						chrome.tabs.executeScript(tabId, {file: 'inject.js'});
						chrome.browserAction.setIcon({path:"icon.png"});
						
					}catch(e){
						console.warn(e);
					}
				});
			}catch(e){
				console.warn(e);
			}
		}
	});
  }
});

chrome.browserAction.onClicked.addListener(function(tab) {   
	var config = {url: tab.url};
	chrome.tabs.executeScript(tab.id,{
		code: "config = "+JSON.stringify(config)+";inject_setup();",
	});	
});

var textConnection = null;
function onKeysDisconnected(e){
	console.warn("disconnect",e);
	textConnection = null;
}	
function receiveKeysText(str){
	console.info("receiveKeysText:",str);
}
function sendKeysText(text){
	if(textConnection==null){
		var hostName = "com.radial.keyboardsimulator";
		textConnection = chrome.runtime.connectNative(hostName);
		textConnection.onMessage.addListener(receiveKeysText);
		textConnection.onDisconnect.addListener(onKeysDisconnected);
	}
	textConnection.postMessage(text);
}


chrome.runtime.onMessage.addListener(
  function(request, sender, sendResponse) {
	console.log(sender.tab ?"from a content script:" + sender.tab.url :"from the extension");
	if (request.method == "sendKeys"){
		try{
		sendKeysText(request.text);
		sendResponse("ok");
		}catch(e){
			sendResponse({error:e});
		}
	}
});

