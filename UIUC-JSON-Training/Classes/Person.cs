namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// A Person with a list of their completed trainings.
    /// </summary>
    internal class Person
    {
        // Name of the person.
        private string Name { get; set; }

        // List of trainings this person has completed.
        // Trainings of the same name with different dates
        // indicate that the person has completed the training
        // more than once.
        private IList<Completion> Completions { get; set; }
    }
}