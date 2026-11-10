using System.Collections.Generic;

/// <summary>
/// Add extra columns to the per-frame trial CSV.
/// Put a MonoBehaviour that implements this on the same GameObject as the TrialLogger
/// (e.g. a future eye-tracking / pupil script). Its columns are appended after the standard ones.
/// </summary>
public interface ILogColumnProvider
{
    /// Column names, written once in the header. Must always return the same names in the same order.
    IEnumerable<string> ColumnNames { get; }

    /// Append exactly one value per column name (use invariant culture: '.' as decimal separator).
    void AppendValues(List<string> row);
}
