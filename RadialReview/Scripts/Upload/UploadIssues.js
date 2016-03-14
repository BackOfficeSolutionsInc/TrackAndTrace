﻿
var uploader = UploadSteps({
    defaultData: { recurrenceId: recurrenceId, type: "Issues", csv: false },
    uploadFileUrl: "/Upload/UploadRecurrenceFile",
    uploadSelectionUrl: "/Upload/ProcessIssuesSelection",
    confirmSelectionUrl: "/Upload/SubmitIssues",
    afterUpload: function (d) {
        uploader.addSelectionStep("Select issues (Do not select header)", validateIssue);

        if (d.Data.FileType == "CSV") {
            uploader.addSelectionStep("Select details (Do not select header)", validateDetails, true);
            uploader.addSelectionStep("Select owners (Do not select header)", validateUsers, true);
        }
    }
});

function validateIssue(rect) {
    var allTrue = true;
    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);

    issuesRect = rect;
    uploader.addSelectionData("issues", issuesRect);
    return allTrue;
}

function validateUsers(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    userRect = rect;
    uploader.addSelectionData("users", userRect);
    return allTrue;
}

function validateDetails(rect) {
    var allTrue = true;

    allTrue = allTrue && uploader.verify.atLeastOneCell(rect);
    allTrue = allTrue && uploader.verify.eitherColumnOrRow(rect);
    allTrue = allTrue && uploader.verify.similarSelection(issuesRect, rect);

    detailsRect = rect;
    uploader.addSelectionData("details", detailsRect);
    return allTrue;
}