﻿
window.consoleStore = new Array(1000);
var oldConsoleLog = console.log;
var oldConsoleError = console.error;
var oldConsoleInfo = console.info;
var getStackTrace = function () {
    var obj = {};
    Error.captureStackTrace(obj, getStackTrace);
    return obj.stack;
};
function interceptLogger(name, oldLogger) {
    return function () {
        var lineNum = "na";
        try {
            try {
                try {
                    var f = getStackTrace();
                    line = f.split("\n")[2].split("/");
                    lineNum = line[line.length - 1].split(")")[0];//substr(0,line[line.length-1].length-1)
                } catch (e) {
                    debugger;
                }
                window.consoleStore.shift();
                
                var args = [];
                for (var i in arguments) {
                    args.push(arguments[i]);
                }

                window.consoleStore.push({
                    t: name,
                    dat: args,
                    ln: lineNum,
                    dt: +new Date()
                });
            } catch (e) {
                debugger;
            }
            //arguments.push(line);
            
            //args.push("(" + lineNum + ")");
            oldLogger.apply(console, arguments);
        } catch (e) {
            debugger;
        }
    }
}
console.log = interceptLogger("log", oldConsoleLog);
console.error = interceptLogger("error", oldConsoleError);
console.info = interceptLogger("info", oldConsoleInfo);