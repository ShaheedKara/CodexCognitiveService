using ImageResizer;
using MicroMk1.Models;
using Microsoft.ProjectOxford.Vision;
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
    public class DescribeController : ComputerVisionBaseController
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
                return View("Index");
            }

            var model = new DescribeImageModel();

          
            var features = new[]
            {
                VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description,
                VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags
            };

           
            await RunOperationOnImage(async stream =>
            {
                model.Result = await VisionServiceClient.AnalyzeImageAsync(stream, features);
            });

           
            await RunOperationOnImage(async stream => {
                var bytes = new byte[stream.Length];

                await stream.ReadAsync(bytes, 0, bytes.Length);

                var base64 = Convert.ToBase64String(bytes);
                model.ImageDump = String.Format("data:image/png;base64,{0}", base64);
            });



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
                        // In case something goes wrong have to be dumb to get this
                        TempData["Message"] = ex.Message;
                    }
                }
            }












            //  var imageUrl = await imageService.UploadImageAsync(ImageToAnalyze);
            //  TempData["LatestImage"] = imageUrl.ToString(); 
            //var latestImage = string.Empty;
            //if (TempData["LatestImage"] != null)
            //{
            //    ViewBag.LatestImage = Convert.ToString(TempData["LatestImage"]);
            //}
            return View(model);
        }
        //public ActionResult LatestImage()
        //{
        //    var latestImage = string.Empty;
        //    if (TempData["LatestImage"] != null)
        //    {
        //        ViewBag.LatestImage = Convert.ToString(TempData["LatestImage"]);
        //    }

        //    return View();
        //}
        [HttpPost]
        public ActionResult Search(string term)
        {
            return RedirectToAction("Thank", new { id = term });
        }
        public ActionResult Thank(string id)
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
                    blob.FetchAttributes();

                    if (String.IsNullOrEmpty(id) || HasMatchingMetadata(blob, id))
                    {
                        var caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name;

                        blobs.Add(new BlobInfo()
                        {
                            ImageUri = blob.Uri.ToString(),
                            ThumbnailUri = blob.Uri.ToString().Replace("/pictures/", "/pictures/"),
                            Caption = caption
                        });
                    }
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            ViewBag.Search = id;
            return View();
        }
        private bool HasMatchingMetadata(CloudBlockBlob blob, string term)
        {
            foreach (var item in blob.Metadata)
            {
                if (item.Key.StartsWith("Tag") && item.Value.Equals(term, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}