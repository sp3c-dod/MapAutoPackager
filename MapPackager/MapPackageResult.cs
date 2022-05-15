using System.Collections.Generic;

namespace MapPackager
{
    /// <summary>
    /// 
    /// </summary>
    public class MapPackageResult
    {
        /// <summary>
        /// 
        /// </summary>
        public bool ZipCreationSuccesful { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ZipFilePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<AssociatedFile> AssociatedFiles { get; set; }
    }
}
