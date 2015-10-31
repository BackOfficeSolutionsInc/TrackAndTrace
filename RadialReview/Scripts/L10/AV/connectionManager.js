﻿var WebRtcDemo = WebRtcDemo || {};

/************************************************
ConnectionManager.js

Implements WebRTC connectivity for sharing video
streams, and surfaces functionality to the rest
of the app.

WebRTC API has been normalized using 'adapter.js'

************************************************/
var DefaultIceServers= [{ url: 'stun:74.125.142.127:19302' }];

var previous_offers = [];
var ConnectionEstablished = {};
WebRtcDemo.ConnectionManager = (function () {

	var _signaler,
        _connections = {},
       // _iceServers =  ICE_SERVERS || DefaultIceServers, // stun.l.google.com - Firefox does not support DNS names.

	     onicecandidateQueue = {},
	     onicecandidateAllowed = [],
        /* Callbacks */
        _onReadyForStreamCallback = function () { console.log('UNIMPLEMENTED: _onReadyForStreamCallback'); },
        _onStreamAddedCallback = function () { console.log('UNIMPLEMENTED: _onStreamAddedCallback'); },
        _onStreamRemovedCallback = function () { console.log('UNIMPLEMENTED: _onStreamRemovedCallback'); },

        // Initialize the ConnectionManager with a signaler and callbacks to handle events
        _initialize = function (signaler, onReadyForStream, onStreamAdded, onStreamRemoved) {
        	_signaler = signaler;

        	_onReadyForStreamCallback = onReadyForStream || _onReadyForStreamCallback;
        	_onStreamAddedCallback = onStreamAdded || _onStreamAddedCallback;
        	_onStreamRemovedCallback = onStreamRemoved || _onStreamRemovedCallback;
        },
		my_onicecandiate = function (connection,candidate,partnerClientId) {

			console.log("my_onicecandiate");
			console.log(candidate);
			console.log(partnerClientId);

			var rtcIC = new RTCIceCandidate(candidate);

        	_getConnection(partnerClientId).addIceCandidate(rtcIC);
		},
        // Create a new WebRTC Peer Connection with the given partner
        _createConnection = function (partnerClientId) {
        	console.log('WebRTC: creating connection...');
			
        	// Create a new PeerConnection
        	var connection = new RTCPeerConnection({ iceServers: ICE_SERVERS });
			/*var dataChannel = connection.createDataChannel("myLabel", {
				ordered: false, // do not guarantee order
				maxRetransmitTime: 3000, // in milliseconds
			});
			dataChannel.onerror = function (error) {console.log("Data Channel Error:", error);};
			dataChannel.onmessage = function (event) {console.log("Got Data Channel Message:", event.data);};
			dataChannel.onopen = function () {dataChannel.send("Hello World!");};
			dataChannel.onclose = function () {console.log("The Data Channel is Closed");};


	        connection.dataChannel = dataChannel;*/

	        ConnectionEstablished[partnerClientId] = 0;

        	// ICE Candidate Callback
        	connection.onicecandidate = function (event) {
        		if (event.candidate) {
        			// Found a new candidate
        			console.log('WebRTC: new ICE candidate');
        			_signaler.sendSignal(JSON.stringify({ "candidate": event.candidate }), partnerClientId);
        		} else {
        			// Null candidate means we are done collecting candidates.
        			console.log('WebRTC: ICE candidate gathering complete');
			        ConnectionEstablished[partnerClientId] += 1;
        		}
        	};

	        connection.ongatheringchange = function(event) {
			   console.log("ongatheringchange");
		        if (event.currentTarget.iceGatheringState === "complete")
			        console.log("End of candidates");
	        };

        	// State changing
        	connection.onstatechange = function () {
        		// Not doing anything here, but interesting to see the state transitions
        		var states = {
        			'iceConnectionState': connection.iceConnectionState,
        			'iceGatheringState': connection.iceGatheringState,
        			'readyState': connection.readyState,
        			'signalingState': connection.signalingState
        		};

        		console.log(JSON.stringify(states));
        	};

        	// Stream handlers
        	connection.onaddstream = function (event) {
        		console.log('WebRTC: adding stream');
        		// A stream was added, so surface it up to our UI via callback
        		_onStreamAddedCallback(connection, event, partnerClientId);
        	};

        	connection.onremovestream = function (event) {
        		console.log('WebRTC: removing stream');
        		// A stream was removed
        		_onStreamRemovedCallback(connection, event.stream.id);
        	};

        	// Store away the connection
        	_connections[partnerClientId] = connection;

        	// And return it
        	return connection;
        },

        // Process a newly received SDP signal
        _receivedSdpSignal = function (connection, partnerClientId, sdp) {
        	console.log('WebRTC: processing sdp signal');
        	connection.setRemoteDescription(new RTCSessionDescription(sdp), function () {
        		if (connection.remoteDescription.type == "offer") {
        			console.log('WebRTC: received offer, sending response...');
        			_onReadyForStreamCallback(connection);
        			connection.createAnswer(function (desc) {
        				connection.setLocalDescription(desc, function () {
        					_signaler.sendSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);


						    onicecandidateAllowed.push(partnerClientId);
							
							//ConnectionEstablished[partnerClientId] = true;
        					if (partnerClientId in onicecandidateQueue) {
        						for (var cc in onicecandidateQueue[partnerClientId]) {
							        //debugger;
							        var candidate = onicecandidateQueue[partnerClientId][cc];
							        console.log("A");
									my_onicecandiate(connection, candidate, partnerClientId);
        						}
        						//Clear out queue
        						onicecandidateQueue[partnerClientId] = [];
					        }


        				});
        			},
                    function (error) { console.log('Error creating session description: ' + error); });
        		} else if (connection.remoteDescription.type == "answer") {
        			console.log('WebRTC: received answer');
					//If we havent initiated an offer, do so now.
			        //_initiateOffer(partnerClientId);

		        }
        	});
        },

        // Hand off a new signal from the signaler to the connection
        _newSignal = function (partnerClientId, data) {
        	var signal = JSON.parse(data),
                connection = _getConnection(partnerClientId);

        	console.log('WebRTC: received signal');

        	// Route signal based on type
        	if (signal.sdp) {
        		_receivedSdpSignal(connection, partnerClientId, signal.sdp);
        	} else if (signal.candidate) {
        		_receivedCandidateSignal(connection, partnerClientId, signal.candidate);
        	}
        },

        // Process a newly received Candidate signal
        _receivedCandidateSignal = function (connection, partnerClientId, candidate) {
        	try {
        		console.log('WebRTC: processing candidate signal');
				var index = onicecandidateAllowed.indexOf(partnerClientId);
        		if (index > -1) { 
					console.log("Candidate Signal > -1");
					console.log("B");
        			my_onicecandiate(connection,candidate, partnerClientId);
        		} else {
			        if (!(partnerClientId in onicecandidateQueue))
				        onicecandidateQueue[partnerClientId] = [];
			        onicecandidateQueue[partnerClientId].push(candidate);
		        }
        	} catch (e) {
        		console.error(e);
        	}
        },

        // Retreive an existing or new connection for a given partner
        _getConnection = function (partnerClientId) {
	        console.log("Get Connection:" + partnerClientId);
        	var connection = _connections[partnerClientId] || _createConnection(partnerClientId);
	        console.log(connection);
        	return connection;
        },

        // Close all of our connections
        _closeAllConnections = function () {
        	for (var connectionId in _connections) {
        		_closeConnection(connectionId);
        	}
        },

        // Close the connection between myself and the given partner
        _closeConnection = function (partnerClientId) {
        	var connection = _connections[partnerClientId];

        	if (connection) {
        		// Let the user know which streams are leaving
        		// todo: foreach connection.remoteStreams -> onStreamRemoved(stream.id)
        		_onStreamRemovedCallback(null, partnerClientId);

        		// Close the connection
        		connection.close();
        		delete _connections[partnerClientId]; // Remove the property
        	}
        },

        // Send an offer for audio/video
        _initiateOffer = function (partnerClientId, stream) {
        	// Get a connection for the given partner
        	var connection = _getConnection(partnerClientId);

	        if (previous_offers.indexOf(partnerClientId) == -1) {

		        // Add our audio/video stream
		        if (stream) {
			        connection.addStream(stream);
			        console.log('stream added on my end');
					previous_offers.push(partnerClientId);
		        } else {
			        console.log('stream NOT added on my end: stream was null');
		        }

		        // Send an offer for a connection
		        connection.createOffer(function(desc) {
			        connection.setLocalDescription(desc, function() {
				        _signaler.sendSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);
			        });
		        }, function(error) { console.log('Error creating session description: ' + error); });
		        
	        } else {
		        console.log("already offered to "+partnerClientId);
	        }
        };

	// Return our exposed API
	return {
		initialize: _initialize,
		newSignal: _newSignal,
		closeConnection: _closeConnection,
		closeAllConnections: _closeAllConnections,
		initiateOffer: _initiateOffer
	};
})();