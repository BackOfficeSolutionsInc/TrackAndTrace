Tours.startL10 = {
    start: function () {
        var a = new Anno({
            target: '#header-tab-l10',
            content: "Click the Level 10 tab.",
            className: 'anno-width-300',
            position: { left: "-15px" },
            arrowPosition: "top",
            buttons: []
        });
        Tours.appendParams(a, '#header-tab-l10', "startL10", "l10");
        a.show();
    },
    l10: function () {
        if ($(".l10-row").length == 0) {

            var a = new Anno([{
                target: '#l10-meeting-list',
                content: "Looks like you don't have any L10 meetings. You'll need to create one first.",
                className: 'anno-width-300',
                position: { left: "190px", top: "50px" },
                arrowPosition: "top",
                buttons: [AnnoButton.BackButton, {
                    text: 'Next',
                    className: 'anno-btn',
                    click: function (anno, evt) {
                        anno.hide();
                        setTimeout(function () {
                            startTour("createL10", "l10");
                        }, 400);
                    }
                }]
            }, anno2]);
            a.show();
        } else {


            var any = "";
            if ($(".l10-row").length == 1)
                any = "Your Level 10 meetings show up here. Click the name of your L10 to get started.";
            else if ($(".l10-row").length > 1)
                any = "Your Level 10 meetings show up here. Click the name of an L10 to get started.";

            var p = {
                target: '#l10-meeting-list',
                content: any,
                className: 'anno-width-300',
                position: { left: "190px", top: "-65px" },
                arrowPosition: "bottom",
                buttons: []
            };

            Tours.appendParams(p, '#l10-meeting-list td', "startL10", "meeting");

            var a = new Anno(p);
            a.show();
        }
    },
    meeting: function () {
        if (meetingStart) {
            this.concludeMeeting();

        } else {
            this.startMeeting();
        }

    },

    concludeMeeting: function () {
        var pages = [{
            target: '#alerts',
            content: "<h2>Woops!</h2>Looks like this meeting was already started. We'll need to conclude the meeting first. ",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            //position: { bottom: "-80px" },
            buttons: [AnnoButton.BackButton, {
                text: 'Conclude Meeting',
                className: 'anno-btn',
                click: function (anno, evt) {
                    var aa = anno;
                    $.ajax({
                    	url: "/l10/ForceConclude/" + window.recurrenceId,
                        method: "POST",
                        success: function () {
                            location.reload();
                        },
                        error: function () {
                            showAlert("Could not conclude meeting.");
                        }
                    });
                }
            }]
        }];
        //pages.push(p);
        var a = new Anno(pages);
        a.show();
    },
    startMeeting: function () {
        var pages = [{
            target: '#alerts',
            content: "<h2>Welcome to the Level 10 meeting!</h2>Use this page to run your L10 meetings.",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            buttons: [{
                text: "Next",
                click: function (a, e) {
                    var anno = a;
                    e.preventDefault();
                    waitUntil(function () { return $(anno._chainNext.target).length > 0; }, function () {
                        setTimeout(function () {
                            anno.switchToChainNext();
                        }, 250);
                    }, function () {
                        showAlert("Could not load tour.");
                    });
                }
            }]
        }, {
            target: '#l10-meeting-startpage',
            content: "<h2>Before you begin...</h2> Make sure all your remote attendees are on this page before starting. If an attendee can't make to the meeting, uncheck them.",
            className: 'anno-width-400',
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            //onShow: function (a) { a.showOverlay(); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        }, Tours.clickToAdvance({
            target: '.videoconference-container',
            content: "<h2>Video Conference Built-In</h2> Click the video conference tab to open video conferencing. Use the 'Join Conference' button to share audio and video.",
            className: 'anno-width-400',
            position: { bottom: "152px", right: "0px" },
            arrowPosition:"bottom-right",
            onShow: function (a) {
                if (!$(".videoconference-container").is(".shifted")) {
                    $(".videoconference-container .clicker").click();
                }
            },            
            //onShow: function (a) { $(".slider-container").addClass("static"); },
            //onHide: function (a) { $(".slider-container").removeClass("static"); },
            //onShow: function (a) { a.showOverlay(); },
            buttons: [AnnoButton.NextButton]
        }),{
            target: '.videoconference-container',
            content: "<h2>Video Conference Bar</h2> Once an attendee joins, their video will show up here.<br/><br/> <i>Your meeting could look like this:</i>",
            className: 'anno-width-400',
            position: { bottom: "152px", left: "0px" },
            arrowPosition: "bottom",
            onShow: function (a) {
                if (!$(".videoconference-container").is(".shifted")) {
                    $(".videoconference-container .clicker").click();
                }

                var vids = [
                    "https://s3.amazonaws.com/Radial/videoDemos/v1.mp4",
                    "https://s3.amazonaws.com/Radial/videoDemos/v2.mp4",
                    "https://s3.amazonaws.com/Radial/videoDemos/v3.mp4"]
                for (var i = 0; i < vids.length; i++) {
                    connection.onstream({
                        mediaElement: $('<video autoplay class="video-stream tour-stream noselect">' +
                            '<source src="'+vids[i]+'" type="video/mp4"></source></video>"'),
                    });
                }
            },
            onHide: function (a) {
                if ($(".videoconference-container").is(".shifted")) {
                    $(".videoconference-container .clicker").click();
                }
                $(".tour-stream").parent().remove();
            },
            //onShow: function (a) { $(".slider-container").addClass("static"); },
            //onHide: function (a) { $(".slider-container").removeClass("static"); },
            //onShow: function (a) { a.showOverlay(); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        },
        {
            target: '#alerts',
            content: "<h2>The meeting leader</h2>The meeting leader controls the meeting. When this person changes pages remote attendees follow their lead.",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        }];

        var p = {
            target: '#l10-meeting-startbutton',
            content: "<h2>Everyone ready?</h2>When you're ready to begin, have the meeting leader click this button to advance to the Segue. Give it a try.",
            className: 'anno-width-400 ',
            onShow: function (a) {setTimeout(function () { $(".slider-container").addClass("static"); }, 1)},
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton,Tours.NextButton]
        };
        Tours.clickToAdvance(p);
        pages.push(p);

        pages.push({
            target: '#alerts',
            content: "<h2>The Segue</h2>Spend a few minutes going around the room. Sharing your personal and professional good news for the last 7 days.",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            buttons: [AnnoButton.NextButton]
        });
        pages.push({
            target: '.elapsed-time',
            content: "<h2>Used to your meetings running long?</h2> Not any more. The ellapsed time-tracker helps keep you on-time.",
            className: 'anno-width-400 ',
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        });
        pages.push({
            target: '.page-segue .page-time',
            content: "<h2>Just enough time on each page</h2> The page timer counts down the minutes remaining on each page. When the timer turns <b style='color:#e23737'>red</b>, it's time to move on.",
            className: 'anno-width-400 ',
            position: { left: "193px", top: "121px" },
            arrowPosition:"left",
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        });
        pages.push({
            target: '.button-bar .issuesModal',
            content: "<h2>Have a new issue?</h2> Use the New Issue button to 'Drop-it-down' to your issues list for discussion later on.",
            className: 'anno-width-400 ',
            position: { top: "-27px", left: "-400px" },
            arrowPosition: "right",
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        });
        pages.push({
            target: '.button-bar .todoModal',
            content: "<h2>Want to add a to-do?</h2> Use the New To-do button to create an action item.",
            className: 'anno-width-400 ',
            position: { top: "22px", left: "-227px" },   
            arrowPosition: "top-right",
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
        });
        p={
            target: '.page-scorecard a',
            content: "<h2>Let's move on to the scorecard</h2> Click on Scorecard.",
            className: 'anno-width-400',
            position: {top: "91px",left: "193px"},
            onShow: function (a) { $(".slider-container").addClass("static"); },
            onHide: function (a) { $(".slider-container").removeClass("static"); },
            buttons: [AnnoButton.BackButton, Tours.NextButton]
        };
        Tours.clickToAdvance(p);
        pages.push(p);

        pages.push({
            target: '#alerts',
            content: "<h2>The Scorecard</h2>The scorecard contains the top 5-15 metrics most important to the success of your team.",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            buttons: [{
                text: "Next",
                click: function (a, e) {
                    waitUntil(function () { return $(".scorecard.meeting-page").length > 0; }, function () {

                        if ($("#ScorecardTable tbody tr").length == 0)
                            this.noScorecard();
                        else
                            this.scorecard()

                    }, function () {
                        showAlert("Could not load tour.");
                    });
                }
            }]
        });      

        var a = new Anno(pages);
        a.show();
    },
    noScorecard: function () {
        var pages = [{
            target: '#alerts',
            content: "<h2>Your scorecard looks a bit empty</h2> You can add measurables to your scorecard from the meeting wizard.",
            className: 'anno-center anno-width-400 ',
            arrowPosition: "none",
            buttons: [AnnoButtons.NextButton]
        }, {
            target: '.page-rocks a',
            content: "<h2>Next up is the Rock Review</h2> Click Rocks Review to view your quarterly rocks.",
            className: 'anno-center anno-width-400 ',
            position: "left",
            buttons: [AnnoButtons.BackButton, {
                text: "Next",
                click: function (a, e) {
                    this.rockReview();
                }
            }]
        }];
        var a = new Anno(pages);
        a.show();
    },
    scorecard: function () {
        var pages = [{
            target: '#ScorecardTable tbody tr td.current',
            content: "<h2>View your current week's measurables</h2> Use the scorecard to identify trends. Take 5 minutes to discuss your measurables capture any issues.",
            className: 'anno-center anno-width-400 ',
            position: "left",
            buttons: [AnnoButtons.NextButton]
        }, {
            target: '#ScorecardTable tbody tr td.buttonHolder .issuesModal',
            content: "<h2>Context-Aware Issues</h2> Have a measurable that is off-track? Context-Aware looks at the surroundings to automatically generate an issue." +
                " Click the Context-Aware Issues button to capture the issue quickly.",
            className: 'anno-center anno-width-400 ',
            position: "left",
            buttons: [AnnoButtons.BackButton, AnnoButtons.NextButton]
        }, {
            target: '#ScorecardTable tbody tr td.buttonHolder .todoModal',
            content: "<h2>Context-Aware To-dos</h2> Forgot to enter a measurable? Use Context-Aware To-do to intelligently generate an action item for the measurable owner.",
            className: 'anno-center anno-width-400 ',
            position: "left",
            buttons: [AnnoButtons.BackButton, AnnoButtons.NextButton]
        },Tours.clickToAdvance({
            target: '.page-rocks a',
            content: "<h2>Next up is the Rock Review</h2> Click Rocks Review to view your quarterly rocks.",
            className: 'anno-center anno-width-400 ',
            position: "left",
            action:function(){
                this.rockReview();
            },
            buttons: [AnnoButtons.BackButton,Tours.NextButton]
        })];
        var a = new Anno(pages);
        a.show();
    },
    rockReview: function(){
        alert("rock review");
    }



};