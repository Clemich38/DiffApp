using System.Collections.Generic;
using System;

namespace DiffApp.ViewModels
{
  public class ProjectFileList
  {
    public string Directory { get; set; }
    public long overallSimilarity { get; set; }
    public long overallSize { get; set; }
    public long processingTime { get; set; }
    public IList<FileList> List { get; set; }
  }

  public class FileList
  {
    public string Directory { get; set; }
    public int id { get; set; }
    public int bestMatch { get; set; }
    public long size { get; set; }
    public string fileName { get; set; }

    public IList<FileItem> List { get; set; }
  }

  public class FileItem
  {
    public string fileName { get; set; }
    public int SimilarityPerc { get; set; }
    public int ProcessingTime { get; set; }
    public bool ToReanalyze { get; set; }
  }

}