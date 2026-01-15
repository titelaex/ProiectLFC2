using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime.Misc;

namespace ProiectLFC2
{ 

public class SemanticVisitor : GrammarBaseVisitor<object>
{
    public List<VariableInfo> GlobalVariables = new List<VariableInfo>();
    public List<FunctionInfo> Functions = new List<FunctionInfo>();
    public List<string> SemanticErrors = new List<string>();

    private Dictionary<string, VariableInfo> globalScope = new Dictionary<string, VariableInfo>();

    private Dictionary<string, FunctionInfo> definedFunctions = new Dictionary<string, FunctionInfo>();

    private FunctionInfo currentFunction = null;
    private Dictionary<string, VariableInfo> currentLocalScope = null; 

    private bool hasMain = false;

    public override object VisitProgram([NotNull] GrammarParser.ProgramContext context)
    {
        base.VisitProgram(context); 

        if (!hasMain)
        {
            AddError(context.Start.Line, "Programul nu contine o functie 'main'");
        }
        return null;
    }

    public override object VisitGlobalDeclaration([NotNull] GrammarParser.GlobalDeclarationContext context)
    {
        string type = context.type().GetText();
        string name = context.ID().GetText();
        bool isConst = context.CONST() != null;
        string initValue = context.initialValue() != null ? context.initialValue().GetText() : "null";

        if (globalScope.ContainsKey(name))
        {
            AddError(context.Start.Line, $"Variabila globala '{name}' este deja definita");
        }
        else
        {
            var v = new VariableInfo { Name = name, Type = type, Value = initValue, IsConst = isConst };
            globalScope.Add(name, v);
            GlobalVariables.Add(v);

            if (context.initialValue() != null)
            {
                CheckTypeCompatibility(context.Start.Line, type, context.initialValue());
            }
        }
        return null;
    }

        public override object VisitFunctionDefinition([NotNull] GrammarParser.FunctionDefinitionContext context)
        {
            string returnType = context.returnType().GetText();
            string name = context.ID().GetText();

            if (definedFunctions.ContainsKey(name))
            {
                AddError(context.Start.Line, $"Functia '{name}' este deja definita");
                return null;
            }

            currentFunction = new FunctionInfo { Name = name, ReturnType = returnType, IsMain = (name == "main") };
            currentLocalScope = new Dictionary<string, VariableInfo>();

            if (currentFunction.IsMain)
            {
                if (hasMain) AddError(context.Start.Line, "Functia 'main' este definita multiplu");
                hasMain = true;
                if (context.parameterList() != null)
                    AddError(context.Start.Line, "Functia 'main' nu trebuie sa aiba parametri");
            }

            if (context.parameterList() != null)
            {
                foreach (var param in context.parameterList().parameter())
                {
                    string pType = param.type().GetText();
                    string pName = param.ID().GetText();
                    currentFunction.Parameters.Add($"{pType} {pName}");

                    if (currentLocalScope.ContainsKey(pName))
                        AddError(param.Start.Line, $"Parametrul '{pName}' este duplicat");
                    else
                        currentLocalScope.Add(pName, new VariableInfo { Name = pName, Type = pType });
                }
            }

            definedFunctions.Add(name, currentFunction);
            Functions.Add(currentFunction);

            base.VisitFunctionDefinition(context);

            if (returnType != "void")
            {
                var block = context.block();
                bool endsWithReturn = false;

                if (block.statement().Length > 0)
                {
                    var lastStmt = block.statement()[block.statement().Length - 1];
                    if (lastStmt.returnStatement() != null)
                    {
                        endsWithReturn = true;
                    }
                }

                if (!endsWithReturn)
                {
                    AddError(context.Stop.Line, $"Functia '{name}' (tip {returnType}) nu se incheie cu o instructiune return.");
                }
            }
            currentFunction = null;
            currentLocalScope = null;

            return null;
        }

        public override object VisitDeclarationStatement([NotNull] GrammarParser.DeclarationStatementContext context)
    {
        if (currentFunction == null) return null; 

        string type = context.type().GetText();
        string name = context.ID().GetText();
        bool isConst = context.CONST() != null;

        if (currentLocalScope.ContainsKey(name))
        {
            AddError(context.Start.Line, $"Variabila locala '{name}' este redeclarata");
        }
        else
        {
            var v = new VariableInfo { Name = name, Type = type, IsConst = isConst, Value = context.expression()?.GetText() ?? "null" };
            currentLocalScope.Add(name, v);
            currentFunction.LocalVariables.Add(v);
        }

        if (context.expression() != null)
        {
            string exprType = GetExpressionType(context.expression());
            if (!AreTypesCompatible(type, exprType))
            {
                AddError(context.Start.Line, $"Nu se poate converti '{exprType}' la '{type}' în initializarea variabilei '{name}'");
            }
        }

        return base.VisitDeclarationStatement(context);
    }

