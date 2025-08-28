using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace DataFlow.Core.Excel;

public class ExcelWriter
{
    private readonly string _filePath;
    private string _sheetName = "Sheet1";
    private bool _includeHeaders = true;
    private bool _autoFit = true;
    private bool _append = false;
    private XLTableTheme? _tableTheme = XLTableTheme.TableStyleMedium2;
    private bool _createTable = false;

    public ExcelWriter(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        _filePath = filePath;
    }

    public ExcelWriter Sheet(string sheetName)
    {
        _sheetName = sheetName ?? throw new ArgumentNullException(nameof(sheetName));
        return this;
    }

    public ExcelWriter WithHeaders(bool includeHeaders = true)
    {
        _includeHeaders = includeHeaders;
        return this;
    }

    public ExcelWriter WithAutoFit(bool autoFit = true)
    {
        _autoFit = autoFit;
        return this;
    }

    public ExcelWriter WithAppend(bool append = false)
    {
        _append = append;
        return this;
    }

    public ExcelWriter AsTable(XLTableTheme? theme = null)
    {
        _createTable = true;
        _tableTheme = theme;
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var rowsList = rows.ToList();
        if (!rowsList.Any())
            return;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        IXLWorkbook workbook;
        IXLWorksheet worksheet;

        if (_append && File.Exists(_filePath))
        {
            workbook = new XLWorkbook(_filePath);
            worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == _sheetName) 
                       ?? workbook.Worksheets.Add(_sheetName);
        }
        else
        {
            workbook = new XLWorkbook();
            worksheet = workbook.Worksheets.Add(_sheetName);
        }

        using (workbook)
        {
            var startRow = _append ? (worksheet.LastRowUsed()?.RowNumber() ?? 0) + 1 : 1;
            var currentRow = startRow;
            var columnNames = rowsList.First().GetColumnNames().ToList();

            if (_includeHeaders && (!_append || startRow == 1))
            {
                for (int col = 0; col < columnNames.Count; col++)
                {
                    worksheet.Cell(currentRow, col + 1).Value = columnNames[col];
                    worksheet.Cell(currentRow, col + 1).Style.Font.Bold = true;
                }
                currentRow++;
            }

            foreach (var row in rowsList)
            {
                for (int col = 0; col < columnNames.Count; col++)
                {
                    var value = row[columnNames[col]];
                    var cell = worksheet.Cell(currentRow, col + 1);
                    SetCellValue(cell, value);
                }
                currentRow++;
            }

            if (_createTable && !_append)
            {
                var range = worksheet.Range(startRow, 1, currentRow - 1, columnNames.Count);
                var table = range.CreateTable();
                table.Theme = _tableTheme ?? XLTableTheme.TableStyleMedium2;
            }

            if (_autoFit)
            {
                worksheet.Columns().AdjustToContents();
            }

            workbook.SaveAs(_filePath);
        }
    }

    public void Write<T>(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemsList = items.ToList();
        if (!itemsList.Any())
            return;

        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToList();

        var rows = itemsList.Select(item =>
        {
            var row = new DataRow();
            foreach (var prop in properties)
            {
                row[prop.Name] = prop.GetValue(item);
            }
            return row;
        });

        Write(rows);
    }

    private void SetCellValue(IXLCell cell, object value)
    {
        if (value == null)
        {
            cell.Value = string.Empty;
            return;
        }

        switch (value)
        {
            case string str:
                cell.Value = str;
                break;
            case int intVal:
                cell.Value = intVal;
                break;
            case long longVal:
                cell.Value = longVal;
                break;
            case float floatVal:
                cell.Value = floatVal;
                break;
            case double doubleVal:
                cell.Value = doubleVal;
                break;
            case decimal decimalVal:
                cell.Value = decimalVal;
                break;
            case bool boolVal:
                cell.Value = boolVal;
                break;
            case DateTime dateVal:
                cell.Value = dateVal;
                cell.Style.DateFormat.SetFormat("yyyy-MM-dd HH:mm:ss");
                break;
            case TimeSpan timeVal:
                cell.Value = timeVal;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}