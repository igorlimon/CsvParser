# CsvParser
Instructions :

You have to parse a CSV file and output a JSON file with the same data.
·  Any unquoted field that can be a number in the CSV should be a number in the output JSON.
·  There can be commas within a quoted field.
·  Double quotes can be inhibited by doubling them.
·  The software should have a “header” option, if specified then the first line of the csv is considered as the header line and the JSON output should contain objects with the appropriate corresponding key, if no “header” option is specified then there will be no header line in the csv and the JSON output should contain arrays.

Example CSV:
Year,Car,Model,Description
1997,Ford,E350"1997","Ford","E350"
1997,Ford,E350,"Super,
luxurious truck"
1997,Ford,E350,"Super,
""luxurious"" truck"


Bonus: There can be embedded line breaks in a field within double quotes like this:
1997,Ford,E350,"Go get
one nowthey are going
fast"
