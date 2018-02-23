// Saves options to chrome.storage
var defaults = {
    TT_URL: 'https://tractiontoolsalpha.com',
	DEBUG_INFO:false,
	OPENER: "Hi {0},",
	CLOSER: "Have a great day!{Enter}{Enter}{Enter}Best,{Enter}{1}",
}


function save_options() {
  var TT_URL = document.getElementById('TT_URL').value;
  var DEBUG_INFO = document.getElementById('DEBUG_INFO').checked;
  var OPENER = document.getElementById('OPENER').value;
  var CLOSER = document.getElementById('CLOSER').value;
  chrome.storage.sync.set({
    TT_URL: TT_URL,
    DEBUG_INFO: DEBUG_INFO,
    OPENER: OPENER,
    CLOSER: CLOSER,
  }, function() {	
		window.close();  
  });
}

function load_defaults(){
	chrome.storage.sync.set(defaults,function(){
		restore_options();
	});
}

// Restores select box and checkbox state using the preferences
// stored in chrome.storage.
function restore_options() {
  // Use default value color = 'red' and likesColor = true.
  chrome.storage.sync.get(defaults, function(items) {
    document.getElementById('TT_URL').value = items.TT_URL;
	document.getElementById('DEBUG_INFO').checked = items.DEBUG_INFO;
    document.getElementById('OPENER').value = items.OPENER;
    document.getElementById('CLOSER').value = items.CLOSER;
	
  });
}
document.addEventListener('DOMContentLoaded', restore_options);
document.getElementById('save').addEventListener('click',save_options);
document.getElementById('restoreDefaults').addEventListener('click',load_defaults);
//},1);