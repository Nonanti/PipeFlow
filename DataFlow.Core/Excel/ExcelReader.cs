using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace DataFlow.Core.Excel;

public class ExcelReader
{
    private readonly string _filePath;
    private string _sheetName;
    private int? _sheetIndex;
    private bool _hasHeaders = true;
    private int _startRow = 1;
    private int _startColumn = 1;
    private int? _endRow;
    private int? _endColumn;

    public ExcelReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        
        _filePath = filePath;
    }

    public ExcelReader Sheet(string sheetName)
    {
        _sheetName = sheetName;
        _sheetIndex = null;
        return this;
    }

    public ExcelReader Sheet(int sheetIndex)
    {
        _sheetIndex = sheetIndex;
        _sheetName = null;
        return this;
    }

    public ExcelReader WithHeaders(bool hasHeaders = true)
    {
        _hasHeaders = hasHeaders;
        return this;
    }

    public ExcelReader Range(int startRow, int startColumn, int? endRow = null, int? endColumn = null)
    {
        _startRow = startRow;
        _startColumn = startColumn;
        _endRow = endRow;
        _endColumn = endColumn;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        using var workbook = new XLWorkbook(_filePath);
        
        IXLWorksheet worksheet;
        if (!string.IsNullOrEmpty(_sheetName))
        {
            worksheet = workbook.Worksheet(_sheetName);
        }
        else if (_sheetIndex.HasValue)
        {
            worksheet = workbook.Worksheet(_sheetIndex.Value);
        }
        else
        {
            worksheet = workbook.Worksheets.First();
        }

        var lastRow = _endRow ?? worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastColumn = _endColumn ?? worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow == 0 || lastColumn == 0)
            yield break;

        var headers = new List<string>();
        var currentRow = _startRow;

        if (_hasHeaders)
        {
            for (int col = _startColumn; col <= lastColumn; col++)
            {
                var headerValue = worksheet.Cell(currentRow, col).Value.ToString();
                if (string.IsNullOrWhiteSpace(headerValue))
                    headerValue = $"Column{col}";
                headers.Add(headerValue);
            }
            currentRow++;
        }
        else
        {
            for (int col = _startColumn; col <= lastColumn; col++)
            {
                headers.Add($"Column{col - _startColumn + 1}");
            }
        }

        for (int row = currentRow; row <= lastRow; row++)
        {
            var dataRow = new DataRow();
            var isEmptyRow = true;

            for (int col = _startColumn; col <= lastColumn; col++)
            {
                var cell = worksheet.Cell(row, col);
                var value = GetCellValue(cell);
                
                if (value != null)
                    isEmptyRow = false;

                var headerIndex = col - _startColumn;
                if (headerIndex < headers.Count)
                {
                    dataRow[headers[headerIndex]] = value;
                }
            }

            if (!isEmptyRow)
                yield return dataRow;
        }
    }

    private object GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        switch (cell.Value.Type)
        {
            case XLDataType.Number:
                if (cell.Value.IsNumber)
                {
                    var number = cell.Value.GetNumber();
                    if (number % 1 == 0)
                        return Convert.ToInt64(number);
                    return number;
                }
                return cell.Value.GetNumber();

            case XLDataType.Text:
                return cell.Value.GetText();

            case XLDataType.Boolean:
                return cell.Value.GetBoolean();

            case XLDataType.DateTime:
                return cell.Value.GetDateTime();

            case XLDataType.TimeSpan:
                return cell.Value.GetTimeSpan();

            default:
                return cell.Value.ToString();
        }
    }
}