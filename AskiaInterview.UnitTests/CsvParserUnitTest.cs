using System;
using System.Collections.Generic;
using System.Linq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Shouldly;
using Xunit;

namespace AskiaInterview.UnitTests
{
    public class CsvParserUnitTest
    {
        private const string ColumnSeparator = "#";

        [Fact]
        public void Parse_PassNullData_ThrowNullArgumentException()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string nullInstance = default;
            var sut = fixture.Freeze<CsvParser>();

            // act & arrange
            var expectedException = Should.Throw<ArgumentNullException>(() =>
            {
                sut.Parse(nullInstance);
            });
            expectedException.Message.ShouldContain("'data'");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        [InlineData("\r")]
        [InlineData("\n")]
        public void Parse_PassEmptyData_ReturnEmptyArray(string data)
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.ShouldBe("[]");
        }

        [Fact]
        public void Parse_OneRowWithOneColumn_OnlyOneValueIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string row = CreateRow(1);
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.ShouldBe($"[[\"{data}\"]]");
        }

        [Fact]
        public void Parse_RowWithDecimal_NumberWithoutQuotesIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var decimalNumber = 123.001d;
            string row = $"{decimalNumber}";
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.ShouldBe($@"[[{data}]]");
        }

        [Fact]
        public void Parse_RowWithInteger_NumberWithoutQuotesIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var column = fixture.Create<int>();
            string row = $"{column}";
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.ShouldBe($@"[[{data}]]");
        }

        [Fact]
        public void Parse_RowWithStringThatContainNumber_StringWithQuotesIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var number = fixture.Create<int>();
            var text = fixture.Create<string>();
            string row = $"{number}___{text}";
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.ShouldBe($"[[\"{data}\"]]");
        }

        [Fact]
        public void Parse_TwoRowsWithOneColumn_TwoRowsAreReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string row1 = CreateRow(1);
            string row2 = CreateRow(1);
            string data = $@"{row1}
{row2}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedRow1 = GetJsonArray(row1);
            var expectedRow2 = GetJsonArray(row2);
            string expectedResult = $@"[{expectedRow1},{expectedRow2}]";
            result.ShouldBe(expectedResult);
        }

        [Fact]
        public void Parse_OneRowWithTwoColumns_OneRowWithTwoValuesIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string row = CreateRow(2);
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedRow = GetJsonArray(row);
            result.ShouldBe($"[{expectedRow}]");
        }

        [Fact]
        public void Parse_TwoRowsWithTwoColumns_TwoRowsWithTwoValuesAreReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string row1 = CreateRow(2);
            string row2 = CreateRow(2);
            string data = $@"{row1}
{row2}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedRow1 = GetJsonArray(row1);
            var expectedRow2 = GetJsonArray(row2);
            string expectedResult = $@"[{expectedRow1},{expectedRow2}]";
            result.ShouldBe($"{expectedResult}");
        }

        [Fact]
        public void Parse_ManyRowsWithManyColumns_SameNumberOfRowsWithSameNumberOfColumnsAreReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            Random rnd = new Random();
            int numberOfRows = rnd.Next(1, 1);
            int numberOfColumns = rnd.Next(1, 1);
            string data = string.Empty;
            var rows = new List<string>();
            for (int i = 0; i < numberOfRows; i++)
            {
                string row = CreateRow(numberOfColumns);
                rows.Add(row);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                data += rows[i];
                if (i < rows.Count - 1)
                    data += @"
";
            }
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            string expectedResult = string.Empty;
            for (int i = 0; i < rows.Count; i++)
            {
                expectedResult += GetJsonArray(rows[i]);
                if (i < rows.Count - 1)
                {
                    expectedResult += ",";
                }
            }
            result.ShouldBe($"[{expectedResult}]", $"Failed parse of {numberOfRows} of rows with {numberOfColumns} each one.");
        }

        [Fact]
        public void Parse_OneRowWithOneColumnThatContainCommaWithinQuote_OneRowWithOneColumnIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = GetColumnWithCommaWithinQuote();
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedNumberOfArraySquareBrackets = 2;
            result.Count(el => el == '[').ShouldBe(expectedNumberOfArraySquareBrackets);
            result.Count(el => el == ']').ShouldBe(expectedNumberOfArraySquareBrackets);
            var expectedNumberOfArraySeparators = 0;
            result.Count(el => el == ',').ShouldBe(expectedNumberOfArraySeparators);
        }

        [Fact]
        public void Parse_OneRowWithTwoColumnThatContainCommaWithinQuote_OneRowWithTwoColumnsIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var column1WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var column2WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var row = $"{column1WithCommaWithinQuote}{ColumnSeparator}{column2WithCommaWithinQuote}";
            string data = $"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedNumberOfArraySquareBrackets = 2;
            result.Count(el => el == '[').ShouldBe(expectedNumberOfArraySquareBrackets);
            result.Count(el => el == ']').ShouldBe(expectedNumberOfArraySquareBrackets);
            var expectedNumberOfArraySeparators = 1;
            result.Count(el => el == ',').ShouldBe(expectedNumberOfArraySeparators);
        }

        [Fact]
        public void Parse_TwoRowWithOneColumnThatContainCommaWithinQuote_TwoRowsWithOneColumnAreReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var row1 = GetColumnWithCommaWithinQuote();
            var row2 = GetColumnWithCommaWithinQuote();
            string data = @$"{row1}
{row2}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedNumberOfArraySquareBrackets = 3;
            result.Count(el => el == '[').ShouldBe(expectedNumberOfArraySquareBrackets);
            result.Count(el => el == ']').ShouldBe(expectedNumberOfArraySquareBrackets);
            var expectedNumberOfArraySeparators = 1;
            result.Count(el => el == ',').ShouldBe(expectedNumberOfArraySeparators);
        }

        [Fact]
        public void Parse_TwoRowsWithTwoColumnThatContainCommaWithinQuote_OnlyOneValueIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var column11WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var column12WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var column21WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var column22WithCommaWithinQuote = GetColumnWithCommaWithinQuote();
            var row1 = $"{column11WithCommaWithinQuote}{ColumnSeparator}{column12WithCommaWithinQuote}";
            var row2 = $"{column21WithCommaWithinQuote}{ColumnSeparator}{column22WithCommaWithinQuote}";
            string data = @$"{row1}
{row2}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedNumberOfArraySquareBrackets = 3;
            result.Count(el => el == '[').ShouldBe(expectedNumberOfArraySquareBrackets);
            result.Count(el => el == ']').ShouldBe(expectedNumberOfArraySquareBrackets);
            var expectedNumberOfArraySeparators = 3;
            result.Count(el => el == ',').ShouldBe(expectedNumberOfArraySeparators);
        }

        [Fact]
        public void Parse_RowWithDoubleQuote_DoubleQuoteIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            var columnData = fixture.Create<string>();
            string row = $"\"\"{columnData}";
            string data = @$"{row}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            string expectedResult = $@"[[""\""\""{columnData}""]]";
            result.ShouldBe($"{expectedResult}");
        }

        [Fact]
        public void Parse_ParseData_AnArrayIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"Ab{ColumnSeparator}12";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            result.First().ShouldBe('[');
            result.Last().ShouldBe(']');
        }

        [Fact]
        public void Parse_ParseTwoRowsWithoutHeader_AnArrayWithTwoArraysIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string row1 = @"Abc";
            string row2 = @"def";
            string data = @$"{row1}
{row2}";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedNumberOfArraySquareBrackets= 3;
            result.Count(el => el == '[').ShouldBe(expectedNumberOfArraySquareBrackets);
            result.Count(el => el == ']').ShouldBe(expectedNumberOfArraySquareBrackets);
            var expectedNumberOfArraySeparators = 1;
            result.Count(el => el == ',').ShouldBe(expectedNumberOfArraySeparators);
        }

        [Fact]
        public void Parse_ParseDataWithOneHeader_AnArrayWithObjectThatHasOnePropertyIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"prop1
