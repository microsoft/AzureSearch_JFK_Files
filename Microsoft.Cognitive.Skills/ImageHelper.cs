using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using PdfSharp.Pdf.Filters;
using static PdfSharp.Pdf.PdfDictionary;
using System.Threading.Tasks;
using System.Net.Http;

namespace Microsoft.Cognitive.Skills
{

    public static class ImageHelper
    {

        public static byte[] CreateThumbnailJpgStream(Image loBMP, int lnWidth, int lnHeight, out int newWidth, out int newHeight)
        {

            System.Drawing.Bitmap bmpOut = null;

            ImageFormat loFormat = loBMP.RawFormat;

            decimal lnRatio;
            int lnNewWidth = 0;
            int lnNewHeight = 0;

            if (loBMP.Width > loBMP.Height)
            {
                lnRatio = (decimal)lnWidth / loBMP.Width;
                lnNewWidth = lnWidth;
                decimal lnTemp = loBMP.Height * lnRatio;
                lnNewHeight = (int)lnTemp;
            }
            else
            {
                lnRatio = (decimal)lnHeight / loBMP.Height;
                lnNewHeight = lnHeight;
                decimal lnTemp = loBMP.Width * lnRatio;
                lnNewWidth = (int)lnTemp;
            }

            // if we are going to end up with a larger image just return what we have
            if (lnHeight * lnWidth > loBMP.Width * loBMP.Height)
            {
                newHeight = loBMP.Height;
                newWidth = loBMP.Width;

                return ImageToJpegBytes(loBMP);
            }


            // *** This code creates cleaner (though bigger) thumbnails and properly
            // *** and handles GIF files better by generating a white background for
            // *** transparent images (as opposed to black)
            using (bmpOut = new Bitmap(lnNewWidth, lnNewHeight))
            {
                using (Graphics g = Graphics.FromImage(bmpOut))
                {
                    newHeight = lnNewHeight;
                    newWidth = lnNewWidth;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    g.FillRectangle(Brushes.White, 0, 0, lnWidth, lnHeight);
                    g.DrawImage(loBMP, 0, 0, lnNewWidth, lnNewHeight);
                    MemoryStream outStream = new MemoryStream(1024 * 1024 * 2);

                    // write the image to disk
                    bmpOut.Save(outStream, ImageFormat.Jpeg);
                    bmpOut.Dispose();

                    outStream.Position = 0;
                    var data = new byte[outStream.Length];
                    Array.Copy(outStream.GetBuffer(), data, outStream.Length);
                    return data;

                }
            }

        }

        public static Image ResizeFit(this Image loBMP, int lnWidth, int lnHeight)
        {
            ImageFormat loFormat = loBMP.RawFormat;

            decimal lnRatio;
            int lnNewWidth = 0;
            int lnNewHeight = 0;

            if (loBMP.Width > loBMP.Height)
            {
                lnRatio = (decimal)lnWidth / loBMP.Width;
                lnNewWidth = lnWidth;
                decimal lnTemp = loBMP.Height * lnRatio;
                lnNewHeight = (int)lnTemp;
            }
            else
            {
                lnRatio = (decimal)lnHeight / loBMP.Height;
                lnNewHeight = lnHeight;
                decimal lnTemp = loBMP.Width * lnRatio;
                lnNewWidth = (int)lnTemp;
            }

            // if we are going to end up with a larger image just return what we have
            if (lnHeight * lnWidth > loBMP.Width * loBMP.Height)
            {
                return loBMP;
            }


            // *** This code creates cleaner (though bigger) thumbnails and properly
            // *** and handles GIF files better by generating a white background for
            // *** transparent images (as opposed to black)
            var bmpOut = new Bitmap(lnNewWidth, lnNewHeight);
            using (Graphics g = Graphics.FromImage(bmpOut))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                g.FillRectangle(Brushes.White, 0, 0, lnWidth, lnHeight);
                g.DrawImage(loBMP, 0, 0, lnNewWidth, lnNewHeight);
                MemoryStream outStream = new MemoryStream(1024 * 1024 * 2);
            }
            return bmpOut;

        }


