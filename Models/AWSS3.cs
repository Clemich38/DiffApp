using System.Collections.Generic;
using System;
using Amazon.S3.Model;

namespace DiffApp.ViewModels.AWS
{
  public class S3BuketsList
  {
    public ListBucketsResponse ListBuckets { get; set; }
  }
}