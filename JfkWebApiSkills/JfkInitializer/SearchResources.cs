using System.Collections.Generic;
using System.Configuration;
using Microsoft.Azure.Search.Models;

namespace JfkInitializer
{
    static class SearchResources
    {
        public static DataSource GetDataSource(string name) =>
            DataSource.AzureBlobStorage(
                name: name,
                storageConnectionString: ConfigurationManager.AppSettings["JFKFilesBlobStorageAccountConnectionString"],
                containerName: ConfigurationManager.AppSettings["JFKFilesBlobContainerName"],
                description: "Data source for cognitive search example"
            );

        public static Skillset GetSkillset(string name, string azureFunctionHostKey, string blobContainerNameForImageStore)
        {
            string azureFunctionEndpointUri = string.Format("https://{0}.azurewebsites.net", ConfigurationManager.AppSettings["AzureFunctionSiteName"]);
            return new Skillset()
            {
                Name = name,
                Description = "JFK Files Skillset",
                Skills = new List<Skill>()
                {
                    new OcrSkill()
                    {
                        Context = "/document/normalized_images/*",
                        DefaultLanguageCode = OcrSkillLanguage.En,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "image", source: "/document/normalized_images/*")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "text"),
                            new OutputFieldMappingEntry(name: "layoutText")
                        }
                    },
                    new ImageAnalysisSkill()
                    {
                        Context = "/document/normalized_images/*",
                        VisualFeatures = new List<VisualFeature>() { VisualFeature.Tags, VisualFeature.Description },
                        Details = new List<ImageDetail>() { ImageDetail.Celebrities },
                        DefaultLanguageCode = ImageAnalysisSkillLanguage.En,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "image", source: "/document/normalized_images/*")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "tags", targetName: "Tags"),
                            new OutputFieldMappingEntry(name: "description", targetName: "Description")
                        }
                    },
                    new MergeSkill()
                    {
                        Description = "Merge native text content and inline OCR content where images were present",
                        Context = "/document",
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/content"),
                            new InputFieldMappingEntry(name: "itemsToInsert", source: "/document/normalized_images/*/text"),
                            new InputFieldMappingEntry(name: "offsets", source: "/document/normalized_images/*/contentOffset")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText", targetName: "nativeTextAndOcr")
                        }
                    },
                    new MergeSkill()
                    {
                        Description = "Merge text content with image captions",
                        Context = "/document",
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/nativeTextAndOcr"),
                            new InputFieldMappingEntry(name: "itemsToInsert", source: "/document/normalized_images/*/Description/captions/*/text")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText", targetName: "fullTextAndCaptions")
                        }
                    },
                    new MergeSkill()
                    {
                        Description = "Merge text content with image tags",
                        Context = "/document",
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/fullTextAndCaptions"),
                            new InputFieldMappingEntry(name: "itemsToInsert", source: "/document/normalized_images/*/Tags/*/name")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText", targetName: "finalText")
                        }
                    },
                    new SplitSkill()
                    {
                        Description = "Split text into pages for subsequent skill processing",
                        Context = "/document/finalText",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = 5000,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/finalText")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "textItems", targetName: "pages")
                        }

                    },
                    new LanguageDetectionSkill()
                    {
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/finalText")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "languageCode")
                        }
                    },
                    new EntityRecognitionSkill()
                    {
                        Context = "/document/finalText/pages/*",
                        Categories = new List<EntityCategory>() { EntityCategory.Person, EntityCategory.Location, EntityCategory.Organization },
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text", source: "/document/finalText/pages/*"),
                            new InputFieldMappingEntry(name: "languageCode", source: "/document/languageCode")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "persons", targetName: "people"),
                            new OutputFieldMappingEntry(name: "locations"),
                            new OutputFieldMappingEntry(name: "organizations"),
                            new OutputFieldMappingEntry(name: "namedEntities", targetName: "entities")
                        }
                    },
                    new ShaperSkill()
                    {
                        Description = "Create a custom OCR image metadata object used to generate an HOCR document",
                        Context = "/document/normalized_images/*",
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "layoutText", source: "/document/normalized_images/*/layoutText"),
                            new InputFieldMappingEntry(name: "imageStoreUri", source: "/document/normalized_images/*/imageStoreUri"),
                            new InputFieldMappingEntry(name: "width", source: "/document/normalized_images/*/width"),
                            new InputFieldMappingEntry(name: "height", source: "/document/normalized_images/*/height")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "output", targetName: "ocrImageMetadata")
                        }
                    },
                    new WebApiSkill()
                    {
                        Description = "Upload image data to the annotation store",
                        Context = "/document/normalized_images/*",
                        Uri = string.Format("{0}/api/image-store?code={1}", azureFunctionEndpointUri, azureFunctionHostKey),
                        HttpHeaders = new Dictionary<string, string>()
                        {
                            ["BlobContainerName"] = blobContainerNameForImageStore
                        },
                        BatchSize = 1,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "imageData", source: "/document/normalized_images/*/data")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "imageStoreUri")
                        }
                    },
                    new WebApiSkill()
                    {
                        Description = "Generate HOCR for webpage rendering",
                        Context = "/document",
                        Uri = string.Format("{0}/api/hocr-generator?code={1}", azureFunctionEndpointUri, azureFunctionHostKey),
                        BatchSize = 1,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "ocrImageMetadataList", source: "/document/normalized_images/*/ocrImageMetadata"),
                            new InputFieldMappingEntry(name: "wordAnnotations", source: "/document/cryptonyms")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "hocrDocument")
                        }
                    },
                    new WebApiSkill()
                    {
                        Description = "Cryptonym linker",
                        Context = "/document",
                        Uri = string.Format("{0}/api/link-cryptonyms-list?code={1}", azureFunctionEndpointUri, azureFunctionHostKey),
                        BatchSize = 1,
                        Inputs = new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "words", source: "/document/normalized_images/*/layoutText/words/*/text")
                        },
                        Outputs = new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "cryptonyms")
                        }
                    }
                },
                CognitiveServices = new CognitiveServicesByKey(key: ConfigurationManager.AppSettings["CognitiveServicesAccountKey"])
            };
        }

        public static SynonymMap GetSynonymMap(string name) =>
            new SynonymMap(
                name: name,
                synonyms: @"GPFLOOR,oswold,ozwald,ozwold,oswald
                            silvia, sylvia
                            sever, SERVE, SERVR, SERVER
                            novenko, nosenko, novenco, nosenko"
            );

        public static Index GetIndex(string name, string synonymMapName) => new Index()
        {
            Name = name,
            Fields = new List<Field>()
            {
                new Field("id",              DataType.String)                      { IsSearchable = true,  IsFilterable = true,  IsRetrievable = true, IsSortable = true,  IsFacetable = false, IsKey = true },
                new Field("fileName",        DataType.String)                      { IsSearchable = false, IsFilterable = false, IsRetrievable = true, IsSortable = false, IsFacetable = false },
                new Field("metadata",        DataType.String)                      { IsSearchable = false, IsFilterable = false, IsRetrievable = true, IsSortable = false, IsFacetable = false },
                new Field("text",            DataType.String)                      { IsSearchable = true,  IsFilterable = false, IsRetrievable = true, IsSortable = false, IsFacetable = false, SynonymMaps = new List<string>() { synonymMapName } },
                new Field("entities",        DataType.Collection(DataType.String)) { IsSearchable = false, IsFilterable = true,  IsRetrievable = true, IsSortable = false, IsFacetable = true  },
                new Field("cryptonyms",      DataType.Collection(DataType.String)) { IsSearchable = false, IsFilterable = true,  IsRetrievable = true, IsSortable = false, IsFacetable = true  },
                new Field("demoBoost",       DataType.Int32)                       { IsSearchable = false, IsFilterable = true,  IsRetrievable = true, IsSortable = false, IsFacetable = false },
                new Field("demoInitialPage", DataType.Int32)                       { IsSearchable = false, IsFilterable = false, IsRetrievable = true, IsSortable = false, IsFacetable = false },
            },
            ScoringProfiles = new List<ScoringProfile>()
            {
                new ScoringProfile()
                {
                    Name = "demoBooster",
                    FunctionAggregation = ScoringFunctionAggregation.Sum,
                    Functions = new List<ScoringFunction>()
                    {
                        new MagnitudeScoringFunction()
                        {
                            FieldName = "demoBoost",
                            Interpolation = ScoringFunctionInterpolation.Linear,
                            Boost = 1000,
                            Parameters = new MagnitudeScoringParameters()
                            {
                                BoostingRangeStart = 0,
                                BoostingRangeEnd = 100,
                                ShouldBoostBeyondRangeByConstant = true
                            }
                        }
                    }
                }
            },
            CorsOptions = new CorsOptions()
            {
                AllowedOrigins = new List<string>() { "*" }
            },
            Suggesters = new List<Suggester>()
            {
                new Suggester()
                {
                    Name = "sg-jfk",
                    SourceFields = new List<string>() { "entities" }
                }
            }
        };

        public static Indexer GetIndexer(string name, string dataSourceName, string indexName, string skillsetName) => new Indexer()
        {
            Name = name,
            DataSourceName = dataSourceName,
            TargetIndexName = indexName,
            SkillsetName = skillsetName,
            Parameters = new IndexingParameters()
            {
                BatchSize = 1,
                MaxFailedItems = 0,
                MaxFailedItemsPerBatch = 0,
                Configuration = new Dictionary<string, object>()
                {
                    ["dataToExtract"] = "contentAndMetadata",
                    ["imageAction"] = "generateNormalizedImages",
                    ["normalizedImageMaxWidth"] = 2000,
                    ["normalizedImageMaxHeight"] = 2000
                }
            },
            FieldMappings = new List<FieldMapping>()
            {
                new FieldMapping() { SourceFieldName = "metadata_storage_name",           TargetFieldName = "fileName"        },
                new FieldMapping() { SourceFieldName = "metadata_custom_demoBoost",       TargetFieldName = "demoBoost"       },
                new FieldMapping() { SourceFieldName = "metadata_custom_demoInitialPage", TargetFieldName = "demoInitialPage" }
            },
            OutputFieldMappings = new List<FieldMapping>()
            {
                new FieldMapping() { SourceFieldName = "/document/finalText",                          TargetFieldName = "text"       },
                new FieldMapping() { SourceFieldName = "/document/hocrDocument/metadata",              TargetFieldName = "metadata"   },
                new FieldMapping() { SourceFieldName = "/document/finalText/pages/*/entities/*/value", TargetFieldName = "entities"   },
                new FieldMapping() { SourceFieldName = "/document/cryptonyms",                         TargetFieldName = "cryptonyms" }
            }
        };
    }
}
