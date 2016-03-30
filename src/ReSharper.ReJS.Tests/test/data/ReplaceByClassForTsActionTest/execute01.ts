Namespace.SubNamespace.PageClass = function/*{caret}*/(contentUrl) {
	this._needInit = false;
};

Namespace.SubNamespace.PageClass.prototype = {
	getIsMobile: function () {
		return true;
	},
	getSystemSwitcher: function () {
		return this._needInit;
	}
}
