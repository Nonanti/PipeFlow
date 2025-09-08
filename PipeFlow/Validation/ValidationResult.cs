using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeFlow.Core.Validation;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public DataRow Row { get; set; }

    public ValidationResult()
    {
        Errors = new List<ValidationError>();
        IsValid = true;
    }

    public ValidationResult(DataRow row) : this()
    {
        Row = row;
    }

    public void AddError(string column, string message, object value = null)
    {
        Errors.Add(new ValidationError
        {
            ColumnName = column,
            Message = message,
            Value = value
        });
        IsValid = false;
    }

    public string GetErrorSummary()
    {
        if (IsValid)
            return "Valid";
        
        return string.Join("; ", Errors.Select(e => $"{e.ColumnName}: {e.Message}"));
    }
}

public class ValidationError
{
    public string ColumnName { get; set; }
    public string Message { get; set; }
    public object Value { get; set; }

    public override string ToString()
    {
        return $"{ColumnName}: {Message}" + (Value != null ? $" (value: {Value})" : "");
    }
}