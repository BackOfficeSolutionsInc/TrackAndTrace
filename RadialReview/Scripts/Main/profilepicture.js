
function getInitials(name, initials) {
	if (typeof (name) === "undefined" || name == null) {
		name = "";
	}
	if (typeof (initials) === "undefined") {
		var m = name.match(/\b\w/g) || [];
		var arr = [];
		if (m.length > 0)
			arr.push(m[0]);
		if (m.length > 1)
			arr.push(m[1]);
		initials = arr.join(' ');
	}
	return initials;
}

function profilePicture(url, name, initials) {
	var picture = "";
	var hash = 0;
	if (typeof (name) !== "string") {
		name = "";
	}
	if (name.length != 0) {
		for (var i = 0; i < name.length; i++) {
			{
				var chr = name.charCodeAt(i);
				hash = ((hash << 5) - hash) + chr;
				hash |= 0; // Convert to 32bit integer
			}
		}
		//console.log(name + ": " + hash + " = " + Math.abs(hash) % 360);
		hash = Math.abs(hash) % 360;
	}

	if (url !== "/i/userplaceholder" && url !== null) {
		picture = "<span class='picture' style='background: url(" + url + ") no-repeat center center;'></span>";
	} else {
		if (name == "")
			name = "n/a";

		initials = getInitials(name, initials).toUpperCase();
		picture = "<span class='picture' style='background-color:hsla(" + hash + ", 36%, 49%, 1);color:hsla(" + hash + ", 36%, 72%, 1)'><span class='initials'>" + initials + "</span></span>";
	}

	return "<span class='profile-picture'>" +
		      "<span class='picture-container' title='" + escapeString(name) + "'>" +
					picture +
			  "</span>" +
		   "</span>";
}
