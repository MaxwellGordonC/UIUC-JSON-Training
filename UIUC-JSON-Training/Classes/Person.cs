namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// A Person with a list of their completed trainings.
    /// </summary>
    internal class Person
    {
        // Name of the person.
        public string Name { get; set; }

        // List of trainings this person has completed.
        // Trainings of the same name with different dates
        // indicate that the person has completed the training
        // more than once.
        public required IList<Completion> Completions { get; set; }

        /// <summary>
        /// If a person has completed a training regardless of expiry.
        /// </summary>
        /// <param name="name">Name of training to check.</param>
        /// <returns>True, if a person has completed it.</returns>
        public bool HasCompletedTraining(string name)
        {
            // Loop through every completion.
            for (int i = 0; i < Completions.Count; i++)
            {
                // If the completion contains the training name we are looking for, then it has been completed.
                if (string.Equals(Completions[i].Name, name, StringComparison.OrdinalIgnoreCase) ) 
                {
                    return true;
                }
            }

            return false;
        }
    }
}