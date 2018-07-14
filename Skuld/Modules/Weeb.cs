﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Kitsu.Anime;
using Kitsu.Manga;
using Skuld.Commands;
using Skuld.Core.Globalization;
using Skuld.Core.Services;
using Skuld.Extensions;
using Skuld.Services;
using Skuld.Utilities.Discord;
using SysEx.Net;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Skuld.Modules
{
    [Group]
    public class Weeb : SkuldBase<ShardedCommandContext>
    {
        public DatabaseService Database { get; set; }
        public Locale Locale { get; set; }
        public SysExClient SysExClient { get; set; }
        public GenericLogger Logger { get; set; }

        [Command("anime"), Summary("Gets information about an anime")]
        public async Task GetAnime([Remainder]string animetitle)
        {
            var usr = await Database.GetUserAsync(Context.User.Id);
            var loc = Locale.GetLocale(Locale.defaultLocale);
            if (usr != null)
                loc = Locale.GetLocale(usr.Language);

            var raw = await Anime.GetAnimeAsync(animetitle);
            var data = raw.Data;
            if (data.Count > 1) // do pagination
            {
                var pages = data.PaginateList();

                IUserMessage sentmessage = null;

                if (pages.Count() > 1)
                {
                    sentmessage = await PagedReplyAsync(new PaginatedMessage
                    {
                        Title = loc.GetString("SKULD_SEARCH_WEEB_MKSLCTN") + " 30s",
                        Color = Color.Purple,
                        Pages = pages
                    }, true);
                }
                else
                {
                    sentmessage = await ReplyAsync(Context.Channel, new EmbedBuilder
                    {
                        Title = loc.GetString("SKULD_SEARCH_WEEB_MKSLCTN") + " 30s",
                        Color = Color.Purple,
                        Description = pages[0]
                    }.Build());
                }

                var responce = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
                if (responce == null)
                {
                    await sentmessage.DeleteAsync();
                }

                var selection = Convert.ToInt32(responce.Content);

                var anime = data[selection - 1];

                await ReplyAsync(Context.Channel, anime.ToEmbed(loc));
            }
            else
            {
                var anime = data[0];

                await ReplyAsync(Context.Channel, anime.ToEmbed(loc));
            }
        }

        [Command("manga"), Summary("Gets information about a manga")]
        public async Task GetMangaAsync([Remainder]string mangatitle)
        {
            var usr = await Database.GetUserAsync(Context.User.Id);
            var loc = Locale.GetLocale(Locale.defaultLocale);
            if (usr != null)
                loc = Locale.GetLocale(usr.Language);

            var raw = await Manga.GetMangaAsync(mangatitle);
            var data = raw.Data;
            if (data.Count > 1) // do pagination
            {
                var pages = data.PaginateList();

                IUserMessage sentmessage = null;

                if (pages.Count > 1)
                {
                    sentmessage = await PagedReplyAsync(new PaginatedMessage
                    {
                        Title = loc.GetString("SKULD_SEARCH_MKSLCTN") + " 30s",
                        Color = Color.Purple,
                        Pages = pages
                    }, true);
                }
                else
                {
                    sentmessage = await ReplyAsync(Context.Channel, new EmbedBuilder
                    {
                        Title = loc.GetString("SKULD_SEARCH_MKSLCTN") + " 30s",
                        Color = Color.Purple,
                        Description = pages[0]
                    }.Build());
                }

                var responce = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
                if (responce == null)
                {
                    await sentmessage.DeleteAsync();
                }

                var selection = Convert.ToInt32(responce.Content);

                var manga = data[selection - 1];

                await ReplyAsync(Context.Channel, manga.ToEmbed(loc));
            }
            else
            {
                var manga = data[0];

                await ReplyAsync(Context.Channel, manga.ToEmbed(loc));
            }
        }

        [Command("weebgif"), Summary("Gets a weeb gif")]
        public async Task WeebGif()
        {
            var gif = await SysExClient.GetWeebReactionGifAsync();

            var embed = new EmbedBuilder
            {
                ImageUrl = gif.URL,
                Color = EmbedUtils.RandomColor()
            };

            await ReplyAsync(Context.Channel, embed.Build());
        }
    }
}