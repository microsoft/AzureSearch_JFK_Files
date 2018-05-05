namespace Microsoft.CognitiveSearch
{
    public static class Config
    {
        /**************  UPDATE THESE CONSTANTS WITH YOUR SETTINGS  **************/
        public const string AZURE_STORAGE_CONTAINER_NAME = "imagestoreblob";
        public const string AZURE_SEARCH_INDEX_NAME = "jfkindex";

        // Redaction classifier endpoint we are providing to you.
        public const string REDACTION_ENDPOINT = "https://jfk-redaction-classifier.azurewebsites.net/score";

        /*************************************************************************/

    }
}
