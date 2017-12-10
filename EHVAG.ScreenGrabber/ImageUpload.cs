using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;

namespace EHVAG.ScreenGrabber
{
	public class ImageUploadResult
	{
		public string Link { get; set; }
		public string DeleteUrl { get; set; }
	}

	public class ImageUploadStatus
	{
		public ConfigEntry Config { get; set; }
		public string Status { get; set; }
		public ImageUploadResult Result { get; set; }
	}

	public class ImageUpload
	{
		public static readonly List<ImageUploadStatus> UploadProgress = new List<ImageUploadStatus>();

		public static async Task<ImageUploadResult> Upload(Bitmap bmp, ConfigEntry configEntry)
		{
			if (configEntry.Url == "clipboard")
			{
				MultiPlatformClipboard.SetBitmap(bmp);

				return new ImageUploadResult()
				{
					Link = "clipboard://",
					DeleteUrl = "lol,ctrl+c"
				};
			}

			var status = new ImageUploadStatus()
			{
				Config = configEntry,
				Status = "Preparing",
				Result = null,
			};
			UploadProgress.Add(status);

			var toSend = GetImage(bmp, configEntry);

			var handler = new WebRequestHandler();
			if (!string.IsNullOrEmpty(configEntry.ClientCertificate))
			{
				var cert = X509Certificate.CreateFromCertFile(configEntry.ClientCertificate);
				handler.ClientCertificates.Add(cert);
			}

			if (configEntry.IgnoreSSLErrors)
			{
				System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
				{
					Console.WriteLine("Validating 2");
					return true;
				};

				handler.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
				{
					Console.WriteLine("Validating.");
					return true;
				};
			}

			using (var http = new HttpClient(handler))
			{
				HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, configEntry.Url);
				if (configEntry.CustomRequestHeaders != null)
				{
					foreach (var pair in configEntry.CustomRequestHeaders)
						msg.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
				}

				msg.Content = GetContent(toSend, configEntry);

				HttpResponseMessage response;
				try
				{
					response = await http.SendAsync(msg);
				}
				catch (Exception e)
				{
					Console.WriteLine("Error while uploading: ");
					Console.WriteLine(e);

					return null;
				}

				var res = await response.Content.ReadAsStringAsync();

				status.Result = GetInformation(res, configEntry);
				return status.Result;
			}
		}

		private static HttpContent GetContent(byte[] data, ConfigEntry configEntry)
		{
			if (configEntry.BodyFormatInfo == "raw")
			{
				return new ByteArrayContent(data);
			}
			else if (configEntry.BodyFormatInfo.StartsWith("base64-post|", StringComparison.InvariantCulture))
			{
				var fieldName = configEntry.BodyFormatInfo.Split('|')[1];
				var base64String = Convert.ToBase64String(data);

				return new FormUrlEncodedContent(new KeyValuePair<string, string>[]
				{
					new KeyValuePair<string, string>(fieldName, base64String)
				});
			}
			else
			{
				throw new Exception("unknown BodyFormatInfo");
			}
		}

		private static byte[] GetImage(Bitmap bmp, ConfigEntry configEntry)
		{
			int maxSize = int.MaxValue;
			if (configEntry.MaxPngSize > 0)
				maxSize = configEntry.MaxPngSize;
			
			using (var ms = new MemoryStream())
			{
				bmp.Save(ms, ImageFormat.Png);

				if (ms.Position < maxSize)
					return ms.ToArray();
			}

			using (var ms = new MemoryStream())
			{
				bmp.Save(ms, ImageFormat.Jpeg);
				return ms.ToArray();
			}
		}

		private static ImageUploadResult GetInformation(string body, ConfigEntry entry)
		{
			if (entry.ResponseInformation.Type == "json")
			{
				var res = new ImageUploadResult();
				var parsed = JObject.Parse(body);


				if (entry.ResponseInformation.Link != null)
					res.Link = parsed.SelectToken(entry.ResponseInformation.Link).Value<string>();
				
				if (entry.ResponseInformation.DeleteUrl != null)
					res.DeleteUrl = parsed.SelectToken(entry.ResponseInformation.DeleteUrl).Value<string>();

				return res;
			}
			else
			{
				throw new InvalidDataException("Unknown response type!");
			}
		}
	}
}
