var SpeechRecog = function () {
	this.create_email = false;
	this.final_transcript = '';
	this.recognizing = false;
	this.available = ('webkitSpeechRecognition' in window);
	this.ignore_onend;
	this.start_timestamp;
	this.recognition = false;
	this.wasAborted = false;
	this.forceQuit = false;
	
	this.onstart			= function (event)	{ };
	this.onerror			= function (event)	{ };
	this.onend				= function (event)	{ };
	this.onresult			= function (event)	{ };
	this.onfinalresult		= function (event)	{ };
	this.oninterimresult	= function (event)	{ };

	var first_char = /\S/;
	this.capitalize = function capitalize(s) {
		return s.replace(first_char, function (m) { return m.toUpperCase(); });
	};

	this.Stop = function () {
	    var originalIgnoreOnEnd = this.ignore_onend;
	    this.forceQuit = true;
	    this.recognition.stop();
	};

	this.Init = function () {


		var _self = this;
		//ENSURE ONLY ONE
		var windowId = "wid_"+new Date().getTime();
		Intercom.getInstance().on("AnyWebkitSpeechRecognition_Close", function(data) {
			console.log("AnyWebkitSpeechRecognition_Close: " + data);
			if (_self.wasAborted) {
				console.log("AnyWebkitSpeechRecognition_Close: restart");
				_self.wasAborted = true;
				_self.ignore_onend = true;
				_self.recognition.start();
			}
		});
		$(window).on("beforeunload", function() {
			Intercom.getInstance().emit("AnyWebkitSpeechRecognition_Close", windowId);
		});
		/*Intercom.getInstance().on("AnyWebkitSpeechRecognition", function(data) {
			Intercom.getInstance().emit("AnyWebkitSpeechRecognition_Response", {response:"YES",windowId :windowId});
		});
		Intercom.getInstance().emit("AnyWebkitSpeechRecognition", windowId);*/
		//

		console.log("SpeechRecog: Initializing");
		if (!this.available) {
			console.log("SpeechRecog not available.");
			return false;
		} else {
			console.log("Starting SpeechRecog starting.");
			//start_button.style.display = 'inline-block';

			this.recognition = new window.webkitSpeechRecognition();
			this.recognition.continuous = true;
			this.recognition.interimResults = true;
			this.recognition.onstart = function (event) {
				console.log("SpeechRecog: onstart");
				_self.recognizing = true;
				_self.onstart(event);

			};;
			this.recognition.onerror = function (event) {
				console.log("SpeechRecog: onerror "+event.error);
				if (event.error == 'no-speech') {
					/*start_img.src = '/intl/en/chrome/assets/common/images/content/mic.gif';
					//showInfo('info_no_speech');*/
					_self.ignore_onend = true;
				}
				if (event.error == 'audio-capture') {
					/*start_img.src = '/intl/en/chrome/assets/common/images/content/mic.gif';
					//showInfo('info_no_microphone');*/
					_self.ignore_onend = true;
				}
				if (event.error == 'not-allowed') {
					if (event.timeStamp - _self.start_timestamp < 100) { /*showInfo('info_blocked');*/
					} else { /*showInfo('info_denied');*/
					}
					_self.ignore_onend = false;
				}
				if (event.error == 'aborted') {
					console.log("Aborted: "+new Date());
					if (_self.ignore_onend) {
						/*setTimeout(function() {
							//Ok try again in a second.
							_self.ignore_onend = true;
							_self.recognition.start();
						}, 1000);*/
						_self.wasAborted = true;
					};
					_self.ignore_onend = false;
				}
				_self.onerror(event);
			};;

			this.recognition.onend = function (event) {
				console.log("SpeechRecog: onend");
				_self.recognizing = false;
				if (_self.ignore_onend && _self.forceQuit==false) {
					console.log("SpeechRecog: restarting");
					_self.recognition.start();
					return;
				}
				//start_img.src = '/intl/en/chrome/assets/common/images/content/mic.gif';
				if (!_self.final_transcript) {
					//showInfo('info_start');
					console.log("final_transcript");
					return;
				}
				//showInfo('');
				/*if (window.getSelection) {
					window.getSelection().removeAllRanges();
					var range = document.createRange();
					range.selectNode(document.getElementById('final_span'));
					window.getSelection().addRange(range);
				}*/
				this.forceQuit = false;
				_self.onend(event);
			};

			this.recognition.onresult = function (event) {
				console.log("SpeechRecog: onresult");
				var interim_transcript = '';
				if (typeof (event.results) == 'undefined') {
					_self.recognition.onend = null;
					_self.recognition.stop();
					_self.available = false;
					//upgrade();
					return;
				}
				var anyFinal = false;
				for (var i = event.resultIndex; i < event.results.length; ++i) {
					if (event.results[i].isFinal) {
						console.log("final: " + event.results[i][0].transcript);
						_self.final_transcript += event.results[i][0].transcript;
						_self.onfinalresult(event.results[i][0]);
						anyFinal = true;
					} else {
						interim_transcript += event.results[i][0].transcript;
					}
				}
				_self.final_transcript = _self.capitalize(_self.final_transcript);
				if (!anyFinal) {
					_self.oninterimresult(interim_transcript);
				}
				//_self.final_span.innerHTML = linebreak(final_transcript);
				//_self.interim_span.innerHTML = linebreak(interim_transcript);
			};
			return true;
		}
	};
	this.Start = function() {
		if (this.recognition == false) {
			console.log("Init was not called. Calling.");
			this.Init();
		}

		this.final_transcript = '';
		this.recognition.lang = 'en-US'; // select_dialect.value;
		this.recognition.start();
		//this.ignore_onend = true;
		//this.final_span.innerHTML = '';
		//interim_span.innerHTML = '';
		//start_img.src = '/intl/en/chrome/assets/common/images/content/mic-slash.gif';
		//showInfo('info_allow');
		//showButtons('none');
		this.start_timestamp = new Date();
	};

}