    public override object VisitAssignmentStatement([NotNull] GrammarParser.AssignmentStatementContext context)
    {
        string varName = context.ID().GetText();
        VariableInfo varInfo = GetVariable(varName);

        if (varInfo == null)
        {
            AddError(context.Start.Line, $"Variabila '{varName}' nu este declarata");
        }
        else
        {
            if (varInfo.IsConst)
            {
                AddError(context.Start.Line, $"Nu se poate modifica variabila constanta '{varName}'");
            }

            // atribuire tip simpla
            if (context.expression() != null)
            {
                string exprType = GetExpressionType(context.expression());
                if (!AreTypesCompatible(varInfo.Type, exprType))
                {
                    AddError(context.Start.Line, $"Tip incompatibil la atribuire pentru '{varName}'. Asteptat: {varInfo.Type}, Primit: {exprType}");
                }
            }
        }
        return base.VisitAssignmentStatement(context);
    }

        public override object VisitCallStatement([NotNull] GrammarParser.CallStatementContext context)
        {
            string funcName = context.ID().GetText();

            if (funcName == "main")
            {
                AddError(context.Start.Line, "Functia 'main' nu poate fi apelata explicit.");
                return base.VisitCallStatement(context);
            }

            if (!definedFunctions.ContainsKey(funcName))
            {
                AddError(context.Start.Line, $"Functia '{funcName}' nu este definita.");
                return base.VisitCallStatement(context);
            }

            var funcDef = definedFunctions[funcName];
            var expressions = context.expressionList() != null ? context.expressionList().expression() : new GrammarParser.ExpressionContext[0];

            if (funcDef.Parameters.Count != expressions.Length)
            {
                AddError(context.Start.Line, $"Functia '{funcName}' asteapta {funcDef.Parameters.Count} argumente, dar a primit {expressions.Length}.");
            }
            else
            {
                for (int i = 0; i < funcDef.Parameters.Count; i++)
                {
                    string paramString = funcDef.Parameters[i];
                    string paramType = paramString.Split(' ')[0]; 

                    string argType = GetExpressionType(expressions[i]);

                    if (!AreTypesCompatible(paramType, argType))
                    {
                        AddError(context.Start.Line, $"Argumentul {i + 1} al functiei '{funcName}' este incompatibil. Asteptat: '{paramType}', Primit: '{argType}'.");
                    }
                }
            }

            if (currentFunction != null && funcName == currentFunction.Name)
            {
                currentFunction.IsRecursive = true;
            }

            return base.VisitCallStatement(context);
        }

        public override object VisitIfStatement([NotNull] GrammarParser.IfStatementContext context)
    {
        if (currentFunction != null) currentFunction.ControlStructures.Add($"if (linia {context.Start.Line})");
        return base.VisitIfStatement(context);
    }

    public override object VisitWhileStatement([NotNull] GrammarParser.WhileStatementContext context)
    {
        if (currentFunction != null) currentFunction.ControlStructures.Add($"while (linia {context.Start.Line})");
        return base.VisitWhileStatement(context);
    }

    public override object VisitForStatement([NotNull] GrammarParser.ForStatementContext context)
    {
        if (currentFunction != null) currentFunction.ControlStructures.Add($"for (linia {context.Start.Line})");
        return base.VisitForStatement(context);
    }


    private void AddError(int line, string msg)
    {
        SemanticErrors.Add($"Eroare Semantica (L{line}): {msg}");
    }

    private VariableInfo GetVariable(string name)
    {
        if (currentLocalScope != null && currentLocalScope.ContainsKey(name))
            return currentLocalScope[name];
        if (globalScope.ContainsKey(name))
            return globalScope[name];
        return null;
    }

    private void CheckTypeCompatibility(int line, string targetType, GrammarParser.InitialValueContext val)
    {
        if (val.STRING_LITERAL() != null && targetType != "string")
            AddError(line, $"Nu se poate initializa tipul '{targetType}' cu un string");
        if (val.NUMBER() != null && targetType == "string")
            AddError(line, $"Nu se poate initializa tipul 'string' cu un numar");
    }

