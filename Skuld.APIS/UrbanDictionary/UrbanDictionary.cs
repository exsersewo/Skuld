﻿using Newtonsoft.Json;
using Skuld.APIS.UrbanDictionary.Models;
using Skuld.APIS.Utilities;
using Skuld.Core.Extensions;
using System;
using System.Threading.Tasks;

namespace Skuld.APIS
{
    public class UrbanDictionaryClient
    {
        private static readonly Uri RandomEndpoint = new Uri("http://api.urbandictionary.com/v0/random");
        private static readonly Uri QueryEndPoint = new Uri("http://api.urbandictionary.com/v0/define?term=");

        private readonly RateLimiter rateLimiter;

        public UrbanDictionaryClient()
        {
            rateLimiter = new RateLimiter();
        }

        public async Task<UrbanWord> GetRandomWordAsync()
        {
            if (rateLimiter.IsRatelimited()) return null;

            var raw = await HttpWebClient.ReturnStringAsync(RandomEndpoint).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<UrbanWord>(raw);
        }

        public async Task<UrbanWord> GetPhraseAsync(string phrase)
        {
            if (rateLimiter.IsRatelimited()) return null;

            var raw = await HttpWebClient.ReturnStringAsync(new Uri(QueryEndPoint + phrase)).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<UrbanWordContainer>(raw).List.RandomValue();
        }
    }
}