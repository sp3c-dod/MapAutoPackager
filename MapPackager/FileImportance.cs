namespace MapPackager
{
    public enum FileImportance
    {
        /// <summary>
        /// This file is required for the map to load in the game without errors
        /// </summary>
        Required,

        /// <summary>
        /// This file is important for the map to work as intended, but the map will load without them (e.g. a sound)
        /// </summary>
        Important,

        /// <summary>
        /// This file enhancing the user's experience of the map, but doesn't affect the map itself (e.g. minimap overview, config, etc..)
        /// </summary>
        Optional,

        /// <summary>
        /// This file relates to the map, but is not part of the map itself (e.g. bot waypoints, readme.txt, etc...)
        /// </summary>
        Extra
    }
}
