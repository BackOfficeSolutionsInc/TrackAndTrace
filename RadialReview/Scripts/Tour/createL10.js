function genWizard(isSamePage) {
	return function () {
		setTimeout(function () {
			$("[href='#Basics']").click();
		}, 200);

		var meetingName = "Level 10";
		var shortName = "L10";
		if (isSamePage) {
			meetingName = "Same Page Meeting"
			shortName = "Same Page Meeting"
		}




		var pages = [{
			target: '#alerts',
			content: "<h2>This is the " + meetingName + " wizard!</h2> Use this screen to build your " + shortName + " meeting. <br>" +
				" <br><i>Note:You only need to create one meeting per team.</i>",
			className: 'anno-center anno-width-400',
			//position: { bottom: "-60px" },
			arrowPosition: "none",
			onShow: function (a) {
				$(".anno-overlay").css({ opacity: .35 });
			},
			onHide: function (a) {
				$(".anno-overlay").animate({ opacity: 1 });
			}
			// buttons: [AnnoButton.BackButton,AnnoButton.NextButton]
			//}, {
			//    target: '#alerts',
			//    content: "",
			//    className: 'anno-center anno-width-400 ',
			//    arrowPosition: "none",
			//    //position: { bottom: "-80px" },
			//    //buttons: []
		}, {
			target: '#l10-wizard-name',
			content: "<h2>Let's give it a name!</h2>Set your meetings name here. <br> <br><i>Ex: Leadership Team, Sales Team, Ops Team...</i>",
			className: 'anno-width-400 ',
			//position: { bottom: "-80px" },
			buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		}];
		if (!isSamePage){
			pages.push({
				target: '#l10-wizard-teamtype',
				content: "<h2>What kind of team is this?</h2>Select the team type from this drop-down.",
				className: 'anno-width-400 ',
				//position: { bottom: "-80px" },
				buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
			});
		}

		pages.push({
			target: '#alerts',
			content: "<h2>Where's the save button?</h2>There isn't one. Any changes you make to your meeting are automatically saved.",
			className: 'anno-center anno-width-400',
			//position: { bottom: "-60px" },
			arrowPosition: "none",
			buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		});

		var p = {
			target: '#l10-wizard-attendees-btn',
			content: "<h2>Let's add some attendees.</h2> Click Attendees.",
			className: 'anno-width-400 ',
			//position: { bottom: "-80px" },
			buttons: [AnnoButton.BackButton, {
				text: 'Next',
				className: 'anno-btn',
				click: function (anno, evt) {
					$("[href='#Attendees']").click();
					anno.switchToChainNext();
				}
			}]
		};
		Tours.clickToAdvance(p);
		pages.push(p);

		pages.push({
			target: '#alerts',
			content: "<h2>Who's in your meeting?</h2>Your attendees will show up here.",
			className: 'anno-center anno-width-400',
			//position: { bottom: "-60px" },
			arrowPosition: "none",
			//arrowPosition: "none",
			//position: { bottom: "-80px" },  
			onShow: function (a) {
				$(".anno-overlay").css({ opacity: .35 });
			},
			onHide: function (a) {
				$(".anno-overlay").animate({ opacity: 1 });
			},
			buttons: [{
				text: 'Back',
				className: 'anno-btn-low-importance',
				click: function (anno, evt) {
					$("[href='#Basic']").click();
					anno.switchToChainPrev();
				}
			}, AnnoButton.NextButton]
		});
		p = {
			target: '.create-row',
			content: "Click this button to add users.",
			className: 'anno-width-400',
			position: "center-left",
			onShow: function (a) {
				a.showOverlay();
			},
			// arrowPosition: "none",
			//arrowPosition: "none",
			//position: { bottom: "-80px" },
			buttons: [AnnoButton.BackButton, {
				text: 'Next',
				className: 'anno-btn',
				//click: function (anno, evt) {
				//    $("#l10-wizard-attendees .create-row").click();
				//    anno.switchToChainNext();
				//}
			}, {
				text: "",
				className: "right-anno-overlay",
				click: function (anno, evt) {
					anno.switchToChainNext();
				}
			}]
		};
		Tours.clickToAdvance(p);
		pages.push(p);

		//p = {
		//    target: '.livesearch-container',
		//    content: "Have existing users you want to add to this meeting? You can search for them here.",
		//    className: 'anno-width-400',
		//    buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		//};
		//pages.push(p);
		//p = {
		//    target: '.create-user',
		//    content: "Use this button to create a new user and add them to your meeting.",
		//    className: 'anno-width-400',
		//    buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		//};
		//pages.push(p);
		//p = {
		//    target: '.upload-user',
		//    content: "Use this button to upload a list of users to your meeting. An account is created for newly added users.",
		//    className: 'anno-width-400',
		//    buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		//};
		//pages.push(p);

		pages.push({
			target: '#alerts',
			content: "<h2>Three ways to add users</h2><ol style='padding-left: 20px;'><li>You can use the search function to add existing users.</li><li>You can create a new user and add to the meeting.</li><li>You can upload a list of users.</li>",
			className: 'anno-center anno-width-400',
			//position: { bottom: "-60px" },
			arrowPosition: "none",
			onShow: function (a) {
				a.hideOverlay();
				if (!$(".new-user-container").is("isSearching")) {
					$(".new-user-container .create-row").click();
				}
			},
			//arrowPosition: "none",
			//position: { bottom: "-80px" },
			buttons: [{
				text: 'Back',
				className: 'anno-btn-low-importance',
				click: function (anno, evt) {
					$('.anno-btn-low-importance').remove();
					anno.hide();

					setTimeout(function () {
						//debugger;
						//$("#l10-wizard-attendees .create-row").click();
						anno.switchToChainPrev();
					}, 600);
				}
			}, AnnoButton.NextButton]
		});

		var lastMessage = "<h2>Try it out for yourself!</h2>Use the menu to edit your attendees, scorecard measurables, rocks, to-dos and issues.";
		if (isSamePage) {
			lastMessage = "<h2>Try it out for yourself!</h2>Use the menu to edit your attendees and issues."
		}

		pages.push({
			target: '#l10-wizard-menu',
			content: lastMessage,
			className: 'anno-width-400 ',
			onShow: function (a) {
				a.showOverlay();
			},
			//arrowPosition: "none",
			//position: { bottom: "-80px" },
			buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		});

		pages.push({
			target: '#header-tab-l10',
			content: "When you're done, you can start the meeting from the L10 tab.",
			className: 'anno-width-300',
			position: { left: "-15px" },
			arrowPosition: "top",
			buttons: [AnnoButton.BackButton, AnnoButton.DoneButton]
		});
		//pages.push({
		//    target: '#header-tab-l10',
		//    content: "When you're done, you can start the meeting from the L10 tab.",
		//    className: 'anno-width-300',
		//    position: { left: "-15px" },
		//    arrowPosition: "top",
		//    buttons: [AnnoButton.BackButton, AnnoButton.NextButton]
		//});

		var a = new Anno(pages);
		//Tours.waitForTarget(a);
		a.show();
	}

}