        public static byte[] ImageToJpegBytes(Image loBMP)
        {
            MemoryStream outStream = new MemoryStream(1024 * 1024 * 2);
            // write the image to disk
            loBMP.Save(outStream, ImageFormat.Jpeg);
            var data = new byte[outStream.Length];
            Array.Copy(outStream.GetBuffer(), data, outStream.Length);
            return data;
        }


        public static IEnumerable<Bitmap> GetPageImages(Stream stream, string name)
        {
            IEnumerable<Bitmap> images = (PdfReader.TestPdfFile(stream) == 0) ?
                ConvertTiffToBmps(stream) :  // assume it is some kind of image format
                ConvertPdfResourcesToBmps(stream, name); // parse the pdf image

            return images;
        }

        public static IEnumerable<byte[]> ConvertToJpegs(Stream stream, int maxWidth, int maxHeight, string name)
        {
            var images = GetPageImages(stream, name);

            int imageCount = 0;
            return images.Select(img => {
                int w, h;
                var bmp = ImageHelper.CreateThumbnailJpgStream(img, maxWidth, maxHeight, out w, out h);
                imageCount++;
                return bmp;
            });
        }


        public static IEnumerable<Bitmap> ConvertTiffToBmps(Stream stream)
        {
            using (Image imageFile = Image.FromStream(stream))
            {
                // rotate the image if needed
                CheckImageRotate(imageFile);

                FrameDimension frameDimensions = new FrameDimension(
                    imageFile.FrameDimensionsList[0]);

                // Gets the number of pages from the tiff image (if multipage) 
                int frameNum = imageFile.GetFrameCount(frameDimensions);

                for (int frame = 0; frame < frameNum; frame++)
                {
                    // yeild each frame as a bitmap. 
                    imageFile.SelectActiveFrame(frameDimensions, frame);
                    yield return new Bitmap(imageFile);
                }

            }
        }


        public static IEnumerable<Bitmap> ConvertPdfResourcesToBmps(Stream stream, string name)
        {
            PdfDocument document = PdfReader.Open(stream);

            int imageCount = 0;
            // Iterate pages
            int pageNum = 0;
            foreach (PdfPage page in document.Pages)
            {

                Console.WriteLine($"Processing page {++pageNum} of {document.Pages.Count}");
                // Get resources dictionary
                PdfDictionary resources = page.Elements.GetDictionary("/Resources");
                if (resources != null)
                {
                    // Get external objects dictionary
                    PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                    if (xObjects != null)
                    {
                        ICollection<PdfItem> items = xObjects.Elements.Values;
                        // Iterate references to external objects
                        foreach (PdfItem item in items)
                        {
                            PdfReference reference = item as PdfReference;
                            if (reference != null)
                            {
                                PdfDictionary xObject = reference.Value as PdfDictionary;
                                // Is external object an image?
                                if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image")
                                {
                                    var bmp = ExportImage(document, xObject);
                                    imageCount++;
                                    yield return bmp;
                                }
                            }
                        }
                    }
                }
            }
        }

        static string ExportVectors(PdfDocument document, PdfDictionary image, ref int count)
        {
            // get the stream bytes
            byte[] imgData = image.Stream.Value;

            // sometimes an image can be dual encoded, if so decode the first layer
            var filters = image.Elements.GetArray("/Filter");
            string filter;
            if (filters != null && filters.Elements.GetName(0) == "/FlateDecode")
            {
                // FlateDecode
                imgData = new FlateDecode().Decode(image.Stream.Value);
                filter = filters.Elements.GetName(1);
            }
            else if (filters != null && filters.Elements.Count == 1)
                filter = filters.Elements.GetName(0);
            else
                filter = image.Elements.GetName("/Filter");

            if (filter == "/FlateDecode")
                imgData = new FlateDecode().Decode(image.Stream.Value);

            string data = new StreamReader(new MemoryStream(imgData)).ReadToEnd();

            File.WriteAllText("content_" + cnt++ + ".txt", data);

            // TODO: read orientation and reorient image if needed.
            //    bmp.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate270FlipNone);

            return data;
        }


