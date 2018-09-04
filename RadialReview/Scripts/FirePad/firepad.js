class FireClass {
    
    constructor(text, padID, firepad_container) {
        this.text=text.value;
        this.padID = padID.value;
        this.firepad_container = firepad_container;
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
        
        var text = this.text;
        firepadRef = firebase.database().ref(this.padID);

        //// Create CodeMirror (with lineWrapping on).
        var codeMirror = CodeMirror(this.firepad_container, { lineWrapping: true });
        
        //// Create Firepad (with rich text toolbar and shortcuts enabled).
        var firepad = Firepad.fromCodeMirror(firepadRef, codeMirror,
            { richTextToolbar: true, richTextShortcuts: true });

        //// Initialize contents.
        firepad.on('ready', function () {
            
            if (firepad.isHistoryEmpty()) {
                   firepad.setHtml(text + '<br/><br/><br/><br/>');                
            }
        });
    }    
}