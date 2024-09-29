namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// Represents a training course.
    /// </summary>
    internal class Training
    {
        public Training(string name)
        {
            Name = name;
            Graduates = new Dictionary<Person, int>();
        }

        // Course name.
        public string Name { get; set; }

        public Dictionary<Person, int> Graduates;

        /// <summary>
        /// Increment or create the count for a person who has completed the course.
        /// </summary>
        /// <param name="graduate">The person who completed the course.</param>
        public void IncrementCountForGraduate(Person graduate)
        {
            // Initialize it.
            if (!Graduates.ContainsKey(graduate))
            {
                Graduates[graduate] = 0;
            }
            
            Graduates[graduate]++;
        }
    }
}
