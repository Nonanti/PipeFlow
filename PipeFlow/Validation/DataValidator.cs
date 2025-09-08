using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PipeFlow.Core.Validation;

public class DataValidator
{
    private readonly List<IValidationRule> _rules;

    public DataValidator()
    {
        _rules = new List<IValidationRule>();
    }

    public ColumnValidator Column(string columnName)
    {
        return new ColumnValidator(this, columnName);
    }

    internal void AddRule(IValidationRule rule)
    {
        _rules.Add(rule);
    }

    public ValidationResult Validate(DataRow row)
    {
        var result = new ValidationResult(row);

        foreach (var rule in _rules)
        {
            rule.Validate(row, result);
        }

        return result;
    }

    public IEnumerable<ValidationResult> ValidateAll(IEnumerable<DataRow> rows)
    {
        foreach (var row in rows)
        {
            yield return Validate(row);
        }
    }
}

public class ColumnValidator
{
    private readonly DataValidator _validator;
    private readonly string _columnName;

    internal ColumnValidator(DataValidator validator, string columnName)
    {
        _validator = validator;
        _columnName = columnName;
    }

    public ColumnValidator Required()
    {
        _validator.AddRule(new RequiredRule(_columnName));
        return this;
    }

    public ColumnValidator Email()
    {
        _validator.AddRule(new EmailRule(_columnName));
        return this;
    }

    public ColumnValidator Regex(string pattern, string errorMessage = null)
    {
        _validator.AddRule(new RegexRule(_columnName, pattern, errorMessage));
        return this;
    }

    public ColumnValidator Range(double min, double max)
    {
        _validator.AddRule(new RangeRule(_columnName, min, max));
        return this;
    }

    public ColumnValidator MinLength(int minLength)
    {
        _validator.AddRule(new MinLengthRule(_columnName, minLength));
        return this;
    }

    public ColumnValidator MaxLength(int maxLength)
    {
        _validator.AddRule(new MaxLengthRule(_columnName, maxLength));
        return this;
    }

    public ColumnValidator Custom(Func<object, bool> validator, string errorMessage)
    {
        _validator.AddRule(new CustomRule(_columnName, validator, errorMessage));
        return this;
    }

    public ColumnValidator In(params object[] allowedValues)
    {
        _validator.AddRule(new InRule(_columnName, allowedValues));
        return this;
    }

    public ColumnValidator NotIn(params object[] forbiddenValues)
    {
        _validator.AddRule(new NotInRule(_columnName, forbiddenValues));
        return this;
    }

    public ColumnValidator Type<T>()
    {
        _validator.AddRule(new TypeRule(_columnName, typeof(T)));
        return this;
    }

    public DataValidator And => _validator;
}

public interface IValidationRule
{
    void Validate(DataRow row, ValidationResult result);
}

public class RequiredRule : IValidationRule
{
    private readonly string _columnName;

    public RequiredRule(string columnName)
    {
        _columnName = columnName;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName) || row[_columnName] == null || string.IsNullOrWhiteSpace(row[_columnName]?.ToString()))
        {
            result.AddError(_columnName, "Field is required");
        }
    }
}

public class EmailRule : IValidationRule
{
    private readonly string _columnName;
    private readonly Regex _emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

    public EmailRule(string columnName)
    {
        _columnName = columnName;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        var email = value.ToString();
        if (!_emailRegex.IsMatch(email))
        {
            result.AddError(_columnName, "Invalid email format", email);
        }
    }
}

public class RegexRule : IValidationRule
{
    private readonly string _columnName;
    private readonly Regex _regex;
    private readonly string _errorMessage;

    public RegexRule(string columnName, string pattern, string errorMessage = null)
    {
        _columnName = columnName;
        _regex = new Regex(pattern, RegexOptions.Compiled);
        _errorMessage = errorMessage ?? $"Value must match pattern: {pattern}";
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        if (!_regex.IsMatch(value.ToString()))
        {
            result.AddError(_columnName, _errorMessage, value);
        }
    }
}

public class RangeRule : IValidationRule
{
    private readonly string _columnName;
    private readonly double _min;
    private readonly double _max;

    public RangeRule(string columnName, double min, double max)
    {
        _columnName = columnName;
        _min = min;
        _max = max;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        if (double.TryParse(value.ToString(), out double numValue))
        {
            if (numValue < _min || numValue > _max)
            {
                result.AddError(_columnName, $"Value must be between {_min} and {_max}", numValue);
            }
        }
        else
        {
            result.AddError(_columnName, "Value must be numeric", value);
        }
    }
}

public class MinLengthRule : IValidationRule
{
    private readonly string _columnName;
    private readonly int _minLength;

    public MinLengthRule(string columnName, int minLength)
    {
        _columnName = columnName;
        _minLength = minLength;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        var str = value.ToString();
        if (str.Length < _minLength)
        {
            result.AddError(_columnName, $"Minimum length is {_minLength} characters", str);
        }
    }
}

public class MaxLengthRule : IValidationRule
{
    private readonly string _columnName;
    private readonly int _maxLength;

    public MaxLengthRule(string columnName, int maxLength)
    {
        _columnName = columnName;
        _maxLength = maxLength;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        var str = value.ToString();
        if (str.Length > _maxLength)
        {
            result.AddError(_columnName, $"Maximum length is {_maxLength} characters", str);
        }
    }
}

public class CustomRule : IValidationRule
{
    private readonly string _columnName;
    private readonly Func<object, bool> _validator;
    private readonly string _errorMessage;

    public CustomRule(string columnName, Func<object, bool> validator, string errorMessage)
    {
        _columnName = columnName;
        _validator = validator;
        _errorMessage = errorMessage;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (!_validator(value))
        {
            result.AddError(_columnName, _errorMessage, value);
        }
    }
}

public class InRule : IValidationRule
{
    private readonly string _columnName;
    private readonly object[] _allowedValues;

    public InRule(string columnName, object[] allowedValues)
    {
        _columnName = columnName;
        _allowedValues = allowedValues;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        if (!_allowedValues.Contains(value))
        {
            result.AddError(_columnName, $"Value must be one of: {string.Join(", ", _allowedValues)}", value);
        }
    }
}

public class NotInRule : IValidationRule
{
    private readonly string _columnName;
    private readonly object[] _forbiddenValues;

    public NotInRule(string columnName, object[] forbiddenValues)
    {
        _columnName = columnName;
        _forbiddenValues = forbiddenValues;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        if (_forbiddenValues.Contains(value))
        {
            result.AddError(_columnName, $"Value must not be one of: {string.Join(", ", _forbiddenValues)}", value);
        }
    }
}

public class TypeRule : IValidationRule
{
    private readonly string _columnName;
    private readonly Type _expectedType;

    public TypeRule(string columnName, Type expectedType)
    {
        _columnName = columnName;
        _expectedType = expectedType;
    }

    public void Validate(DataRow row, ValidationResult result)
    {
        if (!row.ContainsColumn(_columnName))
            return;

        var value = row[_columnName];
        if (value == null)
            return;

        try
        {
            Convert.ChangeType(value, _expectedType);
        }
        catch
        {
            result.AddError(_columnName, $"Value must be of type {_expectedType.Name}", value);
        }
    }
}

public enum ValidationErrorHandling
{
    ThrowException,
    Skip,
    Log,
    Fix
}