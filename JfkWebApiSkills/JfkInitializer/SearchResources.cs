using Azure.Search.Documents.Indexes.Models;
using System.Collections.Generic;
using System.Configuration;

namespace JfkInitializer
{
    static class SearchResources
    {
        public static SearchIndexerDataSourceConnection GetDataSource(string name) =>
            new SearchIndexerDataSourceConnection(
                name: name,
                type: SearchIndexerDataSourceType.AzureBlob,
                connectionString: ConfigurationManager.AppSettings["JFKFilesBlobStorageAccountConnectionString"],
                container: new SearchIndexerDataContainer(ConfigurationManager.AppSettings["JFKFilesBlobContainerName"]))
            {
                Description = "Data source for cognitive search example"
            };

        public static SearchIndexerSkillset GetSkillset(string name, string blobContainerNameForImageStore)
        {
            string azureFunctionEndpointUri = string.Format("https://{0}.azurewebsites.net", ConfigurationManager.AppSettings["AzureFunctionSiteName"]);
            return new SearchIndexerSkillset(
                name: name, 
                skills: new List<SearchIndexerSkill>()
                {
                    new OcrSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "image")
                            {
                                Source = "/document/normalized_images/*"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "text"),
                            new OutputFieldMappingEntry(name: "layoutText")
                        })
                    {
                        Context = "/document/normalized_images/*",
                        DefaultLanguageCode = OcrSkillLanguage.En
                    },
                    new ImageAnalysisSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "image")
                            {
                                Source = "/document/normalized_images/*"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "tags") 
                            { 
                                TargetName = "Tags"
                            },
                            new OutputFieldMappingEntry(name: "description")
                            {
                                TargetName = "Description"
                            }
                        })
                    {
                        Context = "/document/normalized_images/*",
                        VisualFeatures = { VisualFeature.Tags, VisualFeature.Description },
                        Details = { ImageDetail.Celebrities },
                        DefaultLanguageCode = ImageAnalysisSkillLanguage.En
                    },
                    new MergeSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text") 
                            { 
                                Source = "/document/content"
                            },
                            new InputFieldMappingEntry(name: "itemsToInsert") 
                            { 
                                Source = "/document/normalized_images/*/text"
                            },
                            new InputFieldMappingEntry(name: "offsets")
                            {
                                Source = "/document/normalized_images/*/contentOffset"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText")
                            {
                                TargetName = "nativeTextAndOcr"
                            }
                        })
                    {
                        Description = "Merge native text content and inline OCR content where images were present",
                        Context = "/document"
                    },
                    new MergeSkill(                        
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text") 
                            { 
                                Source = "/document/nativeTextAndOcr"
                            },
                            new InputFieldMappingEntry(name: "itemsToInsert")
                            {
                                Source = "/document/normalized_images/*/Description/captions/*/text"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText")
                            {
                                TargetName = "fullTextAndCaptions"
                            }
                        })
                    {
                        Description = "Merge text content with image captions",
                        Context = "/document"
                    },
                    new MergeSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text") 
                            { 
                                Source = "/document/fullTextAndCaptions"
                            },
                            new InputFieldMappingEntry(name: "itemsToInsert")
                            {
                                Source = "/document/normalized_images/*/Tags/*/name"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "mergedText")
                            {
                                TargetName = "finalText"
                            }
                        })
                    {
                        Description = "Merge text content with image tags",
                        Context = "/document"
                    },
                    new SplitSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text")
                            {
                                Source = "/document/finalText"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "textItems")
                            {
                                TargetName = "pages"
                            }
                        })
                    {
                        Description = "Split text into pages for subsequent skill processing",
                        Context = "/document/finalText",
                        TextSplitMode = TextSplitMode.Pages,
                        MaximumPageLength = 5000

                    },
                    new LanguageDetectionSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text")
                            {
                                Source = "/document/finalText"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "languageCode")
                        }),
                    new EntityRecognitionSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "text")
                            {
                                Source = "/document/finalText/pages/*"
                            },
                            new InputFieldMappingEntry(name: "languageCode")
                            {
                                Source = "/document/languageCode"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "persons") 
                            { 
                                TargetName = "people"
                            },
                            new OutputFieldMappingEntry(name: "locations"),
                            new OutputFieldMappingEntry(name: "organizations"),
                            new OutputFieldMappingEntry(name: "namedEntities")
                            {
                                TargetName = "entities"
                            }
                        },
                        skillVersion: EntityRecognitionSkill.SkillVersion.V3)
                    {
                        Context = "/document/finalText/pages/*",
                        Categories = { EntityCategory.Person, EntityCategory.Location, EntityCategory.Organization },
                    },
                    new ShaperSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "layoutText") 
                            { 
                                Source = "/document/normalized_images/*/layoutText"
                            },
                            new InputFieldMappingEntry(name: "imageStoreUri")
                            {
                                Source = "/document/normalized_images/*/imageStoreUri"
                            },
                            new InputFieldMappingEntry(name: "width")
                            {
                                Source = "/document/normalized_images/*/width"
                            },
                            new InputFieldMappingEntry(name: "height")
                            {
                                Source = "/document/normalized_images/*/height"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "output")
                            {
                                TargetName = "ocrImageMetadata"
                            }
                        })
                    {
                        Description = "Create a custom OCR image metadata object used to generate an HOCR document",
                        Context = "/document/normalized_images/*"
                    },
                    new WebApiSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "imageData")
                            {
                                Source = "/document/normalized_images/*/data"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "imageStoreUri")
                        },
                        uri: string.Format("{0}/api/image-store?code={1}", azureFunctionEndpointUri, ConfigurationManager.AppSettings["AzureFunctionHostKey"]))
                    {
                        Description = "Upload image data to the annotation store",
                        Context = "/document/normalized_images/*",
                        HttpHeaders = 
                        {
                            ["BlobContainerName"] = blobContainerNameForImageStore
                        },
                        BatchSize = 1
                    },
                    new WebApiSkill(
                        inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "ocrImageMetadataList")
                            { 
                                Source = "/document/normalized_images/*/ocrImageMetadata"
                            },
                            new InputFieldMappingEntry(name: "wordAnnotations")
                            {
                                Source = "/document/cryptonyms"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "hocrDocument")
                        },
                        uri: string.Format("{0}/api/hocr-generator?code={1}", azureFunctionEndpointUri, ConfigurationManager.AppSettings["AzureFunctionHostKey"]))
                    {
                        Description = "Generate HOCR for webpage rendering",
                        Context = "/document",
                        BatchSize = 1,
                    },
                    new WebApiSkill(inputs: new List<InputFieldMappingEntry>()
                        {
                            new InputFieldMappingEntry(name: "words")
                            {
                                Source = "/document/normalized_images/*/layoutText/words/*/text"
                            }
                        },
                        outputs: new List<OutputFieldMappingEntry>()
                        {
                            new OutputFieldMappingEntry(name: "cryptonyms")
                        },
                        uri: string.Format("{0}/api/link-cryptonyms-list?code={1}", azureFunctionEndpointUri, ConfigurationManager.AppSettings["AzureFunctionHostKey"]))
                    {
                        Description = "Cryptonym linker",
                        Context = "/document",
                        BatchSize = 1
                    }
            })
            {
                Name = name,
                Description = "JFK Files Skillset",
                CognitiveServicesAccount = new CognitiveServicesAccountKey(key: ConfigurationManager.AppSettings["CognitiveServicesAccountKey"])
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

        public static SearchIndex GetIndex(string name, string synonymMapName) => 
            new SearchIndex(name: name)
            {
                Fields = new List<SearchField>()
                {
                    new SearchField("id",              SearchFieldDataType.String)                                 { IsSearchable = true,  IsFilterable = true,  IsHidden = false, IsSortable = true,  IsFacetable = false, IsKey = true },
                    new SearchField("fileName",        SearchFieldDataType.String)                                 { IsSearchable = false, IsFilterable = false, IsHidden = false, IsSortable = false, IsFacetable = false },
                    new SearchField("metadata",        SearchFieldDataType.String)                                 { IsSearchable = false, IsFilterable = false, IsHidden = false, IsSortable = false, IsFacetable = false },
                    new SearchField("text",            SearchFieldDataType.String)                                 { IsSearchable = true,  IsFilterable = false, IsHidden = false, IsSortable = false, IsFacetable = false, SynonymMapNames = { synonymMapName } },
                    new SearchField("entities",        SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsSearchable = false, IsFilterable = true,  IsHidden = false, IsSortable = false, IsFacetable = true  },
                    new SearchField("cryptonyms",      SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsSearchable = false, IsFilterable = true,  IsHidden = false, IsSortable = false, IsFacetable = true  },
                    new SearchField("demoBoost",       SearchFieldDataType.Int32)                                  { IsSearchable = false, IsFilterable = true,  IsHidden = false, IsSortable = false, IsFacetable = false },
                    new SearchField("demoInitialPage", SearchFieldDataType.Int32)                                  { IsSearchable = false, IsFilterable = false, IsHidden = false, IsSortable = false, IsFacetable = false },
                },
                ScoringProfiles = 
                {
                    new ScoringProfile(name: "demoBooster")
                    {
                        FunctionAggregation = ScoringFunctionAggregation.Sum,
                        Functions = 
                        {
                            new MagnitudeScoringFunction(
                                fieldName: "demoBoost",
                                boost: 1000,
                                parameters: new MagnitudeScoringParameters(
                                    boostingRangeStart: 0,
                                    boostingRangeEnd: 100)
                                {
                                    ShouldBoostBeyondRangeByConstant = true
                                })
                            {
                                Interpolation = ScoringFunctionInterpolation.Linear
                            }
                        }
                    }
                },
                CorsOptions = new CorsOptions(allowedOrigins: new List<string>() { "*" }),
                Suggesters = 
                {
                    new SearchSuggester(name: "sg-jfk", sourceFields: "entities")
                }
            };

        public static SearchIndexer GetIndexer(string name, string dataSourceName, string indexName, string skillsetName) => 
            new SearchIndexer(
                name: name,
                dataSourceName: dataSourceName,
                targetIndexName: indexName)
            {
                SkillsetName = skillsetName,
                Parameters = new IndexingParameters()
                {
                    BatchSize = 1,
                    MaxFailedItems = 0,
                    MaxFailedItemsPerBatch = 0,
                    Configuration = 
                    {
                        ["dataToExtract"] = BlobIndexerDataToExtract.ContentAndMetadata,
                        ["imageAction"] = BlobIndexerImageAction.GenerateNormalizedImages,
                        ["normalizedImageMaxWidth"] = 2000,
                        ["normalizedImageMaxHeight"] = 2000
                    }
                },
                FieldMappings = 
                {
                    new FieldMapping(sourceFieldName: "metadata_storage_name")           { TargetFieldName = "fileName"        },
                    new FieldMapping(sourceFieldName: "metadata_custom_demoBoost")       { TargetFieldName = "demoBoost"       },
                    new FieldMapping(sourceFieldName: "metadata_custom_demoInitialPage") { TargetFieldName = "demoInitialPage" }
                },
                OutputFieldMappings = 
                {
                    new FieldMapping(sourceFieldName: "/document/finalText")                         { TargetFieldName = "text"       },
                    new FieldMapping(sourceFieldName: "/document/hocrDocument/metadata")             { TargetFieldName = "metadata"   },
                    new FieldMapping(sourceFieldName: "/document/finalText/pages/*/entities/*/text") { TargetFieldName = "entities"   },
                    new FieldMapping(sourceFieldName: "/document/cryptonyms")                        { TargetFieldName = "cryptonyms" }
                }
            };
    }
}
