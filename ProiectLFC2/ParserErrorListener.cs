using Antlr4.Runtime;
using System;
using System.IO;

namespace ProiectLFC2
{
    public class ParserErrorListener : BaseErrorListener
    {
        private const string ErrorFilePath = "compiler_errors.txt";
        public int ErrorCount { get; private set; } = 0;
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string translatedMessage = TranslateErrorMessage(msg);
            string errorMessage = $"Eroare sintactica (L{line}:{charPositionInLine}): {translatedMessage}";
            ErrorCount++;
            try
            {
                File.AppendAllText(ErrorFilePath, errorMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la scrierea în fisier: " + ex.Message);
            }
        }

            private string TranslateErrorMessage(string originalMsg)
            {
            return originalMsg
                .Replace("extraneous input", "intrare neasteptata (in plus)")
                .Replace("expecting", "se astepta")
                .Replace("missing", "lipseste")
                .Replace("mismatched input", "intrare nepotrivita")
                .Replace("no viable alternative at input", "nu exista o alternativa valida la intrare")
                .Replace("at input", "la intrare")
                .Replace("rule", "regula")
                .Replace("extraneous", "strain/in plus");
            }
    }
}