using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace NServiceBus.Profiler.Common.CodeParser
{
    public class JsonParser : BaseParser
    {
        protected static readonly char[] JsonSymbol = new[] { ':', '[', ']', ',', '{', '}' };
        protected static readonly char[] JsonQuotes = new[] { '"' };

        protected bool IsInsideBlock;

        protected override List<CodeLexem> Parse(SourcePart text)
        {
            var list = new List<CodeLexem>();

            while (text.Length > 0)
            {
                var lenght = text.Length;

                TryExtract(list, ref text, ByteOrderMark);
                TryExtract(list, ref text, "[", LexemType.Symbol);
                TryExtract(list, ref text, "{", LexemType.Symbol);

                if (TryExtract(list, ref text, "\"", LexemType.Quotes))
                {
                    ParseJsonPropertyName(list, ref text); // Extract Name
                    TryExtract(list, ref text, "\"", LexemType.Quotes);
                    TryExtract(list, ref text, ":", LexemType.Symbol);
                    TrySpace(list, ref text);
                    TryExtractValue(list, ref text); // Extract Value
                }

                ParseSymbol(list, ref text); // Parse extras
                TrySpace(list, ref text);
                TryExtract(list, ref text, "\r\n", LexemType.LineBreak);
                TryExtract(list, ref text, "\n", LexemType.LineBreak);
                TryExtract(list, ref text, "}", LexemType.Symbol);
                TryExtract(list, ref text, "]", LexemType.Symbol);

                if (lenght == text.Length)
                    break;
            }

            return list;
        }

        private void TryExtractValue(List<CodeLexem> res, ref SourcePart text)
        {
            if (text[0] == '{')
            {
                res.Add(new CodeLexem(LexemType.Symbol, CutString(ref text, 1)));
            }
            else if (text[0] == '[')
            {
                res.Add(new CodeLexem(LexemType.Symbol, CutString(ref text, 1)));
            }
            else if (text[0] == '"')
            {
                res.Add(new CodeLexem(LexemType.Quotes, CutString(ref text, 1)));
                //var end = text.IndexOf('"');
                var end = FindEndQuoteForValue(text);
                res.Add(new CodeLexem(LexemType.Value, CutString(ref text, end)));
                res.Add(new CodeLexem(LexemType.Quotes, CutString(ref text, 1)));
            }
            else
            {
                var end = text.IndexOfAny(new[] { ',', '}' });
                res.Add(new CodeLexem(LexemType.Value, CutString(ref text, end)));
                res.Add(new CodeLexem(LexemType.Symbol, CutString(ref text, 1)));
            }
        }

        private int FindEndQuoteForValue(SourcePart text)
        {
          if (text == null || text.Length < 2)
            return -1;

          var start = 1;
          var end = -1;
          var length = text.Length;
          var indexMatch = false;
          while (!indexMatch && start > 0 && end < text.Length)
          {
            end = text.Substring(start, length - start).IndexOf("\"") + start;
            if (end != -1)
            {
              indexMatch = text.Substring(end - 1, 1).IndexOf("\\") == -1;
            }
            start = end + 1;
          }

          return indexMatch ? end : -1;
        }
        private void ParseSymbol(ICollection<CodeLexem> res, ref SourcePart text)
        {
            var index = text.IndexOfAny(JsonSymbol);
            if (index != 0)
                return;

            res.Add(new CodeLexem(LexemType.Symbol, text.Substring(0, 1)));
            text = text.Substring(1);
        }

        private void ParseJsonPropertyName(ICollection<CodeLexem> res, ref SourcePart text)
        {
            var index = text.IndexOf("\":");
            if (index <= 0)
                return;

            res.Add(new CodeLexem(LexemType.Property, CutString(ref text, index)));
        }

        public override Inline ToInline(CodeLexem codeLexem)
        {
            switch (codeLexem.Type)
            {
                case LexemType.Symbol:
                case LexemType.Object:
                    return CreateRun(codeLexem.Text, Colors.LightGray);
                case LexemType.Property:
                    return CreateRun(codeLexem.Text, Colors.Blue);
                case LexemType.Value:
                case LexemType.Space:
                case LexemType.PlainText:
                case LexemType.String:
                    return CreateRun(codeLexem.Text, Colors.Black);
                case LexemType.LineBreak:
                    return new LineBreak();
                case LexemType.Complex:
                case LexemType.Quotes:
                    return CreateRun(codeLexem.Text, Colors.Brown);
            }

            throw new NotImplementedException(string.Format("Lexem type {0} has no specific colors.", codeLexem.Type));
        }
    }
}