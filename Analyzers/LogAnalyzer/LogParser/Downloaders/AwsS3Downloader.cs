using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ParserUtilities.Utilities.CacheFile;
using ParserUtilities.Utilities.OutputFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Downloaders {
	public class AwsS3Downloader {

		private static string Combine(string bucket, string key) {
			return bucket + "\\" + key.Replace("/", "\\");
		}

		public static List<string> DownloadFromPrefix(string bucket, string prefix, bool ignoreCache = false) {
			ListObjectsResponse listResponse;
				var allFiles = new List<string>();

			using (var client = new AmazonS3Client(RegionEndpoint.USWest2)) {
				var listRequest = new ListObjectsRequest() {
					BucketName = bucket,
					Prefix = prefix,
				};
				//List all files
				listResponse = client.ListObjects(listRequest);
			}
			foreach (S3Object entry in listResponse.S3Objects) {
				//download all files					
				var file = FileCache.GetOrAddCachedFile(Config.GetCacheDirectory(), Combine(entry.BucketName, entry.Key), f => {
					//var request = new GetObjectRequest() {
					//	BucketName = entry.BucketName,
					//	Key = entry.Key,
					//};
					//using (GetObjectResponse response = client.GetObject(request)) {
					//	response.WriteResponseStreamToFile(f);
					//}

					string url;
					var request1 = new GetPreSignedUrlRequest {
						BucketName = entry.BucketName,
						Key = entry.Key,
						Expires = DateTime.Now.AddMinutes(5)
					};
					using (var client = new AmazonS3Client(RegionEndpoint.USWest2)) {
						url = client.GetPreSignedURL(request1);
					}
					using (var wc = new WebClient()) {
						//wc.Proxy = WebProxy.GetDefaultProxy();
						wc.Proxy = null;//GlobalProxySelection.GetEmptyWebProxy();

						wc.DownloadFileTaskAsync(new Uri(url), f).GetAwaiter().GetResult();
					}
				}, forceExecute: ignoreCache);

				allFiles.Add(file);
			}

			return allFiles;
		}

		public class WebClientWithTimeout : WebClient {
			protected override WebRequest GetWebRequest(Uri address) {
				WebRequest wr = base.GetWebRequest(address);
				wr.Timeout = 5000; // timeout in milliseconds (ms)
				return wr;
			}
		}
	}
}