Tours.createL10 = {
	start: function () {
		var a = new Anno({
			target: '#header-tab-l10',
			content: "Click the Level 10 tab.",
			className: 'anno-width-300',
			position: { left: "-15px" },
			arrowPosition: "top",
			buttons: [],
			onShow: function (a) {
				$(".navbar-collapse").addClass("in");
			},
		});
		Tours.appendParams(a, '#header-tab-l10', "createL10", "l10");
		a.show();
	},
	l10: function () {


		var any = "Once created, your Level 10 meetings will show up here.";
		if ($(".l10-row").length == 1)
			any = "Your Level 10 meetings show up here. You've already got one L10 meeting.";
		else if ($(".l10-row").length > 1)
			any = "Your Level 10 meetings show up here. You've already got a few L10 meetings.";

		var anno2 = {
			target: '#l10-create-meeting',
			content: "Click this button to create a new Level 10 meeting",
			className: 'anno-width-300',
			buttons: [],
			onHide: function (a) {
				//a.hideOverlay();
			},
		};
		;//, '#l10-create-meeting', "createL10", "wizard");

		var anno3 = {
			//target: '#l10-create-new-meeting',
			target: '#l10-create-meeting-dropdown',
			content: "Click Create a Level 10 or a Same Page Meeting",
			className: 'anno-width-300',
			position: "left",
			buttons: [],
			//position: { bottom: "-60px" },
			//arrowPosition: "none",
			onShow: function (a) {
				//setTimeout(function () {
				//	debugger;
				//	a.showOverlay();
				//}, 500);
			}
		};
		Tours.appendParams(anno3, '#l10-create-new-meeting', "createL10", "wizard");
		Tours.appendParams(anno3, '#samepage-create-new-meeting', "createL10", "wizardSamePage");

		var a = new Anno([{
			target: '#l10-meeting-list',
			content: any,
			className: 'anno-width-300',
			position: { left: "190px", top: "50px" },
			arrowPosition: "top"
		}, Tours.clickToAdvance(anno2), anno3]);
		a.show();
	},
	wizard: genWizard(false),
	wizardSamePage: genWizard(true)
};