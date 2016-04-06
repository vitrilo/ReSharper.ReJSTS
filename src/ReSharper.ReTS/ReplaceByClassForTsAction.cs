using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.JavaScript.Bulbs.TypeScript;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Services.TypeScript;
using JetBrains.ReSharper.Psi.JavaScript.Tree.TypeScript;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;
using ReSharper.ReTs.Shared;

namespace ReSharper.ReTs
{
	[ContextAction(Name = "ReplaceByClassAction", Description = "Replaces class in prototype style to class in ES6 style",
		Group = "TypeScript")]
	public class ReplaceByClassForTsAction : ContextActionBase
	{
		public ReplaceByClassForTsAction(ITypeScriptContextActionDataProvider provider)
		{
			_provider = provider;
		}

		public override string Text
		{
			get{ return _replacement; }
		}

		//Namespace.SubNamespace.PageClass = function/*{caret}*/(contentUrl) {
		//	this._needInit = false;
		//};
		//Namespace.SubNamespace.PageClass.prototype = {
		//	getIsMobile: function () {
		//		return true;
		//	},
		//	getSystemSwitcher: function () {
		//		return this._needInit;
		//	}
		//}
		//
		//ITsExpressionStatement - constructor
		// CompoundExpression
		//  BinaryExpression
		//	 ReferenceExpression - Namespace.SubNamespace.PageClass
		//		ReferenceExpression - Namespace.SubNamespace
		//			ReferenceExpression - Namespace
		//	WhiteSpace
		//	EqTokenElement
		//	WhiteSpace
		//	FunctionExpression 	function(contentUrl) {this._needInit = false;}
		//		FunctionExpressionSignature 	function(contentUrl)
		//			FunctionKeywordTokenElement	function
		//		Whitespace
		//		ITsBlock
		//			LbraceTokenElement
		//			RbraceTokenElement
		//ITsExpressionStatement - prototype
		// CompoundExpression
		//  BinaryExpression
		//		ReferenceExpression
		//		ITsObjectLiteral
		//			ITsObjectPropertyInitializer
		//				IPropertyNameIdentifier
		//				WhiteSpace
		//				Block
		//					FunctionExpression
		public override bool IsAvailable(IUserDataHolder cache)
		{
			//JetBrains.ReSharper.Psi.JavaScript.Tree.ITsFunctionExpression
			//JetBrains.ReSharper.Psi.JavaScript.Tree.IFunctionStatement
			//JetBrains.ReSharper.Psi.JavaScript.Tree.ITsFunctionExpressionSignature

			var index = _provider.GetSelectedElement<ITsSimpleAssignmentExpression>(true, true);
			_indexExpression = null;
			_indexDeclaration = null;
			if (index != null && index.IsValid() && index.Source is ITsFunctionExpression && index.Dest is ITsReferenceExpression)
			{
				_indexExpression = index;
				_replacement = "Convert to ES6 Class";
				return true;
			}

			if (index != null && index.IsValid() && index.Source is ITsObjectLiteral && index.Dest is ITsReferenceExpression)
			{
				_indexExpression = index;
				_replacement = "Convert to ES6 Static Class";
				return true;
			}

			var indexDeclaration = _provider.GetSelectedElement<ITsFunctionStatement>(true, true);
			if (indexDeclaration != null && indexDeclaration.IsValid())
			{
				_indexDeclaration = indexDeclaration;
				_replacement = "Convert to ES6 Class";
				return true;
			}


			return false;
		}

		protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
		{
			if (_indexExpression != null)
			{
				ExecuteOnExpression(_indexExpression, progress);
			}
			else if (_indexDeclaration != null)
			{
				ExecuteOnDeclaration(_indexDeclaration, progress);
			}
			return null;
		}

		protected Action<ITextControl> ExecuteOnExpression(ITsSimpleAssignmentExpression index, IProgressIndicator progress)
		{
			var start = TsExpressionStatementNavigator.GetByExpression(TsCompoundExpressionNavigator.GetByExpression(index));
			var info = new TsClass();
			if (index.Source is ITsFunctionExpression && index.Dest is ITsReferenceExpression)
			{
				info.NameFull = index.Dest.GetText();
				info.Sources.Add(index);
				info.ConstructorFunction = new TsFunction(index.Source as ITsFunctionExpression);
				info.FindFieldsInsideFunction(info.ConstructorFunction.Block);
				if (start != null)
				{
					var prot = info.FindPrototype(start.NextSibling);
					if (prot != null)
					{
						info.Sources.Add(prot.Item1);
						info.AnalyzePrototype(prot.Item2);
					}
					info.CollectPrototypeSeparateMethods(start.NextSibling);
				}

			}
			else if (index.Source is ITsObjectLiteral && index.Dest is ITsReferenceExpression)
			{
				info.NameFull = index.Dest.GetText();
				info.Sources.Add(index);
				info.AnalyzeStaticClass(index.Source as ITsObjectLiteral);
			}

			TsElementFactory factory = TsElementFactory.GetInstance(index);
			using (WriteLockCookie.Create())
			{
				ModificationUtil.ReplaceChild(info.Sources[0], factory.CreateStatement(info.TransformForTypescript()));
				for (var i = 1; i < info.Sources.Count; i++)
				{
					ModificationUtil.DeleteChild(info.Sources[i]);
				}
			}

			return null;
		}

		protected Action<ITextControl> ExecuteOnDeclaration(ITsFunctionStatement index, IProgressIndicator progress)
		{
			var start = index;
			var info = new TsClass();
			
			info.NameFull = index.DeclaredName;
			info.Sources.Add(index);
			info.ConstructorFunction = new TsFunction(index);
			info.FindFieldsInsideFunction(info.ConstructorFunction.Block);
			if (start != null)
			{
				var prot = info.FindPrototype(start.NextSibling);
				if (prot != null)
				{
					info.Sources.Add(prot.Item1);
					info.AnalyzePrototype(prot.Item2);
				}
				info.CollectPrototypeSeparateMethods(start.NextSibling);
			}

			TsElementFactory factory = TsElementFactory.GetInstance(index);
			using (WriteLockCookie.Create())
			{
				ModificationUtil.ReplaceChild(info.Sources[0], factory.CreateStatement(info.TransformForTypescript()));
				for (var i = 1; i < info.Sources.Count; i++)
				{
					ModificationUtil.DeleteChild(info.Sources[i]);
				}
			}
			
			return null;
		}

		private readonly ITypeScriptContextActionDataProvider _provider;
		private ITsSimpleAssignmentExpression _indexExpression;
		private string _replacement = "None";
		private ITsFunctionStatement _indexDeclaration;

		#region Nested types

		
		#endregion
	}

	
}