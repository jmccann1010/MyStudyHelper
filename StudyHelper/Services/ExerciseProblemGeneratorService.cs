using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service that generates random exercise problems and validates answers.
/// </summary>
public class ExerciseProblemGeneratorService : IExerciseProblemGeneratorService
{
    private readonly ILogger<ExerciseProblemGeneratorService> _logger;
    private readonly IConfiguration _configuration;

    // Tolerance for answer validation (configurable)
    private readonly decimal _currencyTolerance;
    private readonly decimal _ratioTolerance;

    public ExerciseProblemGeneratorService(
        ILogger<ExerciseProblemGeneratorService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _currencyTolerance = _configuration.GetValue("ExerciseSettings:CurrencyTolerance", 1.00m);
        _ratioTolerance = _configuration.GetValue("ExerciseSettings:RatioTolerance", 0.01m);
    }

    /// <summary>
    /// Generates a random exercise problem from available equations.
    /// </summary>
    public ExerciseProblem GenerateProblem(List<SubjectMatterEquation> equations)
    {
        if (equations == null || equations.Count == 0)
        {
            throw new ArgumentException("No equations available to generate problem", nameof(equations));
        }

        // Filter to solvable equations (skip multi-step for MVP)
        var solvable = equations.Where(e =>
            e.Type != EquationType.MultiStep &&
            e.Variables.Count >= 2 &&
            e.Variables.Count <= 5).ToList();

        if (!solvable.Any())
        {
            throw new InvalidOperationException("No solvable equations available");
        }

        // Select random equation
        var equation = solvable[Random.Shared.Next(solvable.Count)];

        // Select variable to solve for (prefer left side)
        var solveFor = SelectVariableToSolveFor(equation);

        // Generate realistic values
        var givenValues = GenerateRealisticValues(equation, solveFor);

        // Calculate correct answer
        var correctAnswer = CalculateAnswer(equation, givenValues, solveFor);

        // Determine if result is a ratio
        var isRatio = equation.Type == EquationType.Ratio || equation.Type == EquationType.Division;

        // Format problem text
        var problemText = FormatProblemText(equation, givenValues, solveFor, isRatio);

        // Generate solution steps
        var solutionSteps = GenerateSolutionSteps(equation, givenValues, solveFor, correctAnswer, isRatio);

        _logger.LogDebug("Generated problem for {EquationId}, solving for {Variable}, answer: {Answer}",
            equation.EquationId, solveFor, correctAnswer);

        return new ExerciseProblem
        {
            Equation = equation,
            GivenValues = givenValues,
            SolveForVariable = solveFor,
            CorrectAnswer = correctAnswer,
            ProblemText = problemText,
            SolutionSteps = solutionSteps,
            Module = equation.Module,
            IsRatioResult = isRatio
        };
    }

    /// <summary>
    /// Validates a user's answer against the correct solution.
    /// </summary>
    public ExerciseResult ValidateAnswer(ExerciseProblem problem, decimal userAnswer)
    {
        // Use different tolerance for ratios vs currency
        var tolerance = problem.IsRatioResult ? _ratioTolerance : _currencyTolerance;

        var difference = Math.Abs(userAnswer - problem.CorrectAnswer);
        var isCorrect = difference <= tolerance;

        var feedbackMessage = isCorrect
            ? "Correct! Excellent work!"
            : FormatIncorrectFeedback(problem, userAnswer, difference);

        _logger.LogDebug("Validated answer: User={User}, Correct={Correct}, IsCorrect={IsCorrect}",
            userAnswer, problem.CorrectAnswer, isCorrect);

        return new ExerciseResult
        {
            IsCorrect = isCorrect,
            UserAnswer = userAnswer,
            CorrectAnswer = problem.CorrectAnswer,
            Problem = problem,
            FeedbackMessage = feedbackMessage,
            SolutionSteps = problem.SolutionSteps
        };
    }

