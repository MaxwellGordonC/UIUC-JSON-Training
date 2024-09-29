namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// Represents a training course.
    /// </summary>
    internal class Training
    {
        // Course name.
        public string Name { get; set; }

        // Everyone who has taken this course.
        public HashSet<Person> Graduates { get; private set; }

        public Training(string name)
        {
            Name = name;
            Graduates = new HashSet<Person>();
        }

        /// <summary>
        /// Increment or create the count for a person who has completed the course.
        /// </summary>
        /// <param name="person">The person who completed the course.</param>
        public void IncrementCountForGraduate(Person person)
        {
            // Hash sets are unique, so they will not get added more than once.
            Graduates.Add(person);
        }

        /// <summary>
        /// Gets the number of graduates for a given course.
        /// </summary>
        /// <returns></returns>
        public int GetGraduateCount()
        {
            return Graduates.Count;
        }
    }
}
