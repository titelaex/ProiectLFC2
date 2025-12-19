using Antlr4.Runtime;
using System;
using System.IO;

namespace ProiectLFC2
{
    public class ParserErrorListener : BaseErrorListener
    {
        private const string ErrorFilePath = "compiler_errors.txt";

        // Varianta CORECTĂ pentru versiunea ta de ANTLR (fără TextWriter)
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string errorMessage = $"Eroare sintactica (L{line}:{charPositionInLine}): {msg}";

            Console.WriteLine(errorMessage); // Afișăm în consolă
            try
            {
                // Scriem în fișier
                File.AppendAllText(ErrorFilePath, errorMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la scrierea în fisier: " + ex.Message);
            }
        }
    }
}