    /// <summary>
    /// Selects which variable to solve for in the equation.
    /// For division/ratio equations, always solve for the left side to avoid
    /// needing to generate a pre-calculated ratio value.
    /// For other equation types, randomly selects any variable to make exercises more robust.
    /// </summary>
    private static string SelectVariableToSolveFor(SubjectMatterEquation equation)
    {
        // For division/ratio equations, always solve for the left side (the ratio itself)
        // to avoid the complexity of solving backwards
        if ((equation.Type == EquationType.Division || equation.Type == EquationType.Ratio) &&
            !string.IsNullOrEmpty(equation.LeftSide) &&
            equation.Variables.Contains(equation.LeftSide))
        {
            return equation.LeftSide;
        }

        // For all other equation types, randomly select any variable to solve for
        // This makes exercises more robust by varying which operands are given vs. solved
        // Example: For A = B + C, sometimes solve for A (given B, C), 
        //          sometimes solve for B (given A, C), sometimes solve for C (given A, B)
        var solvableVariables = equation.Variables
            .Where(v => !decimal.TryParse(v, out _)) // Exclude numeric constants
            .ToList();

        if (!solvableVariables.Any())
        {
            throw new InvalidOperationException("No solvable variables found in equation");
        }

        return solvableVariables[Random.Shared.Next(solvableVariables.Count)];
    }

    /// <summary>
    /// Generates realistic values for equation variables.
    /// Skips numeric constants and ensures the solve-for variable is excluded.
    /// </summary>
    private Dictionary<string, decimal> GenerateRealisticValues(
        SubjectMatterEquation equation, string solveFor)
    {
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in equation.Variables.Where(v => v != solveFor))
        {
            // Skip numeric constants (they'll be handled by GetTermValue)
            if (decimal.TryParse(variable, out _))
            {
                continue;
            }

            values[variable] = GenerateValueForVariable(variable, equation.Type);
        }

