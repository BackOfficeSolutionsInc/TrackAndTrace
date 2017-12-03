try{
	if (window.location.href.search("beta.com") > 0) {

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

}catch(e){
	console.error(e);
}