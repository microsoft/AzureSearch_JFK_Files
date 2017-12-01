using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Cognitive.Skills
{
    // uses HOCR format for representing the document metadata
    // see https://en.wikipedia.org/wiki/HOCR
    public class AnnotatedDocument
    {
        private readonly string header = @"<?xml version='1.0' encoding='UTF-8'?>
<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml' xml:lang='en' lang='en'>
 <head>
  <title></title>
  <meta http-equiv='Content-Type' content='text/html;charset=utf-8' />
  <meta name='ocr-system' content='Microsoft Cognitive Services' />
  <meta name='ocr-capabilities' content='ocr_page ocr_carea ocr_par ocr_line ocrx_word'/>
 </head>
 <body>";
        private readonly string footer = "</body></html>";

        private List<AnnotatedPage> pages = new List<AnnotatedPage>();

        public AnnotatedDocument(IEnumerable<AnnotatedPage> pages)
        {
            Metadata = header + Environment.NewLine + string.Join(Environment.NewLine, pages.Select(p => p.Metadata)) + Environment.NewLine + footer;
            Text = string.Join(Environment.NewLine, pages.Select(p => p.Text));
        }

        public string Metadata { get; set; }

        public string Text { get; set; }

        public T Get<T>(string name)
        {
            throw new NotImplementedException();
        }
    }

    public class AnnotatedPage
    {
        StringWriter metadata = new StringWriter();
        StringWriter text = new StringWriter() { NewLine = " " };

        public AnnotatedPage(OcrResult hw, ImageReference image) : this(hw, image, 0)
        {
        }

        public AnnotatedPage(OcrResult hw, ImageReference image, int pageNumber)
        {
            // page
            metadata.WriteLine($"  <div class='ocr_page' id='page_{pageNumber}' title='image \"{image.Url}\"; bbox 0 0 {image.Width} {image.Height}; ppageno {pageNumber}'>");
            metadata.WriteLine($"    <div class='ocr_carea' id='block_{pageNumber}_1'>");

            var allwords = new List<WordResult>();

            int li = 0;
            int wi = 0;
            foreach (var line in hw.lines)
            {
                metadata.WriteLine($"    <span class='ocr_line' id='line_{pageNumber}_{li}' title='baseline -0.002 -5; x_size 30; x_descenders 6; x_ascenders 6'>");

                var words = line.words.FirstOrDefault()?.boundingBox == null ? line.words : line.words.OrderBy(l => l.boundingBox[0]).ToArray();

                foreach (var word in words)
                {
                    var bbox = word.boundingBox != null && word.boundingBox.Length == 8 ? $"bbox {word.boundingBox[0]} {word.boundingBox[1]} {word.boundingBox[4]} {word.boundingBox[5]}" : "";
                    metadata.WriteLine($"      <span class='ocrx_word' id='word_{pageNumber}_{li}_{wi}' title='{bbox}'>{word.text}</span>");
                    text.WriteLine(word.text);
                    wi++;
                    allwords.Add(word);
                }
                li++;
                metadata.WriteLine(" </span>"); // line

            }

            metadata.WriteLine("    </div>"); // reading area
            metadata.WriteLine("  </div>"); // page
        }

        public string Metadata
        {
            get { return metadata.ToString() + metadata.NewLine + "</body></html>"; }
        }

        

        public string Text
        {
            get { return text.ToString(); }
        }
    }
}
