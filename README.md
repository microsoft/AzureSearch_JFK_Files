# The JFK Files
Explore the JFK Assassination files using Azure Search and Microsoft Cognitive Services.
You can watch the demo in action in a short [online video](https://channel9.msdn.com/Shows/AI-Show/Using-Cognitive-Search-to-Understand-the-JFK-Documents)
or explore the JFK files yourself with our [online demo](https://aka.ms/jfkfiles-demo).

## Cognitive Search Pattern
This project demonstrates the Cognitive Search pattern in AzureSearch using the JFK files.

![Cognitive Search Pattern](images/cognitive-search-pattern.jpg)

This pattern feeds data into the cloud, applies a set of cognitive skills which extracts knowledge and stores it as annotations and creates new experiences exploring the data using Cognitive Search.

## JFK Files Architecture
Note: This diagram of visuals are inspired by the [CIA's JFK document management system in 1997](https://www.archives.gov/files/research/jfk/releases/docid-32404466.pdf) included in the JFK files.

![Architecture](images/overview.jpg)

1. We have provided a subset of the [JFK PDF documents](https://www.archives.gov/research/jfk/2017-release) and images that have been uploaded to the cloud into Azure Blob Storage.
2. An [Azure Search](https://azure.microsoft.com/en-us/services/search/) service is used to take advantage of Cognitive Search capabilities, and an [Azure Function](https://azure.microsoft.com/en-us/services/functions/) is used to create skills for the Cognitive Search skillset.
    1. Uses the [Cognitive Services Vision API](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) to extract text information from the image via OCR, handwriting, and image captioning,
    2. Applies the [Azure Machine Learning Named Entity Recognition Service](https://docs.microsoft.com/en-us/azure/machine-learning/studio-module-reference/named-entity-recognition) to extract named entities from the documents,
    3. Annotates text using a custom [CIA Cryptonyms](https://www.maryferrell.org/php/cryptdb.php) skill,
    4. Generates [HOCR content](https://en.wikipedia.org/wiki/HOCR) based on results.
3. A simple Web App uses the [AzSearch.js library](https://github.com/Yahnoosh/AzSearch.js) to search the index and explore the documents

## Limitations and Considerations
1. This is a demo to showcase a Cognitive Search use case.  It is not intended to be a framework or scalable architecture for all scenarios, though it can give you an idea of what your scenario might end up looking like.
2. The OCR technology is not perfect and the handwriting capability is in preview.  The results will vary greatly by scan and image quality.
3. The code currently only processes images and will support most scanned PDFs. Native PDFs and some scanned PDF formats may not be parsed correctly.
4. Cognitive Search is currently only available in public preview in the South Central US and West Europe Azure regions, so the Azure Search service you use with this demo must be provisioned in one of these two regions.
5. **IMPORTANT: The JFK Files sample creates a public website and a publicly readable storage container for any extracted images.  It is not suitable for using with non-public data.**

## Setting up your own JFK files library

These instructions will help you have your own version of the JFK files demo running in Azure in about 20 minutes, with most of that time being provisioning/deployment time.

### Prerequisites
1. An Azure Subscription you can access and deploy resources too.
    1. Note that this demo does not work properly on the free tier of these resources, and you will incur charges for resources provisioned during this demo for as long as they are provisioned.  For an estimation of cost, reference the [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/).
2. [Visual Studio 2017](https://www.visualstudio.com/downloads/) with [Azure Developer Tools](https://azure.microsoft.com/en-us/tools/) enabled.  The free community edition will work fine.
3. Basic familiarity with using the [Azure Portal](https://portal.azure.com) and cloning and compiling code from github.

### Deploy Required Resources

1. Click the below button to upload the provided ARM template to the Azure portal, which is written to automatically deploy and configure the following resources:
    1. An Azure Search service, default set to [Standard S1](https://azure.microsoft.com/en-us/pricing/details/search/) tier, and deployed to South Central US (regardless of the resource group location you select).
    2. An Azure Blob Storage Account, default set to [Standard LRS](https://azure.microsoft.com/en-us/pricing/details/storage/) tier.
    3. An Azure App Service plan, default set to [Basic B1](https://azure.microsoft.com/en-us/pricing/details/app-service/) tier.
    4. An Azure Web App Service, using the plan from # 3.
    5. An Azure Function instance, using the storage account from # 2 and the plan from # 3.  The Azure Function will be prepublished with the code provided in this repository as part of the template deployment.

    </br>
    <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FMicrosoft%2FAzureSearch_JFK_Files%2Fmagottei%2Fjfk%2Fazuredeploy.json" target="_blank">
        <img src="http://azuredeploy.net/deploybutton.png"/>
    </a>

2. Be sure to select the appropriate subscription to deploy to, and create a new resource group for these resources.
3. Review the rest of the parameters as well as the terms and conditions, select the checkbox for "I agree to the terms and conditions stated above", and press "Purchase".
4. Monitor the status of the deployment by following the link that appears in the Azure portal notifications.  It takes about 5 minutes for the resources to be fully provisioned and deployed.

    ![Deployment in progress](images/deploymentInProgress.JPG)

### Initialize the code

5. While you are waiting for the resources to finish provisioning, either git clone or download this repo, and open the provided solution file *JfkWebApiSkills/JfkWebApiSkills.sln* using Visual Studio 2017.
6. In the root of the *JfkInitializer* project, open the *App.config* file.  You will be copying and pasting some secrets into this file momentarily.
7. Make sure that the *JfkInitializer* project is set as your default project if it isn't already.

### Run the initializer

8. Once your resources are finished deploying (you should get a notification in the Azure portal), navigate to the *Outputs* section of the deployment.

    ![Deployment completed](images/deploymentCompleted.JPG)

    ![Outputs of deployment](images/outputs.JPG)

9. Copy and paste each of the provided outputs from this interface into their corresponding value location in the *App.config* file from before.  Note that one value will be missing from the outputs, the *SearchServiceQueryKey*.
10. In order to obtain the *SearchServiceQueryKey*, navigate back to the *Overview* section of the deployment and find your newly created Azure Search service in the list (Its type will be *Microsoft.Search/searchServices*).  Select it.

    ![Search service in deployment](images/searchServiceInDeployment.JPG)

11. Select the *Keys* section, followed by *Manage query keys*.  Copy and paste the key value shown into the *SearchServiceQueryKey* value of your *App.config* file.

    ![Search service keys](images/searchServiceKeys.JPG)

12. Now you should have filled in all of the required configurations in the *App.config* file.  Save your changes, and press the start button at the top of Visual Studio in order to run the initializer.

    ![Run initializer](images/runInitializer.JPG)

13. It will take about 10 minutes for the appropriate search resources to be created, as well as for the website to be deployed.  Once the website is deployed, a URL will appear in the console.  Copy and paste this URL into a browser to start interacting with what you have created!

    ![URL in console](images/urlInConsole.JPG)

### Results

This project will create several things for you.  It will deploy an Azure Function using the code provided in the *JfkWebApiSkills* project, as well as create an Azure Search data source, skillset, synonym map, index, and indexer.  It will also create a blob storage container that the images will be reuploaded to, as well as deploys the JFK Files frontend to an Azure web app that you can immediately interact with.  Feel free to play with the code to see how all of these things are accomplished so that you can start using Cognitive Search for your data enrichment scenario today.