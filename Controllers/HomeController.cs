using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using Amazon.S3;
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using Amazon.S3.Model;

using DiffApp.Diff;
using DiffApp.ViewModels;
using DiffApp.ViewModels.AWS;

namespace DiffApp.Controllers
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
            S3BuketsList buketsList = new S3BuketsList();

            return View(buketsList);
        }
        public async Task<IActionResult> Login()
        {
            S3BuketsList buketsList = new S3BuketsList();

            // var options = Configuration.GetAWSOptions();

            // AmazonS3Config config = new AmazonS3Config();

            // using (var client = new AmazonS3Client(Amazon.RegionEndpoint.APNortheast1))
            // {
            //     var a = 0;
            //     // var response = client.ListBuckets();
            // }
            // var b = 1;

            CredentialProfile basicProfile;
            AWSCredentials awsCredentials;
            var sharedFile = new SharedCredentialsFile();
            if (sharedFile.TryGetProfile("SimCheckAppProfile", out basicProfile) &&
                AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
            {
                using (var client = new AmazonS3Client(awsCredentials, Amazon.RegionEndpoint.APNortheast1))
                {
                    Amazon.S3.Model.ListBucketsResponse response = await client.ListBucketsAsync();
                    buketsList.ListBuckets = response;
                }
            }
            
            return View("Index", buketsList);
        }

        
        public async Task<IActionResult> DownloadFile()
        {
            S3BuketsList buketsList = new S3BuketsList();

            CredentialProfile basicProfile;
            AWSCredentials awsCredentials;
            var sharedFile = new SharedCredentialsFile();
            if (sharedFile.TryGetProfile("SimCheckAppProfile", out basicProfile) &&
                AWSCredentialsFactory.TryGetAWSCredentials(basicProfile, sharedFile, out awsCredentials))
            {
                using (var client = new AmazonS3Client(awsCredentials, Amazon.RegionEndpoint.APNortheast1))
                {
                    GetObjectRequest getObjectRequest = new GetObjectRequest();
                    getObjectRequest.BucketName = "devpiece";
                    getObjectRequest.Key = "product_file/20151120064251-4834-index.html.zip";

                    // Get the path to upload the pictures
                    string filesPath = Path.Combine(_environment.WebRootPath, "files/PieceDL");
                    string file1Path = Path.Combine(filesPath, getObjectRequest.Key);
                    System.Threading.CancellationToken token;
                    GetObjectResponse getObjectResponse = await client.GetObjectAsync(getObjectRequest);

                    await getObjectResponse.WriteResponseStreamToFileAsync(file1Path, false, token);
                }
            }
            
            return View("Index", buketsList);
        }

        // product_file/20161116114608-00000130-dotnet.highcharts.samples.zip
        // product_file/20151120064251-4834-index.html.zip

        public IActionResult DiffResult(bool fast)
        {
            int perc = 0;

            // Get the path to upload the pictures
            string filesPath = Path.Combine(_environment.WebRootPath, "files");
            string file1Path = Path.Combine(filesPath, "File1.txt");
            string file2Path = Path.Combine(filesPath, "File2.txt");

            if (System.IO.File.Exists(file1Path) && System.IO.File.Exists(file2Path))
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                perc = Diff.Diff.GetSimilarityValue(file1Path, file2Path, true, fast? Objects.DiffMode.FastByLine : Objects.DiffMode.SlowByChar);

                ViewData["Error"] = "No Error";

                @ViewData["ProcessingTime"] = stopwatch.ElapsedMilliseconds + "ms";
                stopwatch.Stop();
            }
            else
            {
                ViewData["Error"] = "File not found!";  
                @ViewData["ProcessingTime"] = 0 + "ms";
            }    
            
            ViewData["Similarity"] = "Similarity: " + perc + "%";

            return View();
        }
        public async Task<IActionResult> DiffResultAsync(bool fast)
        {
            Task<int> percTask;
            int perc = 0;

            // Get the path to upload the pictures
            string filesPath = Path.Combine(_environment.WebRootPath, "files");
            string file1Path = Path.Combine(filesPath, "File1.txt");
            string file2Path = Path.Combine(filesPath, "File2.txt");

            if (System.IO.File.Exists(file1Path) && System.IO.File.Exists(file2Path))
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                percTask = Task.Run(() => DiffApp.Diff.Diff.GetSimilarityValue(file1Path, file2Path, true, fast? Objects.DiffMode.FastByLine : Objects.DiffMode.SlowByChar));

                ViewData["Error"] = "No Error";

                await Task.Delay(10);

                @ViewData["ProcessingTime"] = stopwatch.ElapsedMilliseconds + "ms";

                perc = await percTask;

                @ViewData["ProcessingTime2"] = stopwatch.ElapsedMilliseconds + "ms";

                stopwatch.Stop();
            }
            else
            {
                ViewData["Error"] = "File not found!";  
                @ViewData["ProcessingTime"] = 0 + "ms";
            }    
            

            ViewData["Similarity"] = "Similarity: " + perc + "%";

            return View();
        }

        public async Task<IActionResult> ProjectDiffResult()
        {
            Stopwatch stopwatch = new Stopwatch();

            // Get the path to project files
            string filesPath = Path.Combine(_environment.WebRootPath, "files/project1");
            string[] filesListStr = Directory.GetFiles(filesPath, "*.*", SearchOption.AllDirectories);

            ProjectFileList ProjectFileListModel = new ProjectFileList
            {
                List = new List<FileList>(),
            };

            int id = 0;
            stopwatch.Start();
            foreach(string name in filesListStr)
            {
                FileList fileList = await DirDiffResult(name, id++);

                FileInfo fInfo = new FileInfo(name);
                fileList.size = fInfo.Length;

                ProjectFileListModel.List.Add(fileList);
                ProjectFileListModel.overallSimilarity += fileList.bestMatch * fileList.size;
                ProjectFileListModel.overallSize += fileList.size;
            }

            ProjectFileListModel.processingTime = stopwatch.ElapsedMilliseconds;
            ProjectFileListModel.overallSimilarity = ProjectFileListModel.overallSimilarity / ProjectFileListModel.overallSize;

            return View(ProjectFileListModel);
        }

        public async Task<FileList> DirDiffResult(string fileName, int id)
        {
            // Get the path to project files
            string filesPath = Path.Combine(_environment.WebRootPath, "files/project2");
            string[] filesListStr = Directory.GetFiles(filesPath, ("*" + Path.GetExtension(fileName)), SearchOption.AllDirectories);

            // Remove the file that don't have the same extension
            // filesListStr = filesListStr.Where(x => Path.GetExtension(x).Equals(Path.GetExtension(fileName))).ToArray();

            List<Task<int>> taskList =  new List<Task<int>>();

            FileList fileListModel = new FileList
            {
                List = new List<FileItem>(),
                Directory = "",
                id = id,
                bestMatch = 0,
                fileName = fileName
            };

            // Launch a diff process for each file in the folder
            foreach(string name in filesListStr)
            {
                fileListModel.List.Add(
                    new FileItem
                    {
                        fileName = name,
                        SimilarityPerc = 0,
                        ProcessingTime = 0,
                        ToReanalyze = false
                    }
                );
                Task<int> taskRes = Task.Run(() => DiffApp.Diff.Diff.GetSimilarityValue(fileName, name, true, Objects.DiffMode.FastByLine));
                taskList.Add(taskRes);  
            }

            // Wait for all diff process to be over
            IEnumerable<int> res = await Task.WhenAll(taskList);

            // FileList the Model with the return values
            for (int i = 0; i < res.Count(); i++)
            {
                fileListModel.List.ElementAt(i).SimilarityPerc = res.ElementAt(i);
                
                if(res.ElementAt(i) > 30)
                    fileListModel.List.ElementAt(i).ToReanalyze = true;

                if(res.ElementAt(i) > fileListModel.bestMatch)
                    fileListModel.bestMatch = res.ElementAt(i);
            }

        return fileListModel;
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
