angular.module('rockstate',[]).directive("rockstate", function() {
  return {
    restrict: "E",
	scope: {
		rock:"=rock"
	},
	require: 'ngModel',
    template:	"<div class=\"rockstate rockstate-thin editor\">"+
					"<div class=\"rockstate-contents\">"+
						"<input type=\"hidden\" class=\"changeable\" value=\"{{rock.State}}\" name=\"{{rock.Key}}\" \/>"+
						"<span class=\"rockstate-val rockstate-AtRisk\" data-value=\"AtRisk\"><span class=\"center\">OFF TRACK<\/span><\/span>"+
						"<span class=\"rockstate-val rockstate-OnTrack\" data-value=\"OnTrack\"> <span class=\"center\">ON TRACK<\/span><\/span>"+
						"<span class=\"rockstate-val rockstate-Complete\" data-value=\"Complete\"><span class=\"center\">COMPLETE<\/span><\/span>"+
						"<div class=\"fill cursor\"><\/div>"+
						"<div class=\"fill hover\"><\/div>"+
					"<\/div>"+
				"<\/div>"

  };
});