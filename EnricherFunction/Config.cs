using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnricherFunction
{
    public static class Config
    {
        /**************  UPDATE THESE CONSTANTS WITH YOUR SETTINGS  **************/

        // Azure Blob Storage used to store extracted page images
        public const string IMAGE_AZURE_STORAGE_ACCOUNT_NAME = "";
        public const string IMAGE_BLOB_STORAGE_ACCOUNT_KEY = "";

        // Cognitive Services Vision API used to process images
        public const string VISION_API_KEY = "";
        // The region URL base should match where you deployed your cognitive service to.  default is westus.
        // For list of region urls see https://westus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fa
        public const string VISION_API_REGION = "westus.api.cognitive.microsoft.com";

        // Cognitive Entity Linking Service
        public const string ENTITY_LINKING_API_KEY = "";

        // Azure Search service used to index documents
        public const string AZURE_SEARCH_SERVICE_NAME = "";
        public const string AZURE_SEARCH_ADMIN_KEY = "";

        /*************************************************************************/

        // settings you can change if you want but the defaults should work too
        public const string IMAGE_BLOB_STORAGE_CONTAINER = "jfkimages";
        public const string LIBRARY_BLOB_STORAGE_CONTAINER = "jfk";
        public const string AZURE_SEARCH_INDEX_NAME = "jfkdocs";
    }
}
