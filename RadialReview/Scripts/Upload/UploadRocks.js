
var uploader = UploadSteps({
	defaultData: { recurrenceId: window.recurrenceId, type: "Rocks", csv: false },
    uploadFileUrl: "/Upload/UploadRecurrenceFile",
    uploadSelectionUrl: "/Upload/ProcessRocksSelection",
    confirmSelectionUrl: "/Upload/SubmitRocks",
    afterUpload: function (d) {
        uploader.addSelectionStep("Select rocks (Do not select header)", validateTodo);

        if (d.Data.FileType == "CSV") {
            uploader.addSelectionStep("Select owners (Do not select header)", validateUsers, true);
            uploader.addSelectionStep("Select due date (Do not select header)", validateDate, true);
            uploader.addSelectionStep("Select details (Do not select header)", validateDetails, true);
        }
    }
});

function validateTodo(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    rocksRect = rect;
    uploader.addSelectionData("rocks", rocksRect);
    return allTrue;
}

function validateUsers(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(rocksRect, rect);

    userRect = rect;
    uploader.addSelectionData("users", userRect);
    return allTrue;
}

function validateDetails(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(rocksRect, rect);

    detailsRect = rect;
    uploader.addSelectionData("details", detailsRect);
    return allTrue;
}
function validateDate(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(rocksRect, rect);

    duedateRect = rect;
    uploader.addSelectionData("duedate", duedateRect);
    return allTrue;
}