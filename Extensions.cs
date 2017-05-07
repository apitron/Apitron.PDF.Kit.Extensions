/*----- License -----
Copyright 2017 Apitron LTD.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Apitron.PDF.Kit.FixedLayout;
using Apitron.PDF.Kit.FixedLayout.Resources.XObjects;
using System;
using System.Data.Common;
using System.IO;
using Apitron.PDF.Kit.FixedLayout.Resources.GraphicsStates;
using Apitron.PDF.Kit.FlowLayout;
using Apitron.PDF.Kit.FlowLayout.Content;
using Apitron.PDF.Kit.Interactive.Annotations;
using Apitron.PDF.Kit.Interactive.Forms;
using Apitron.PDF.Kit.Interactive.Forms.Signature;
using Apitron.PDF.Kit.Interactive.Forms.SignatureSettings;
using Apitron.PDF.Kit.Styles;
using Apitron.PDF.Kit.Styles.Appearance;
using Apitron.PDF.Kit.Styles.Text;
using Image = Apitron.PDF.Kit.FixedLayout.Resources.XObjects.Image;

namespace Apitron.PDF.Kit
{
    /// <summary>
    /// The powerpack of extensions making it easier to do most common tasks with PDF documents.
    /// It uses the public API provided by Apitron PDF Kit and can be easily adapted to your own needs.
    /// Copyright Apitron LTD. 2017.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Signs the first page of the document using given certificate and signature image.
        /// </summary>
        /// <param name="doc">Document to sign.</param>
        /// <param name="pathToSigningCertificate">Signing certificate.</param>
        /// <param name="certPassword">Certificate's password.</param>
        /// <param name="pathToSignatureImage">Path to image that will represent the signature visually on page.</param>
        /// <param name="signatureBoundary">Visual signature boundaries.</param>
        /// <param name="outputFilePath">Output file path, optional. If not set, incremental save will be performed.</param>
        /// <returns>Identifier assigned to the created signature field. Using this id you can find this field in doc's AcroForm dictionary.</returns>
        public static string Sign(this FixedDocument doc, string pathToSigningCertificate,
            string certPassword, string pathToSignatureImage, Boundary signatureBoundary, string outputFilePath = null)
        {
            return Sign(doc, pathToSigningCertificate, certPassword, pathToSignatureImage, signatureBoundary, 0, 0,
                outputFilePath);
        }

        /// <summary>
        /// Signs every page of the document using given certificate and signature image.
        /// </summary>
        /// <param name="doc">Document to sign.</param>
        /// <param name="pathToSigningCertificate">Signing certificate.</param>
        /// <param name="certPassword">Certificate's password.</param>
        /// <param name="pathToSignatureImage">Path to image that will represent the signature visually on page.</param>
        /// <param name="signatureBoundary">Visual signature boundaries.</param>
        /// <param name="outputFilePath">Output file path, optional. If not set, incremental save will be performed.</param>
        /// <returns>Identifier assigned to the created signature field. Using this id you can find this field in doc's AcroForm dictionary.</returns>
        public static string SignAll(this FixedDocument doc, string pathToSigningCertificate,
            string certPassword, string pathToSignatureImage, Boundary signatureBoundary, string outputFilePath = null)
        {
            return Sign(doc, pathToSigningCertificate, certPassword, pathToSignatureImage, signatureBoundary, 0,
                doc.Pages.Count - 1, outputFilePath);
        }

        /// <summary>
        /// Signs the range of document pages using given certificate and signature image.
        /// </summary>
        /// <param name="doc">Document to sign.</param>
        /// <param name="pathToSigningCertificate">Signing certificate.</param>
        /// <param name="certPassword">Certificate's password.</param>
        /// <param name="pathToSignatureImage">Path to image that will represent the signature visually on page.</param>
        /// <param name="signatureBoundary">Visual signature boundaries.</param>
        /// <param name="signaturePageIndexStart">The index of the first page to sign.</param>
        /// <param name="signaturePageIndexEnd">The index of the last page to sign.</param>
        /// <param name="outputFilePath">Output file path, optional. If not set, incremental save will be performed.</param>
        /// <returns>Identifier assigned to the created signature field. Using this id you can find this field in doc's AcroForm dictionary.</returns>
        public static string Sign(this FixedDocument doc, string pathToSigningCertificate,
            string certPassword, string pathToSignatureImage, Boundary signatureBoundary,
            int signaturePageIndexStart = 0, int signaturePageIndexEnd = 0, string outputFilePath = null)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (string.IsNullOrEmpty(pathToSigningCertificate))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(pathToSigningCertificate));
            }

            if (certPassword == null)
            {
                throw new ArgumentNullException(nameof(certPassword));
            }

            if (string.IsNullOrEmpty(pathToSignatureImage))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(pathToSignatureImage));
            }

            if (signatureBoundary == null)
            {
                throw new ArgumentNullException(nameof(signatureBoundary));
            }

            string imageId = Guid.NewGuid().ToString("N");
            string signatureFieldId = Guid.NewGuid().ToString("N");

            // register signature image resource
            doc.ResourceManager.RegisterResource(new Image(imageId, pathToSignatureImage));

            // create signature field and initialize it using a stored
            // password protected certificate
            SignatureField signatureField = new SignatureField(signatureFieldId);
            using (Stream signatureDataStream = File.OpenRead(pathToSigningCertificate))
            {
                signatureField.Signature = Signature.Create(new Pkcs12Store(signatureDataStream, certPassword));
            }

            // add signature field to a document
            doc.AcroForm.Fields.Add(signatureField);

            // create signature view using the image resource
            SignatureFieldView signatureView = new SignatureFieldView(signatureField, signatureBoundary);
            signatureView.ViewSettings.Graphic = Graphic.Image;
            signatureView.ViewSettings.GraphicResourceID = imageId;
            signatureView.ViewSettings.Description = Description.None;

            // add view to pages' annotations
            for (int i = signaturePageIndexStart; i <= signaturePageIndexEnd; ++i)
            {
                doc.Pages[i].Annotations.Add(signatureView);
            }

            // save to specified file or do an incremental update
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (Stream outputStream = File.Create(outputFilePath))
                {
                    doc.Save(outputStream);
                }
            }
            else
            {
                doc.Save();
            }

            return signatureFieldId;
        }

        /// <summary>
        /// Adds a textual watermark on all pages of the specified document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="watermarkText">Watermark text.</param>
        /// <param name="outputFilePath">Output file path, optional. If not set, incremental save will be performed.</param>
        public static void WatermarkText(this FixedDocument doc, string watermarkText, string outputFilePath = null)
        {
            // save to specified file or do an incremental update
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (Stream outputStream = File.Create(outputFilePath))
                {
                    WatermarkText(doc, watermarkText, outputStream);
                }
            }
            else
            {
                WatermarkText(doc, watermarkText, outputFilePath);
            }
        }

        /// <summary>
        /// Adds a watermark on all pages of the specified document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="watermarkText">Watermark text.</param>
        /// <param name="outputStream">Output stream, optional. If not set, incremental save will be performed.</param>
        public static void WatermarkText(this FixedDocument doc, string watermarkText, Stream outputStream=null)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (string.IsNullOrEmpty(watermarkText))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(watermarkText));
            }

           
            // register graphics state that sets transparency level for content
            GraphicsState gsTransparency = new GraphicsState("gsTransparency");
            gsTransparency.CurrentNonStrokingAlpha = 0.3;
            gsTransparency.CurrentStrokingAlpha = 0.3;
            doc.ResourceManager.RegisterResource(gsTransparency);

            // create watermark content template using given text
            double fontSizeInPoints = 20;
            double padding = 10;
            double borderThickness = 2;

            double totalAddedSpace = (padding + borderThickness) * 2;

            TextBlock watermarkTextBlock = new TextBlock(watermarkText)
            {
                Font = new Font("Times New Roman", fontSizeInPoints),
                Color = RgbColors.Red,
                Padding = new Thickness(padding),
                BorderColor = RgbColors.Red,
                Border = new Border(borderThickness),
                BorderRadius = 5,
                Background = RgbColors.Pink
            };

            double textBlockWidth = watermarkTextBlock.Measure(doc.ResourceManager) + totalAddedSpace;
            double textBlockHeight = fontSizeInPoints + totalAddedSpace + 10;

            FixedContent watermarkContent = new FixedContent(Guid.NewGuid().ToString("N"), new Boundary(textBlockWidth, textBlockHeight));
            watermarkContent.Content.SetGraphicsState(gsTransparency.ID);
            watermarkContent.Content.AppendContentElement(watermarkTextBlock, textBlockWidth, textBlockHeight);

            // register watermark XObject it will be referenced in all watermark annotations
            doc.ResourceManager.RegisterResource(watermarkContent);

            // add annotations to every page
            foreach (Page page in doc.Pages)
            {
                WatermarkAnnotation watermarkAnnotation = CreateWatermarkAnnotation(page.Boundary.MediaBox, watermarkContent,true);
                doc.ResourceManager.RegisterResource(watermarkAnnotation.Watermark);
                page.Annotations.Add(watermarkAnnotation);
            }

            // save to specified file or do an incremental update
            if (outputStream != null)
            {
                doc.Save(outputStream);
            }
            else
            {
                doc.Save();
            }
        }

        /// <summary>
        /// Adds a watermark on all pages of the specified document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="pathToWatermarkImage">Path to image to be used as watermark.</param>
        /// <param name="outputFilePath">Output file path, optional. If not set, incremental save will be performed.</param>
        public static void Watermark(this FixedDocument doc, string pathToWatermarkImage, string outputFilePath=null)
        {
            // save to specified file or do an incremental update
            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (Stream outputStream = File.Create(outputFilePath))
                {
                    Watermark(doc,pathToWatermarkImage,outputStream);
                }
            }
            else
            {
                Watermark(doc,pathToWatermarkImage,outputFilePath);
            }
        }

        /// <summary>
        /// Adds a watermark on all pages of the specified document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="pathToWatermarkImage">Path to image to be used as watermark.</param>
        /// <param name="outputStream">Output stream, optional. If not set, incremental save will be performed.</param>
        public static void Watermark(this FixedDocument doc, string pathToWatermarkImage, Stream outputStream=null)
        {
            if (!File.Exists(pathToWatermarkImage))
            {
                throw new FileNotFoundException("The image data file is not found", pathToWatermarkImage);
            }

            using (Stream imageDataStream = File.OpenRead(pathToWatermarkImage))
            {
                Watermark(doc, imageDataStream, outputStream);
            }
        }

        /// <summary>
        /// Adds a watermark on all pages of the specified document.
        /// </summary>
        /// <param name="doc">Document to process.</param>
        /// <param name="imageDataStream">Stream containing image data to be used as watermark. Caller is reposible for closing it.</param>
        /// <param name="outputStream">Output stream, optional. If not set, incremental save will be performed. Caller is responsible for closing it.</param>
        public static void Watermark(this FixedDocument doc, Stream imageDataStream, Stream outputStream = null)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (imageDataStream == null)
            {
                throw new ArgumentNullException(nameof(imageDataStream));
            }

            // create and register image resource
            string imageResourceId = Guid.NewGuid().ToString("N");
            Image imageResource = new Image(imageResourceId, imageDataStream);
            doc.ResourceManager.RegisterResource(imageResource);

            // register watermark XObject it will be referenced in all watermark annotations
            FixedContent watermarkContent = new FixedContent(Guid.NewGuid().ToString("N"),
                new Boundary(imageResource.Width, imageResource.Height));
            watermarkContent.Content.AppendImage(imageResourceId, 0, 0, imageResource.Width, imageResource.Height);
            doc.ResourceManager.RegisterResource(watermarkContent);

            // add annotations to every page
            foreach (Page page in doc.Pages)
            {
                WatermarkAnnotation watermarkAnnotation = CreateWatermarkAnnotation(page.Boundary.MediaBox, watermarkContent);
                doc.ResourceManager.RegisterResource(watermarkAnnotation.Watermark);
                page.Annotations.Add(watermarkAnnotation);
            }

            // save to specified file or do an incremental update
            if (outputStream != null)
            {
                doc.Save(outputStream);
            }
            else
            {
                doc.Save();
            }
        }

        /// <summary>
        /// Creates watermark annotations object referencing specified XObject.
        /// </summary>
        /// <param name="pageBoundary">Targer page boundary.</param>
        /// <param name="watermarkXobject">XObject containing visual content for the watermark.</param>
        /// <param name="rotated">Indicates that watermark object should be rotated and aligned between the lower left and upper right corners.</param>
        /// <returns>Initialized watermarp annotation.</returns>
        private static WatermarkAnnotation CreateWatermarkAnnotation(Boundary pageBoundary,
            FixedContent watermarkXobject, bool rotated=false)
        {
            // create watermark content XObject and reference existing "real" watermark XObject containing content
            FixedContent watermarkStub = new FixedContent(Guid.NewGuid().ToString("N"), pageBoundary);

            if (rotated)
            {
                // calculate rotation angle
                double alpha = Math.Atan(watermarkStub.Boundary.Height/watermarkStub.Boundary.Width);
                double beta = Math.PI/2.0 - alpha;

                double centeringDelta = watermarkXobject.Boundary.Height*Math.Cos(beta);

                double rotatedWidth = watermarkXobject.Boundary.Width*Math.Cos(alpha) + centeringDelta;
                double rotatedHeight = watermarkXobject.Boundary.Width*Math.Sin(alpha) + watermarkXobject.Boundary.Height*Math.Sin(beta);

                watermarkStub.Content.SaveGraphicsState();
                watermarkStub.Content.Translate(centeringDelta+(watermarkStub.Boundary.Width - rotatedWidth)/2.0,
                    (watermarkStub.Boundary.Height - rotatedHeight)/2.0);
                watermarkStub.Content.SetRotate(alpha);
                watermarkStub.Content.AppendXObject(watermarkXobject.ID, 0, 0);
                watermarkStub.Content.RestoreGraphicsState();
            }
            else
            {
                watermarkStub.Content.AppendXObject(watermarkXobject.ID, (watermarkStub.Boundary.Width - watermarkXobject.Boundary.Width) / 2.0, 
                    (watermarkStub.Boundary.Height - watermarkXobject.Boundary.Height) / 2.0);
                watermarkStub.Content.RestoreGraphicsState();
            }

            // create annotation and wire its visual part
            WatermarkAnnotation annotation = new WatermarkAnnotation(pageBoundary);
            annotation.Watermark = watermarkStub;

            return annotation;
        }
    }
}