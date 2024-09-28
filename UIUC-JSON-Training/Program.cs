using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Define the arguments with their descriptions.
        var trainingDataOption = new Option<string>
        (
            "--trainingData",
            description: "Path to the training JSON data file"
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
            expiryThresholdDateOption,
            fiscalYearOption,
            trainingListOption
        };

        rootCommand.SetHandler<string, string, int, List<string>>(OnHandleArgs, trainingDataOption, expiryThresholdDateOption, fiscalYearOption, trainingListOption);

        // Execute the command
        var commandLineBuilder = new CommandLineBuilder( rootCommand ).UseDefaults();
        var parser = commandLineBuilder.Build();
        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    private static void OnHandleArgs(string trainingDataPath, string expiryThresholdDate, int fiscalYear, List<string>trainingList)
    {
        // Validate the input file path.
        if (!File.Exists(trainingDataPath))
        {
            Console.WriteLine($"Error: The file '{trainingDataPath}' does not exist.");
            return;
        }

        // Validate the date.
        if (!DateTime.TryParse(expiryThresholdDate, out DateTime thresholdDate))
        {
            Console.WriteLine("Error: The date provided is not in a valid format (MM/DD/YYYY).");
            return;
        }

        // Process the logic here.
        Console.WriteLine($"Processing file: {trainingDataPath}");
        Console.WriteLine($"Threshold date: {thresholdDate.ToShortDateString()}");
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
    }
}
