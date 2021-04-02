﻿using Newtonsoft.Json;
using System;

namespace Skuld.APIS.Wikia.Models
{
	public class LocalWikiSearchResult
	{
		[JsonProperty(PropertyName = "id")]
		public int ID { get; set; }

		[JsonProperty(PropertyName = "title")]
		public string Title { get; set; }

		[JsonProperty(PropertyName = "url")]
		public Uri Permalink { get; set; }

		[JsonProperty(PropertyName = "ns")]
		public int NameSpace { get; set; }

		[JsonProperty(PropertyName = "quality")]
		public int Quality { get; set; }

		[JsonProperty(PropertyName = "snippet")]
		public string Snippet { get; set; }
	}
}