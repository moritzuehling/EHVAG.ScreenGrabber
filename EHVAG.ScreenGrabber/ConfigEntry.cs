using System;
using System.Collections.Generic;

namespace EHVAG.ScreenGrabber
{
	public class ConfigEntry
	{
		public string Name { get; set; }
		public string Url { get; set; }
		public string BodyFormatInfo { get; set; }
		public int MaxPngSize { get; set; }
		public Dictionary<string, string> CustomRequestHeaders { get; set; }
		public ResponseInformation ResponseInformation { get; set; }
	}

	public class ResponseInformation
	{
		public string Type { get; set; }
		public string Link { get; set; }
		public string DeleteUrl { get; set; }
	}
}
