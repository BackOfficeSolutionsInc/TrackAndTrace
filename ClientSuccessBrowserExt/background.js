// this is the background code...

// listen for our browerAction to be clicked
// chrome.browserAction.onClicked.addListener(function (tab) {
	// // for the current tab, inject the "inject.js" file & execute it
	
// });
chrome.tabs.onUpdated.addListener(function (tabId , info) {
  chrome.browserAction.setIcon({path:"gray.png"});
  if (info.status === 'complete') {	  
	chrome.tabs.query({'active': true, 'lastFocusedWindow': true}, function (tabs) {	  
		var config = {url: tabs[0].url};
		chrome.tabs.executeScript(tabId,{
			code: "var config = "+JSON.stringify(config)+";"			
		},function() {
			chrome.tabs.executeScript(tabId, {file: 'inject.js'});
			chrome.browserAction.setIcon({path:"icon.png"});
		});
	});
  }
});

chrome.browserAction.onClicked.addListener(function(tab) { 
	chrome.tabs.executeScript(tab.id,{
		code: "inject_setup()",
	});
});
