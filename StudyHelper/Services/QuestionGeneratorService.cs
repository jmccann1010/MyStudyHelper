using StudyHelper.Models;

namespace StudyHelper.Services;

/// <summary>
/// Service for generating multiple choice quiz questions from parsed markdown content.
/// </summary>
public class QuestionGeneratorService : IQuestionGeneratorService
{
    private readonly ILogger<QuestionGeneratorService> _logger;

    private static readonly string[] GenericDistractorTemplates = new[]
    {
        "This concept is unrelated to {0}",
        "This does not apply to {0}",
        "This is not a characteristic of {0}",
        "This contradicts the principles of {0}",
        "This is outside the scope of {0}"
    };

    public QuestionGeneratorService(ILogger<QuestionGeneratorService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Generates a multiple choice question with four answer options from parsed markdown sections.
    /// Supports bidirectional questions (term→definition and definition→term).
    /// </summary>
    public QuizQuestion GenerateQuestion(List<MarkdownSection> sections, QuestionDirection? direction = null)
    {
        ArgumentNullException.ThrowIfNull(sections);

        if (sections.Count == 0)
        {
            _logger.LogError("Cannot generate question: sections list is empty");
            throw new InvalidOperationException("Cannot generate question: sections list is empty");
        }

        // If direction is not specified, randomly select one (50/50 chance)
        var actualDirection = direction ?? (Random.Shared.Next(2) == 0 
            ? QuestionDirection.TermToDefinition 
            : QuestionDirection.DefinitionToTerm);

        _logger.LogInformation("Generating question with direction: {Direction}", actualDirection);

        // Filter sections that have sufficient content
        var validSections = sections.Where(s => 
            !string.IsNullOrWhiteSpace(s.Heading) && 
            (s.BulletPoints.Count > 0 || s.ContentLines.Count > 0 || s.TermDefinitions.Count > 0)
        ).ToList();

        if (validSections.Count == 0)
        {
            _logger.LogError("Cannot generate question: no sections with sufficient content");
            throw new InvalidOperationException("Cannot generate question: no sections with sufficient content");
        }

        // Prioritize term-definition questions if available
        var sectionsWithTerms = validSections.Where(s => s.TermDefinitions.Count >= 4).ToList();
        if (sectionsWithTerms.Count > 0)
        {
            var section = sectionsWithTerms[Random.Shared.Next(sectionsWithTerms.Count)];
            return GenerateTermDefinitionQuestion(section, actualDirection);
        }

        // Select a random section
        var selectedSection = validSections[Random.Shared.Next(validSections.Count)];

        // Generate question based on available content
        if (selectedSection.BulletPoints.Count >= 4)
        {
            return GenerateBulletPointQuestion(selectedSection, validSections);
        }
        else if (selectedSection.BulletPoints.Count > 0)
        {
            return GenerateBulletPointQuestionWithMixedDistractors(selectedSection, validSections);
        }
        else
        {
            return GenerateConceptQuestion(selectedSection, validSections);
        }
    }

    /// <summary>
    /// Generates a term-definition question where the user must match a term to its correct definition.
    /// All answer choices are complete definitions from the term-definition pairs.
    /// Supports bidirectional questions (term→definition and definition→term).
    /// </summary>
    private QuizQuestion GenerateTermDefinitionQuestion(MarkdownSection section, QuestionDirection direction)
    {
        // Select 4 random term-definition pairs
        var selectedPairs = section.TermDefinitions
            .OrderBy(_ => Random.Shared.Next())
            .Take(4)
            .ToList();

        // Pick one as the correct answer
        var correctAnswerIndex = Random.Shared.Next(4);
        var correctPair = selectedPairs[correctAnswerIndex];

        string questionText;
        List<string> answerOptions;
        string explanation;

        if (direction == QuestionDirection.TermToDefinition)
        {
            // Traditional: show term, ask for definition
            questionText = $"What is the definition of \"{correctPair.Key}\"?";
            answerOptions = selectedPairs.Select(pair => pair.Value).ToList();
            explanation = $"The definition of \"{correctPair.Key}\" is: {correctPair.Value}";
        }
        else
        {
            // Bidirectional: show definition, ask for term
            questionText = $"Which term is defined as: \"{correctPair.Value}\"?";
            answerOptions = selectedPairs.Select(pair => pair.Key).ToList();
            explanation = $"The term for this definition is: \"{correctPair.Key}\"";
        }

        return new QuizQuestion
        {
            Term = correctPair.Key,
            Definition = correctPair.Value,
            Direction = direction,
            QuestionText = questionText,
            AnswerOptions = answerOptions,
            CorrectAnswerIndex = correctAnswerIndex,
            Explanation = explanation,
            Module = section.Module,
            Topic = section.Heading
        };
    }

    /// <summary>
    /// Generates a question where all answers come from bullet points in the same section.
    /// </summary>
    private QuizQuestion GenerateBulletPointQuestion(MarkdownSection section, List<MarkdownSection> allSections)
    {
        // Select 4 random bullet points
        var selectedBullets = section.BulletPoints
            .OrderBy(_ => Random.Shared.Next())
            .Take(4)
            .ToList();

        var correctAnswerIndex = Random.Shared.Next(4);
        var correctAnswer = selectedBullets[correctAnswerIndex];

        var questionText = $"Which of the following is related to {section.Heading}?";

        return new QuizQuestion
        {
            QuestionText = questionText,
            AnswerOptions = selectedBullets,
            CorrectAnswerIndex = correctAnswerIndex,
            Explanation = correctAnswer,
            Module = section.Module,
            Topic = section.Heading
        };
    }

    /// <summary>
    /// Generates a question with one correct answer from bullet points and distractors from other sections.
    /// </summary>
    private QuizQuestion GenerateBulletPointQuestionWithMixedDistractors(MarkdownSection section, List<MarkdownSection> allSections)
    {
        var correctAnswer = section.BulletPoints[Random.Shared.Next(section.BulletPoints.Count)];
        var answerOptions = new List<string> { correctAnswer };

        // Gather potential distractors from other sections
        var distractors = new List<string>();
        foreach (var otherSection in allSections.Where(s => s != section && s.BulletPoints.Count > 0))
        {
            distractors.AddRange(otherSection.BulletPoints);
        }

        // If not enough distractors, use content lines
        if (distractors.Count < 3)
        {
            foreach (var otherSection in allSections.Where(s => s != section && s.ContentLines.Count > 0))
            {
                distractors.AddRange(otherSection.ContentLines.Take(3));
            }
        }

        // Select 3 random distractors
        var selectedDistractors = distractors
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .ToList();

        // If still not enough distractors, create diverse generic ones
        var usedTemplates = new HashSet<string>();
        while (selectedDistractors.Count < 3)
        {
            var template = GenericDistractorTemplates[Random.Shared.Next(GenericDistractorTemplates.Length)];
            var distractor = string.Format(template, section.Heading);

            if (!selectedDistractors.Contains(distractor) && usedTemplates.Add(template))
            {
                selectedDistractors.Add(distractor);
            }
        }

        answerOptions.AddRange(selectedDistractors);

        // Shuffle answer options
        var shuffledAnswers = answerOptions.OrderBy(_ => Random.Shared.Next()).ToList();
        var correctAnswerIndex = shuffledAnswers.IndexOf(correctAnswer);

        var questionText = $"Which statement best describes an aspect of {section.Heading}?";

        return new QuizQuestion
        {
            QuestionText = questionText,
            AnswerOptions = shuffledAnswers,
            CorrectAnswerIndex = correctAnswerIndex,
            Explanation = $"{correctAnswer}",
            Module = section.Module,
            Topic = section.Heading
        };
    }

    /// <summary>
    /// Generates a concept-based question using content lines and headings.
    /// </summary>
    private QuizQuestion GenerateConceptQuestion(MarkdownSection section, List<MarkdownSection> allSections)
    {
        var correctAnswer = section.ContentLines.Count > 0 
            ? section.ContentLines[Random.Shared.Next(section.ContentLines.Count)]
            : section.Heading;

        var answerOptions = new List<string> { correctAnswer };

        // Generate distractors from other sections
        var distractors = new List<string>();
        foreach (var otherSection in allSections.Where(s => s != section))
        {
            if (otherSection.ContentLines.Count > 0)
            {
                distractors.AddRange(otherSection.ContentLines.Take(2));
            }
            if (otherSection.BulletPoints.Count > 0)
            {
                distractors.AddRange(otherSection.BulletPoints.Take(2));
            }
        }

        // Select 3 random distractors
        var selectedDistractors = distractors
            .OrderBy(_ => Random.Shared.Next())
            .Take(3)
            .ToList();

        // If still not enough distractors, create diverse generic ones
        var usedTemplates = new HashSet<string>();
        while (selectedDistractors.Count < 3)
        {
            var template = GenericDistractorTemplates[Random.Shared.Next(GenericDistractorTemplates.Length)];
            var distractor = string.Format(template, section.Heading);

            if (!selectedDistractors.Contains(distractor) && usedTemplates.Add(template))
            {
                selectedDistractors.Add(distractor);
            }
        }

        answerOptions.AddRange(selectedDistractors);

        // Shuffle answer options
        var shuffledAnswers = answerOptions.OrderBy(_ => Random.Shared.Next()).ToList();
        var correctAnswerIndex = shuffledAnswers.IndexOf(correctAnswer);

        var questionText = $"What is {section.Heading}?";

        return new QuizQuestion
        {
            QuestionText = questionText,
            AnswerOptions = shuffledAnswers,
            CorrectAnswerIndex = correctAnswerIndex,
            Explanation = $"{correctAnswer}",
            Module = section.Module,
            Topic = section.Heading
        };
    }
}
