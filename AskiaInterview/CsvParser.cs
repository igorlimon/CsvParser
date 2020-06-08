using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace AskiaInterview
{
    public class CsvParser
    {
        private const char ColumnSeparator = '#';
        private bool _includeHeaders;
        private string _doubleQuotePlaceholder;
        private string _separatorPlaceholder;

        public CsvParser()
        {
            _doubleQuotePlaceholder = Guid.NewGuid().ToString().Replace("-", string.Empty);
            _separatorPlaceholder = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        public string Parse(string data, bool includeHeader = false)
        {
            _includeHeaders = includeHeader;
            ValidateDataForNull(data);
            CheckUniquePlaceHolder(data);

            if (string.IsNullOrWhiteSpace(data))
            {
                return "[]";
            }

            var result = "[";
            var headers = new List<string>();
            var rows = data.Split('\n');
            string currentRow = string.Empty;
            bool foundBeginOfEmbeddedLineBreak = false;
            bool foundEndOfEmbeddedLineBreak = false;
            for (int i = 0; i < rows.Length; i++)
            {
                if (_includeHeaders && i == 0)
                {
                    headers = rows[i].Split(ColumnSeparator).Select(RemoveLineEnds).ToList();
                    continue;
                }

                if (foundBeginOfEmbeddedLineBreak)
                {
                    foundEndOfEmbeddedLineBreak = rows[i].Count(e => e == '"') % 2 != 0;
                }

                if (!foundBeginOfEmbeddedLineBreak)
                {
                    foundBeginOfEmbeddedLineBreak = rows[i].Count(e => e == '"') % 2 != 0;
                }

                if (foundBeginOfEmbeddedLineBreak && !foundEndOfEmbeddedLineBreak)
                {
                    currentRow += rows[i];
                    continue;
                }

                foundBeginOfEmbeddedLineBreak = false;
                foundEndOfEmbeddedLineBreak = false;
                currentRow += RemoveLineEnds(rows[i]);

                var dataWithoutSpecialCharacters = EscapeSpecialCharacters(currentRow);
                var columns = dataWithoutSpecialCharacters.Split(ColumnSeparator);
                string rowAsJson = string.Empty;
                rowAsJson += GetRowPrefix();
                rowAsJson += GetData(headers, columns);
                rowAsJson += GetRowSuffix();
                result += $"{rowAsJson}";
                result += GetItemSeparator(i, rows.Length);
                currentRow = string.Empty;
            }

            result += "]";

            return result;
        }

        private string GetData(List<string> headers, string[] columns)
        {
            string column = string.Empty;
            ValidateNumberOfColumns(columns.Length, headers.Count);
            for (int j = 0; j < columns.Length; j++)
            {
                column += GetColumnPrefix(headers, j);
                column += GetColumnData(columns[j]);
                column += GetItemSeparator(j, columns.Length);
            }

            return column;
        }

        private string GetColumnPrefix(List<string> headers, int j)
        {
            if (_includeHeaders)
            {
                return $"\"{headers[j]}\":";
            }

            return string.Empty;
        }

        private char GetRowSuffix()
        {
            return _includeHeaders ? '}' : ']';
        }

        private char GetRowPrefix()
        {
            return _includeHeaders ? '{' : '[';
        }

        private void ValidateNumberOfColumns(int numberOfColumns, int numberOfHeaders)
        {
            if (_includeHeaders && numberOfColumns != numberOfHeaders)
            {
                throw new InvalidOperationException("Number of column should be same as number of headers otherwise data can be lost!");
            }
        }

        private static string GetItemSeparator(int index, int numberOfRows)
        {
            if (index < numberOfRows - 1)
            {
                return ",";
            }

            return string.Empty;
        }

        private string GetColumnData(string column)
        {
            string result;
            if (decimal.TryParse(column, out decimal columnAsDecimal))
            {
                result = column.Contains(".") ? columnAsDecimal.ToString(CultureInfo.InvariantCulture) : ((int)columnAsDecimal).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var restoreDoubleQuotes = column.Replace(_doubleQuotePlaceholder, $"\"\"");
                var restoreEscapedSeparators = restoreDoubleQuotes.Replace(_separatorPlaceholder, ColumnSeparator.ToString());
                result = $"\"{HttpUtility.JavaScriptStringEncode(restoreEscapedSeparators)}\"";
            }

            return result;
        }

        private string EscapeSpecialCharacters(string row)
        {
            string result = string.Empty;
            var replaceDoubleQuotes = row.Replace("\"\"", _doubleQuotePlaceholder);
            int currentPosition = 0;
            int nextPosition = replaceDoubleQuotes.IndexOf(ColumnSeparator);
            while (nextPosition != -1)
            {
                var column = replaceDoubleQuotes.Substring(currentPosition, nextPosition - currentPosition);
                var existUnClosedQuote = column.Count(e => e == '"') % 2 != 0;
                if (existUnClosedQuote)
                {
                    string parsedData = result + column;
                    bool isFirstLine = result.Length == 0;
                    bool isQuoteClosed = parsedData.Count(e => e == '"') % 2 == 0;
                    if (!isFirstLine && isQuoteClosed)
                    {
                        result += column + ColumnSeparator;
                    }
                    else
                    {
                        result += column + _separatorPlaceholder;
                    }
                }
                else
                {
                    result += column + ColumnSeparator;
                }

                currentPosition = nextPosition + 1;
                nextPosition = replaceDoubleQuotes.IndexOf(ColumnSeparator, currentPosition);
            }

            result += replaceDoubleQuotes.Substring(currentPosition);
            return result;
        }

        private static string RemoveLineEnds(string row)
        {
            var replaceCarriageReturn = row.Replace("\n", string.Empty);
            var replaceLineFeed = replaceCarriageReturn.Replace("\r", string.Empty);
            return replaceLineFeed;
        }

        private void CheckUniquePlaceHolder(string data)
        {
            if (data.Contains(_doubleQuotePlaceholder))
            {
                // TODO implement more robust algorithm to find unique place holder for current row.
                _doubleQuotePlaceholder = Guid.NewGuid().ToString().Replace("-", string.Empty);
                _separatorPlaceholder = Guid.NewGuid().ToString().Replace("-", string.Empty);
            }
        }

        private static void ValidateDataForNull(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
        }
    }
}
