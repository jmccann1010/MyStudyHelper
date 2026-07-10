using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for generating random exercise problems and validating answers.
/// </summary>
public interface IExerciseProblemGeneratorService
{
    /// <summary>
    /// Generates a random exercise problem from the available equations.
    /// </summary>
    /// <param name="equations">List of equations to choose from.</param>
    /// <returns>A generated exercise problem.</returns>
    ExerciseProblem GenerateProblem(List<SubjectMatterEquation> equations);

    /// <summary>
    /// Validates a user's answer against the correct solution.
    /// </summary>
    /// <param name="problem">The original problem.</param>
    /// <param name="userAnswer">The user's submitted answer.</param>
    /// <returns>The validation result.</returns>
    ExerciseResult ValidateAnswer(ExerciseProblem problem, decimal userAnswer);
}
