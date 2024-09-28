namespace UIUC_JSON_Training.Classes
{
    /// <summary>
    /// A class representing a completed training module.
    /// </summary>
    internal class Completion
    {
        // Name of completed training.
        private string Name {  get; set; }

        // Date of completion.
        private DateTime Timestamp { get; set; }

        // When the completion is no longer considered valid.
        // Null implies that it never expires.
        private DateTime? Expires { get; set; }
    }
}