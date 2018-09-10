using ImageResizer;
using MicroMk1.Models;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MicroMk1.Controllers
{
    public class HandwritingController : ComputerVisionBaseController
    {
        public ActionResult Index()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("pictures");
            List<BlobInfo> blobs = new List<BlobInfo>();

            foreach (IListBlobItem item in container.ListBlobs())
            {
                var blob = item as CloudBlockBlob;

                if (blob != null)
                {
                    blobs.Add(new BlobInfo()
                    {
                        ImageUri = blob.Uri.ToString(),
                        ThumbnailUri = blob.Uri.ToString().Replace("/pictures/", "/pictures/")
                    });
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase file)
        {
            if (Request.HttpMethod == "GET")
            {
                return View();
            }

            var model = new HandwritingModel();
            HandwritingRecognitionOperation op = null;

            await RunOperationOnImage(async stream =>
            {
                op = await VisionServiceClient.CreateHandwritingRecognitionOperationAsync(stream);
            });

            while (true)
            {
                await Task.Delay(5000);

                var result = await VisionServiceClient.GetHandwritingRecognitionOperationResultAsync(op);
                if (result.Status == HandwritingRecognitionOperationStatus.NotStarted ||
                   result.Status == HandwritingRecognitionOperationStatus.Running)
                {
                    continue;
                }

                model.Result = result.RecognitionResult;
                break;
            }

            model.ImageDump = GetInlineImageWithLines(model.Result);




            if (file != null && file.ContentLength > 0)
            {
               
                if (!file.ContentType.StartsWith("image"))
                {
                    TempData["Message"] = "Only image files may be uploaded";
                }
                else
                {
                    try
                    {
                       
                        CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                        CloudBlobClient client = account.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference("pictures");
                        CloudBlockBlob photo = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                        await photo.UploadFromStreamAsync(file.InputStream);

                        using (var outputStream = new MemoryStream())
                        {
                            file.InputStream.Seek(0L, SeekOrigin.Begin);
                            var settings = new ResizeSettings { MaxWidth = 192 };
                            ImageBuilder.Current.Build(file.InputStream, outputStream, settings);
                            outputStream.Seek(0L, SeekOrigin.Begin);
                            container = client.GetContainerReference("pictures");
                            CloudBlockBlob thumbnail = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                            await thumbnail.UploadFromStreamAsync(outputStream);

                        }
                    }
                    catch (Exception ex)
                    {
                        // In case something goes wrong
                        TempData["Message"] = ex.Message;
                    }
                }
            }























            return View(model);
        }
    }
}