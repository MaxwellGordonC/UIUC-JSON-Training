using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using UIUC_JSON_Training.Classes;

internal class Program
{
    // Defines the start and end of a fiscal year.
    // The command arguments library used in this project
    // is limited to 8 arguments, so I opted for constants,
    // especially since these dates are unlikely to change.
    const int FISCAL_YEAR_START_MONTH = 7;
    const int FISCAL_YEAR_START_DAY = 1;
    const int FISCAL_YEAR_END_MONTH = 6;
    const int FISCAL_YEAR_END_DAY = 30;

    const string EXPIRED_MESSAGE = "Expired";
    const string EXPIRES_SOON_MESSAGE = "Expires soon";

    // Internal list of unique trainings.
    static Dictionary<string, Training> TrainingDict = new Dictionary<string, Training>();

    // Status of a training course.
    public enum ExpiryStatus
    {
        Expired,
        ExpiringSoon,
        NotExpired
    }

    /// <summary>
    /// Write errors to the console with red text.
    /// </summary>
    /// <param name="message">Error message</param>
    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Write success messages to the console with green text.
    /// </summary>
    /// <param name="message">Success message</param>
    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Write info messages to the console with yellow text.
    /// </summary>
    /// <param name="message">Info message</param>
    private static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(message);
        Console.ResetColor();
    }


    /// <summary>
    /// Mainline. Get the commandline arguments, and then process them.
    /// </summary>
    /// <param name="args">Commandline arguments</param>
    public static async Task<int> Main(string[] args)
    {
        // Define the arguments with their descriptions.
        // If an argument is not provided, a default will be used.
        var trainingDataOption = new Option<string>
        (
            "--trainingData",
            description: "Path to the training JSON data file",
            // If none is provided, use the relative execution path to the input folder.
            getDefaultValue: () => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\input", "trainings.json"))
        );

        var outputDirectoryOption = new Option<string>
        (
            "--outputDirectory",
            description: "The directory where the output data will be generated.",
            // If none is provided, use the output folder relative to the execution path.
            getDefaultValue: () => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\output"))
        );

        var expiryThresholdDateOption = new Option<string>
        (
            "--expiryThresholdDate",
            description: "Expiration threshold date in MM/DD/YYYY format",
            getDefaultValue: () => "10/01/2023"
        );

        var fiscalYearOption = new Option<int>
        (
            "--fiscalYear",
            description: "Fiscal year that trainings have been completed in",
            getDefaultValue: () => 2024
        );

        var trainingListOption = new Option<List<string>>
        (
            "--trainingList",
            description: "List of space-separated required trainings (e.g., \"Electrical Safety for Labs\" \"X-Ray Safety\")",
            getDefaultValue: () => new List<string> { "Electrical Safety for Labs", "X-Ray Safety", "Laboratory Safety Training" }
        )
        { AllowMultipleArgumentsPerToken = true };


        // Create the root command.
        var rootCommand = new RootCommand
        {
            trainingDataOption,
            outputDirectoryOption,
            expiryThresholdDateOption,
            fiscalYearOption,
            trainingListOption
        };

        // Handle the command with the OnHandleArgsAsync function.
        rootCommand.SetHandler(OnHandleArgsAsync, trainingDataOption, outputDirectoryOption, expiryThresholdDateOption, fiscalYearOption, trainingListOption);

        // Execute the command.
        var commandLineBuilder = new CommandLineBuilder( rootCommand ).UseDefaults();
        var parser = commandLineBuilder.Build();
        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Do basic processing on the commandline arguments.
    /// </summary>
    /// <param name="trainingDataPath">Path of training data JSON file.</param>
    /// <param name="expiryThresholdDate">"Expires soon" threshold date.</param>
    /// <param name="fiscalYear">The fiscal year.</param>
    /// <param name="trainingList">The list of training programs to be checked.</param>
    private static async Task OnHandleArgsAsync(string trainingDataPath, string outputDirectory, string expiryThresholdDate, int fiscalYear, List<string>trainingList)
    {
        // Validate the input file path.
        if (!File.Exists(trainingDataPath))
        {
            WriteError($"Error: The file '{trainingDataPath}' does not exist.");
            return;
        }

        // Validate the date.
        DateOnly expiryDate;
        if (!DateOnly.TryParse(expiryThresholdDate, out DateOnly thresholdDate))
        {
            WriteError("Error: The date provided is not in a valid format (MM/DD/YYYY).");
            return;
        }
        else
        {
            // Convert the date into a DateOnly.
            expiryDate = thresholdDate;
        }

        WriteInfo("Processing file: ");
        Console.WriteLine(trainingDataPath);
        
        WriteInfo("Output directory: ");
        Console.WriteLine(outputDirectory);

        WriteInfo("Threshold date: ");
        Console.WriteLine(expiryDate.ToShortDateString());

        WriteInfo("Fiscal year: ");
        Console.WriteLine(fiscalYear);

        // The trainings specified in the commandline args.
        List<Training> specifiedTrainings = new List<Training>();

        // List all of the trainings.
        StringBuilder trainingBuilder = new StringBuilder();
        for (int i = 0; i < trainingList.Count; i++)
        {
            specifiedTrainings.Add(new Training(trainingList[i]));
            trainingBuilder.Append(trainingList[i]);

            // Append commas and spaces for readability.
            if (i < trainingList.Count - 1)
            {
                trainingBuilder.Append(", ");
            }
        }

        WriteInfo("Trainings: ");
        Console.WriteLine(trainingBuilder.ToString());

        // Parse the JSON file.
        try
        {
            // Get the JSON file as a string.
            string jsonTrainingData = File.ReadAllText(trainingDataPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            List<Person> people = JsonSerializer.Deserialize<List<Person>>(jsonTrainingData, options);

            if (people is null)
            {
                WriteError("Error: No people found in the provided file.");
                return;
            }

            // Create a dictionary for trainings to avoid redundant looping.
            UpdateTrainingDictionary(people);

            // Spacing for the console output.
            Console.WriteLine();

            // Generate the output data.
            await ListCompletedTrainingsWithCountsAsync(people, outputDirectory);
            await ListGraduatesForYearAsync(outputDirectory, fiscalYear, specifiedTrainings);
            await ListPeopleWithExpiredCoursesAsync(people, expiryDate, outputDirectory);
        }
        catch (Exception ex)
        {
            WriteError("Error parsing JSON file:");
            WriteError(ex.Message);
        }
    }

    /// <summary>
    /// Update the global training dictionary to create a lookup
    /// of people who have graduated from a course.
    /// </summary>
    /// <param name="people">lList of people from input.</param>
    public static void UpdateTrainingDictionary(List<Person> people)
    {
        foreach (Person person in people)
        {
            foreach (Completion completion in person.Completions)
            {
                Training training;

                // Check if the training already exists in the dictionary.
                if (!TrainingDict.TryGetValue(completion.Name, out training))
                {
                    training = new Training(completion.Name);
                    TrainingDict[completion.Name] = training;
                }

                // Create a reverse lookup for trainings.
                completion.TrainingClass = training;
                training.IncrementCountForGraduate(person);
            }
        }
    }


    /// <summary>
    /// Requirement: List each completed training with a count of how many people have completed that training.
    /// </summary>
    /// <param name="people">List of people.</param>
    /// <param name="outputDirectory">Output directory.</param>
    public static async Task ListCompletedTrainingsWithCountsAsync(List<Person> people, string outputDirectory)
    {
        List<object> outputData = new List<object>();

        // Iterate through the existing TrainingDict to get counts.
        foreach (Training training in TrainingDict.Values)
        {
            outputData.Add(new
            {
                Name = training.Name,
                Count = training.GetGraduateCount(),
            });
        }

        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string outputFilePath = Path.Combine(outputDirectory, "CompletedTrainingsWithCounts.json");

        await File.WriteAllTextAsync(outputFilePath, jsonOutput);

        WriteSuccess("Completed trainings written to: ");
        Console.WriteLine(outputFilePath);
        Console.WriteLine();
    }

    /// <summary>
    /// Requirement: Given a list of trainings and a fiscal year,
    /// for each specified training, list all people that completed
    /// that training in the specified fiscal year.
    /// </summary>
    /// <param name="outputDirectory">Output directory.</param>
    /// <param name="fiscalYear">Year the training was completed. Defined as 7/1/n-1 – 6/30/n.</param>
    /// <param name="specifiedTrainings">List of specified trainings.</param>
    public static async Task ListGraduatesForYearAsync(string outputDirectory, int fiscalYear, List<Training> specifiedTrainings)
    {
        // Define the date range for the fiscal year.
        DateOnly fiscalYearStart = new DateOnly(fiscalYear - 1, FISCAL_YEAR_START_MONTH, FISCAL_YEAR_START_DAY);
        DateOnly fiscalYearEnd = new DateOnly(fiscalYear, FISCAL_YEAR_END_MONTH, FISCAL_YEAR_END_DAY);

        List<object> outputData = new List<object>();

        foreach (Training specifiedTraining in specifiedTrainings)
        {
            if (TrainingDict.TryGetValue(specifiedTraining.Name, out Training training))
            {
                // Use HashSet to avoid duplicates.
                HashSet<string> graduatesOfTraining = new HashSet<string>();

                // Now check each graduate's completions for this training.
                foreach (var graduate in training.Graduates)
                {
                    // Use LINQ to filter for completions within the fiscal year and for the specific training.
                    // "c" represents a completion object within a graduate.
                    bool bCompletedInFiscalYear = graduate.Completions.Any
                    (
                        c => c.Name == training.Name
                        && c.Timestamp >= fiscalYearStart
                        && c.Timestamp <= fiscalYearEnd
                    );

                    // Add the graduate's name if they completed the training within the fiscal year.
                    if (bCompletedInFiscalYear)
                    {
                        graduatesOfTraining.Add(graduate.Name);
                    }
                }

                // Add this training and its graduates to the output data.
                outputData.Add(new
                {
                    Training = training.Name,
                    Graduates = graduatesOfTraining.ToList()
                });
            }
        }

        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string outputFilePath = Path.Combine(outputDirectory, $"GraduatesFiscalYear.json");

        await File.WriteAllTextAsync(outputFilePath, jsonOutput);

        WriteSuccess($"Graduate list for fiscal year {fiscalYear} written to: ");
        Console.WriteLine(outputFilePath);
        Console.WriteLine();
    }


    /// <summary>
    /// Gets the expiry status for a completion.
    /// </summary>
    /// <param name="completion">Completion training to be checked.</param>
    /// <param name="referenceDate">Date to check against.</param>
    /// <returns>The expiry status.</returns>
    public static ExpiryStatus GetExpiryStatus(Completion completion, DateOnly referenceDate)
    {
        // If no expiration is given, it never expires.
        if (completion.Expires is null)
        {
            return ExpiryStatus.NotExpired;
        }

        // Cast, since completion.Expires is a DateOnly?. 
        DateOnly expirationDate = (DateOnly)completion.Expires;

        if (expirationDate < referenceDate)
        {
            // The expiration date is less than the reference date, meaning it has already passed.
            return ExpiryStatus.Expired;
        }
        else if (expirationDate <= referenceDate.AddMonths(1))
        {
            // The expiration date is within one month.
            return ExpiryStatus.ExpiringSoon;
        }
        else
        {
            return ExpiryStatus.NotExpired;
        }
    }


    /// <summary>
    /// Requirement: Given a date, find all people that have any completed trainings
    /// that have already expired, or will expire within one month of the specified date.
    /// A training is considered expired the day after its expiration date.
    /// For each person found, list each completed training that met the previous criteria,
    /// with an additional field to indicate expired vs expires soon.
    /// </summary>
    /// <param name="people">The people from the training data.</param>
    /// <param name="expiryDate">The date to consider courses expired.</param>
    /// <param name="outputDirectory">Output directory.</param>
    public static async Task ListPeopleWithExpiredCoursesAsync(List<Person> people, DateOnly expiryDate, string outputDirectory)
    {
        List<object> outputData = new List<object>();

        // Look through each person to find expired trainings.
        foreach (Person person in people)
        {
            // Early exit if no completions exist.
            if (person.Completions.Count == 0)
            {
                continue;
            }

            var personEntry = new
            {
                Name = person.Name,
                Trainings = new List<ExpiredTraining>()
            };

            // Check each completion for expiration.
            foreach (Completion completion in person.Completions)
            {
                ExpiryStatus status = GetExpiryStatus(completion, expiryDate);

                // Ignore it if it's not expired.
                if (status == ExpiryStatus.NotExpired)
                {
                    continue;
                }

                string expiryMessage = status == ExpiryStatus.Expired ? EXPIRED_MESSAGE : EXPIRES_SOON_MESSAGE;

                personEntry.Trainings.Add(new ExpiredTraining
                {
                    Training = completion.Name,
                    Expires = expiryMessage
                });
            }

            // Only add the person entry if they have trainings.
            if (personEntry.Trainings.Count > 0)
            {
                outputData.Add(personEntry);
            }
        }

        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        string outputFilePath = Path.Combine(outputDirectory, $"ExpiredTrainings.json");

        await File.WriteAllTextAsync(outputFilePath, jsonOutput);

        WriteSuccess($"Expired training list for {expiryDate} written to: ");
        Console.WriteLine(outputFilePath);
        Console.WriteLine();
    }
}