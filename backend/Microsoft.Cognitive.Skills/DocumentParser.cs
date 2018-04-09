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
using System.Runtime.InteropServices;

namespace Microsoft.Cognitive.Skills
{

    public static class DocumentParser
    {

        public static DocumentMetadata Parse(Stream stream)
        {
            var images = (PdfReader.TestPdfFile(stream) == 0) ?
                GetImagePages(stream) :  // assume it is some kind of image format
                GetPdfPages(stream); // parse the pdf image

            return new DocumentMetadata() {
                 Pages = images
            };
        }


        private static IEnumerable<PageImage> GetImagePages(Stream stream)
        {
            using (Image imageFile = Image.FromStream(stream))
            {
                // rotate the image if needed
                ImageHelper.CheckImageRotate(imageFile);
                FrameDimension frameDimension = new FrameDimension(imageFile.FrameDimensionsList[0]);

                // Gets the number of pages from the tiff image (if multipage) 
                int frameNum = imageFile.GetFrameCount(frameDimension);

                for (int frame = 0; frame < frameNum; frame++)
                {
                    yield return new ImagePageMetadata(imageFile, frameDimension, frame);
                }

            }
        }

        private static IEnumerable<PageImage> GetPdfPages(Stream stream)
        {
            PdfDocument document = PdfReader.Open(stream);

            // Iterate pages
            int pageNum = 0;
            foreach (PdfPage page in document.Pages)
            {
                pageNum++;
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
                                    yield return new PdfPageMetadata(document, xObject, pageNum);
                                }
                            }
                        }
                    }
                }
            }
        }

        private class ImagePageMetadata : PageImage
        {
            int frame;
            Image image;
            FrameDimension frameDimension;

            public ImagePageMetadata(Image image, FrameDimension frameDimension, int frame)
            {
                this.image = image;
                this.frameDimension = frameDimension;
                this.frame = frame;
                PageNumber = frame;
            }


            public override Bitmap GetImage()
            {
                image.SelectActiveFrame(frameDimension, frame);
                return new Bitmap(image);
            }
        }



        private class PdfPageMetadata : PageImage
        {
            PdfDocument document;
            PdfDictionary image;

            public PdfPageMetadata(PdfDocument document, PdfDictionary image, int pageNumber)
            {
                this.document = document;
                this.image = image;
                PageNumber = pageNumber;
            }


            public override Bitmap GetImage()
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
                        return BmpFromRawData(imgData);

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

                return ImageHelper.ConvertTiffToBmps(new MemoryStream(imgData)).First();
            }

            private Bitmap BmpFromRawData(byte[] imgData)
            {
                int w = image.Elements.GetInteger("/Width");
                int h = image.Elements.GetInteger("/Height");
                int bpc = image.Elements.GetInteger("/BitsPerComponent");
                var cSpace = image.Elements.GetArray("/ColorSpace");
                var cSpaceName = cSpace != null ? cSpace.Elements.GetName(0) :  image.Elements.GetString("/ColorSpace");
                var bytesPerPixel = cSpaceName == "/DeviceRGB" ? 3 : 1;

                var pixelFormat = cSpaceName == "/Indexed" ?
                    PixelFormat.Format8bppIndexed 
                    : bpc == 1 ? PixelFormat.Format1bppIndexed : PixelFormat.Format24bppRgb;


                if (cSpaceName == "/DeviceRGB")
                {
                    //change order of RGB bytes            
                    byte[] grb = new byte[imgData.Length];
                    for (int i = 0; i < imgData.Length; i = i + 3)
                    {
                        grb[i] = imgData[i + 2];
                        grb[i + 1] = imgData[i + 1];
                        grb[i + 2] = imgData[i];
                    }
                }

                Bitmap bmp = new Bitmap(w, h, pixelFormat);
                var bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, pixelFormat);
                int length = (int)Math.Ceiling(w * bytesPerPixel * bpc / 8.0);
                for (int i = 0; i < h; i++)
                {
                    int offset = i * length;
                    int scanOffset = i * bmpData.Stride;
                    Marshal.Copy(imgData, offset, new IntPtr(bmpData.Scan0.ToInt32() + scanOffset), length);
                }

                if (cSpace != null)
                    SetColorPallete(bmp, cSpace);

                bmp.UnlockBits(bmpData);

                return bmp;
            }

            private void SetColorPallete(Bitmap bmp, PdfArray cSpace)
            {
                var globals2 = cSpace.Elements.GetReference(3).Value as PdfDictionary;
                var palData = new FlateDecode().Decode(globals2.Stream.Value);

                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < palData.Length; i += 3)
                    pal.Entries[i / 3] = Color.FromArgb(255, palData[i], palData[i + 1], palData[i + 2]);

                bmp.Palette = pal;
            }
        }

    }



    public class DocumentMetadata
    {
        // More document Metadata goes here

        public IEnumerable<PageImage> Pages { get; set; }
    }

    public abstract class PageImage
    {
        public int PageNumber { get; set; }

        public string Id { get; set; }

        public abstract Bitmap GetImage();
    }
}
