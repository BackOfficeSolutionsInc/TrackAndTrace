// this is the background code...

// listen for our browerAction to be clicked
// chrome.browserAction.onClicked.addListener(function (tab) {
	// // for the current tab, inject the "inject.js" file & execute it
	
// });
chrome.tabs.onUpdated.addListener(function (tabId , info) {
  if (info.status === 'complete') {
	chrome.browserAction.setIcon({path:"gray.png"});
	try{
		chrome.tabs.query({'active': true, 'lastFocusedWindow': true}, function (tabs) {
			if (tabs.length>0){
				var config = {url: tabs[0].url};
				//console.log("setting config.",config);	  
				try{
					if (config.url.indexOf("chrome://extensions")==-1 && config.url.indexOf("chrome://newtab/")==-1){		
						//chrome.input.ime.activate();			
						chrome.tabs.executeScript(tabId,{
							code: "var config = "+JSON.stringify(config)+";"			
						},function() {
							try{	
								chrome.tabs.executeScript(tabId, {file: 'inject.js'});
								
							}catch(e){
								console.warn(e);
							}
						});
					}
				}catch(e){
					console.warn(e);
				}
			}
		});
	}catch(e){
		console.warn(e);
	}
  }
});

chrome.browserAction.onClicked.addListener(function(tab) {   
	var config = {url: tab.url};
	
	setTimeout(function(){
		//var builder = [{type: "keydown", requestId: "d"+(new Date()), key: "Tab", code: "Tab"},{type: "keyup", requestId: "u"+(new Date()), key: "Tab", code: "Tab"}];
		
		for(var i =0;i<5;i++){
		
			var builder = [
				//{type: "keydown", requestId: "d"+(new Date()), key: " ", code: "Space"},{type: "keyup", requestId: "u"+(new Date()), key: " ", code: "Space"},
				{type: "keydown", requestId: "d"+(new Date()), key: "Tab", code: "Tab"},{type: "keyup", requestId: "u"+(new Date()), key: "Tab", code: "Tab"}
			];
			
			chrome.input.ime.sendKeyEvents({contextID:0,keyData:builder},function(e){
				if(chrome.runtime.lastError) {
					console.warn("Whoops.. " + chrome.runtime.lastError.message);
				}
			});	
		}
		
	},1000);
	
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

chrome.input.ime.onDeactivated.addListener(function(){
	debugger;
	console.error("IME Deactivated");
})

imeActivated=false;
requestCodeCounter = 0;
function sendKeysInternal(text, sendResponse){
	console.log("sending: '"+text+"'");
	try{
		if (!imeActivated){
			imeActivated = true;
			chrome.input.ime.activate(function(){
				if(chrome.runtime.lastError) {
					console.error("activate failed ('"+text+"'): "+chrome.runtime.lastError.message);
					imeActivated=false;
				}
			});
		}
	}catch(w){
		console.warn("activate",w);
	}
	var builder =[];
	function addKey(key,code){
		builder.push({type:"keydown",requestId: "d"+requestCodeCounter,key:key,code:code});
		builder.push({type:"keyup",requestId: "u"+requestCodeCounter,key:key,code:code});
		requestCodeCounter+=1;
	}
	
	function addChar(key){
		if (key==" "){			
			addKey(" ","Space");
		}else{		
			addKey(key,"Key"+key.toUpperCase());
		}
	}
	
	function transform(key){
		if (key=="Up") return "ArrowUp";
		if (key=="Left") return "ArrowLeft";
		if (key=="Right") return "ArrowRight";
		if (key=="Down") return "ArrowDown";
		return key;
	}
	
	var open=false;
	var openTxt = "";
	for(var ci in text){
		var c= text[ci];		
		if (!open){
			if (c == "{"){
				open=true;
			}else{
				addChar(c);
			}
		}else{
			if (c=="}"){
				open=false;
				var openTxtTransform =transform(openTxt);
				
				addKey(openTxtTransform,openTxtTransform);
				openTxt="";
			}else{
				openTxt+=c;
			}
		}
	}
	
	console.log(builder);
	
	chrome.input.ime.sendKeyEvents({contextID:0,keyData:builder},function(e){
		if(chrome.runtime.lastError) {
			console.warn("Whoops.. " + chrome.runtime.lastError.message);
			// if (sendResponse){
				// sendResponse(JSON.stringify({error:chrome.runtime.lastError}));
			// }			
		}else{
			console.log("Sent text:"+text);
		}
	});	
	
}


chrome.runtime.onMessage.addListener(
  function(request, sender, sendResponse) {
	console.log(sender.tab ?"from a content script:" + sender.tab.url :"from the extension");
	if (request.method == "sendKeys"){		
		try{
			sendKeysInternal(request.text,sendResponse);
			sendResponse("ok");
		}catch(e){
			sendResponse({error:e});
		}
		/*
		try{
		sendKeysText(request.text);
		sendResponse("ok");
		}catch(e){
			sendResponse({error:e});
		}*/
	}
	if (request.method == "setIcon"){
		chrome.browserAction.setIcon({path:request.icon});
		sendResponse("ok");
	}	
});