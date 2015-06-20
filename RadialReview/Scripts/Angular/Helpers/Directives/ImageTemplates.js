angular.module('imageTemplates', []).directive("profileImage", function () {
	


	return {
		restrict: "E",
		scope: {
			user: "="
		},
		link: function(scope, element, attrs) {
			var hash = 0, i, chr, len;
			var str = scope.user.Name;
			if (str.length != 0) {
				for (i = 0, len = str.length; i < len; i++) {
					chr = str.charCodeAt(i);
					hash = ((hash << 5) - hash) + chr;
					hash |= 0; // Convert to 32bit integer
				}
			}
			hash = hash % 360;
			scope.colorCode = hash;
		},
		template: "<span class='picture-container' title='{{user.Name}}'>" +
			"<span ng-if='user.ImageUrl!=\"/i/userplaceholder\" && user.ImageUrl!=null && user.ImageUrl!=\"\"' class='picture' style='background: url({{user.ImageUrl}}) no-repeat center center;'></span>" +
			"<span ng-if='user.ImageUrl==\"/i/userplaceholder\"' class='picture' style='background-color:hsla({{colorCode}}, 36%, 49%, 1);color:hsla({{colorCode}}, 36%, 72%, 1)'>{{user.Initials}}</span>" +
			"<span ng-if='user.ImageUrl==null || user.ImageUrl==\"\"' class='picture' style='color:#ccc'>n/a</span>" +
			"</span>"
	};
});