using System;
using Newtonsoft.Json;

namespace CalvinAAllen.AzRebrandly
{
	public class Mapping {
		[JsonProperty(PropertyName = "id")]
		public string Id { get; } = Guid.NewGuid().ToString();
		
		public string DestinationUrl { get; set; }
		public string ShortUrl { get; set; }
	}
}