        private string GetExpressionType(GrammarParser.ExpressionContext context)
        {
            if (context == null) return "void";

            if (context is GrammarParser.UnaryContext unaryCtx)
            {
                return GetUnaryExpressionType(unaryCtx.unaryExpression());
            }
            var leftExpr = context.GetRuleContext<GrammarParser.ExpressionContext>(0);
            var rightExpr = context.GetRuleContext<GrammarParser.ExpressionContext>(1);

            if (leftExpr != null && rightExpr != null)
            {
                string op = context.GetChild(1).GetText();

                if (op == "<" || op == ">" || op == "<=" || op == ">=" || op == "==" || op == "!=" ||
                    op == "&&" || op == "||")
                {
                    return "bool";
                }

                var leftType = GetExpressionType(leftExpr);
                var rightType = GetExpressionType(rightExpr);

                if (leftType == "string" || rightType == "string") return "string"; 
                if (leftType == "double" || rightType == "double") return "double";
                if (leftType == "float" || rightType == "float") return "float";
                if (leftType == "int" && rightType == "int") return "int";

                return "float"; 
            }

            return "unknown";
        }

        private string GetUnaryExpressionType(GrammarParser.UnaryExpressionContext context)
        {
            if (context == null) return "void";
            if (context is GrammarParser.UnaryOperatorContext unaryOpCtx)
            {
                if (unaryOpCtx.NOT() != null) return "bool"; 
                return GetPrimaryType(unaryOpCtx.primary());
            }

            if (context is GrammarParser.PreIncrementContext preIncCtx)
                return GetPrimaryType(preIncCtx.primary());

            if (context is GrammarParser.PreDecrementContext preDecCtx)
                return GetPrimaryType(preDecCtx.primary());

            if (context is GrammarParser.PostIncrementDecrementContext postCtx)
                return GetPrimaryType(postCtx.primary());

            return "unknown";
        }

        private string GetPrimaryType(GrammarParser.PrimaryContext context)
        {
            if (context == null) return "void";

            if (context.ID() != null)
            {
                var v = GetVariable(context.ID().GetText());
                return v != null ? v.Type : "unknown";
            }

            if (context.initialValue() != null)
            {
                if (context.initialValue().STRING_LITERAL() != null) return "string";
                string numText = context.initialValue().NUMBER().GetText();
                if (numText.Contains(".") || numText.ToLower().Contains("e")) return "float";
                return "int";
            }

            if (context.expression() != null)
            {
                return GetExpressionType(context.expression());
            }

            if (context.callStatement() != null)
            {
                string funcName = context.callStatement().ID().GetText();
                if (definedFunctions.ContainsKey(funcName))
                    return definedFunctions[funcName].ReturnType;
                return "unknown";
            }

            return "unknown";
        }
        public override object VisitReturnStatement([NotNull] GrammarParser.ReturnStatementContext context)
        {
            if (currentFunction == null)
            {
                AddError(context.Start.Line, "Instructiunea 'return' folosita in afara unei functii.");
                return null;
            }

            string returnType = currentFunction.ReturnType;
            bool hasExpression = context.expression() != null;

            if (returnType == "void")
            {
                if (hasExpression)
                {
                    AddError(context.Start.Line, $"Functia '{currentFunction.Name}' este de tip void si nu poate returna o valoare.");
                }
            }

            else
            {
                if (!hasExpression)
                {
                    AddError(context.Start.Line, $"Functia '{currentFunction.Name}' trebuie sa returneze o valoare de tip '{returnType}'.");
                }
                else
                {
                    string exprType = GetExpressionType(context.expression());
                    if (!AreTypesCompatible(returnType, exprType))
                    {
                        AddError(context.Start.Line, $"Tip returnat incompatibil. Asteptat: '{returnType}', Gasit: '{exprType}'.");
                    }
                }
            }

            return base.VisitReturnStatement(context);
        }


        private bool AreTypesCompatible(string target, string source)
        {
            if (target == source) return true;
            if (source == "unknown" || source == "error") return true; 

            if (target == "string") return source == "string";
            if (source == "string") return target == "string";

            if (target == "double" && (source == "float" || source == "int")) return true;
            if (target == "float" && source == "int") return true;

            if (target == "int" && source == "bool") return true; 

            return false;
        }
    }
}