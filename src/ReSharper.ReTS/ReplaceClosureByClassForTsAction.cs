using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.JavaScript.Bulbs.TypeScript;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Services.TypeScript;
using JetBrains.ReSharper.Psi.JavaScript.Tree.TypeScript;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharper.ReJS.Shared;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Resources.Shell;


namespace ReSharper.ReJS
{
	[ContextAction(Name = "ReplaceClosureByClassForTsAction", Description = "Replaces Closure by class in ES6 style",
		Group = "TypeScript")]
	public class ReplaceClosureByClassForTsAction : ContextActionBase
	{
		public ReplaceClosureByClassForTsAction(ITypeScriptContextActionDataProvider provider)
		{
			_provider = provider;
		}

		public override string Text
		{
			get{ return _replacement; }
		}

		//(function ($scope, mainFormModel, dataAccess) {
		//	$scope.logout = function () {}
		//	var f1 = function() { }
		//	var f_private = function() { }
		//	var dataService = {
		//		f1: f1,
		//		f2:f2
		//	};
		//	return dataService;
		//
		//	function f2() { f_private();}
		//})(window['$scope'], window['mainFormModel'], window['dataAccess']);
		//
		//ExpressionStatement
		//			CompaundExpression
		//				ITsInvocationExpression //angular.controller(...)
		//					ITsReferenceExpression //angular.controller
		//					ITsLiteralExpression //"UserProfileCtrl"
		//					FunctionExpression <OR ITsArrayLiteral> //function ($scope, mainFormModel, dataAccess) {...}
		//						FunctionExpressionSignature
		//							ParametersList ($scope, mainFormModel, dataAccess)
		//						ITsBlock
		//							LbraceTokenElement
		//							ExpressionStatement //FOR-EACH
		//								CompaundExpression
		//									ITsSimpleAssignmentExpression
		//										ITsReferenceExpression //$scope.changeUserProfile
		//										FunctionExpression //function (profileId) {}
		//											ITsFunctionStatementSignature
		//											ITsBlock
		//							RbraceTokenElement
		public override bool IsAvailable(IUserDataHolder cache)
		{
			var index = _provider.GetSelectedElement<ITsFunctionExpression>(true, true);

			if (index != null && index.IsValid())
			{
				_index = index;
				_replacement = "Convert Closure to ES6 Class";
				return true;
			}
			return false;
		}


		protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
		{
			ITreeNode parent = _index;
			while (true)
			{
				if (parent.Parent == null)
				{
					return null;
				}
				if (parent.Parent is ITsFileSection)
				{
					break;
				}
				parent = parent.Parent;
			}

			var info = new TsClass();
			info.NameFull = "SomeNamespace.SomeClass";

			info.ConstructorFunction = _index;

			TsElementFactory factory = TsElementFactory.GetInstance(_index);
			using (WriteLockCookie.Create())
			{
				info.FindFieldsInsideFunction(info.ConstructorFunction.Block);
				info.FindAndMoveMethodsInsideFunction(info.ConstructorFunction.Block);
				info.FindAndMoveNgMethodsInsideFunction(info.ConstructorFunction.Block);
				info.CreateFieldsFromConstructorParams();
				//Replace Ng Controller body to its class name
				ModificationUtil.ReplaceChild(info.ConstructorFunction, factory.CreateRefenceName(info.NameFull));
				//Insert ES6 class Before
				ModificationUtil.AddChildBefore(parent.Parent, parent, factory.CreateModuleMember(info.TransformForTypescript(true)));
			}
			
			return null;
		}

		private readonly ITypeScriptContextActionDataProvider _provider;
		private ITsFunctionExpression _index;
		
		private string _replacement = "None";
	}
}