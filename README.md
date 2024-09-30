# Application Developer Programming Exercise
This is a small, modular, and efficient C# application made for my UIUC Application Developer application.

# Notes
- All of the input parameters can be changed using commandline arguments.
  - For simplicity, if no arguments are provided, defaults will be used.
  - Default directories (if none are provided) assume that the project is being run in Visual Studio in debug mode. The program will attempt to step back 4 levels in order to find the "input" and "output" folders.
- This was written using .NET 8.0
- This project **depends on** System.CommandLine: https://www.nuget.org/packages/System.CommandLine
 
## Command-Line Arguments
### `--trainingData`
- **Description**: Path to the training JSON data file.
- **Default**: If no path is provided, the default will be the `trainings.json` file located in the `input` folder relative to the execution path.

### `--outputDirectory`
- **Description**: The directory where the output data will be generated.
- **Default**: If no directory is provided, the default will be the `output` folder relative to the execution path.

### `--expiryThresholdDate`
- **Description**: Expiration threshold date in `MM/DD/YYYY` format. Trainings that expire on or before this date will be considered for output.
- **Default**: `10/01/2023`.

### `--fiscalYear`
- **Description**: The fiscal year that trainings have been completed in.
- **Default**: `2024`.

### `--trainingList`
- **Description**: List of space-separated required trainings (e.g., `"Electrical Safety for Labs"` `"X-Ray Safety"`). This is the list of trainings you want to check for expiration or near expiration.
- **Default**: `"Electrical Safety for Labs" "X-Ray Safety" "Laboratory Safety Training"`.

### Usage example with custom args:
`UIUC-JSON-Training --fiscalYear=2023 --expiryThresholdDate="11/01/2023"`

## Requirements
- Reads all data from a .Json file.
- Generate output as JSON in the three following ways.
  - List each completed training with a count of how many people have completed that training.
  - Given a list of trainings and a fiscal year (defined as 7/1/n-1 – 6/30/n), for each specified training, list all people that completed that training in the specified fiscal year.
     - Use parameters: Trainings = "Electrical Safety for Labs", "X-Ray Safety", "Laboratory Safety Training"; Fiscal Year = 2024
  - Given a date, find all people that have any completed trainings that have already expired, or will expire within one month of the specified date (A training is considered expired the day after its expiration date). For each person found, list each completed training that met the previous criteria, with an additional field to indicate expired vs expires soon.
    - Use date: Oct 1st, 2023
-  A note for all tasks. It is possible for a person to have completed the same training more than once. In this event, only the most recent completion should be considered.

### Requirements for the above application:
1. The app should work with any data in the specified format.
2. The app should be checked into a publicly accessible Github or Azure Devops repository that the reviewers can pull and run, without any modification.
3. In addition to the application code, your repository should contain the three output .Json files.
