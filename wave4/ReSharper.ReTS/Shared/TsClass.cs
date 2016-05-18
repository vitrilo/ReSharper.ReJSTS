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

namespace ReSharper.ReTs.Shared
{
	public class TsFunction
	{
		public ITreeNode Node;
		public ITsParametersList Parameters;
		public ITsBlock Block;
		public TsFunction(ITsFunctionExpression expression)
		{
			Node = expression;
			Parameters = expression.ParameterList;
			Block = expression.Block;
			//expression.GetText()
		}

		public TsFunction(ITsFunctionStatement statement)
		{
			Node = statement;
			Parameters = statement.Signatures[0].Signature.ParameterList;
			Block = statement.Block;
		}

		public static string ExtractName(ITsFunctionStatement statement)
		{
			return statement.DeclaredName;
		}

		public static string ExtractName(ITsObjectPropertyInitializer initializer)
		{
			//initializer.PropertyName.ProjectedName
			return initializer.DeclaredName; 
		}
	}

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
		public TsFunction ConstructorFunction;
		public Dictionary<String, ITreeNode> PrototypeFields = new Dictionary<string, ITreeNode>();
		public Dictionary<String, ITreeNode> StaticFields = new Dictionary<string, ITreeNode>();
		public List<Tuple<string, TsFunction>> StaticMethods = new List<Tuple<string, TsFunction>>();
		public List<Tuple<string, TsFunction>> PrototypeMethods = new List<Tuple<string, TsFunction>>();
		private bool handleThis;
		
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
						if (!PrototypeFields.ContainsKey(name))
						{
							PrototypeFields.Add(name, null);
						}
					}
				}

			}
		}
		public void CreateFieldsFromConstructorParams()
		{
			TsElementFactory factory = TsElementFactory.GetInstance(ConstructorFunction.Block);
			foreach (var par in ConstructorFunction.Parameters.Parameters)
			{
				var name = par.NameNode.GetText();
				if (!PrototypeFields.ContainsKey(name))
				{
					PrototypeFields.Add(name, null);
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
							PrototypeMethods.Add(new Tuple<string, TsFunction>(name, new TsFunction(expr.Source as ITsFunctionExpression)));
							ModificationUtil.DeleteChild(expr);
						}
					}
				}

				//function f1(){}
				if (instruction is ITsFunctionStatement)
				{
					var expr = (instruction as ITsFunctionStatement);
					PrototypeMethods.Add(new Tuple<string, TsFunction>(expr.DeclaredName, new TsFunction(expr)));
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
							PrototypeMethods.Add(new Tuple<string, TsFunction>(declaration.NameNode.GetText(),  new TsFunction(declaration.Value as ITsFunctionExpression)));
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
							PrototypeMethods.Add(new Tuple<string, TsFunction>(name, new TsFunction(expr.Source as ITsFunctionExpression)));
							ModificationUtil.ReplaceChild(expr, factory.CreateStatement("$scope." + name + " = this." + name + ".bind(this);"));
						}
					}
				}
			}
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
							expr.Dest.GetText() == NameFull + ".prototype" && expr.Source is ITsObjectLiteral)
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
			var suffix = NameFull + ".";
			var suffixPrototype = NameFull + ".prototype.";
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
							var f = (ITsFunctionExpression) expr.Source;
							if (expr.Dest.GetText().StartsWith(suffixPrototype))
							{
								PrototypeMethods.Add(new Tuple<string, TsFunction>(
									expr.Dest.GetText().Substring(suffixPrototype.Length),
									new TsFunction(f)));
							}
							else
							{
								StaticMethods.Add(new Tuple<string, TsFunction>(
									expr.Dest.GetText().Substring(suffix.Length),
									new TsFunction(f)));
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
					PrototypeMethods.Add(new Tuple<string, TsFunction>(item.DeclaredName, new TsFunction(fun)));
					FindFieldsInsideFunction(fun.Block);
				}
				else
				{
					PrototypeFields[item.DeclaredName] = item.Value;
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
					StaticMethods.Add(new Tuple<string, TsFunction>(item.DeclaredName, new TsFunction(fun)));
				}
				else
				{
					StaticFields[item.DeclaredName] = item.Value;
				}
			}

		}

		//TODO: Possible need more wise implementation, like treewalker
		private string AddThisIfNeeded(string source)
		{
			if (!handleThis)
			{
				return source;
			}
			foreach (var key in PrototypeFields.Keys)
			{
				source = source.Replace(key, "this." + key);
			}
			foreach (var m in PrototypeMethods)
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
			this.handleThis = handleThis;
			var result = new StringBuilder();

			if (Namespace != "")
			{
				result.AppendLine("module {0} {{", Namespace);
				result.AppendLine();
			}
			result.AppendLine("class {0} {{", Name);
			result.AppendLine();
			foreach (var field in PrototypeFields)
			{
				result.AppendLine("\t{0} {1}={2};",
					field.Key.StartsWith("_") ? "private" : "public",
					field.Key,
					"null"); //Value will be in constuctor
			}
			foreach (var field in StaticFields)
			{
				result.AppendLine("\t{0} static {1}={2};", 
					field.Key.StartsWith("_") ? "private" : "public", 
					field.Key, field.Value != null ? 
					field.Value.GetText() : "null");
			}
			if (ConstructorFunction != null)
			{
				result.AppendLine("\tconstructor({0}){{", ConstructorFunction.Parameters.GetText());
				foreach (var field in PrototypeFields)
				{
					if (field.Value != null)
					{
						result.AppendLine("\tthis.{0}={1};",
							field.Key,
							field.Value.GetText());
					}
				}
				result.AppendLine(AddThisIfNeeded(ConstructorFunction.Block.GetText().Trim('{', '}')));
				result.AppendLine("\t}");
			}

			foreach (var method in PrototypeMethods)
			{
				result.AppendLine(string.Format("\t{0} {1}({2}){3}", 
					method.Item1.StartsWith("_") ? "private" : "public", 
					method.Item1,
					method.Item2.Parameters.GetText(),
					AddThisIfNeeded(method.Item2.Block.GetText())));
			}

			foreach (var method in StaticMethods)
			{
				result.AppendLine(string.Format("\t{0} static {1}({2}){3}",
					method.Item1.StartsWith("_") ? "private" : "public",
					method.Item1,
					method.Item2.Parameters.GetText(),
					method.Item2.Block.GetText().Replace("this.", Name + ".")));
			}

			result.AppendLine("}");
			if (Namespace != "")
			{
				result.AppendLine("}");
			}

			return result.ToString();
		}

		
	}
}
