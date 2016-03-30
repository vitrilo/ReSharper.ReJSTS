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
			
			if (index != null && index.IsValid() && index.Source is ITsFunctionExpression && index.Dest is ITsReferenceExpression)
			{
				_indexExpression = index;
				_replacement = "Convert to ES6 Class";
					//string.Format("{0}.{1}", index.IndexedExpression.GetText(), index.AccessedPropertyName);
				return true;
			}

			if (index != null && index.IsValid() && index.Source is ITsObjectLiteral && index.Dest is ITsReferenceExpression)
			{
				_indexExpression = index;
				_replacement = "Convert to ES6 Static Class";
				//string.Format("{0}.{1}", index.IndexedExpression.GetText(), index.AccessedPropertyName);
				return true;
			}
			return false;
		}

		
		protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
		{
			var parent = TsExpressionStatementNavigator.GetByExpression(TsCompoundExpressionNavigator.GetByExpression(_indexExpression));
			var info = new TsClass();
			if(_indexExpression.Source is ITsFunctionExpression && _indexExpression.Dest is ITsReferenceExpression)
			{
				info.NameFull = _indexExpression.Dest.GetText();
				info.Sources.Add(_indexExpression);
				info.ConstructorFunction = _indexExpression.Source as ITsFunctionExpression;
				info.FindFieldsInsideFunction(info.ConstructorFunction.Block);
				if (parent != null)
				{
					var prot = info.FindPrototype(parent.NextSibling);
					if (prot != null)
					{
						info.Sources.Add(prot.Item1);
						info.AnalyzePrototype(prot.Item2);
					}
					else
					{
						info.CollectPrototypeSeparateMethods(parent.NextSibling);
					}
				}
				
			}
			else if(_indexExpression.Source is ITsObjectLiteral && _indexExpression.Dest is ITsReferenceExpression)
			{
				info.NameFull = _indexExpression.Dest.GetText();
				info.Sources.Add(_indexExpression);
				info.AnalyzeStaticClass(_indexExpression.Source as ITsObjectLiteral);
			}

			TsElementFactory factory = TsElementFactory.GetInstance(_indexExpression);
			using (WriteLockCookie.Create())
			{
				ModificationUtil.ReplaceChild(info.Sources[0], factory.CreateModuleMember(info.TransformForTypescript()));
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

		#region Nested types

		
		#endregion
	}

	
}