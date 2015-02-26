using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;

namespace RadialReview.Utilities
{
	public class AwsUtil
	{
		public static List<S3Object> GetObjectsInFolder(string bucket, string prefix)
		{
			var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
			var request = new ListObjectsRequest();
			request.BucketName = bucket;
			request.Prefix = prefix.EndsWith("/") ? prefix : prefix + "/";


			var allExisting = new List<S3Object>();
		
			do{
				var response = client.ListObjects(request);
				allExisting.AddRange(response.S3Objects.Where(x => x.Key != request.Prefix));

				if (response.IsTruncated)
					request.Marker = response.NextMarker;
				else
					request = null;
			} while (request != null);
			return allExisting;
		}

		public static Stream GetObject(string bucket, string key)
		{
			using (var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1))
			{
				var request = new GetObjectRequest { BucketName = bucket, Key = key };
				using (var response = client.GetObject(request))
				{
					using (var s = response.ResponseStream)
					{
						return StreamUtil.ReadIntoStream(s);
					}
				}
			}
		}
	}
}