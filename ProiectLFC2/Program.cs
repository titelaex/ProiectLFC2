using Antlr4.Runtime;
using System;
using System.IO;
using System.Text;

public class Program
{
    private const string SourceFilePath = "source.minilang";
    private const string TokensOutputFilePath = "token_list.txt";
    private const string ErrorFilePath = "compiler_errors.txt";

    public static void Main(string[] args)
    {
        try
        {
            // Curățăm fișierul de erori la fiecare rulare
            if (File.Exists(ErrorFilePath)) File.WriteAllText(ErrorFilePath, string.Empty);

            string sourceCode = File.ReadAllText(SourceFilePath);

            // --- ANALIZA LEXICALĂ ---
            AntlrInputStream inputStream = new AntlrInputStream(sourceCode);
            GrammarLexer lexer = new GrammarLexer(inputStream);
            
            // Aatașăm listener-ul corectat
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener());

            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            tokenStream.Fill(); // Forțăm încărcarea tuturor token-urilor

            // Procesăm token-urile pentru a salva lista și a găsi erorile specifice
            ProcessTokens(tokenStream);

            // --- ANALIZA SINTACTICĂ (Persoana 2) ---
            GrammarParser parser = new GrammarParser(tokenStream);
            // ... restul logicii pentru parser
             Console.WriteLine("Compilare terminată.");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Fișierul '{SourceFilePath}' nu există.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Eroare: " + ex.Message);
        }
    }

    private static void ProcessTokens(CommonTokenStream tokenStream)
    {
        StringBuilder tokenListOutput = new StringBuilder();

        foreach (var token in tokenStream.GetTokens())
        {
            // 1. Gestionarea Erorilor Lexicale Specifice (definite în .g4)
            if (token.Type == GrammarLexer.ERROR_TOKEN)
            {
                ReportError(token.Line, $"Caracter nepermis: '{token.Text}'");
            }

            // 2. Salvarea în lista de token-uri (doar cele valide, ignorând EOF)
            if (token.Type != GrammarLexer.Eof && token.Channel == Lexer.DefaultTokenChannel)
            {
                string tokenName = GrammarLexer.DefaultVocabulary.GetSymbolicName(token.Type);
                // Dacă e unul din token-urile noastre de eroare, îl marcăm ca ERROR în lista de output
                if (token.Type == GrammarLexer.ERROR_TOKEN)
                {
                    tokenName = "ERROR";
                }
                
                tokenListOutput.AppendLine($"<{tokenName}, \"{token.Text}\", {token.Line}>");
            }
        }

        File.WriteAllText(TokensOutputFilePath, tokenListOutput.ToString());
        Console.WriteLine($"Lista de unități lexicale salvată în {TokensOutputFilePath}");
    }

    private static void ReportError(int line, string message)
    {
        string fullMessage = $"Eroare Lexicală (L{line}): {message}";
        Console.WriteLine(fullMessage);
        File.AppendAllText(ErrorFilePath, fullMessage + Environment.NewLine);
    }
}