using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Downloaders {
	public class AwsS3Downloader {

		private static string Combine(string bucket, string key) {
			return bucket + "\\" + key.Replace("/","\\");
		}

		public static List<string> DownloadFromPrefix(string bucket, string prefix, bool ignoreCache=false) {
			using (var client = new AmazonS3Client(RegionEndpoint.USWest2)) {
				var listRequest = new ListObjectsRequest() {
					BucketName = bucket,
					Prefix = prefix,
				};
				var allFiles = new List<string>();

				//List all files
				var listResponse = client.ListObjects(listRequest);
				foreach (S3Object entry in listResponse.S3Objects) {
					//download all files					
					var file = FileCache.GetOrAddCachedFile(Config.GetCacheDirectory(),Combine(entry.BucketName, entry.Key), f => {
						var request = new GetObjectRequest() {
							BucketName = entry.BucketName,
							Key = entry.Key,
						};
						using (GetObjectResponse response = client.GetObject(request)) {
							response.WriteResponseStreamToFile(f);
						}
					}, forceExecute: ignoreCache);

					allFiles.Add(file);
				}

				return allFiles;
			}
		}

		

	}
}
