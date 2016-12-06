angular.module('imageTemplates', []).directive("profileImage", function () {
    return {
        restrict: "E",
        scope: {
            user: "="
        },
        link: function(scope, element, attrs) {
        	var hash = 0, i, chr, len;        	
            if (scope.user && scope.user.Name) {
                var str = scope.user.Name;
                if (str.length != 0) {
                    for (i = 0, len = str.length; i < len; i++) {
                        chr = str.charCodeAt(i);
                        hash = ((hash << 5) - hash) + chr;
                        hash |= 0; // Convert to 32bit integer
                    }
                }
                hash = hash % 360;
                scope.user.colorCode = hash;

                if (typeof(scope.user.Initials)==="undefined") {
                	scope.user.Initials=getInitials(scope.user.Name).toUpperCase();
                }
            }
        },
        template: "<span class='picture-container' title='{{user.Name}}'>" +
			"<span ng-if='user.ImageUrl!=\"/i/userplaceholder\" && user.ImageUrl!=null && user.ImageUrl!=\"\"' class='picture' style='background: url({{user.ImageUrl}}) no-repeat center center; background-color:hsla({{::user.colorCode}}, 36%, 49%, 1);color:hsla({{::user.colorCode}}, 36%, 72%, 1)'></span>" +
			"<span ng-if='user.ImageUrl==\"/i/userplaceholder\"' class='picture keep-background' style='background-color:hsla({{::user.colorCode}}, 36%, 49%, 1);color:hsla({{::user.colorCode}}, 36%, 72%, 1)'><span class='initials'>{{user.Initials}}</span></span>" +
			"<span ng-if='user.ImageUrl==null || user.ImageUrl==\"\"' class='picture keep-background' style='color:#ccc'>n/a</span>" +
			"</span>"
    };
});