        return values;
    }

    /// <summary>
    /// Generates a realistic value for a specific variable based on its name and equation type.
    /// </summary>
    /// <param name="variable">The variable name to generate a value for.</param>
    /// <param name="type">The equation type context.</param>
    /// <returns>A realistic decimal value appropriate for the variable type.</returns>
    private static decimal GenerateValueForVariable(string variable, EquationType type)
    {
        var varLower = variable.ToLowerInvariant();

        // Shares and per-share amounts
        if (varLower.Contains("shares") || varLower.Contains("outstanding"))
        {
            // Generate share counts in thousands
            if (varLower.Contains("weighted") || varLower.Contains("average"))
                return GenerateRandomAmount(1000, 50000) * 1000; // 1M to 50M shares
            return GenerateRandomAmount(100, 10000) * 1000; // 100K to 10M shares
        }

        if (varLower.Contains("per share") || varLower.Contains("eps") || varLower.Contains("market value"))
        {
            // Per-share amounts typically $0.50 to $50
            return Math.Round((decimal)Random.Shared.NextDouble() * 49.5m + 0.5m, 2);
        }

        if (varLower.Contains("dividend") && varLower.Contains("preferred"))
        {
            // Preferred dividends: typically $10K to $500K
            return GenerateRandomAmount(10000, 500000);
        }

        // Financial statement items (dollars)
        if (varLower.Contains("asset") || varLower.Contains("liabilit") ||
            varLower.Contains("equity") || varLower.Contains("capital"))
        {
            // Balance sheet items: $50K to $10M
            if (varLower.Contains("total"))
                return GenerateRandomAmount(500000, 10000000);
            return GenerateRandomAmount(50000, 1000000);
        }

        if (varLower.Contains("revenue") || varLower.Contains("sales"))
        {
            // Revenue/Sales: typically larger amounts $100K to $5M
            if (varLower.Contains("net"))
                return GenerateRandomAmount(100000, 5000000);
            return GenerateRandomAmount(150000, 6000000);
        }

        if (varLower.Contains("expense") || varLower.Contains("cost") || varLower.Contains("cogs"))
        {
            // Expenses: $30K to $2M
            if (varLower.Contains("operating"))
                return GenerateRandomAmount(50000, 1500000);
            return GenerateRandomAmount(30000, 2000000);
        }

        if (varLower.Contains("income") || varLower.Contains("earning"))
        {
            // Income/earnings: $10K to $1M (usually less than revenue)
            if (varLower.Contains("net") || varLower.Contains("operating"))
                return GenerateRandomAmount(20000, 1000000);
            return GenerateRandomAmount(10000, 800000);
        }

        if (varLower.Contains("gain") || varLower.Contains("loss"))
        {
            // Gains/losses: typically smaller amounts $1K to $200K
            return GenerateRandomAmount(1000, 200000);
        }

        if (varLower.Contains("profit"))
        {
            // Profit (Gross/Net): $10K to $1M
            if (varLower.Contains("gross"))
                return GenerateRandomAmount(50000, 1500000);
            return GenerateRandomAmount(10000, 1000000);
        }

        if (varLower.Contains("inventory"))
            return GenerateRandomAmount(20000, 500000);

        if (varLower.Contains("receivable") || varLower.Contains("payable"))
            return GenerateRandomAmount(10000, 300000);

        if (varLower.Contains("depreciation") || varLower.Contains("amortization"))
            return GenerateRandomAmount(5000, 100000);

        if (varLower.Contains("cash"))
        {
            if (varLower.Contains("flow") || varLower.Contains("change"))
                return GenerateRandomAmount(10000, 500000);
            return GenerateRandomAmount(15000, 600000);
        }

        if (varLower.Contains("investment"))
            return GenerateRandomAmount(15000, 600000);

        if (varLower.Contains("debt"))
            return GenerateRandomAmount(50000, 2000000);

        if (varLower.Contains("retained earnings") || varLower.Contains("beginning") || varLower.Contains("ending"))
            return GenerateRandomAmount(100000, 3000000);

        if (varLower.Contains("bank") || varLower.Contains("deposit") || varLower.Contains("check"))
        {
            // Bank reconciliation amounts: smaller values $1K to $50K
            return GenerateRandomAmount(1000, 50000);
        }

        if (varLower.Contains("service charge") || varLower.Contains("nsf") || varLower.Contains("error"))
        {
            // Small adjustment amounts: $10 to $1000
            return Math.Round((decimal)Random.Shared.NextDouble() * 990m + 10m, 2);
        }

        // For current/non-current in ratio context
        if (type == EquationType.Division || type == EquationType.Ratio)
        {
            if (varLower.Contains("current"))
                return GenerateRandomAmount(50000, 800000);
        }

        // Default for unknown variables
        return GenerateRandomAmount(10000, 500000);
    }

    /// <summary>
    /// Gets the value for a term variable, handling both regular variables and numeric constants.
    /// Numeric constants (like "2" in division) are returned as-is, while variables are looked up in the dictionary.
    /// </summary>
    /// <param name="variableName">The variable name or numeric constant.</param>
    /// <param name="givenValues">Dictionary of variable values.</param>
    /// <returns>The numeric value of the variable or constant.</returns>
    private static decimal GetTermValue(string variableName, Dictionary<string, decimal> givenValues)
    {
        // Check if it's a numeric constant
        if (decimal.TryParse(variableName, out var constantValue))
        {
            return constantValue;
        }

        // Otherwise look it up in the dictionary
        if (!givenValues.TryGetValue(variableName, out var value))
        {
            throw new KeyNotFoundException(
                $"Variable '{variableName}' not found in given values. Available keys: {string.Join(", ", givenValues.Keys)}");
        }

        return value;
    }

    /// <summary>
    /// Generates a random dollar amount within a specified range, rounded to the nearest $1,000.
    /// </summary>
    /// <param name="min">Minimum amount (inclusive).</param>
    /// <param name="max">Maximum amount (exclusive).</param>
    /// <returns>Random amount rounded to nearest $1,000.</returns>
    private static decimal GenerateRandomAmount(int min, int max)
    {
        var amount = Random.Shared.Next(min, max);
        // Round to nearest 1000 for cleaner numbers
        return Math.Round(amount / 1000m) * 1000;
    }

    /// <summary>
    /// Calculates the correct answer for the equation.
    /// </summary>
    private decimal CalculateAnswer(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        var result = equation.Type switch
        {
            EquationType.Addition => CalculateAddition(equation, givenValues, solveFor),
            EquationType.Subtraction => CalculateSubtraction(equation, givenValues, solveFor),
            EquationType.Multiplication => CalculateMultiplication(equation, givenValues, solveFor),
            EquationType.Division or EquationType.Ratio => CalculateDivision(equation, givenValues, solveFor),
            EquationType.Complex => CalculateComplex(equation, givenValues, solveFor),
            _ => throw new NotSupportedException($"Equation type {equation.Type} not supported")
        };

        // Round to appropriate precision
        return Math.Round(result, 2);
    }

    /// <summary>
    /// Calculates answer for addition equations (A = B + C).
    /// </summary>
    private decimal CalculateAddition(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        if (solveFor == equation.LeftSide)
        {
            // Solving for left side: sum all right side terms
            return equation.RightSideTerms
                .Select(t => GetTermValue(t.Variable, givenValues))
                .Sum();
        }
        else
        {
            // Solving for one right side term: left minus other terms
            var leftValue = GetTermValue(equation.LeftSide, givenValues);
            var otherTerms = equation.RightSideTerms
                .Where(t => t.Variable != solveFor)
                .Select(t => GetTermValue(t.Variable, givenValues))
                .Sum();
            return leftValue - otherTerms;
        }
    }

    /// <summary>
    /// Calculates answer for subtraction equations (A = B - C).
    /// </summary>
    private decimal CalculateSubtraction(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        if (equation.RightSideTerms.Count < 2)
        {
            throw new InvalidOperationException("Subtraction requires at least 2 terms");
        }

        var firstTerm = equation.RightSideTerms[0];
        var secondTerm = equation.RightSideTerms[1];

        if (solveFor == equation.LeftSide)
        {
            // A = B - C
            return GetTermValue(firstTerm.Variable, givenValues) - GetTermValue(secondTerm.Variable, givenValues);
        }
        else if (solveFor == firstTerm.Variable)
        {
            // B = A + C
            return GetTermValue(equation.LeftSide, givenValues) + GetTermValue(secondTerm.Variable, givenValues);
        }
        else
        {
            // C = B - A
            return GetTermValue(firstTerm.Variable, givenValues) - GetTermValue(equation.LeftSide, givenValues);
        }
    }

    /// <summary>
    /// Calculates answer for multiplication equations (A = B * C).
    /// </summary>
    private decimal CalculateMultiplication(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        if (solveFor == equation.LeftSide)
        {
            // A = B * C
            decimal result = 1;
            foreach (var term in equation.RightSideTerms)
            {
                result *= GetTermValue(term.Variable, givenValues);
            }
            return result;
        }
        else
        {
            // Solving for one term: divide left by other terms
            var leftValue = GetTermValue(equation.LeftSide, givenValues);
            foreach (var term in equation.RightSideTerms.Where(t => t.Variable != solveFor))
            {
                var divisor = GetTermValue(term.Variable, givenValues);
                if (divisor == 0)
                    throw new DivideByZeroException("Cannot divide by zero");
                leftValue /= divisor;
            }
            return leftValue;
        }
    }

    /// <summary>
    /// Calculates answer for division equations (A = B / C) and ratios.
    /// </summary>
    private decimal CalculateDivision(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        var numeratorTerm = equation.RightSideTerms.FirstOrDefault(t => t.IsNumerator);
        var denominatorTerm = equation.RightSideTerms.FirstOrDefault(t => t.IsDenominator);

        if (numeratorTerm == null || denominatorTerm == null)
        {
            throw new InvalidOperationException("Division requires numerator and denominator");
        }

        if (solveFor == equation.LeftSide)
        {
            // Ratio = Numerator / Denominator
            var numerator = GetTermValue(numeratorTerm.Variable, givenValues);
            var denominator = GetTermValue(denominatorTerm.Variable, givenValues);

            if (denominator == 0)
                throw new DivideByZeroException("Cannot divide by zero");

            return numerator / denominator;
        }
        else if (solveFor == numeratorTerm.Variable)
        {
            // Numerator = Ratio * Denominator
            return GetTermValue(equation.LeftSide, givenValues) * GetTermValue(denominatorTerm.Variable, givenValues);
        }
        else
        {
            // Denominator = Numerator / Ratio
            var ratio = GetTermValue(equation.LeftSide, givenValues);
            if (ratio == 0)
                throw new DivideByZeroException("Cannot divide by zero ratio");
            return GetTermValue(numeratorTerm.Variable, givenValues) / ratio;
        }
    }

    /// <summary>
    /// Calculates answer for complex equations with multiple operators.
    /// </summary>
    private decimal CalculateComplex(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor)
    {
        if (solveFor == equation.LeftSide)
        {
            // Calculate left side from right side terms
            decimal result = 0;
            bool first = true;

            foreach (var term in equation.RightSideTerms)
            {
                var value = GetTermValue(term.Variable, givenValues);

                if (first)
                {
                    result = value;
                    first = false;
                }
                else
                {
                    result += term.Operator == "-" ? -value : value;
                }
            }
            return result;
        }
        else
        {
            // Solving for a term on right side
            var leftValue = GetTermValue(equation.LeftSide, givenValues);
            var targetTerm = equation.RightSideTerms.First(t => t.Variable == solveFor);
            var targetIndex = equation.RightSideTerms.IndexOf(targetTerm);

            decimal result = leftValue;

            // Reverse operations for other terms
            for (int i = 0; i < equation.RightSideTerms.Count; i++)
            {
                if (i == targetIndex) continue;

                var term = equation.RightSideTerms[i];
                var value = GetTermValue(term.Variable, givenValues);

                if (i == 0)
                {
                    // First term is always positive in original
                    result -= value;
                }
                else
                {
                    // Reverse the operator
                    result -= term.Operator == "-" ? -value : value;
                }
            }

            // Apply target term's operator if needed
            if (targetIndex > 0 && targetTerm.Operator == "-")
            {
                result = -result;
            }

            return result;
        }
    }

    /// <summary>
    /// Formats the problem text for display.
    /// </summary>
    private static string FormatProblemText(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor,
        bool isRatioEquation)
    {
        var text = "Given the following information:\n\n";

        foreach (var kvp in givenValues)
        {
            // Use explicit flag instead of heuristic for ratio formatting
            if (isRatioEquation)
            {
                text += $"• {kvp.Key} = {kvp.Value:F2}\n";
            }
            else
            {
                text += $"• {kvp.Key} = {kvp.Value:C0}\n";
            }
        }

        text += $"\nCalculate: {solveFor}";

        return text;
    }

    /// <summary>
    /// Generates step-by-step solution explanation.
    /// </summary>
    private static string GenerateSolutionSteps(
        SubjectMatterEquation equation,
        Dictionary<string, decimal> givenValues,
        string solveFor,
        decimal answer,
        bool isRatioEquation)
    {
        var steps = $"Using the equation: {equation.DisplayName}\n\n";

        steps += "Substituting the given values:\n";

        // Show the equation with values substituted
        var formula = equation.Formula;
        foreach (var kvp in givenValues)
        {
            var valueStr = isRatioEquation
                ? kvp.Value.ToString("F2")
                : kvp.Value.ToString("C0");
            formula = formula.Replace(kvp.Key, valueStr);
        }
        formula = formula.Replace(solveFor, "?");

        steps += formula + "\n\n";

        // Show calculation steps
        steps += $"Solving for {solveFor}:\n";

        var answerStr = isRatioEquation
            ? answer.ToString("F2")
            : answer.ToString("C0");

        steps += $"{solveFor} = {answerStr}";

        return steps;
    }

    /// <summary>
    /// Formats feedback message for incorrect answers.
    /// </summary>
    private static string FormatIncorrectFeedback(ExerciseProblem problem, decimal userAnswer, decimal difference)
    {
        if (problem.IsRatioResult)
        {
            return $"Not quite. The correct ratio is {problem.CorrectAnswer:F2}.";
        }
        else
        {
            if (difference > problem.CorrectAnswer * 0.1m)
            {
                return $"Not quite. Check your calculation. The correct answer is {problem.CorrectAnswer:C0}.";
            }
            else
            {
                return $"Close! The correct answer is {problem.CorrectAnswer:C0}.";
            }
        }
    }
}
