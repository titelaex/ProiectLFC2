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
                AddError(context.Start.Line, $"Functia '{funcName}' nu este definita (sau este definita dupa apel).");
                return base.VisitCallStatement(context); 
            }

            var funcDef = definedFunctions[funcName];
            int paramCount = funcDef.Parameters.Count;
            int argCount = context.expressionList() != null ? context.expressionList().expression().Length : 0;

            if (paramCount != argCount)
            {
                AddError(context.Start.Line, $"Functia '{funcName}' asteapta {paramCount} argumente, dar a primit {argCount}.");
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
        string rawText = context.GetText();
        if (rawText.Contains("\"")) return "string";
        if (rawText.Contains("true") || rawText.Contains("false") || rawText.Contains("==") || rawText.Contains("<")) return "bool";

        if (context.ChildCount == 1 && context.GetChild(0) is GrammarParser.PrimaryContext prim)
        {
            if (prim.ID() != null)
            {
                var v = GetVariable(prim.ID().GetText());
                return v != null ? v.Type : "unknown";
            }
            if (prim.initialValue() != null)
            {
                if (prim.initialValue().STRING_LITERAL() != null) return "string";
                return "float"; 
            }
        }

        return "float"; 
    }

    private bool AreTypesCompatible(string target, string source)
    {
        if (target == "string" && source != "string") return false;
        if (target != "string" && source == "string") return false;
        return true;
    }
}
}