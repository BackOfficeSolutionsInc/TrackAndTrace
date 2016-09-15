/*
 * Undo.js - A undo/redo framework for JavaScript
 * 
 * http://jzaefferer.github.com/undo
 *
 * Copyright (c) 2011 Jörn Zaefferer
 * 
 * MIT licensed.
 */
(function () {

    // based on Backbone.js' inherits	
    var ctor = function () { };
    var inherits = function (parent, protoProps) {
        var child;

        if (protoProps && protoProps.hasOwnProperty('constructor')) {
            child = protoProps.constructor;
        } else {
            child = function () { return parent.apply(this, arguments); };
        }

        ctor.prototype = parent.prototype;
        child.prototype = new ctor();

        if (protoProps) extend(child.prototype, protoProps);

        child.prototype.constructor = child;
        child.__super__ = parent.prototype;
        return child;
    };

    function extend(target, ref) {
        var name, value;
        for (name in ref) {
            value = ref[name];
            if (value !== undefined) {
                target[name] = value;
            }
        }
        return target;
    };

    var Undo = {
        version: '0.1.15'
    };

    Undo.Stack = function () {
        this.commands = [];
        this.stackPosition = -1;
        this.savePosition = -1;
    };

    extend(Undo.Stack.prototype, {
        execute: function (command) {
            this._clearRedo();
            command.execute();
            this.commands.push(command);
            this.stackPosition++;
            this.changed();
        },
        undo: function () {
            this.commands[this.stackPosition].undo();
            this.stackPosition--;
            this.changed();
        },
        canUndo: function () {
            return this.stackPosition >= 0;
        },
        redo: function () {
            this.stackPosition++;
            this.commands[this.stackPosition].redo();
            this.changed();
        },
        canRedo: function () {
            return this.stackPosition < this.commands.length - 1;
        },
        save: function () {
            this.savePosition = this.stackPosition;
            this.changed();
        },
        dirty: function () {
            return this.stackPosition != this.savePosition;
        },
        _clearRedo: function () {
            // TODO there's probably a more efficient way for this
            this.commands = this.commands.slice(0, this.stackPosition + 1);
        },
        changed: function () {
            // do nothing, override
        },
        rollback: function () {
        	this.stackPosition++;
        	var cmds =this.commands[this.stackPosition];
        	if (cmds.rollback)
        		cmds.rollback();
        	else
        		cmds.undo();
        	this.changed();
        }
    });

    Undo.Command = function (name) {
        this.name = name;
    }

    var up = new Error("override me!");

    extend(Undo.Command.prototype, {
        execute: function () {
            throw up;
        },
        undo: function () {
            throw up;
        },
        redo: function () {
            this.execute();
        },
        rollback: function () {
			//override
        	this.undo();
		}
    });

    Undo.Command.extend = function (protoProps) {
        var child = inherits(this, protoProps);
        child.extend = Undo.Command.extend;
        return child;
    };

    // AMD support
    if (typeof define === "function" && define.amd) {
        // Define as an anonymous module
        define(Undo);
    } else if (typeof module != "undefined" && module.exports) {
        module.exports = Undo
    } else {
        this.Undo = Undo;
    }
}).call(this);



var undoStack = new Undo.Stack();
$(document).keydown(function (event) {
    var keyCode = event.keyCode;
    if (event.metaKey === true || event.ctrlKey === true) {
    	if (keyCode === 89) {
    		console.log("Redo called");
            //fire your custom redo logic
            event.preventDefault();
            event.preventDefault();
            undoStack.canRedo() && undoStack.redo()
            return false;
        }
        else if (keyCode === 90) {
            //special case (CTRL-SHIFT-Z) does a redo (on a mac for example)
            if (event.shiftKey === true) {
                //fire your custom redo logicevent.preventDefault();
                undoStack.canRedo() && undoStack.redo()
            }
            else {
				console.log("Undo called");
                //fire your custom undo logic
                event.preventDefault();
                undoStack.canUndo() && undoStack.undo();
            }
            event.preventDefault();
            return false;
        }
    }
});