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
        private readonly string _doubleQuotePlaceholder;
        private readonly string _separatorPlaceholder;

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

                var dataWithoutSpecialCharacters = ReplaceSpecialCharacters(currentRow);
                var columns = dataWithoutSpecialCharacters.Split(ColumnSeparator);
                string rowAsJson = string.Empty;
                rowAsJson += GetRowPrefix();
                rowAsJson += GetData(headers, columns);
                rowAsJson += GetRowSuffix();
                result += $"{rowAsJson}";
                result += GetRowSeparator(i, rows.Length);
                currentRow = string.Empty;
            }

            result += "]";

            return result;
        }

        private string GetData(List<string> headers, string[] columns)
        {
            string column = string.Empty;
            var numberOfColumns = GetNumberOfColumns(columns.Length, headers.Count);
            for (int j = 0; j < numberOfColumns; j++)
            {
                column += GetColumnPrefix(headers, j);
                column += GetColumnData(columns[j]);
                column += GetColumnSuffix(j, columns.Length);
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

        private int GetNumberOfColumns(int numberOfColumns, int numberOfHeaders)
        {
            if (_includeHeaders && numberOfColumns != numberOfHeaders)
            {
                throw new InvalidOperationException("Number of column should be same as number of headers otherwise data can be lost!");
            }

            return _includeHeaders ? numberOfHeaders : numberOfColumns;
        }

        private static string GetRowSeparator(int i, int numberOfRows)
        {
            if (i < numberOfRows - 1)
            {
                return ",";
            }

            return string.Empty;
        }

        private static string GetColumnSuffix(int j, int numberOfColumns)
        {
            if (j < numberOfColumns - 1)
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
                if (column.Contains("."))
                {
                    result = columnAsDecimal.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    result = ((int)columnAsDecimal).ToString();
                }
            }
            else
            {
                var restoreDoubleQuotes = column.Replace(_doubleQuotePlaceholder, $"\"\"");
                var restoreEscapedSeparators = restoreDoubleQuotes.Replace(_separatorPlaceholder, ColumnSeparator.ToString());
                result = $"\"{HttpUtility.JavaScriptStringEncode(restoreEscapedSeparators)}\"";
            }

            return result;
        }

        private string ReplaceSpecialCharacters(string row)
        {
            string result = string.Empty;
            var replaceDoubleQuotes = row.Replace("\"\"", _doubleQuotePlaceholder);
            int currentPosition = 0;
            int nextPosition = replaceDoubleQuotes.IndexOf(ColumnSeparator);
            while (nextPosition != -1)
            {
                var a = replaceDoubleQuotes.Substring(currentPosition, nextPosition - currentPosition);
                if (a.Count(e => e == '"') % 2 != 0)
                {
                    var b = result + a;
                    if (result.Length > 0 && b.Count(e => e == '"') % 2 == 0)
                    {
                        result += a + ColumnSeparator;
                    }
                    else
                    {
                        result += a + _separatorPlaceholder;
                    }
                }
                else
                {
                    result += a + ColumnSeparator;
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
