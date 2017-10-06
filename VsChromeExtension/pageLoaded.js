var whiteBlock = "if (typeof(whiteBlocked)===\"undefined\" || !whiteBlocked){";
	whiteBlock+= "var div = document.createElement('DIV');";
	whiteBlock+= "div.id='white-cover';";
	whiteBlock+= "div.style.position = 'fixed';";
	whiteBlock+= "div.style.top = '0';";
	whiteBlock+= "div.style.left = '0';";
	whiteBlock+= "div.style.right = '0';";
	whiteBlock+= "div.style.bottom = '0';";
	whiteBlock+= "div.style.backgroundColor = 'white';";
	whiteBlock+= "div.style.textAlign= 'center';";
    whiteBlock+= "div.style.paddingTop= '40px';";
    whiteBlock+= "div.style.fontSize= '20px';";
    whiteBlock+= "div.style.fontFamily= 'monospace';";
	whiteBlock+= "div.style.color = 'black';";
	whiteBlock+= "div.style.zIndex = '1000000000000000';";
	whiteBlock+= "div.innerHTML = 'Page Blocked. <a style=\"color:blue\" href=\"#\" onclick=\"while(document.getElementById(\\\'white-cover\\\')){document.getElementById(\\\'white-cover\\\').parentNode.removeChild(document.getElementById(\\\'white-cover\\\'))};whiteBlocked=false;\">Close</a>';";
	whiteBlock+= "var body = document.body;";
	whiteBlock+= "body.appendChild(div);";
	whiteBlock+= "whiteBlocked=true;}";
	
var customBlock = function(txt){
	var wb = "if (typeof(whiteBlocked)===\"undefined\" || !whiteBlocked){";
	wb+= "var div = document.createElement('DIV');";
	wb+= "div.id='white-cover';";
	wb+= "div.style.position = 'fixed';";
	wb+= "div.style.top = '0';";
	wb+= "div.style.left = '0';";
	wb+= "div.style.right = '0';";
	wb+= "div.style.bottom = '0';";
	wb+= "div.style.backgroundColor = 'white';";
	wb+= "div.style.textAlign= 'center';";
    wb+= "div.style.paddingTop= '40px';";
    wb+= "div.style.fontSize= '20px';";
    wb+= "div.style.fontFamily= 'monospace';";	
	wb+= "div.style.color = 'black';";
	wb+= "div.style.zIndex = '1000000000000000';";
	wb+= "div.innerHTML = '"+txt+" <a style=\"color:blue\" href=\"#\" onclick=\"while(document.getElementById(\\\'white-cover\\\')){document.getElementById(\\\'white-cover\\\').parentNode.removeChild(document.getElementById(\\\'white-cover\\\'))};whiteBlocked=false;\">Close</a>';";
	wb+= "var body = document.body;";
	wb+= "body.appendChild(div);";
	wb+= "whiteBlocked=true;}";
	return wb;
}
	
	
addUrlFunc("pageLoaded","netflix",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code: whiteBlock+"document.getElementsByClassName('icon-player-pause')[0].click();"});
});

addUrlFunc("pageLoaded","reddit",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code:whiteBlock});
});

addUrlFunc("pageLoaded","facebook",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code:whiteBlock});
});

addUrlFunc("pageLoaded","quora",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code:whiteBlock});
});

addUrlFunc("pageLoaded","ycombinator",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code:whiteBlock});
});

addUrlFunc("testDone","",function(tab,callback){
	chrome.tabs.executeScript(tab.id, {code:customBlock("Test completed")});
});