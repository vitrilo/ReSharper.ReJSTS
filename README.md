Resharper-ReTs (Refactorings for TypeScript)
==============

Contain some refactorings for TypeScript language.

Mainly useful for converting old-fashion code into **ES6 Class** in Typescript.

May be useful for **Angular 1x code** (migration) converting to 2x (in progress), because it actively use ES6 Classes in Typescript


###Available quick-fixes

####Replace Prototype-styled code to ES6 Class

```javascript
function MyClass/*{caret}*/(field2) {
	this._field1 = false;
	this._field2 = field2;
};
MyClass.prototype = {
	f1: function() {},
	private _f2: function() {}
};
MyClass.f3static: function() {}

//OR--------------------------------

Namespace.MyClass = function/*{caret}*/(field2) {
	this._field1 = false;
	this._field2 = field2;
};
Namespace.MyClass.prototype = {
	f1: function() {},
	private _f2: function() {}
};
Namespace.MyClass.f3static: function() {}

//OR--------------------------------

Namespace.MyClass = function/*{caret}*/(field2) {
	this._field1 = false;
	this._field2 = field2;
};
Namespace.MyClass.prototype.f1 = function () { };
Namespace.MyClass.prototype._f1 = function () { };
Namespace.MyClass.f3static: function() {}
```

By using "Convert to ES6 Class" on function (see caret position in example) will be replaced by: 
```javascript
module Namespace{
	export class MyClass {
		private _field1=null;
		private _field2=null;

		constructor(field2) {
			this._field1 = false;
			this._field2 = field2;
		}

		public f1() {}
		private _f2() {}
		public static f3() {}
	}
};
```

####Replace Static-styled code to ES6 Class

```javascript
Namespace.MyStaticClass/*{caret}*/ = {
	f1: function() {},
	_f2: function() {}
};
```

By using "Convert to ES6 Static Class" will be replaced by: 

```javascript
module Namespace {
	export class MyStaticClass {
		static f1() {}
		private static _f2() {}
	}
}
```


####Convert closure to ES6 Class

```javascript
angular.controller("TestCtrl", ["$scope", "mainFormModel", "dataAccess", function ($scope, mainFormModel, dataAccess) {
	this.contrF1 = function (one) { };
	$scope.scopeF2 = function (two) { };
	$scope.scopeF3 = function (three) { };
}
]);
```

By using "Convert closure to ES6 Class" will be replaced by: 

```javascript
export class SomeClass {
	public $scope=null;
	public mainFormModel=null;
	public dataAccess=null;

	constructor($scope, mainFormModel, dataAccess) {
		this.dataAccess = this.dataAccess;
		this.mainFormModel = this.mainFormModel;
		this.$scope = this.$scope;;
		this.$scope.scopeF2 = this.scopeF2.bind(this);
		this.$scope.scopeF3 = this.scopeF3.bind(this);
	}

	contrF1(one) {}
	scopeF2(two) {}
	scopeF3(three) {}
}
angular.controller("TestCtrl", ["$scope", "mainFormModel", "dataAccess", SomeClass]);
```

####Install

Available in [ReSharper Gallery](https://resharper-plugins.jetbrains.com/packages/ReSharper.ReTs.R100/)

- Comartible with Resharper #10 (any Visual Studio)

####Notes
Fork of "Resharper-ReJs" by Alexander Zaytsev