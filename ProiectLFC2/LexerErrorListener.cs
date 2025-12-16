using Antlr4.Runtime;
using System;
using System.IO;

public class LexerErrorListener : IAntlrErrorListener<int>
{
    private const string ErrorFilePath = "compiler_errors.txt";

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        // Aici ajungem doar pentru erori fundamentale, când Lexerul nu știe ce să facă cu un caracter
        // și nu s-a potrivit cu nicio regulă (nici măcar cu ERROR_TOKEN din .g4).

        string errorMessage = $"Eroare Lexicală Critică (L{line}:{charPositionInLine}): {msg}";

        Console.WriteLine(errorMessage);
        try
        {
            File.AppendAllText(ErrorFilePath, errorMessage + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Nu s-a putut scrie în fișierul de erori: " + ex.Message);
        }
    }
}