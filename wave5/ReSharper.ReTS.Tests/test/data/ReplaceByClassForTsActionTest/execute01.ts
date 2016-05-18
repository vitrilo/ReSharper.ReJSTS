Namespace.SubNamespace.MyClass = function/*{caret}*/(field2) {
	this._field1 = false;
	this._field2 = field2;
};

Namespace.SubNamespace.MyClass.prototype = {
	f1: function () {},
	_f2: function () {}
}
Namespace.SubNamespace.MyClass.f1Static = function() {};
