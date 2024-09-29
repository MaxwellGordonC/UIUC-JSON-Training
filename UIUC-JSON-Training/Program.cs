using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using UIUC_JSON_Training.Classes;

internal class Program
{
    // Internal list of unique trainings.
    internal static List<Training> Trainings = new List<Training>();

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
        rootCommand.SetHandler<string, string, string, int, List<string>>(OnHandleArgs, trainingDataOption, outputDirectoryOption, expiryThresholdDateOption, fiscalYearOption, trainingListOption);

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
    private static void OnHandleArgs(string trainingDataPath, string outputDirectory, string expiryThresholdDate, int fiscalYear, List<string>trainingList)
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

        // List all of the trainings.
        StringBuilder trainingBuilder = new StringBuilder();
        for (int i = 0; i < trainingList.Count; i++)
        {
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
             ListCompletedTrainingsWithCounts(people, outputDirectory);
        }
        catch (Exception ex)
        {
            WriteError("Error parsing JSON file:");
            WriteError(ex.Message);
        }
    }

    /// <summary>
    /// Requirement: List each completed training with a count of how many people have completed that training.
    /// </summary>
    /// <param name="trainingList">JSON training data.</param>
    /// <param name="outputDirectory">Output directory.</param>
    public static void ListCompletedTrainingsWithCounts(List<Person> people, string outputDirectory)
    {
        // Dictionary to track unique trainings and their completion counts.
        // The string is the completion training name from the JSON data.
        Dictionary<string, Training> trainingDict = new Dictionary<string, Training>();

        // Loop through each person and their completions.
        foreach (Person person in people)
        {
            foreach (Completion completion in person.Completions)
            {
                Training training;
                // Check if the training already exists in the dictionary.
                if (!trainingDict.TryGetValue(completion.Name, out training))
                {
                    // If it does not exist, create a new Training object and add it to the dictionary.
                    training = new Training(completion.Name);
                    trainingDict[completion.Name] = training;
                }

                training.IncrementCountForGraduate(person);
            }
        }

        // Clear the existing Trainings list and populate it with unique trainings.
        Trainings.Clear();
        Trainings.AddRange(trainingDict.Values);


        // Prepare the output data structure.
        var outputData = new List<object>();

        foreach (Training training in Trainings)
        {
            outputData.Add(new
            {
                Name = training.Name,
                Count = training.Graduates.Count,
            });
        }

        // Serialize the data to JSON.
        string jsonOutput = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Define the output file path.
        string outputFilePath = Path.Combine(outputDirectory, "CompletedTrainings.json");

        // Write the JSON to a file.
        File.WriteAllText(outputFilePath, jsonOutput);

        // Optional: Print a confirmation message.
        Console.WriteLine($"Output written to: {outputFilePath}");
    }
}