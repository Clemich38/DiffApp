using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ImageSharp;
using ImageSharp.Formats;

using ThumbnailCreator.ViewModels;

namespace ThumbnailCreator.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _environment;

        public HomeController(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            MainModel viewModel = new MainModel { ImageList = new List<string>() };

            // Get the path to upload the pictures
            string uplaodPath = Path.Combine(_environment.WebRootPath, "uploads");

            // Delete the content of the folder
            string[] filePaths = Directory.GetFiles(uplaodPath);
            foreach (string filePath in filePaths)
                System.IO.File.Delete(filePath);

            return View(viewModel);
        }

        public IActionResult Upload(ICollection<IFormFile> files, int width, int height)
        {
            Configuration.Default.AddImageFormat(new JpegFormat());
            Configuration.Default.AddImageFormat(new BmpFormat());
            Configuration.Default.AddImageFormat(new GifFormat());
            Configuration.Default.AddImageFormat(new PngFormat());

            // Get the path to upload the pictures
            string uplaodPath = Path.Combine(_environment.WebRootPath, "uploads");

            // Delete the content of the folder
            string[] filePaths = Directory.GetFiles(uplaodPath);
            foreach (string filePath in filePaths)
                System.IO.File.Delete(filePath);

            // Instantiate the viewModel to store the fileName list
            MainModel viewModel =  new MainModel{ImageList = new List<string>()};
            Image image = null;

            // Go through all browsed files
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    file.OpenReadStream();
                    // Add file name to the viewModel list
                    viewModel.ImageList.Add("/uploads/thumb_" + file.FileName);
                    image = new Image(file.OpenReadStream());

                    // Create a new image file to store the resize image
                    using (var output = System.IO.File.OpenWrite(Path.Combine(uplaodPath, "thumb_" + file.FileName)))
                    {
                        image.Resize(width, height);
                        image.Save(output);
                    }
                }
            }
            return View("Index", viewModel);
        }



        [HttpGet]  
        public FileResult Download()  
        { 
            // Get all the image files in the upload folder and add them to the zip file to download
            var outputStream = new MemoryStream();
            string uplaodPath = Path.Combine(_environment.WebRootPath, "uploads");
            DirectoryInfo di = new DirectoryInfo(uplaodPath);

            using (var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                foreach (FileInfo fi in di.GetFiles())
                {
                    var zipEntry = zip.CreateEntry(fi.Name);
                    using (var zipStream = zipEntry.Open())
                    using (var stream = System.IO.File.OpenRead(Path.Combine(uplaodPath, fi.Name)))
                        stream.CopyTo(zipStream);
                }
            }
            outputStream.Position = 0;

            // stuff the zip package into a FileStreamResult
            var fsr = new FileStreamResult(outputStream, "application/zip");    
            return fsr;
        }  
    }
}
