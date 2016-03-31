using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.JavaScript.Bulbs.TypeScript;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Services.TypeScript;
using JetBrains.ReSharper.Psi.JavaScript.Tree.TypeScript;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharper.ReJS.Shared;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Resources.Shell;

namespace ReSharper.ReJS
{
	[ContextAction(Name = "ReplaceNGControllerByClassAction", Description = "Replaces Angular Controller to class in ES6 style",
		Group = "TypeScript")]
	public class ReplaceNgContrByClassForTsAction : ContextActionBase
	{
		public ReplaceNgContrByClassForTsAction(ITypeScriptContextActionDataProvider provider)
		{
			_provider = provider;
		}

		public override string Text
		{
			get{ return _replacement; }
		}

		//angular.controller("UserProfileCtrl", function ($scope, mainFormModel, dataAccess) {
		//	$scope.logout = function () {}
		//});
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

		//angular.controller("UserProfileCtrl", ["$scope", "mainFormModel", "dataAccess", function ($scope, mainFormModel, dataAccess) {
		//		$scope.logout = function () {
		//			AppConnection.logout().done(function () {
		//				window.location.href = "logout.html";
		//			});
		//		}
		//	}
		//]);
		//					ITsArrayLiteral
		//						ITsLiteralExpression //FOR-EACH
		//						FunctionExpression //function ($scope, mainFormModel, dataAccess) {...}
		public override bool IsAvailable(IUserDataHolder cache)
		{
			var index = _provider.GetSelectedElement<ITsReferenceExpression>(true, true);

			if (index != null && index.IsValid() && index.GetText().EndsWith(".controller") && index.Parent is ITsInvocationExpression)
			{
				_indexInvocation = index.Parent as ITsInvocationExpression;
				_replacement = "Convert NG Controller to ES6 Class";
				return true;
			}
			return false;
		}


		protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
		{
			var parent =
				TsExpressionStatementNavigator.GetByExpression(TsCompoundExpressionNavigator.GetByExpression(_indexInvocation));

			var arguments = _indexInvocation.Arguments.ToArray();
			var info = new TsClass();
			var contrName = arguments[0].GetText().Trim('"');
			info.NameFull = "SomeNamespace." + contrName.Substring(0, 1).ToUpper() + contrName.Substring(1);

			var last = arguments[arguments.Length - 1];
			if (last is ITsFunctionExpression)
			{
				info.ConstructorFunction = last as ITsFunctionExpression;
			}
			else if (last is ITsArrayLiteral)
			{
				var arr = last as ITsArrayLiteral;
				info.ConstructorFunction = arr.ArrayElements[arr.ArrayElements.Count - 1] as ITsFunctionExpression;
			}
			else
			{
				return null;
			}
			TsElementFactory factory = TsElementFactory.GetInstance(_indexInvocation);
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
		private ITsInvocationExpression _indexInvocation;
		
		private string _replacement = "None";
	}
}