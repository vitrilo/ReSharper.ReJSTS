using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Services.TypeScript;
using JetBrains.ReSharper.Psi.JavaScript.Tree.TypeScript;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharper.ReJS.Shared
{
	public class TsClass
	{
		public String Namespace
		{
			get
			{
				int pos = NameFull.LastIndexOf(".");
				return pos == -1 ? "" : NameFull.Substring(0, pos);
			}
		}

		public String Name
		{
			get
			{
				int pos = NameFull.LastIndexOf(".");
				return pos == -1 ? NameFull : NameFull.Substring(pos + 1);
			}
		}

		public String NameFull;
		public List<ITreeNode> Sources = new List<ITreeNode>();
		public ITsFunctionExpression ConstructorFunction;
		public Dictionary<String, List<ITreeNode>> Fields = new Dictionary<string, List<ITreeNode>>();
		public List<ITsObjectPropertyInitializer> PrototypeMethods = new List<ITsObjectPropertyInitializer>();
		public List<Tuple<string, ITsFunctionExpression>> PrototypeSeparateMethods = new List<Tuple<string, ITsFunctionExpression>>();
		public List<Tuple<string, ITsFunctionStatement>> PrototypeSeparate2Methods = new List<Tuple<string, ITsFunctionStatement>>();
		public List<ITsObjectPropertyInitializer> PrototypeFields = new List<ITsObjectPropertyInitializer>();
		public List<ITsObjectPropertyInitializer> StaticMethods = new List<ITsObjectPropertyInitializer>();
		public List<Tuple<string, ITsFunctionExpression>> StaticSeparateMethods = new List<Tuple<string, ITsFunctionExpression>>();
		public List<ITsObjectPropertyInitializer> StaticFields = new List<ITsObjectPropertyInitializer>();

		public void FindFieldsInsideFunction(ITsBlock body)
		{
			//Find Fields
			foreach (var instruction in body.StatementsEnumerable)
			{
				if (instruction is ITsExpressionStatement)
				{
					var expr = (instruction as ITsExpressionStatement).Expression.Expressions.LastOrDefault() as ITsSimpleAssignmentExpression;
					if (expr != null && /*expr.IsAssignment &&*/ expr.Dest.GetText().StartsWith("this."))
					{
						var name = expr.Dest.GetText().Replace("this.", "");
						var end = name.IndexOf(".");
						if (end != -1)
						{
							name = name.Substring(0, end);
						}
						end = name.IndexOf("[");
						if (end != -1)
						{
							name = name.Substring(0, end);
						}
						if (!Fields.ContainsKey(name))
						{
							Fields.Add(name, null);
						}
					}
				}

			}
		}
		public void CreateFieldsFromConstructorParams()
		{
			TsElementFactory factory = TsElementFactory.GetInstance(ConstructorFunction);
			var pars = ConstructorFunction.Parameters.ToArray();
			foreach (var par in ConstructorFunction.Parameters)
			{
				var name = par.NameNode.GetText();
				if (!Fields.ContainsKey(name))
				{
					Fields.Add(name, null);
					ConstructorFunction.Block.AddStatementAfter(factory.CreateStatement("this." + name + " = " + name + ";"), null);
				}
			}
		}

		public void FindAndMoveMethodsInsideFunction(ITsBlock body)
		{
			foreach (var instruction in body.StatementsEnumerable.ToArray())
			{
				//this.f1 = function(){}
				if (instruction is ITsExpressionStatement)
				{
					var expr = (instruction as ITsExpressionStatement).Expression.Expressions.LastOrDefault() as ITsSimpleAssignmentExpression;
					if (expr != null && /*expr.IsAssignment &&*/ expr.Dest.GetText().StartsWith("this."))
					{
						var name = expr.Dest.GetText().Replace("this.", "");
						var end = name.IndexOf(".");
						if (end != -1)
						{
							name = name.Substring(0, end);
						}
						if (name.IndexOf("[")>-1){ continue; }
						if (expr.Source is ITsFunctionExpression)
						{
							PrototypeSeparateMethods.Add(new Tuple<string, ITsFunctionExpression>(name, expr.Source as ITsFunctionExpression));
							ModificationUtil.DeleteChild(expr);
						}
					}
				}

				//function f1(){}
				if (instruction is ITsFunctionStatement)
				{
					var expr = (instruction as ITsFunctionStatement);
					var name = expr.DeclaredName;
					PrototypeSeparate2Methods.Add(new Tuple<string, ITsFunctionStatement>(name, expr));
					ModificationUtil.DeleteChild(expr);
				}

				//var f1 = function(){}, f2 = function(){}
				if (instruction is ITsVariableDeclarationStatement)
				{
					var expr = (instruction as ITsVariableDeclarationStatement);
					var vars = expr.DeclarationsEnumerable.ToArray();
					foreach (var declaration in vars)
					{
						if (declaration.Value is ITsFunctionExpression)
						{
							PrototypeSeparateMethods.Add(new Tuple<string, ITsFunctionExpression>(declaration.NameNode.GetText(), declaration.Value as ITsFunctionExpression));
							ModificationUtil.DeleteChild(declaration);
						}
					}
					if (expr.Declarations.IsEmpty)
					{
						ModificationUtil.DeleteChild(expr);
					}
				}
			}

		}

		public void FindAndMoveNgMethodsInsideFunction(ITsBlock body)
		{
			TsElementFactory factory = TsElementFactory.GetInstance(body);
			foreach (var instruction in body.StatementsEnumerable)
			{
				if (instruction is ITsExpressionStatement)
				{
					var expr = (instruction as ITsExpressionStatement).Expression.Expressions.LastOrDefault() as ITsSimpleAssignmentExpression;
					if (expr != null && /*expr.IsAssignment &&*/ expr.Dest.GetText().StartsWith("$scope."))
					{
						var name = expr.Dest.GetText().Replace("$scope.", "");
						var end = name.IndexOf(".");
						if (end != -1)
						{
							name = name.Substring(0, end);
						}
						if (name.IndexOf("[") > -1) { continue; }
						if (expr.Source is ITsFunctionExpression)
						{
							PrototypeSeparateMethods.Add(new Tuple<string, ITsFunctionExpression>(name, expr.Source as ITsFunctionExpression));
							ModificationUtil.ReplaceChild(expr, factory.CreateStatement("$scope." + name + " = this." + name + ".bind(this);"));
						}
					}
				}
			}
		}

		//TODO: Possible need more wise implementation, like treewalker
		private string AddThisIfNeeded(string source)
		{
			foreach (var key in Fields.Keys)
			{
				source = source.Replace(key, "this." + key);
			}
			foreach (var m in PrototypeMethods)
			{
				source = source.Replace(m.PropertyName.ProjectedName, "this." + m.PropertyName.ProjectedName);
			}
			foreach (var m in PrototypeSeparateMethods)
			{
				source = source.Replace(m.Item1, "this." + m.Item1);
			}
			foreach (var m in PrototypeSeparate2Methods)
			{
				source = source.Replace(m.Item1, "this." + m.Item1);
			}
			source = source.Replace(".this.", ".");
			source = source.Replace("this.this.", "this.");
			source = source.Replace("this.this.", "this.");
			
			source = source.Replace("this.this.", "this.");
			source = Regex.Replace(source, "this\\.([\\w]+:)", "$1");
		
			return source;
		}

		public string TransformForTypescript(bool handleThis=false)
		{
			var result = new StringBuilder();

			if (Namespace != "")
			{
				result.AppendLine(string.Format("module {0} {{", Namespace));
				result.AppendLine();
			}
			result.AppendLine(string.Format("export class {0} {{", Name));
			result.AppendLine();
			foreach (var field in Fields.Keys)
			{
				result.AppendLine(field.StartsWith("_")
									  ? string.Format("\tprivate {0}=null;", field)
									  : string.Format("\tpublic {0}=null;", field));
			}
			foreach (var field in StaticFields)
			{
				result.AppendLine(field.PropertyName.ProjectedName.StartsWith("_")
									  ? string.Format("\tprivate static {0}={1};", field.PropertyName.ProjectedName, field.Value.GetText())
									  : string.Format("\tpublic static {0}={1};", field.PropertyName.ProjectedName, field.Value.GetText()));
			}
			if (ConstructorFunction != null)
			{
				result.AppendLine(string.Format("\tconstructor({0}){{",
					ConstructorFunction.Parameters.Select(p => p.GetText()).Join(",")));
				foreach (var field in PrototypeFields)
				{
					result.AppendLine(field.GetText().Replace(":", "="));
				}
				var body = ConstructorFunction.Block.GetText().Trim('{', '}');
				if (handleThis)
				{
					body = AddThisIfNeeded(body);
				}
				result.AppendLine(body);
				result.AppendLine(string.Format("\t}}"));
			}

			foreach (var method in PrototypeMethods)
			{
				var text = method.Value.GetText().Trim();
				if (text.StartsWith("function"))
				{
					text = text.Substring(8);
				}
				if (handleThis)
				{
					text = AddThisIfNeeded(text);
				}
				result.AppendLine(string.Format("\t{0}{1}", method.PropertyName.ProjectedName, text));
			}

			foreach (var method in PrototypeSeparateMethods)
			{
				var text = method.Item2.GetText().Trim();
				if (text.StartsWith("function"))
				{
					text = text.Substring(8);
				}
				if (handleThis)
				{
					text = AddThisIfNeeded(text);
				}
				result.AppendLine(string.Format("\t{0}{1}", method.Item1, text));
			}
			foreach (var method in PrototypeSeparate2Methods)
			{
				if (method.Item2.Signatures.Count > 0)
				{
					var pars = method.Item2.Signatures[0].Signature.ParameterList.GetText();
					var block = method.Item2.Block.GetText();
					if (handleThis)
					{
						block = AddThisIfNeeded(block);
					}
					result.AppendLine(string.Format("\t{0}({1}){2}", method.Item1, pars, block));
				}
			}

			foreach (var method in StaticMethods)
			{
				var text = method.Value.GetText().Trim();
				if (text.StartsWith("function"))
				{
					text = text.Substring(8);
				}
				text = text.Replace("this.", Name + ".");
				result.AppendLine(string.Format("\tstatic {0}{1}", method.PropertyName.ProjectedName, text));
			}

			foreach (var method in StaticSeparateMethods)
			{
				var text = method.Item2.GetText().Trim();
				if (text.StartsWith("function"))
				{
					text = text.Substring(8);
				}
				text = text.Replace("this.", Name + ".");
				result.AppendLine(string.Format("\tstatic {0}{1}", method.Item1, text));
			}

			result.AppendLine(string.Format("}}"));
			if (Namespace != "")
			{
				result.AppendLine(string.Format("}}"));
			}

			return result.ToString();
		}

		public Tuple<ITsSimpleAssignmentExpression, ITsObjectLiteral> FindPrototype(ITreeNode lookup)
		{
			for (int i = 0; i < 5; i++)
			{
				if (lookup == null)
				{
					break;
				}
				var stat = lookup as ITsExpressionStatement;
				if (stat != null && stat.Expression != null)
				{
					var expr = stat.Expression.Expressions.LastOrDefault() as ITsSimpleAssignmentExpression;
					if (expr != null)
					{
						if (/*expr.IsAssignment &&*/ expr.Dest is ITsReferenceExpression &&
							expr.Dest.GetText() == this.NameFull + ".prototype" && expr.Source is ITsObjectLiteral)
						{
							return new Tuple<ITsSimpleAssignmentExpression, ITsObjectLiteral>(expr, expr.Source as ITsObjectLiteral);
						}
					}
				}
				lookup = lookup.NextSibling;
			}
			return null;
		}

		public void CollectPrototypeSeparateMethods(ITreeNode lookup)
		{
			var suffix = this.NameFull + ".";
			var suffixPrototype = this.NameFull + ".prototype.";
			for (int i = 0; i < 100; i++)
			{
				if (lookup == null)
				{
					break;
				}
				var stat = lookup as ITsExpressionStatement;
				if (stat != null && stat.Expression != null)
				{
					var expr = stat.Expression.Expressions.LastOrDefault() as ITsSimpleAssignmentExpression;
					if (expr != null)
					{

						if (/*expr.IsAssignment &&*/ expr.Dest is ITsReferenceExpression &&
							expr.Dest.GetText().StartsWith(suffix) && expr.Source is ITsFunctionExpression)
						{
							if (expr.Dest.GetText().StartsWith(suffixPrototype))
							{
								PrototypeSeparateMethods.Add(new Tuple<string, ITsFunctionExpression>(
									expr.Dest.GetText().Substring(suffixPrototype.Length),
									(ITsFunctionExpression)expr.Source));
							}
							else
							{
								StaticSeparateMethods.Add(new Tuple<string, ITsFunctionExpression>(
									expr.Dest.GetText().Substring(suffix.Length),
									(ITsFunctionExpression)expr.Source));
							}
							Sources.Add(lookup);
						}
					}
				}
				lookup = lookup.NextSibling;
			}
		}

		public void AnalyzePrototype(ITsObjectLiteral prot)
		{
			foreach (var node in prot.PropertiesEnumerable)
			{
				var item = node as ITsObjectPropertyInitializer;
				if (item == null)
				{
					continue;
				}
				var fun = item.Value as ITsFunctionExpression;
				if (fun != null)
				{
					this.PrototypeMethods.Add(item);
					this.FindFieldsInsideFunction(fun.Block);
				}
				else
				{
					this.PrototypeFields.Add(item);
					if (!this.Fields.ContainsKey(item.PropertyName.ProjectedName))
					{
						this.Fields.Add(item.PropertyName.ProjectedName, null);
					}
				}
			}

		}

		public void AnalyzeStaticClass(ITsObjectLiteral stat)
		{
			foreach (var node in stat.PropertiesEnumerable)
			{
				var item = node as ITsObjectPropertyInitializer;
				if (item == null)
				{
					continue;
				}
				var fun = item.Value as ITsFunctionExpression;
				if (fun != null)
				{
					this.StaticMethods.Add(item);
				}
				else
				{
					this.StaticFields.Add(item);
				}
			}

		}
	}
}
