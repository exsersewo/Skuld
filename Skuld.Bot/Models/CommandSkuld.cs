﻿namespace Skuld.Bot.Models
{
    public struct CommandSkuld
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ParameterSkuld[] Parameters { get; set; }
        public string[] Aliases { get; set; }
    }
}
