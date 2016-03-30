(function/*{caret}*/ ($scope, mainFormModel, dataAccess) {
	$scope.logout = function (third) {}
	var f1 = function(other) { var yy = 1 + 1; }
	var f_private = function (some) { var yy = 3 + 3;}
	var dataService = {
		f1: f1,
		f2:f2
	};
	return dataService;
		
	function f2(fifth) { f_private();}
})(window['$scope'], window['mainFormModel'], window['dataAccess']);