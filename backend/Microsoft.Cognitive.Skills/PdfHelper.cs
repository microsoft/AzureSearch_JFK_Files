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

namespace Microsoft.Cognitive.Capabilities
{

    public static class PdfHelper
    {
        public static IEnumerable<Bitmap> ConvertPdfResourcesToBmps2(Stream stream)
        {
            var document = org.apache.pdfbox.pdmodel.PDDocument.load(ReadAllBytes(stream));
            var pages = document.getDocumentCatalog().getPages();
            var pdfRenderer = new org.apache.pdfbox.rendering.PDFRenderer(document);
            for (int page = 0; page < document.getNumberOfPages(); ++page)
            {
                var bim = pdfRenderer.renderImageWithDPI(page, 300, org.apache.pdfbox.rendering.ImageType.RGB);

                // suffix in filename will be used as the file format
                var fn = "page-" + cnt++ + ".png";
                org.apache.pdfbox.tools.imageio.ImageIOUtil.writeImage(bim, fn, 300);

                yield return (Bitmap)Image.FromFile(fn);
            }
            document.close();
        }

        private static byte[] ReadAllBytes(Stream source)
        {
            long originalPosition = source.Position;
            source.Position = 0;

            try
            {
                byte[] readBuffer = new byte[4096];
                int totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = source.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = source.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                source.Position = originalPosition;
            }
        }


        public static IEnumerable<PointF[]> GetShapes(string content)
        {
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var points = new List<PointF>();
            foreach (var line in lines.Select(l => l.Split(' ')))
            {
                if (line[0] == "h")
                {
                    if (points.Count > 1)
                        yield return points.ToArray();
                    points.Clear();
                }
                if (line.Length == 3 && line[2] == "l" || line[2] == "m")
                    points.Add(new PointF(float.Parse(line[0]), float.Parse(line[1])));
            }
        }


        public static IEnumerable<string> ConvertPdfResourcesToText(Stream stream)
        {
            PdfDocument document = PdfReader.Open(stream);

            int imageCount = 0;
            // Iterate pages
            int pageNum = 0;
            foreach (PdfPage page in document.Pages)
            {
                
                Console.WriteLine($"Processing page {++pageNum} of {document.Pages.Count}");
                // Get resources dictionary
                var items = page.Elements.GetArray("/Contents");
                if (items != null)
                {
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
                                //yield return ExportImage(document, xObject, ref imageCount);
                            }
                            else
                            {
                                yield return ExportVectors(document, xObject, ref imageCount);
                            }
                        }
                    }
                }
            }
            yield return null;
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

            // for some reason the jpeg image is rotated.  Not sure how to detect this so for now just rotate them 90deg CC
            //if (filter == "/DCTDecode" || filter == "/JBIG2Decode")
            //    bmp.RotateFlip(rotateFlipType: System.Drawing.RotateFlipType.Rotate270FlipNone);

            return data;
        }


        static int cnt = 0;

    }
}
