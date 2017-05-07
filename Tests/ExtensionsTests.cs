#if DEBUG

using System;
using System.Diagnostics;
using System.IO;
using Apitron.PDF.Kit.FixedLayout;
using Apitron.PDF.Kit.FixedLayout.ContentElements;
using Apitron.PDF.Kit.Interactive.Annotations;
using Apitron.PDF.Kit.Interactive.Forms;
using NUnit.Framework;

namespace Apitron.PDF.Kit.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [SetUp]
        public void Initialize()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        }

        [Test]
        public void SignFirstPage()
        {
            string outputFileName = GetFileNameBasedOnCaller();
            string signatureFieldName = string.Empty;

            using (Stream inputStream = File.Open("../../data/letter_unsigned.pdf", FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    signatureFieldName = doc.Sign("../../data/johndoe.pfx", "password", "../../data/signatureImage.png",
                        new Boundary(100, 140, 190, 180), outputFileName);
                }
            }

            using (Stream inputStream = File.Open(outputFileName, FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    SignatureField signatureField = (SignatureField)doc.AcroForm[signatureFieldName];

                    // check the signature
                    Assert.IsTrue(signatureField.IsSigned && signatureField.IsValid);

                    string signatureViewId = signatureField.Views[0].Identity;

                    for (int i = 0; i < doc.Pages.Count; ++i)
                    {
                        foreach (Annotation annotation in doc.Pages[i].Annotations)
                        {
                            WidgetAnnotation signatureFieldView = annotation as WidgetAnnotation;

                            // so we have signature view placed somewhere on the non-first page
                            Assert.False(signatureFieldView != null && signatureFieldView.Identity == signatureViewId && i > 0);
                        }
                    }
                }
            }
        }

        [Test]
        public void SignTwoPages()
        {
            string outputFileName = GetFileNameBasedOnCaller();
            string signatureFieldName = string.Empty;

            using (Stream inputStream = File.Open("../../data/letter_unsigned.pdf", FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    signatureFieldName = doc.Sign("../../data/johndoe.pfx", "password", "../../data/signatureImage.png",
                        new Boundary(100, 140, 190, 180), 0,1,outputFileName);
                }
            }

            using (Stream inputStream = File.Open(outputFileName, FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    SignatureField signatureField = (SignatureField)doc.AcroForm[signatureFieldName];

                    // check the signature
                    Assert.IsTrue(signatureField.IsSigned && signatureField.IsValid);

                    string signatureViewId = signatureField.Views[0].Identity;

                    for (int i = 0; i < doc.Pages.Count; ++i)
                    {
                        foreach (Annotation annotation in doc.Pages[i].Annotations)
                        {
                            WidgetAnnotation signatureFieldView = annotation as WidgetAnnotation;

                            // so we have signature view placed somewhere on the wrong page
                            Assert.False(signatureFieldView != null && signatureFieldView.Identity == signatureViewId && i > 1);
                        }
                    }
                }
            }
        }

        [Test]
        public void SignAll()
        {
            string outputFileName = GetFileNameBasedOnCaller();
            string signatureFieldName = string.Empty;

            using (Stream inputStream = File.Open("../../data/letter_unsigned.pdf", FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    signatureFieldName = doc.SignAll("../../data/johndoe.pfx", "password", "../../data/signatureImage.png",
                        new Boundary(100, 140, 190, 180), outputFileName);
                }
            }

            using (Stream inputStream = File.Open(outputFileName, FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    SignatureField signatureField = (SignatureField)doc.AcroForm[signatureFieldName];

                    // check the signature
                    Assert.IsTrue(signatureField.IsSigned && signatureField.IsValid);

                    string signatureViewId = signatureField.Views[0].Identity;

                    // check that we have signature view placed on all pages
                    for (int i = 0; i < doc.Pages.Count; ++i)
                    {
                        bool annotationFound = false;

                        foreach (Annotation annotation in doc.Pages[i].Annotations)
                        {
                            WidgetAnnotation signatureFieldView = annotation as WidgetAnnotation;

                            if ((annotationFound = (signatureFieldView != null && signatureFieldView.Identity == signatureViewId)))
                            {
                                break;
                            }
                        }

                        Assert.IsTrue(annotationFound);
                    }
                }
            }
        }

        private static string GetFileNameBasedOnCaller()
        {
            StackFrame frame = new StackFrame(1);
            return string.Format("{0}.pdf", frame.GetMethod().Name);
        }

        [Test]
        public void Watermark()
        {
            string outputFileName = GetFileNameBasedOnCaller();

            using (Stream inputStream = File.Open("../../data/letter_unsigned.pdf", FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                     doc.Watermark("../../data/watermarkTransparent.png", outputFileName);
                }
            }

            using (Stream inputStream = File.Open(outputFileName, FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    // check that we have watermarks placed on all pages
                    for (int i = 0; i < doc.Pages.Count; ++i)
                    {
                        bool watermarkAnnotationFound = false;

                        foreach (Annotation annotation in doc.Pages[i].Annotations)
                        {
                            if (watermarkAnnotationFound = (annotation as WatermarkAnnotation!=null))
                            {
                                break;
                            }
                        }

                        Assert.IsTrue(watermarkAnnotationFound);
                    }
                }
            }
        }

        [Test]
        public void WatermarkText()
        {
            string outputFileName = GetFileNameBasedOnCaller();

           // string watermarkText = "Top secret - for internal use - top secret - for internal use";
            string watermarkText = "TOP secret -for INTERNAL use - TOP secret - for INTERNAL use - TOP secret - for INTERNAL use - TOP secret";

            using (Stream inputStream = File.Open("../../data/letter_unsigned.pdf", FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    doc.WatermarkText(watermarkText, outputFileName);
                }
            }

            using (Stream inputStream = File.Open(outputFileName, FileMode.Open))
            {
                using (FixedDocument doc = new FixedDocument(inputStream))
                {
                    // check that we have watermarks placed on first page
                    // TODO: make it working for all pages
                    for (int i = 0; i < 1; ++i)
                    {
                        bool watermarkAnnotationFound = false;

                        foreach (Annotation annotation in doc.Pages[i].Annotations)
                        {
                            WatermarkAnnotation watermarkAnnotation = (annotation as WatermarkAnnotation);

                            if (watermarkAnnotation!=null && (watermarkAnnotationFound = FindTextInElements(watermarkText, watermarkAnnotation.Watermark.Elements)))
                            {
                                break;
                            }
                        }

                        Assert.IsTrue(watermarkAnnotationFound);
                    }
                }
            }

            Process.Start(outputFileName);
        }

        private bool FindText(string watermarkText, IContentElement element)
        {
            if (element.ElementType == ElementType.Text)
            {
                TextContentElement textElement = (TextContentElement) element;

                return textElement.TextObject.Text == watermarkText;
            }

            if (element.ElementType == ElementType.FormXObject)
            {
                FormContentElement elementsContainer = (FormContentElement)element;

                return FindTextInElements(watermarkText, elementsContainer.FormXObject.Elements);
            }

            return false;
        }

        private bool FindTextInElements(string watermarkText, IContentElementsEnumerator elementsContainer)
        {
            foreach (var newElement in elementsContainer)
            {
                if (FindText(watermarkText, newElement))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif