using Antlr4.Runtime;
using System;
using System.IO;

namespace ProiectLFC2
{
    public class LexerErrorListener : IAntlrErrorListener<int>
    {
        private const string ErrorFilePath = "compiler_errors.txt";

        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string errorMessage = $"Eroare lexicala critica (L{line}:{charPositionInLine}): {msg}";

            Console.WriteLine(errorMessage);
            try
            {
                File.AppendAllText(ErrorFilePath, errorMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nu s-a putut scrie în fisierul de erori: " + ex.Message);
            }
        }
    }
}