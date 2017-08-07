﻿using PokeSharp.Models.Common;
using Newtonsoft.Json;

namespace PokeSharp.Models.Pokemon
{
    public class PokeathlonStat
    {
        [JsonProperty(PropertyName = "id")]
        public int? ID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "names")]
        public ListName[] Names { get; set; }
        [JsonProperty(PropertyName = "affecting_natures")]
        public NaturePokeathlonStatAffectSets AffectingNatures { get; set; }
    }
}
