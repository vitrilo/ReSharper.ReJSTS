function/*{caret}*/ MyClass(field2) {
	this._field1 = false;
	this._field2 = field2;
};

MyClass.prototype = {
	f1: function () { },
	_f2: function () { }
}
MyClass.f1Static = function () { };
