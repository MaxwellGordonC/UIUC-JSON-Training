using System.Collections.Generic;
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
    // Especially since these dates are unlikely to change.
    const int FISCAL_YEAR_START_MONTH = 7;
    const int FISCAL_YEAR_START_DAY = 1;
    const int FISCAL_YEAR_END_MONTH = 6;
    const int FISCAL_YEAR_END_DAY = 30;

    // Internal list of unique trainings.
    static Dictionary<string, Training> TrainingDict = new Dictionary<string, Training>();

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
    /// Mainline. Get the commandline arguments, and then process them.
    /// </summary>
    /// <param name="args">Commandline arguments</param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        // Define the arguments with their descriptions.
        var trainingDataOption = new Option<string>
        (
            "--trainingData",
            description: "Path to the training JSON data file"
        )
        { IsRequired = true };

        var outputDirectoryOption = new Option<string>
        (
            "--outputDirectory",
            description: "The directory where the output data will be generated."
        )
        { IsRequired = true };

        var expiryThresholdDateOption = new Option<string>
        (
            "--expiryThresholdDate",
            description: "Expiration threshold date in MM/DD/YYYY format"
        )
        { IsRequired = true };

        var fiscalYearOption = new Option<int>
        (
            "--fiscalYear",
            description: "Fiscal year that trainings have been completed in"
        )
        { IsRequired = true };

        var trainingListOption = new Option<List<string>>
        (
            "--trainingList",
            description: "List of space-separated required trainings (e.g., \"Electrical Safety for Labs\" \"X-Ray Safety\")"
        )
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };


        // Create the root command.
        var rootCommand = new RootCommand
        {
            trainingDataOption,
            outputDirectoryOption,
            expiryThresholdDateOption,
            fiscalYearOption,
            trainingListOption
        };

        // Handle the command with the OnHandleArgs function.
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

        Console.WriteLine($"Processing file: {trainingDataPath}");
        Console.WriteLine($"Output directory: {outputDirectory}");
        Console.WriteLine($"Threshold date: {expiryDate.ToShortDateString()}");
        Console.WriteLine($"Fiscal year: {fiscalYear}");

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
        Console.WriteLine($"Trainings: {trainingBuilder.ToString()}");

        // Parse the JSON file.
        try
        {
            // Get the JSON file as a string.
            string jsonTrainingData = File.ReadAllText(trainingDataPath);

            // Configure JsonSerializerOptions for case-insensitive matching.
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            // Deserialize the data and create objects to represent it.
            List<Person> people = JsonSerializer.Deserialize<List<Person>>(jsonTrainingData, options);

            if (people is null)
            {
                WriteError("Error: No people found in the provided file.");
                return;
            }

            // Generate the output data.
            UpdateTrainingDictionary(people);
            await ListCompletedTrainingsWithCountsAsync(people, outputDirectory);
            await ListGraduatesForYearAsync(outputDirectory, fiscalYear, specifiedTrainings);
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
        // Prepare the output data.
        List<object> outputData = new List<object>();

        // Iterate through the existing TrainingDict to get counts.
        foreach (Training training in TrainingDict.Values)
        {
            outputData.Add(new
            {
                Name = training.Name,
                Count = training.GetGraduateCount(), // Directly get the count from the Training object
            });
        }

        // Serialize the data to JSON.
        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Define the output file path.
        string outputFilePath = Path.Combine(outputDirectory, "CompletedTrainingsWithCounts.json");

        // Write the JSON to a file.
        await File.WriteAllTextAsync(outputFilePath, jsonOutput);

        Console.WriteLine($"Output written to: {outputFilePath}");
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

        // Prepare the output data.
        var outputData = new List<object>();

        // Iterate over the specified trainings.
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
                    Graduates = graduatesOfTraining.ToList()  // Convert HashSet to List for serialization.
                });
            }
        }

        // Serialize the data to JSON.
        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Define the output file path.
        string outputFilePath = Path.Combine(outputDirectory, $"GraduatesFiscalYear{fiscalYear}.json");

        // Write the file.
        await File.WriteAllTextAsync(outputFilePath, jsonOutput);

        Console.WriteLine($"Graduate list for fiscal year {fiscalYear} written to: {outputFilePath}");
    }

    /// <summary>
    /// Requirement: Given a date, find all people that have any completed trainings
    /// that have already expired, or will expire within one month of the specified date.
    /// A training is considered expired the day after its expiration date.
    /// For each person found, list each completed training that met the previous criteria,
    /// with an additional field to indicate expired vs expires soon.
    /// </summary>
    /// <param name="expiryDate">The date to consider courses expired.</param>
    /// <param name="outputDirectory">Output directory.</param>
    public static async Task ListPeopleWithExpiredCoursesAsync(DateOnly expiryDate, string outputDirectory)
    {
        /**
         * for each person
         *  for each completoin
         *      if expiry date is expired
         *          add training to a list of expired trainings
         *      else if date is a month away from expiring
         *          add training to a list of almost expired trainings
         */
    }

}