Ab";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act
            string result = sut.Parse(data, includeHeader);

            // arrange
            var expectedObject = "[{\"prop1\":\"Ab\"}]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_ParseDataWithOnlyHeader_AnEmptyArrayIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = "prop1";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act
            string result = sut.Parse(data, includeHeader);

            // arrange
            var expectedObject = @"[]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_ParseDataWithOneHeaderAndTwoColumns_ExceptionIsThrown()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"prop1
Ab{ColumnSeparator}De";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act & arrange
            Should.Throw<InvalidOperationException>(() =>
            {
                sut.Parse(data, includeHeader);
            });
        }

        [Fact]
        public void Parse_ParseDataWithOneHeaderAndRowWithNumber_DataAsNumberWithimObjectInArrayIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"prop1
123";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act
            string result = sut.Parse(data, includeHeader);

            // arrange
            var expectedObject = @"[{""prop1"":123}]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_ParseDataWithTwoHeaders_AnArrayWithObjectThatHaTwoPropertiesIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"prop1{ColumnSeparator}prop2
Ab{ColumnSeparator}Cd";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act
            string result = sut.Parse(data, includeHeader);

            // arrange
            var expectedObject = @"[{""prop1"":""Ab"",""prop2"":""Cd""}]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_ParseDataWithHeaderAndTwoRows_AnArrayWithTwoObjectsIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"prop1{ColumnSeparator}prop2
