
var uploader = UploadSteps({
	defaultData: { recurrenceId: window.recurrenceId, type: "Users", csv: false },
    uploadFileUrl: "/Upload/UploadRecurrenceFile",
    uploadSelectionUrl: "/Upload/ProcessUserSelection",
    confirmSelectionUrl: "/Upload/SubmitUsers",
    afterUpload: function (d) {
        if (d.Data.FileType == "CSV") {
            uploader.addSelectionStep("Select first name (Do not select header)", validateFName);
            uploader.addSelectionStep("Select last name (Do not select header)", validateLName);
            uploader.addSelectionStep("Select e-mail (Do not select header)", validateEmail);
            //optional
            uploader.addSelectionStep("Select position (Do not select header)", validatePosition, true);
            uploader.addSelectionStep("Select manager first name (Do not select header)", validateMFName, true);
            uploader.addSelectionStep("Select manager last name (Do not select header)", validateMLName, true);
        }
        //} else {
        //    uploader.addSelectionStep("Select Name (Do not select header)", validateName);
        //}
    }
});

function validateName(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    issuesRect = rect;
    uploader.addSelectionData("names", issuesRect);
    return allTrue;
}

function validateFName(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    issuesRect = rect;
    uploader.addSelectionData("fnames", issuesRect);
    return allTrue;
}

function validateLName(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    userRect = rect;
    uploader.addSelectionData("lnames", userRect);
    return allTrue;
}
function validateEmail(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    userRect = rect;
    uploader.addSelectionData("emails", userRect);
    return allTrue;
}

function validatePosition(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    detailsRect = rect;
    uploader.addSelectionData("positions", detailsRect);
    return allTrue;
}
function validateMFName(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    mfnameRect = rect;
    uploader.addSelectionData("mfnames", mfnameRect);
    return allTrue;
}
function validateMLName(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    mlnameRect = rect;
    uploader.addSelectionData("mlnames", mlnameRect);
    return allTrue;
}