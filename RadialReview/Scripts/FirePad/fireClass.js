class FirePadClass {
    
    constructor(padID, firepad_container, readOnly) {
        
        this.padID = padID.value;
        this.firepad_container = firepad_container;
        this.readOnly = readOnly.value;
    }
    
    init() {
        var config = {
            apiKey: "AIzaSyCpfwGkfGnPJdfFvtuirwemLk0ZfNKKDb4",
            authDomain: "tractiontools-72dc6.firebaseapp.com",
            databaseURL: "https://tractiontools-72dc6.firebaseio.com",
            projectId: "tractiontools-72dc6",
            storageBucket: "tractiontools-72dc6.appspot.com",
            messagingSenderId: "194786005639"
        };
        firebase.initializeApp(config);

        //// Get Firebase Database reference.
        var firepadRef;
        
        var initialText = '';
        var padId = this.padID;
        firepadRef = firebase.database().ref(padId);

        firepadRef.on("value", function (snapshot) {
            var firepadSnapshot = snapshot.val();
            initialText = firepadSnapshot.initialText;
        }, function (error) {
            console.log("Error: " + error.code);
        });

        //// Create CodeMirror (with lineWrapping on).
        var codeMirror = CodeMirror(this.firepad_container, { lineWrapping: true });
        
        //// Create Firepad (with rich text toolbar and shortcuts enabled).
        var firepad = Firepad.fromCodeMirror(firepadRef, codeMirror,
            {
                richTextToolbar: true,
                richTextShortcuts: true

            });

        //// Initialize contents.
        if (initialText == 'undefined') {
            initialText = '';
        }
        if (this.readOnly=='True') {
            $('#firepadTextArea').attr('disabled', 'disabled');
        }
        
        firepad.on('ready', function () {
            console.log(firepad.getHtml());
            if (firepad.isHistoryEmpty()) {
                firepad.setHtml(initialText);
            }
            
        });
        firepad.on('synced', function () {
            firebase.database().ref(padId + '/note').set({
                html:firepad.getHtml(),
                text:firepad.getText()
            });

        });

        
        
    }
}