using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Cognitive.Skills;
using EnricherFunction;

namespace DataEnricher
{
    public static class Program
    {
        static ConsoleLogger log = new ConsoleLogger(TraceLevel.Info);

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Initializing Services");
                    InitializeServices();

                    Console.WriteLine("Services have been successfully Initialized");
                }
                else
                {
                    bool deleteall = false;
                    var serviceClient = new SearchServiceClient(Config.AZURE_SEARCH_SERVICE_NAME, new SearchCredentials(Config.AZURE_SEARCH_ADMIN_KEY));
                    var indexClient = serviceClient.Indexes.GetClient(Config.AZURE_SEARCH_INDEX_NAME);

                    if (deleteall)
                    {
                        var sp = new SearchParameters() { Select = new[] { "id" }.ToList() };
                        var ids = indexClient.Documents.Search("*", sp).Results.Select(s => s.Document).ToArray();

                        if (ids.Length > 0)
                        {
                            var batch = IndexBatch.Delete(ids);
                            var result = indexClient.Documents.IndexAsync(batch).Result;
                        }
                    }

                    Console.WriteLine("Indexing images under " + args[0]);
                    var files = Directory.GetFiles(args[0]);

                    // advance to the high water mark if needed
                    int start = 0;
                    string hwmFile = "hwm.txt";
                    if (File.Exists(hwmFile))
                    {
                        var hwm = File.ReadAllText("hwm.txt");
                        start = Array.IndexOf(files.Select(f => f.ToLowerInvariant()).ToArray(), hwm.ToLowerInvariant().Trim());
                        if (start < 0)
                            start = 0;
                    }

                    Dictionary<string, Exception> errors = new Dictionary<string, Exception>();
                    for (var i = start; i < files.Length; i++)
                    {
                        var filepath = files[i];

                        // write the hwm
                        File.WriteAllText(hwmFile, filepath);

                        // get the document record number for the filename
                        string name = Path.GetFileName(filepath).Replace(" ", "_").Replace(".", "_");

                        Console.WriteLine("Processing file {0} : ID={1}  [{2} of {3}]", Path.GetFileName(filepath), name, i + 1, files.Length);

                        using (var file = File.OpenRead(filepath))
                        {
                            try
                            {
                                EnrichFunction.Run(file, name, log).Wait();
                            }
                            catch(Exception e)
                            {
                                errors.Add(filepath, e);
                                Console.WriteLine("ERROR: " + e.ToString());
                            }
                        }
                    }

                    if (errors.Count > 0)
                    {
                        Console.WriteLine("files with errors:");
                        foreach (var err in errors)
                        {
                            Console.WriteLine(errors.Keys);
                        }

                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("errors:");
                        foreach (var err in errors)
                        {
                            Console.WriteLine(err.Key + " : " + err.Value.ToString().Substring(0, Math.Min(300, err.Value.ToString().Length)));
                        }
                    }

                    // remove the hwm since we are done
                    if (File.Exists(hwmFile))
                        File.Delete(hwmFile);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An Error has occured: " + e.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        static void InitializeServices()
        {
            // create the storage containers if needed
            CloudBlobClient blobClient = CloudStorageAccount.Parse($"DefaultEndpointsProtocol=https;AccountName={Config.IMAGE_AZURE_STORAGE_ACCOUNT_NAME};AccountKey={Config.IMAGE_BLOB_STORAGE_ACCOUNT_KEY};EndpointSuffix=core.windows.net").CreateCloudBlobClient();
            blobClient.GetContainerReference(Config.IMAGE_BLOB_STORAGE_CONTAINER).CreateIfNotExists(BlobContainerPublicAccessType.Blob);
            blobClient.GetContainerReference(Config.LIBRARY_BLOB_STORAGE_CONTAINER).CreateIfNotExists(BlobContainerPublicAccessType.Off);

            var searchHelper = new AzureSearchHelper(Config.AZURE_SEARCH_SERVICE_NAME, Config.AZURE_SEARCH_ADMIN_KEY);
            var serviceClient = new SearchServiceClient(Config.AZURE_SEARCH_SERVICE_NAME, new SearchCredentials(Config.AZURE_SEARCH_ADMIN_KEY));

            var demoBoost = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DemoBoost.json"));

            // Create the Synonyms
            Console.WriteLine("Creating Synonym Map");
            var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AddSynonyms.json"));
            searchHelper.Put("synonymmaps/cryptonyms", json, "2016-09-01-Preview");

            // create the index if needed
            Console.WriteLine("Create the index");
            json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CreateIndex.json"));
            searchHelper.Put("indexes/" + Config.AZURE_SEARCH_INDEX_NAME, json, "2016-09-01-Preview");

            // Update documents with boost scores
            //Console.WriteLine("Boosting Documents");
            //json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DemoBoost.json"));
            //searchHelper.Post("indexes/" + Config.AZURE_SEARCH_INDEX_NAME + "/docs/index", json);

            // test the pipeline and index
            Console.WriteLine("Sending a test image through the pipeline");
            using (var file = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "photo.jpg")))
            {
                EnrichFunction.Run(file, "photo_jpg", log).Wait();
            }

            Console.WriteLine("Querying the test image");
            var indexClient = serviceClient.Indexes.GetClient(Config.AZURE_SEARCH_INDEX_NAME);
            var results = indexClient.Documents.Search("oswald", new SearchParameters()
            {
                Facets = new[] { "entities" },
                HighlightFields = new[] { "text" },
            });

            // TODO: Add some additional validations for fields
            if (results.Results.Count > 0)
                Console.WriteLine("Item found in index");
            else
                Console.WriteLine("Item missing from index");
        }



        public class ConsoleLogger : TraceMonitor
        {
            public ConsoleLogger(TraceLevel level) : base(level)
            {
            }
            public override void Trace(TraceEvent traceEvent)
            {
                Console.WriteLine(traceEvent);
            }
        }

    }
}
