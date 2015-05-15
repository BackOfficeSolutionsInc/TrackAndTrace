var WebRtcDemo = WebRtcDemo || {};

// todo:
//  cleanup: proper module loading
//  cleanup: promises to clear up some of the async chaining
//  feature: multiple chat partners

WebRtcDemo.App = (function (viewModel, connectionManager) {
	var viewModel = {};
	var _mediaStream,
		_hub,
		_connect = function (username, onSuccess, onFailure) {
			// Set Up SignalR Signaler
			var hub = $.connection.meetingHub;
			$.support.cors = true;
			$.connection.hub.url = '/signalr/hubs';
			_setupHubCallbacks(hub);
			$.connection.hub.start()
				.done(function () {
					console.log('connected to SignalR hub... connection id: ' + _hub.connection.id);

					// Tell the hub what our username is
					hub.server.join(VideoChatRoomId, _hub.connection.id);

					if (onSuccess) {
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

				// Load the stream into a video element so it starts playing in the UI
				console.log('playing my local video feed');
				//var videoElement = document.querySelector('.video.mine');

				var container = $("<div class='video-container mine streamid_" + viewModel.MyConnectionId + "'><video muted src='' height='116px' autoplay/></div>");
				$(".video-bar").prepend(container);
				var videoElement = $(".video-container.streamid_" + viewModel.MyConnectionId + " video")[0];
				attachMediaStream(videoElement, _mediaStream);

				// UI in calling mode
				viewModel.Mode = ('calling');

				// Hook up the UI
				//_attachUiHandlers();
				setTimeout(function () {
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
			connectionManager.initialize(hub.server, _callbacks.onReadyForStream, _callbacks.onStreamAdded, _callbacks.onStreamRemoved);
			_attachUiHandlers();
		}, function (event) {
			console.error(event);
			// viewModel.Loading(false);
		});
	},

	_attachUiHandlers = function () {
		// Add click handler to users in the "Users" pane
		$('body').on('click', '.start-video', function () {
			_tryGetMedia(true, true, function () {
				$(".start-conference").addClass("hidden");

			}, function () {
				showAlert("Failed to connect to hardware. Make sure camera and audio are enabled in your browser.");
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



		var connected = false;

		$('body').on('click', '.uncollapser .clicker', function () {
			if (!connected) {
				console.log("calling in");
				_hub.server.callMeeting(VideoChatRoomId, true, true);
				connected = true;
			}
			$(".video-bar").toggleClass("shifted");
			$(this).parent().toggleClass("shifted");
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
			hub.server.answerCall(true, callingUser);
			viewModel.Mode = ('incall');
			/*if (_mediaStream) { 
				connectionManager.initiateOffer(callingUser, _mediaStream);
				viewModel.Mode = ('incall');
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

		// Hub Callback: Call Accepted
		hub.client.callAccepted = function (acceptingUser) {
			console.log('call accepted from: ' + JSON.stringify(acceptingUser) + '.  Initiating WebRTC call and offering my stream up...');

			// Callee accepted our call, let's send them an offer with our video stream
			if (_mediaStream) {
				connectionManager.initiateOffer(acceptingUser.ConnectionId, _mediaStream);
				viewModel.Mode = ('incall');
			} else {
				console.log("Error: _mediaStream empty");
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
		};
	},

	// Connection Manager Callbacks
	_callbacks = {
		onReadyForStream: function (connection) {
			// The connection manager needs our stream
			// todo: not sure I like this
			if (_mediaStream) {
				connection.addStream(_mediaStream);
			}
		},
		onStreamAdded: function (connection, event, streamId) {
			console.log('binding remote stream to the partner window');
			// Bind the remote stream to the partner window
			//var otherVideo = document.querySelector('.video.partner');
			var container = $("<div class='video-container streamid_" + streamId + "'><video src='' height='116px' autoplay/></div>");
			$(".video-bar").append(container);

			var otherVideo = $(".video-container.streamid_" + streamId + " video")[0];
			setTimeout(function () {
				attachMediaStream(otherVideo, event.stream); // from adapter.js
			}, 1000);
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