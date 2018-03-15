try {
	window.Config.ServerType = "production";
	if (window.location.href.search("beta.com") > 0) {
		window.Config.ServerType = "beta";
		document.body.className += ' ' + 'beta';
		//if (typeof (Storage) !== "undefined") {
		//	if (typeof(localStorage.getItem("AppVersions"))==="undefined"){
		//		localStorage.setItem()
		//	}
		//	localStorage.setItem("lastname",);
		//} else {
		//	// Sorry! No Web Storage support..
		//}
	}

	if (window.location.href.search("alpha.com") > 0) {
		window.Config.ServerType = "alpha";
	}
	if (window.location.href.search("localhost:") > 0) {
		window.Config.ServerType = "developer";
	}

}catch(e){
	console.error(e);
}