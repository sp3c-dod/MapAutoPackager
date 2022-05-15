namespace MapPackager
{
    /// <summary>
    /// A file associated with the map
    /// </summary>
    public class AssociatedFile
    {
        /// <summary>
        /// Local path to the file. This will only be populated if the file is found.
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// Name of the file associated with the .bsp file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Importance level of the file to the map
        /// </summary>
        public FileImportance FileImportance { get; set; }

        /// <summary>
        /// Whether or the file located and is included in the package
        /// </summary>
        public bool Exists { get; set; }
    }
}
