class FireClass {
    
    constructor(firepadRef, text, padID, firepad_container) {
        this.firepadRef=firepadRef.value;
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
        if (this.firepadRef.trim() == '' || this.firepadRef == 'undefined') {
            firepadRef = getFireBaseRef();
        } else {
            firepadRef = firebase.database().ref(this.firepadRef);

        }

        //// Create CodeMirror (with lineWrapping on).
        var codeMirror = CodeMirror(this.firepad_container, { lineWrapping: true });

        //// Create Firepad (with rich text toolbar and shortcuts enabled).
        var firepad = Firepad.fromCodeMirror(firepadRef, codeMirror,
            { richTextToolbar: true, richTextShortcuts: true });

        //// Initialize contents.
        firepad.on('ready', function () {

            if (firepad.isHistoryEmpty()) {
                if (this.firepadRef == ' ') {
                    firepad.setHtml(this.text + '<br/><br/><br/><br/>');
                    updateFirepad(firepadRef, this.text, this.padID);
                }
            }
        });



    }

    // Helper to get hash from end of URL or generate a random one.
    getFireBaseRef() {
        var ref = firebase.database().ref();
        var hash = window.location.hash.replace(/#/g, '');

        if (hash) {
            ref = ref.child(hash);
        } else {
            ref = ref.push(); // generate unique location.
            window.location = window.location + '#' + ref.key; // add it as a hash to the URL.
        }
        if (typeof console !== 'undefined') {
            console.log('Firebase data: ', ref.toString());
        }
        return ref;
    }
    updateFirepad(firepadRef, text, padID) {

        firebase.database().ref('FirePad/' + padID).child('firepadRef').set(firepadRef.key);
    }
}