        static int cnt = 0;

        static Bitmap ExportImage(PdfDocument document, PdfDictionary image)
        {
            // get the stream bytes
            byte[] imgData = image.Stream.Value;

            // sometimes an image can be dual encoded, if so decode the first layer
            var filters = image.Elements.GetArray("/Filter");
            string filter;
            if (filters != null && filters.Elements.GetName(0) == "/FlateDecode")
            {
                // FlateDecode
                imgData = new FlateDecode().Decode(image.Stream.Value);
                filter = filters.Elements.GetName(1);
            }
            else if (filters != null && filters.Elements.Count == 1)
                filter = filters.Elements.GetName(0);
            else
                filter = image.Elements.GetName("/Filter");

            switch (filter)
            {
                case "/FlateDecode":  // ?
                    imgData = new FlateDecode().Decode(image.Stream.Value);
                    break;

                case "/DCTDecode":  // JPEG format
                    // nativly supported by PDF so nothing to do here
                    break;

                case "/CCITTFaxDecode":  // TIFF format

                    MemoryStream m = new MemoryStream();

                    Tiff tiff = Tiff.ClientOpen("custom", "w", m, new TiffStream());
                    tiff.SetField(TiffTag.IMAGEWIDTH, image.Elements.GetInteger("/Width"));
                    tiff.SetField(TiffTag.IMAGELENGTH, image.Elements.GetInteger("/Height"));
                    tiff.SetField(TiffTag.COMPRESSION, Compression.CCITTFAX4);
                    tiff.SetField(TiffTag.BITSPERSAMPLE, image.Elements.GetInteger("/BitsPerComponent"));
                    tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                    tiff.WriteRawStrip(0, imgData, imgData.Length);
                    tiff.Close();
                    imgData = m.ToArray();
                    break;

                case "/JBIG2Decode":
                    var d = new JBig2Decoder.JBIG2StreamDecoder();

                    var decodeParams = image.Elements.GetDictionary("/DecodeParms");
                    if (decodeParams != null)
                    {
                        var globalRef = decodeParams.Elements.GetObject("/JBIG2Globals");
                        if (globalRef != null)
                        {
                            var globals = document.Internals.GetObject(globalRef.Reference.ObjectID) as PdfDictionary;
                            d.setGlobalData(globals.Stream.Value);

                        }
                    }

                    imgData = d.decodeJBIG2(imgData);
                    break;

                default:
                    throw new Exception("Dont know how to decode PDF image type of " + filter);
            }

            var bmp = ConvertTiffToBmps(new MemoryStream(imgData)).First();
            return bmp;
        }

        public static void CheckImageRotate(Image image)
        {

            if (image.PropertyIdList.Contains(0x0112))
            {
                int rotationValue = image.GetPropertyItem(0x0112).Value[0];
                switch (rotationValue)
                {
                    case 8: // rotated 90 right
                            // de-rotate:
                        image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate270FlipNone);
                        break;

                    case 3: // bottoms up
                        image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate180FlipNone);
                        break;

                    case 6: // rotated 90 left
                        image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate90FlipNone);
                        break;
                    case 1: // landscape, do nothing
                    default:
                        break;
                }
                image.RemovePropertyItem(0x0112);
            }
        }

        public static Image CorrectOrientation(this Image image)
        {
            return image;
        }

        public static Image Rotate(this Image image, string currentOrientation)
        {
            switch (currentOrientation.ToLowerInvariant())
            {
                case "right": // rotated 90 right
                              // de-rotate:
                    image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate270FlipNone);
                    break;

                case "down": // bottoms up
                    image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate180FlipNone);
                    break;

                case "left": // rotated 90 left
                    image.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate90FlipNone);
                    break;
                default:
                    break;
            }

            return image;
        }
    }

}
