var WebRtcDemo = WebRtcDemo || {};

// todo:
//  cleanup: proper module loading
//  cleanup: promises to clear up some of the async chaining
//  feature: multiple chat partners

var connected = false;
var webRtc_NameLookup = {};

WebRtcDemo.App = (function (viewModel, connectionManager) {
	var viewModel = {};

	window.AudioContext = window.AudioContext || window.webkitAudioContext;
	var audioContext = {};
	var sourceI = {},
		inputPoint = {},
		audioRecorder = null;
	var analyserContext = null;
	var analyserNode = {};
	var rafID = null;
	var zeroGain = {};

	var updateAnalysers = function(time) {
		//debugger;
		// analyzer draw code here
		//var SPACING = 3;
		//var BAR_WIDTH = 1;
		//var numBars = Math.round(canvasWidth / SPACING);

		var maxId = null;
		var max = 200;
		
		$(".loudest").removeClass("loudest");
		for (var k in analyserNode) {
			var freqByteData = new Uint8Array(analyserNode[k].frequencyBinCount);
			analyserNode[k].getByteFrequencyData(freqByteData);
			var sum = freqByteData.reduce(function(pv, cv) { return pv + cv; }, 0);
			var html = "";

			if (sum > max) {
				max = sum;
				maxId = k;
			}

			for(var i=0;i<Math.min(5,sum/300);i++)
			{
				html += "<div class='volume-bar'></div>";
			}
			

			$("." + k + " .volume").html(html);
			if (sum > 300) {
				$("." + k).addClass("loudest");
			}

			//console.log(freqByteData);
		}

		/*if (maxId != null) {
			$("." + maxId).addClass("loudest");
		}*/



		//analyserContext.clearRect(0, 0, canvasWidth, canvasHeight);
		//analyserContext.fillStyle = '#F6D565';
		//analyserContext.lineCap = 'round';
		//var multiplier = analyserNode.frequencyBinCount / numBars;

		// Draw rectangle for each frequency bin.
		/*for (var i = 0; i < numBars; ++i) {
			var magnitude = 0;
			var offset = Math.floor(i * multiplier);
			// gotta sum/average the block, or we miss narrow-bandwidth spikes
			for (var j = 0; j < multiplier; j++)
				magnitude += freqByteData[offset + j];
			magnitude = magnitude / multiplier;
			var magnitude2 = freqByteData[i * multiplier];
			//analyserContext.fillStyle = "hsl( " + Math.round((i * 360) / numBars) + ", 100%, 50%)";
			//analyserContext.fillRect(i * SPACING, canvasHeight, BAR_WIDTH, -magnitude);
		}*/

		rafID = window.requestAnimationFrame(updateAnalysers);
	};


	var _mediaStream,
		_hub,
		_connect = function (username, onSuccess, onFailure) {
			// Set Up SignalR Signaler
			var hub = $.connection.meetingHub;
			$.support.cors = true;
			$.connection.hub.url = '/signalr/hubs';
			_setupHubCallbacks(hub);
			$.connection.hub.start(Constants.StartHubSettings)
				.done(function () {
					console.log('connected to SignalR hub... connection id: ' + _hub.connection.id);

					// Tell the hub what our username is
					hub.server.join(VideoChatRoomId, _hub.connection.id);

					if (onSuccess) {
						if (!connected) {
							console.log("~calling in");
							viewModel.Mode = ('calling');
							hub.server.callMeeting(VideoChatRoomId, true, true);
							connected = true;
						}

						onSuccess(hub);
					}
				})
				.fail(function (event) {
					if (onFailure) {
						onFailure(event);
					}
				});

			// Setup client SignalR operations
			_hub = hub;
		},

		_start = function (hub) {
			// Show warning if WebRTC support is not detected
			if (webrtcDetectedBrowser == null) {
				console.log('Your browser doesnt appear to support WebRTC.');
				$('.browser-warning').show();
			}

			// Then proceed to the next step, gathering username
			_getUsername();
		},

		_getUsername = function () {
			//username = window.prompt("What is your name?", '');
			username = "" + (Math.random() * 1000000000);
			_startSession(username);
		},

		_addStreamGain = function(stream,id) {
			audioContext[id] = new AudioContext();
			analyserNode[id] = audioContext[id].createAnalyser();
			analyserNode[id].fftSize = 32;
			//inputPoint[id] = audioContext[id].createGain();
				// Create an AudioNode from the stream.
			sourceI[id] = audioContext[id].createMediaStreamSource(stream);
			sourceI[id].connect(analyserNode[id]);
			//analyserNode[id].connect(audioContext[id].destination);

			//audioRecorder = new Recorder( inputPoint );

			/*zeroGain[id] = audioContext.createGain();
			zeroGain[id].gain.value = 0.0;
			inputPoint[id].connect(zeroGain[id]);
			zeroGain[id].connect(audioContext.destination);*/
		},
		_tryGetMedia = function (audio, video, success, onError) {
			getUserMedia({
				// Permissions to request
				video: video,
				audio: audio
			},
			function (stream) { // succcess callback gives us a media stream
				$('.instructions').hide();
				// Store off the stream reference so we can share it later
				_mediaStream = stream;


				//==========Volume========== 

				_addStreamGain(stream, "streamid_"+viewModel.MyConnectionId );
				

				//========End-Volume========


				// Load the stream into a video element so it starts playing in the UI
				console.log('playing my local video feed');
				//var videoElement = document.querySelector('.video.mine');

				var container = $("<div class='video-container mine streamid_" + viewModel.MyConnectionId + "'><video muted src='' height='116px' autoplay/><div class='video-name'>You</div><div class='volume'></div></div>");

				$(".video-bar").prepend(container);
				var videoElement = $(".video-container.streamid_" + viewModel.MyConnectionId + " video")[0];

				if (video || audio) {
					attachMediaStream(videoElement, _mediaStream);
				}

				// UI in calling mode
				viewModel.Mode = ('calling');

				// Hook up the UI
				//_attachUiHandlers();
				setTimeout(function () {
					console.log("FOUR");
					_hub.server.callMeeting(VideoChatRoomId, true, true);
				}, 1000);

				$(".sendVideo").removeClass("fontastic-icon-eye-slash-close");
				$(".sendVideo").removeClass("fontastic-icon-eye-2");
				$(".sendAudio").removeClass("fontastic-icon-mic-no");
				$(".sendAudio").removeClass("fontastic-icon-mic");

				if (video && _mediaStream.getVideoTracks().length > 0) {
					$(".sendVideo").addClass("fontastic-icon-eye-2");
				} else {
					$(".sendVideo").addClass("fontastic-icon-eye-slash-close");
				}
				if (audio && _mediaStream.getAudioTracks().length > 0) {
					$(".sendAudio").addClass("fontastic-icon-mic");
				} else {
					$(".sendAudio").addClass("fontastic-icon-mic-no");
				}

				if (success)
					success();

			},
			function (error) { // error callback

				console.error(error);
				/*if (video == true && audio == true)
					_tryGetMedia(true, false,success,onError);
				else if (video == false && audio == true)
					_tryGetMedia(false, true,success,onError);
				else if (onError) {*/
				onError();
				//}
			}
		);
		};
	_startSession = function (username) {
		// viewModel.Username(username); // Set the selected username in the UI
		// viewModel.Loading(true); // Turn on the loading indicator

		// Now we have everything we need for interaction, so fire up SignalR
		_connect(username, function (hub) {
			// tell the viewmodel our conn id, so we can be treated like the special person we are.
			viewModel.MyConnectionId = hub.connection.id;

			// Initialize our client signal manager, giving it a signaler (the SignalR hub) and some callbacks
			console.log('initializing connection manager');
			updateAnalysers();
			connectionManager.initialize(hub.server, _callbacks.onReadyForStream, _callbacks.onStreamAdded, _callbacks.onStreamRemoved);
			_attachUiHandlers();
		}, function (event) {
			console.error(event);
			// viewModel.Loading(false);
		});
	},

	_attachUiHandlers = function () {
		// Add click handler to users in the "Users" pane
		$('body').on('click', '.start-video:not(.disabled)', function () {
			$(".start-video").addClass("disabled");
			_tryGetMedia(true, true, function () {
				$(".start-conference").addClass("hidden");
			}, function () {
				showAlert("Failed to connect to hardware. Make sure camera and audio are enabled in your browser.");
				$(".start-video").removeClass("disabled");
			});
		});
		$('body').on('click', '.start-screenshare', function () {
			_tryGetMedia(true, {
				mandatory: {
					chromeMediaSource: 'screen',
					maxWidth: 1280,
					maxHeight: 720
				},
				optional: []
			}, function () {
				$(".start-conference").addClass("hidden");
			}, function () {
				showAlert("Failed to connect to hardware. Make sure camera and audio are enabled in your browser.");
			});
		});



		//var connected = false;

		$('body').on('click', '.uncollapser .clicker', function () {
			if (!connected) {
				console.log("calling in");
				viewModel.Mode = ('calling');
				_hub.server.callMeeting(VideoChatRoomId, true, true);
				connected = true;
			}
			/*$(".video-bar").toggleClass("shifted");
			$(this).parent().toggleClass("shifted");*/
		});
		$('body').on('click', '.sendVideo', function (e) {
			if (_mediaStream.getVideoTracks().length == 1) {
				var old = _mediaStream.getVideoTracks()[0].enabled;
				var newState = !old;
				_mediaStream.getVideoTracks()[0].enabled = newState;
				$(".sendVideo").removeClass("fontastic-icon-eye-slash-close");
				$(".sendVideo").removeClass("fontastic-icon-eye-2");
				if (newState) {
					$(".sendVideo").addClass("fontastic-icon-eye-2");
				} else {
					$(".sendVideo").addClass("fontastic-icon-eye-slash-close");
				}
			}
		});
		$('body').on('click', '.sendAudio', function (e) {
			if (_mediaStream.getAudioTracks().length == 1) {
				var old = _mediaStream.getAudioTracks()[0].enabled;
				var newState = !old;
				_mediaStream.getAudioTracks()[0].enabled = newState;
				$(".sendAudio").removeClass("fontastic-icon-mic-no");
				$(".sendAudio").removeClass("fontastic-icon-mic");
				if (newState) {
					$(".sendAudio").addClass("fontastic-icon-mic");
				} else {
					$(".sendAudio").addClass("fontastic-icon-mic-no");
				}
			}
		});


		$('body').on('click', '.video-container', function () {
			$(".video-overlay").addClass("fade1");
			var self = this;
			setTimeout(function () {
				console.log("FIVE");
				$(".video-overlay video").attr("src", $(self).find("video").attr("src"));
				$(".video-overlay video")[0].muted = $(self).find("video")[0].muted;
				$(".video-overlay").removeClass("hidden1");
				setTimeout(function () {
					$(".video-overlay").removeClass("fade1");
				}, 100);
			}, 150);
		});

		// Add handler for the hangup button
		$('.video-overlay').click(function () {
			$(".video-overlay").addClass("hidden1");
			$(".video-overlay video").attr("src", "");
		});
	},

	_setupHubCallbacks = function (hub) {
		// Hub Callback: Incoming Call
		hub.client.incomingCall = function (callingUser) {

			console.log('incoming call from: ' + JSON.stringify(callingUser));
			webRtc_NameLookup[callingUser.ConnectionId] = callingUser.Name;

			hub.server.answerCall(true, callingUser.ConnectionId);
			viewModel.Mode = ('incall');
			/*if (_mediaStream) {
				console.log("responding with steam..");
				connectionManager.initiateOffer(callingUser.ConnectionId, _mediaStream);
				//viewModel.Mode = ('incall');
			}*/
			// Ask if we want to talk
			/*var e = window.confirm(callingUser.Username + ' is calling.  Do you want to chat?');
			if (e) {
				// I want to chat
				hub.server.answerCall(true, callingUser.ConnectionId);

				// So lets go into call mode on the UI
				viewModel.Mode = ('incall');
			} else {
				// Go away, I don't want to chat with you
				hub.server.answerCall(false, callingUser.ConnectionId);
			}*/
		};

		hub.client.offerTo = function (acceptingUser) {
			console.log('offering stream to: ' + JSON.stringify(acceptingUser));
			webRtc_NameLookup[acceptingUser.ConnectionId] = acceptingUser.Name;
			connectionManager.initiateOffer(acceptingUser.ConnectionId, _mediaStream);
		};

		// Hub Callback: Call Accepted
		hub.client.callAccepted = function (acceptingUser) {
			console.log('call accepted from: ' + JSON.stringify(acceptingUser) + '.  Initiating WebRTC call and offering my stream up...');

			webRtc_NameLookup[acceptingUser.ConnectionId] = acceptingUser.Name;

			// Callee accepted our call, let's send them an offer with our video stream
			if (_mediaStream) {
				connectionManager.initiateOffer(acceptingUser.ConnectionId, _mediaStream);
				viewModel.Mode = ('incall');
			} else {
				console.log("Error: _mediaStream empty");
				hub.server.promptInitiate();
			}
			// Set UI into call mode
		};

		// Hub Callback: Call Declined
		hub.client.callDeclined = function (decliningConnectionId, reason) {
			console.log('call declined from: ' + decliningConnectionId);

			// Let the user know that the callee declined to talk
			console.log(reason);
			// Back to an idle UI
			viewModel.Mode = ('idle');
		};

		// Hub Callback: Call Ended
		hub.client.callEnded = function (connectionId, reason) {
			console.log('call with ' + connectionId + ' has ended: ' + reason);

			// Let the user know why the server says the call is over
			console.log(reason);
			// Close the WebRTC connection
			connectionManager.closeConnection(connectionId);
			// Set the UI back into idle mode
			viewModel.Mode = ('idle');
		};

		// Hub Callback: Update User List
		hub.client.updateUserList = function (userList) {
			viewModel.setUsers = (userList);
		};

		// Hub Callback: WebRTC Signal Received
		hub.client.receiveSignal = function (callingUser, data) {
			connectionManager.newSignal(callingUser.ConnectionId, data);
			var d = JSON.parse(data);
			if (d.sdp && d.sdp.type == "answer") {
				//hacky. answer happens all the time.
				//connectionManager.initiateOffer(callingUser.ConnectionId,_mediaStream);
			}

		};
	},

	// Connection Manager Callbacks
	_callbacks = {
		onReadyForStream: function (connection) {
			// The connection manager needs our stream
			// todo: not sure I like this
			if (_mediaStream) {
				
				console.log("ZERO");
				connection.addStream(_mediaStream);
				
				console.log("SIX");
			}
		},
		onStreamAdded: function (connection, event, streamId) {
			console.log('binding remote stream to the partner window');
			// Bind the remote stream to the partner window
			//var otherVideo = document.querySelector('.video.partner');
			var container = $("<div class='video-container streamid_" + streamId + "'><video src='' height='116px' autoplay/><div class='video-name'>" + webRtc_NameLookup[streamId] + "</div><div class='volume'></div></div>");
			$(".video-bar").append(container);

			var otherVideo = $(".video-container.streamid_" + streamId + " video")[0];

			console.log("ONE");
			var timer = 1000;

			var tryAttachVideo = function() {
				console.log("TWO");
				console.log(ConnectionEstablished[streamId]);
				if (!(streamId in ConnectionEstablished) || ConnectionEstablished[streamId] <= 0) {
					timer *= 1.5;
					setTimeout(tryAttachVideo, timer);
				} else {
					//_addStreamGain(event.stream, "streamid_" + streamId);
					attachMediaStream(otherVideo, event.stream); // from adapter.js
					console.log("THREE");
				}
			};

			setTimeout(tryAttachVideo, timer);
		},
		onStreamRemoved: function (connection, streamId) {
			// todo: proper stream removal.  right now we are only set up for one-on-one which is why this works.
			console.log('removing remote stream from partner window:' + streamId);

			var otherVideo = $(".video-container.streamid_" + streamId);
			// Clear out the partner window
			//var otherVideo = document.querySelector('.video.partner');
			otherVideo.find("video").attr("src", '');
			otherVideo.remove();
		}
	};

	return {
		start: _start, // Starts the UI process
		getStream: function () { // Temp hack for the connection manager to reach back in here for a stream
			return _mediaStream;
		}
	};
})(WebRtcDemo.ViewModel, WebRtcDemo.ConnectionManager);

// Kick off the app
WebRtcDemo.App.start();