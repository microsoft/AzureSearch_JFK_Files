using System.Drawing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Microsoft.Cognitive.Skills
{
    public class ImageStore
    {
        private CloudBlobContainer libraryContainer;

        public ImageStore(string blobConnectionString, string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            libraryContainer = blobClient.GetContainerReference(containerName);
        }

        public async Task<string> UploadImageToLibrary(Stream stream, string name, bool overwrite = false)
        {
            CloudBlockBlob blockBlob = libraryContainer.GetBlockBlobReference(name);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadFromStreamAsync(stream);

                blockBlob.Properties.ContentType = "image/jpg";
                await blockBlob.SetPropertiesAsync();
            }

            return blockBlob.Uri.ToString();
        }

        public Task<string> UploadToBlob(byte[] data, string name, bool overwrite = false)
        {
            return UploadImageToLibrary(new MemoryStream(data), name, overwrite);
        }

        public async Task<ImageReference> UploadToBlob(Image image)
        {
            var name = Guid.NewGuid().ToString();

            var data = ImageHelper.ImageToJpegBytes(image);
            return new ImageReference(image)
            {
                Width = image.Width,
                Height = image.Height,
                Url = await UploadImageToLibrary(new MemoryStream(data), name)
            };
        }

    }

    public static class ImageStoreExtensions
    {
        public static Task<ImageReference> UploadMedia(this Image image, ImageStore store)
        {
            return store.UploadToBlob(image);
        }
    }

    public class ImageReference
    {
        Image image;

        public ImageReference()
        {
        }

        public ImageReference(Image image)
        {
            this.image = image;
        }

        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // consider making this async
        public Image GetImage()
        {
            if (image != null)
                return image;

            if (string.IsNullOrEmpty(Url))
                return null;
            HttpClient client = new HttpClient();
            var result = client.GetAsync(Url).ConfigureAwait(false).GetAwaiter().GetResult();
            var stream = result.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return Image.FromStream(stream);

        }
    }

}