Ab{ColumnSeparator}Cd
De{ColumnSeparator}Fg";
            var sut = fixture.Freeze<CsvParser>();
            var includeHeader = true;

            // act
            string result = sut.Parse(data, includeHeader);

            // arrange
            var expectedObject = "[{\"prop1\":\"Ab\",\"prop2\":\"Cd\"},{\"prop1\":\"De\",\"prop2\":\"Fg\"}]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_OneRowWithOneEmbeddedLineBreakWithOneLine_AnArrayWithOneElementIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"abc{ColumnSeparator}def""ABC
-DEF""";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedObject = @"[[""abc"",""def\""ABC\r-DEF\""""]]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_OneRowWithEmbeddedLineBreakAndTwoLines_AnArrayWithOneElementIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"abc{ColumnSeparator}def""ABC
GHI
-DEF""";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedObject = "[[\"abc\",\"def\\\"ABC\\rGHI\\r-DEF\\\"\"]]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_OneRowWithTwoLineBreaks_AnArrayWithOneElementIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = $@"abc{ColumnSeparator}def""ABC
GHI
-DEF"" 123 ""456
789""";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data);

            // arrange
            var expectedObject = @"[[""abc"",""def\""ABC\rGHI\r-DEF\"" 123 \""456\r789\""""]]";
            result.ShouldBe(expectedObject);
        }

        [Fact]
        public void Parse_TestGivenExample_AnArrayWithThreeObjectIsReturned()
        {
            // arrange
            var fixture = new Fixture()
                .Customize(new AutoMoqCustomization());
            string data = @"Year#Car#Model#Description
1997#Ford#E350""1997""#""Ford""
1997#Ford#E350#""Super#
luxurious truck""
1997#Ford#E350#""Super#
""""luxurious"""" truck""";
            var sut = fixture.Freeze<CsvParser>();

            // act
            string result = sut.Parse(data, true);

            // arrange
            var expectedObject = @"[{""Year"":1997,""Car"":""Ford"",""Model"":""E350\""1997\"""",""Description"":""\""Ford\""""},{""Year"":1997,""Car"":""Ford"",""Model"":""E350"",""Description"":""\""Super#\rluxurious truck\""""},{""Year"":1997,""Car"":""Ford"",""Model"":""E350"",""Description"":""\""Super#\r\""\""luxurious\""\"" truck\""""}]";
            result.ShouldBe(expectedObject);
        }

        /*
    Instructions :

    You have to parse a CSV file and output a JSON file with the same data.
    DONE·  Any unquoted field that can be a number in the CSV should be a number in the output JSON.
    DONE·  There can be commas within a quoted field.
    DONE·  Double quotes can be inhibited by doubling them.
      Done  Only double quote
        DONE comma within double quote
    DONE·  The software should have a “header” option, if specified then the first line of the csv is considered as the header line and the JSON output should contain objects with the appropriate corresponding key, if no “header” option is specified then there will be no header line in the csv and the JSON output should contain arrays.

    Example CSV:
    Year,Car,Model,Description
    1997,Ford,E350"1997","Ford","E350"
    1997,Ford,E350,"Super,
    luxurious truck"
    1997,Ford,E350,"Super,
    ""luxurious"" truck"


   DONE Bonus: There can be embedded line breaks in a field within double quotes like this:
    1997,Ford,E350,"Go get
    one nowthey are go
         */

        private static string CreateRow(int numberOfColumns)
        {
            string row = string.Empty;
            var fixture = new Fixture();
            for (int i = 0; i < numberOfColumns; i++)
            {
                string column = fixture.Create<string>();
                row += column;
                if (i < numberOfColumns - 1)
                {
                    row += ColumnSeparator;
                }
            }

            return row;
        }

        private static string GetJsonArray(string rawRow)
        {
            var strings = rawRow.Split(ColumnSeparator);
            strings[0] = $"\"{strings[0]}\"";
            string expectedRow = strings.Aggregate((row, column) => row + $",\"{column}\"");

            var expected = $"[{expectedRow}]";
            return expected;
        }

        private static string GetColumnWithCommaWithinQuote()
        {
            var fixture = new Fixture();
            string beginOfTheColumn = fixture.Create<string>();
            string endOfTheColumn = fixture.Create<string>();
            var column = $"{beginOfTheColumn}\"ABC{ColumnSeparator}DEF\"{endOfTheColumn}";
            return column;
        }
    }
}
