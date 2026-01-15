using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.IO;
using System.Text;

namespace ProiectLFC2
{
    public class Program
    {
        private const string SourceFilePath = "source.minilang";
        private const string TokensOutputFilePath = "token_list.txt";
        private const string ErrorFilePath = "compiler_errors.txt";
        private const string GlobalVarsFilePath = "global_variables.txt";
        private const string FunctionsFilePath = "functions.txt";

        public static void Main(string[] args)
        {
            try
            {
                File.WriteAllText(ErrorFilePath, string.Empty);

                if (!File.Exists(SourceFilePath))
                {
                    Console.WriteLine($"Fisierul '{SourceFilePath}' nu exista");
                    return;
                }

                string sourceCode = File.ReadAllText(SourceFilePath);

                //  ANALIZA LEXICALA 
                AntlrInputStream inputStream = new AntlrInputStream(sourceCode);
                GrammarLexer lexer = new GrammarLexer(inputStream);
                lexer.RemoveErrorListeners();

                CommonTokenStream tokenStream = new CommonTokenStream(lexer);
                tokenStream.Fill();

                int lexicalErrorCount = ProcessTokens(tokenStream);

                if (lexicalErrorCount > 0)
                {
                    Console.WriteLine($"Au fost gasite {lexicalErrorCount} erori lexicale. Verifica {ErrorFilePath}");
                }

                // ANALIZA SINTACTICA 
                GrammarParser parser = new GrammarParser(tokenStream);
                parser.RemoveErrorListeners();

                ParserErrorListener syntaxListener = new ParserErrorListener();
                parser.AddErrorListener(syntaxListener);

                var tree = parser.program();
                if (syntaxListener.ErrorCount > 0)
                {
                    Console.WriteLine($"Au fost gasite {syntaxListener.ErrorCount} erori sintactice. Verifica {ErrorFilePath}");
                }

                // ANALIZA SEMANTICA 
                SemanticVisitor visitor = new SemanticVisitor();
                visitor.Visit(tree);

                if (visitor.SemanticErrors.Count > 0)
                {
                    File.AppendAllLines(ErrorFilePath, visitor.SemanticErrors);
                    Console.WriteLine($"Au fost gasite {visitor.SemanticErrors.Count} erori semantice. Verifica {ErrorFilePath}");
                }

                StringBuilder globalSb = new StringBuilder();
                foreach (var v in visitor.GlobalVariables)
                {
                    globalSb.AppendLine($"Nume: {v.Name}, Tip: {v.Type}, Valoare: {v.Value}, Const: {v.IsConst}");
                }
                File.WriteAllText(GlobalVarsFilePath, globalSb.ToString());

                StringBuilder funcSb = new StringBuilder();
                foreach (var f in visitor.Functions)
                {
                    funcSb.AppendLine($"Functie: {f.Name} (Tip: {(f.IsMain ? "Main" : "Non-Main")}, Recursiva: {f.IsRecursive})");
                    funcSb.AppendLine($"  Return: {f.ReturnType}");
                    funcSb.AppendLine($"  Parametri: {string.Join(", ", f.Parameters)}");
                    funcSb.AppendLine("  Variabile Locale:");
                    foreach (var l in f.LocalVariables)
                    {
                        funcSb.AppendLine($"    - {l.Type} {l.Name} (= {l.Value})");
                    }
                    funcSb.AppendLine("  Structuri Control:");
                    foreach (var c in f.ControlStructures)
                    {
                        funcSb.AppendLine($"    - {c}");
                    }
                    funcSb.AppendLine("------------------------------------------------");
                }
                File.WriteAllText(FunctionsFilePath, funcSb.ToString());

                Console.WriteLine("\nAnaliza completa. Fisiere generate: token_list.txt, global_variables.txt, functions.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare critica: " + ex.Message);
            }
        }

        private static int ProcessTokens(CommonTokenStream tokenStream)
        {
            StringBuilder tokenListOutput = new StringBuilder();
            int errorCount = 0; 

            foreach (var token in tokenStream.GetTokens())
            {
                if (token.Type == GrammarLexer.ERROR_TOKEN)
                {
                    string errorMsg = $"Eroare Lexicala (L{token.Line}): Caracter neasteptat sau text invalid: '{token.Text}'";
                    File.AppendAllText(ErrorFilePath, errorMsg + Environment.NewLine);
                    errorCount++; 
                }

                if (token.Type != GrammarLexer.Eof && token.Channel == Lexer.DefaultTokenChannel)
                {
                    string tokenName = GrammarLexer.DefaultVocabulary.GetSymbolicName(token.Type);
                    if (token.Type == GrammarLexer.ERROR_TOKEN) tokenName = "ERROR";
                    tokenListOutput.AppendLine($"<{tokenName}, \"{token.Text}\", {token.Line}>");
                }
            }
            File.WriteAllText(TokensOutputFilePath, tokenListOutput.ToString());

            return errorCount;
        }
    }
}