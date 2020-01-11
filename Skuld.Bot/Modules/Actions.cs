﻿using Discord;
using Discord.Commands;
using Miki.API.Images;
using Skuld.Bot.Extensions;
using Skuld.Core.Extensions;
using Skuld.Core.Extensions.Discord;
using Skuld.Core.Models;
using Skuld.Core.Utilities;
using Skuld.Discord.Extensions;
using Skuld.Discord.Preconditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skuld.Bot.Commands
{
    [Group, RequireEnabledModule]
    public class Actions : ModuleBase<ShardedCommandContext>
    {
        public Random Random { get; set; }
        public ImghoardClient Imghoard { get; set; }

        private EmbedBuilder DoAction(string gif, string action, string target)
        {
            List<ulong> prune = new List<ulong>();

            {
                using SkuldDatabaseContext Database = new SkuldDbContextFactory().CreateDbContext(null);

                if (Context.Message.MentionedUsers.Any())
                {
                    foreach (var mentionedUser in Context.Message.MentionedUsers)
                    {
                        var res = Database.BlockedActions.FirstOrDefault(x => x.Blockee == mentionedUser.Id && x.Blocker == Context.User.Id);

                        if (res != null)
                            prune.Add(mentionedUser.Id);
                    }
                }
            }

            foreach (var id in prune)
            {
                target.PruneMention(id);
            }

            return new EmbedBuilder()
                .WithImageUrl(gif)
                .WithTitle(action.CapitaliseFirstLetter())
                .WithDescription(target)
                .WithRandomColor()
                .AddAuthor(Context.Client)
                .AddFooter(Context);
        }

        private string GetMessage(string target, string isnull, string notnull)
            => target == null ? isnull : notnull;

        [Command("slap"), Summary("Slap a user")]
        public async Task Slap([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"B-Baka.... {Context.Client.CurrentUser.Mention} slaps {Context.User.Mention}",
                    $"{Context.User.Mention} slaps {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("stab"), Summary("Stabs a user")]
        public async Task Stab([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"URUSAI!! {Context.Client.CurrentUser.Mention} stabs {target}",
                    $"{Context.User.Mention} stabs {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("hug"), Summary("hugs a user")]
        public async Task Hug([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"{Context.Client.CurrentUser.Mention} hugs {target}",
                    $"{Context.User.Mention} hugs {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("punch"), Summary("Punch a user")]
        public async Task Punch([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"{Context.Client.CurrentUser.Mention} punches {Context.User.Mention}",
                    $"{Context.User.Mention} punches {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("shrug"), Summary("Shrugs")]
        public async Task Shrug()
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            await
                new EmbedBuilder()
                .WithImageUrl(image)
                .WithTitle(Utils.GetCaller())
                .WithDescription($"{Context.User.Mention} shrugs.")
                .WithRandomColor()
                .AddAuthor(Context.Client)
                .AddFooter(Context)
            .QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("adore"), Summary("Adore a user")]
        public async Task Adore([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"I-it's not like I like you or anything... {Context.Client.CurrentUser.Mention} adores {Context.User.Mention}",
                    $"{Context.User.Mention} adores {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("kiss"), Summary("Kiss a user")]
        public async Task Kiss([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"I-it's not like I like you or anything... {Context.Client.CurrentUser.Mention} kisses {Context.User.Mention}",
                    $"{Context.User.Mention} kisses {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("grope"), Summary("Grope a user")]
        public async Task Grope([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"{Context.Client.CurrentUser.Mention} gropes {Context.User.Mention}",
                    $"{Context.User.Mention} gropes {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("pet"), Summary("Pat a user"), Alias("pat", "headpat")]
        public async Task Pet([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = 
                new EmbedBuilder()
                .WithImageUrl(image)
                .WithTitle(Utils.GetCaller().CapitaliseFirstLetter())
                .WithRandomColor()
                .AddAuthor(Context.Client)
                .AddFooter(Context);

            if (Context.Message.MentionedUsers.Any())
            {
                List<ulong> prune = new List<ulong>();

                {
                    using SkuldDatabaseContext Database = new SkuldDbContextFactory().CreateDbContext(null);

                    foreach (var mentionedUser in Context.Message.MentionedUsers)
                    {
                        var res = Database.BlockedActions.FirstOrDefault(x => x.Blockee == mentionedUser.Id && x.Blocker == Context.User.Id);

                        if (res != null)
                            prune.Add(mentionedUser.Id);
                    }
                }

                {
                    using SkuldDatabaseContext Database = new SkuldDbContextFactory().CreateDbContext(null);
                    var initiator = await Database.GetUserAsync(Context.User).ConfigureAwait(false);

                    StringBuilder message = new StringBuilder();

                    var msg = target;

                    foreach (var usr in Context.Message.MentionedUsers)
                    {
                        if (usr.IsBot || usr.IsWebhook || usr.Discriminator == "0000" || prune.Contains(usr.Id))
                            continue;

                        var uzr = await Database.GetUserAsync(usr).ConfigureAwait(false);

                        if (!(uzr.RecurringBlock && uzr.Patted.IsRecurring(2)))
                        {
                            uzr.Patted += 1;
                            initiator.Pats += 1;

                            message.Append(usr.Mention + " ");
                        }
                        else
                        { 
                            msg.PruneMention(usr.Id);
                        }
                    }

                    await Database.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }

        [Command("glare"), Summary("Glares at a user"), Alias("stare")]
        public async Task Stare([Remainder]string target = null)
        {
            var images = await Imghoard.GetImagesAsync(Utils.GetCaller().LowercaseFirstLetter()).ConfigureAwait(false);

            var image = images.Images.RandomValue().Url;

            var action = DoAction(
                image,
                Utils.GetCaller(),
                GetMessage(target,
                    $"{Context.Client.CurrentUser.Mention} glares at {Context.User.Mention}",
                    $"{Context.User.Mention} glares at {target}"
                )
            );

            await action.QueueMessageAsync(Context).ConfigureAwait(false);
        }
    }
}