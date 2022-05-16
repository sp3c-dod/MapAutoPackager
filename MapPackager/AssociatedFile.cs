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
        /// Relative path inside the game directory to the file
        /// </summary>
        public string RelativePath { get; set; }

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

        //TODO: a flag to compare checksum vs just file size (file size should go faster)
        /// <summary>
        /// Found two files in different game directories with the same file name, but the files are different
        /// </summary>
        public bool DuplicateWithDifferencesFound { get; set; }
    }
}
