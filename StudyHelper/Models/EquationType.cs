namespace StudyHelper.Models;

/// <summary>
/// Represents the type of mathematical operation in an equation.
/// </summary>
public enum EquationType
{
    /// <summary>
    /// Addition operation (e.g., A = B + C)
    /// </summary>
    Addition,

    /// <summary>
    /// Subtraction operation (e.g., A = B - C)
    /// </summary>
    Subtraction,

    /// <summary>
    /// Multiplication operation (e.g., A = B * C)
    /// </summary>
    Multiplication,

    /// <summary>
    /// Division operation (e.g., A = B / C)
    /// </summary>
    Division,

    /// <summary>
    /// Complex operation involving multiple operators
    /// </summary>
    Complex,

    /// <summary>
    /// Ratio calculation (division with ratio result)
    /// </summary>
    Ratio,

    /// <summary>
    /// Multi-step calculation requiring intermediate values
    /// </summary>
    MultiStep
}
