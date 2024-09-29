using System.Text.Json.Serialization;

namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// A class representing a completed training module.
    /// </summary>
    internal class Completion
    {
        // Name of completed training.
        public string Name {  get; set; }

        // Date of completion.
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateOnly Timestamp { get; set; }

        // When the completion is no longer considered valid.
        // Null implies that it never expires.
        [JsonConverter(typeof(DateOnlyConverter))]
        public DateOnly? Expires { get; set; }

        // A link to the object for the completed course.
        [NonSerialized()]
        public Training TrainingClass;
    }
}