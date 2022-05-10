namespace MapPackager
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceFileResult
    {
        /// <summary>
        /// 
        /// </summary>
        public bool SuccessfullyGenerated { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] ResGenFileList { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string RawResGenOutput { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
