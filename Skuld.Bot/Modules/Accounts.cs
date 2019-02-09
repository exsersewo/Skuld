﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using ImageMagick;
using Skuld.APIS;
using Skuld.Core;
using Skuld.Core.Extensions;
using Skuld.Core.Models;
using Skuld.Core.Utilities;
using Skuld.Database;
using Skuld.Database.Extensions;
using Skuld.Discord.Commands;
using Skuld.Discord.Extensions;
using Skuld.Discord.Preconditions;
using Skuld.Discord.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Skuld.Bot.Commands
{
    [Group, RequireEnabledModule]
    public class Profiles : InteractiveBase<SkuldCommandContext>
    {
        public SkuldConfig Configuration { get; set; }
        public BaseClient WebHandler { get; set; }

        [Command("money"), Summary("Gets a user's money"), RequireDatabase]
        public async Task Money([Remainder]IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }
            try
            {
                var skuser = Context.DBUser;

                if (user != null)
                {
                    var resp = await DatabaseClient.GetUserAsync(user.Id).ConfigureAwait(false);
                    if (resp.Successful)
                        skuser = resp.Data as SkuldUser;
                    else
                    {
                        await DatabaseClient.InsertUserAsync(user);
                        await Money(user);
                    }
                }

                if (user == null)
                    await $"You have: {Configuration.Preferences.MoneySymbol}{skuser.Money.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                else
                    await $"{user.Mention} has {Configuration.Preferences.MoneySymbol}{skuser.Money.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-MONEY", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("profile"), Summary("Get a users profile")]
        public async Task Profile([Remainder]IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }

            if (user == null)
            {
                user = Context.User as IGuildUser;
            }

            if (await DatabaseClient.CheckConnectionAsync())
            {
                var resp = await DatabaseClient.GetUserAsync(user.Id).ConfigureAwait(false);
                if (!resp.Successful)
                {
                    await DatabaseClient.InsertUserAsync(user);
                    await Profile(user);
                    return;
                }
            }

            SkuldUser profileuser;

            if (Context.User.Id == user.Id)
                profileuser = Context.DBUser;
            else
            {
                var res = await DatabaseClient.GetUserAsync(user.Id);
                profileuser = res.Data as SkuldUser;
            }

            var imagickCache = Path.Combine(AppContext.BaseDirectory, "/storage/imagickCache/");
            var folder = Path.Combine(AppContext.BaseDirectory, "/storage/profiles/");
            var fontsFolder = Path.Combine(AppContext.BaseDirectory, "/storage/fonts/");
            var fontFile = Path.Combine(fontsFolder, "NotoSans-Regular.ttf");

            if (!Directory.Exists(fontsFolder))
            {
                Directory.CreateDirectory(fontsFolder);
                await WebHandler.DownloadFileAsync(new Uri("https://static.skuldbot.uk/fonts/NotoSans-Regular.ttf"), fontFile);
            }

            if (!Directory.Exists(imagickCache))
                Directory.CreateDirectory(imagickCache);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            MagickAnyCPU.CacheDirectory = imagickCache;

            var imageLocation = folder + user.Id + ".png";

            var backgroundImage = folder + user.Id + ".png";

            var imageBackgroundFolder = Path.Combine(AppContext.BaseDirectory, "/storage/backgroundCache/");

            var imageBackgroundFile = Path.Combine(imageBackgroundFolder, profileuser.ID + "_background.png");

            if (!Directory.Exists(imageBackgroundFolder))
                Directory.CreateDirectory(imageBackgroundFolder);

            if (!profileuser.Background.StartsWith('#') && !(profileuser.Background == ""))
            {
                await WebHandler.DownloadFileAsync(new Uri(profileuser.Background), imageBackgroundFile);
            }

            using (MagickImage image = new MagickImage(new MagickColor("#212121"), 600, 510))
            {
                image.Format = MagickFormat.Png;

                if (profileuser.Background.StartsWith('#'))
                {
                    var col = profileuser.Background.FromHex();
                    image.Draw(new DrawableFillColor(new MagickColor(col.R, col.G, col.B)), new DrawableRectangle(0, 0, 600, 228));
                }
                if (profileuser.Background == "")
                {
                    image.Draw(new DrawableFillColor(new MagickColor("#3F51B5")), new DrawableRectangle(0, 0, 600, 228));
                }
                else
                {
                    using (MagickImage img2 = new MagickImage(imageBackgroundFile))
                    {
                        img2.FilterType = FilterType.Quadratic;
                        img2.Resize(600, 0);
                        img2.Crop(600, 228, Gravity.Center);
                        image.Composite(img2);
                    }
                }

                var avatar = user.GetAvatarUrl(ImageFormat.Png) ?? user.GetDefaultAvatarUrl();

                using (MagickImage profileBackground = new MagickImage(avatar, 128, 128))
                {
                    profileBackground.BackgroundColor = MagickColors.None;

                    using (var mask = new MagickImage("xc:black", 128, 128))
                    {
                        mask.Draw(new DrawableFillColor(MagickColors.White), new DrawableCircle(64, 64, 64, 128));

                        mask.Transparent(MagickColors.Black);

                        profileBackground.Composite(mask, CompositeOperator.CopyAlpha);

                        mask.Dispose();
                    }

                    using (MagickImage statusBackground = new MagickImage($"xc:{user.Status.HexFromStatus()}", 32, 32))
                    {
                        statusBackground.BackgroundColor = MagickColors.None;

                        using (var mask = new MagickImage("xc:black", 32, 32))
                        {
                            mask.Draw(new DrawableFillColor(MagickColors.White), new DrawableCircle(16, 16, 16, 32));

                            mask.Transparent(MagickColors.Black);

                            statusBackground.Composite(mask, CompositeOperator.CopyAlpha);

                            mask.Dispose();
                        }

                        profileBackground.Composite(statusBackground, 96, 96, CompositeOperator.Over);

                        statusBackground.Dispose();
                    }

                    image.Composite(profileBackground, 64, 64, CompositeOperator.Over);

                    profileBackground.Dispose();
                }

                var font = new DrawableFont(fontFile);
                var encoding = new DrawableTextEncoding(System.Text.Encoding.Unicode);
                var fontsize = new DrawableFontPointSize(20);
                var fontbig = new DrawableFontPointSize(35);
                var fontmed = new DrawableFontPointSize(30);
                var centeredtext = new DrawableTextAlignment(TextAlignment.Center);
                var white = new DrawableFillColor(new MagickColor(65535, 65535, 65535));

                var experience = await profileuser.GetUserExperienceAsync();

                GuildExperience totalExperience = new GuildExperience();

                foreach (var exp in experience.GuildExperiences)
                {
                    totalExperience.XP += exp.XP;
                    totalExperience.TotalXP += exp.TotalXP;
                    totalExperience.Level += exp.Level;
                }

                int ylevel1 = 365, ylevel2 = 405, ylevel3 = 445;

                //Bar
                image.Draw(new DrawableFillColor(new MagickColor(0, 0, 0, 52428)), new DrawableRectangle(0, 188, 600, 228));

                //Rep
                using (MagickImage label = new MagickImage($"label:{profileuser.Reputation.Count()} Rep", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.White,
                    Width = 580,
                    Height = 30,
                    TextGravity = Gravity.West,
                    FontPointsize = 30,
                    Font = fontFile
                }))
                {
                    image.Composite(label, 20, 193, CompositeOperator.Over);

                    label.Dispose();
                }

                //Money
                using (MagickImage label2 = new MagickImage($"label:{Configuration.Preferences.MoneySymbol}{profileuser.Money.ToString("N0")}", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.White,
                    Width = 580,
                    Height = 30,
                    TextGravity = Gravity.East,
                    FontPointsize = 30,
                    Font = fontFile
                }))
                {
                    image.Composite(label2, 0, 193, CompositeOperator.Over);

                    label2.Dispose();
                }

                //Username
                using (MagickImage label3 = new MagickImage($"label:{user.FullName()}", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.White,
                    Width = 600,
                    Height = 40,
                    TextGravity = Gravity.Center,
                    Font = fontFile
                }))
                {
                    image.Composite(label3, 0, 230, CompositeOperator.Over);

                    label3.Dispose();
                }

                //Title
                if(profileuser.Title != "")
                {
                    using (MagickImage label4 = new MagickImage($"label:{profileuser.Title}", new MagickReadSettings
                    {
                        BackgroundColor = MagickColors.Transparent,
                        FillColor = MagickColors.White,
                        Width = 600,
                        Height = 40,
                        TextGravity = Gravity.Center,
                        Font = fontFile
                    }))
                    {
                        image.Composite(label4, 0, 270, CompositeOperator.Over);

                        label4.Dispose();
                    }
                }

                //YLevel 1
                var dailyText = $"Daily: {profileuser.Daily.FromEpoch().ToString("yyyy/MM/dd HH:mm:ss")}";
                var dmetr = image.FontTypeMetrics(dailyText, true);
                var rightPos = 600 - (dmetr.TextWidth * 2);

                var rank = await profileuser.GetGlobalRankAsync();

                image.Draw(font, fontsize, encoding, white, new DrawableText(22, ylevel1, $"Rank {rank.Position}/{rank.Total}"));
                image.Draw(font, fontsize, encoding, white, new DrawableText(rightPos, ylevel1, dailyText));

                //YLevel 2
                image.Draw(font, fontsize, encoding, white, new DrawableText(22, ylevel2, $"Pasta Karma: {(await profileuser.GetPastaKarmaAsync()).ToString("N0")}"));
                var favcommand = profileuser.GetFavouriteCommand();
                image.Draw(font, fontsize, encoding, white, new DrawableText(rightPos, ylevel2, $"Fav. Cmd: {(favcommand == null ? "N/A" : favcommand.Command)} ({(favcommand == null ? "0" : favcommand.Usage.ToString("N0"))})"));

                //YLevel 3
                image.Draw(font, fontsize, encoding, white, new DrawableText(22, ylevel3, $"Level: {totalExperience.Level} ({totalExperience.TotalXP.ToString("N0")})"));
                image.Draw(font, fontsize, encoding, white, new DrawableText(rightPos, ylevel3, $"Pats: {profileuser.Pats}/Patted: {profileuser.Patted}"));

                ulong xpToNextLevel = DiscordUtilities.GetXPLevelRequirement(totalExperience.Level + 1, DiscordUtilities.PHI);

                //Progressbar
                image.Draw(new DrawableFillColor(new MagickColor("#212121")), new DrawableRectangle(20, 471, 580, 500));
                image.Draw(new DrawableFillColor(new MagickColor("#dfdfdf")), new DrawableRectangle(22, 473, 578, 498));

                var percentage = (double)totalExperience.XP / xpToNextLevel * 100;
                var mapped = percentage.Remap(0, 100, 22, 578);

                image.Draw(new DrawableFillColor(new MagickColor("#009688")), new DrawableRectangle(22, 473, mapped, 498));

                //Current XP
                image.Draw(font, fontsize, encoding, new DrawableText(25, 493, (totalExperience.XP).ToString("N0") + "XP"));

                //XP To Next
                using (MagickImage label5 = new MagickImage($"label:{(xpToNextLevel).ToString("N0")}XP", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.Black,
                    Width = 575,
                    Height = 20,
                    TextGravity = Gravity.East,
                    FontPointsize = 20,
                    Font = fontFile
                }))
                {
                    image.Composite(label5, 0, 475, CompositeOperator.Over);

                    label5.Dispose();
                }

                image.Write(imageLocation);

                image.Dispose();
            }

            await "".QueueMessage(Discord.Models.MessageType.File, Context.User, Context.Channel, imageLocation);
        }

        [Command("profile-ext"), Summary("Get a users extended profile"), RequireDatabase]
        public async Task ExtProfile([Remainder]IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }
            try
            {
                var skuser = Context.DBUser;

                if (user != null)
                {
                    var resp = await DatabaseClient.GetUserAsync(user.Id).ConfigureAwait(false);
                    if (resp.Successful)
                        skuser = resp.Data as SkuldUser;
                    else
                    {
                        await DatabaseClient.InsertUserAsync(user);
                        await ExtProfile(user);
                    }
                }

                var embed = await skuser.GetExtendedProfileAsync(user ?? (IGuildUser)Context.User, Configuration);

                await embed.QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-PROFILEEXT", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("daily"), Summary("Daily Money"), RequireDatabase]
        public async Task Daily(IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }
            try
            {
                var context = await DatabaseClient.GetUserAsync(Context.User.Id);
                if (user == null)
                {
                    if (context.Data is SkuldUser)
                    {
                        var resp = await ((SkuldUser)context.Data).DoDailyAsync(Configuration);

                        context = await DatabaseClient.GetUserAsync(Context.User.Id);

                        if (resp)
                        {
                            await $"You got your daily of: `{Configuration.Preferences.MoneySymbol + Configuration.Preferences.DailyAmount}`, you now have: {Configuration.Preferences.MoneySymbol}{((SkuldUser)context.Data).Money.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                        }
                        else
                        {
                            var remain = new TimeSpan((DateTime.UtcNow.AddDays(1).Date.ToEpoch() - DateTime.UtcNow.ToEpoch()).FromEpoch().Ticks);
                            string remaining = remain.Hours + " Hours " + remain.Minutes + " Minutes " + remain.Seconds + " Seconds";
                            await $"You must wait `{remaining}`".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                        }
                    }
                }
                else
                {
                    var suser = await DatabaseClient.GetUserAsync(user.Id);
                    if (suser.Data is SkuldUser)
                    {
                        var resp = await ((SkuldUser)suser.Data).DoDailyAsync(Configuration, (SkuldUser)context.Data);
                        suser = await DatabaseClient.GetUserAsync(user.Id);
                        if (resp)
                        {
                            await $"You just gave {user.Mention} your daily of: `{Configuration.Preferences.MoneySymbol + Configuration.Preferences.DailyAmount}`, they now have: {Configuration.Preferences.MoneySymbol}{((SkuldUser)suser.Data).Money.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                        }
                        else
                        {
                            var remain = new TimeSpan((DateTime.UtcNow.AddDays(1).Date.ToEpoch() - DateTime.UtcNow.ToEpoch()).FromEpoch().Ticks);
                            string remaining = remain.Hours + " Hours " + remain.Minutes + " Minutes " + remain.Seconds + " Seconds";
                            await $"You must wait `{remaining}`".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-DAILY", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("give"), Summary("Give your money to people"), RequireDatabase]
        public async Task Give(IGuildUser user, ulong amount)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }
            try
            {
                var skuserResp = await DatabaseClient.GetUserAsync(Context.User.Id).ConfigureAwait(false);
                SkuldUser skuser = null;
                if(skuserResp.Data is SkuldUser)
                {
                    skuser = skuserResp.Data as SkuldUser;
                    if (skuser.Money < amount)
                    {
                        await "You can't give more money than you have".QueueMessage(Discord.Models.MessageType.Mention, Context.User, Context.Channel);
                        return;
                    }
                }
                else
                {
                    await "Error parsing user information".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                }

                var skuser2resp = await DatabaseClient.GetUserAsync(user.Id);
                if(skuser2resp.Data is SkuldUser)
                {
                    var skuser2 = skuser2resp.Data as SkuldUser;
                    skuser.Money -= amount;
                    skuser2.Money += amount;

                    var res1 = await DatabaseClient.UpdateUserAsync(skuser);
                    var res2 = await DatabaseClient.UpdateUserAsync(skuser2);

                    if (res1.Successful && res2.Successful)
                    {
                        await $"You just gave {user.Mention} {Configuration.Preferences.MoneySymbol}{amount.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Mention, Context.User, Context.Channel);
                    }
                    else
                    {
                        await "Updating Unsuccessful".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    }
                }
                else
                {
                    await "Error parsing user information".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                }
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-GIVE", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("rank"), Summary("Gets your or someone's current level"), RequireDatabase]
        [Alias("exp")]
        public async Task Level(IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }

            if (user == null)
            {
                user = Context.User as IGuildUser;
            }

            if (await DatabaseClient.CheckConnectionAsync())
            {
                var resp = await DatabaseClient.GetUserAsync(user.Id).ConfigureAwait(false);
                if (!resp.Successful)
                {
                    await DatabaseClient.InsertUserAsync(user);
                    await Profile(user);
                    return;
                }
            }

            SkuldUser profileuser;

            if (Context.User.Id == user.Id)
                profileuser = Context.DBUser;
            else
            {
                var res = await DatabaseClient.GetUserAsync(user.Id);
                profileuser = res.Data as SkuldUser;
            }

            var imagickCache = Path.Combine(AppContext.BaseDirectory, "/storage/imagickCache/");
            var folder = Path.Combine(AppContext.BaseDirectory, "/storage/exp/");
            var fontsFolder = Path.Combine(AppContext.BaseDirectory, "/storage/fonts/");
            var fontFile = Path.Combine(fontsFolder, "NotoSans-Regular.ttf");

            if (!Directory.Exists(fontsFolder))
            {
                Directory.CreateDirectory(fontsFolder);
                await WebHandler.DownloadFileAsync(new Uri("https://static.skuldbot.uk/fonts/NotoSans-Regular.ttf"), fontFile);
            }

            if (!Directory.Exists(imagickCache))
                Directory.CreateDirectory(imagickCache);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            MagickAnyCPU.CacheDirectory = imagickCache;

            var imageLocation = folder + user.Id + ".png";

            var backgroundImage = folder + user.Id + ".png";

            var imageBackgroundFolder = Path.Combine(AppContext.BaseDirectory, "/storage/backgroundCache/");

            var imageBackgroundFile = Path.Combine(imageBackgroundFolder, profileuser.ID + "_background.png");

            if (!Directory.Exists(imageBackgroundFolder))
                Directory.CreateDirectory(imageBackgroundFolder);

            if (!profileuser.Background.StartsWith('#') && !(profileuser.Background == ""))
            {
                await WebHandler.DownloadFileAsync(new Uri(profileuser.Background), imageBackgroundFile);
            }

            using (MagickImage image = new MagickImage(new MagickColor("#212121"), 750, 300))
            {
                image.Format = MagickFormat.Png;

                if (profileuser.Background.StartsWith('#'))
                {
                    var col = profileuser.Background.FromHex();
                    image.Draw(new DrawableFillColor(new MagickColor(col.R, col.G, col.B)), new DrawableRectangle(0, 0, 750, 300));
                }
                if (profileuser.Background == "")
                {
                    image.Draw(new DrawableFillColor(new MagickColor("#3F51B5")), new DrawableRectangle(0, 0, 750, 300));
                }
                else
                {
                    using (MagickImage img2 = new MagickImage(imageBackgroundFile))
                    {
                        img2.FilterType = FilterType.Quadratic;
                        img2.Resize(750, 0);
                        img2.Crop(750, 300, Gravity.Center);
                        image.Composite(img2);
                    }
                }

                //Box
                image.Draw(new DrawableFillColor(new MagickColor(0, 0, 0, 52428)), new DrawableRectangle(20, 20, 730, 280));

                var avatar = user.GetAvatarUrl(ImageFormat.Png) ?? user.GetDefaultAvatarUrl();

                using (MagickImage profileBackground = new MagickImage(avatar, 128, 128))
                {
                    profileBackground.BackgroundColor = MagickColors.None;

                    using (var mask = new MagickImage("xc:black", 128, 128))
                    {
                        mask.Draw(new DrawableFillColor(MagickColors.White), new DrawableCircle(64, 64, 64, 128));

                        mask.Transparent(MagickColors.Black);

                        profileBackground.Composite(mask, CompositeOperator.CopyAlpha);

                        mask.Dispose();
                    }

                    using (MagickImage statusBackground = new MagickImage($"xc:{user.Status.HexFromStatus()}", 32, 32))
                    {
                        statusBackground.BackgroundColor = MagickColors.None;

                        using (var mask = new MagickImage("xc:black", 32, 32))
                        {
                            mask.Draw(new DrawableFillColor(MagickColors.White), new DrawableCircle(16, 16, 16, 32));

                            mask.Transparent(MagickColors.Black);

                            statusBackground.Composite(mask, CompositeOperator.CopyAlpha);

                            mask.Dispose();
                        }

                        profileBackground.Composite(statusBackground, 96, 96, CompositeOperator.Over);

                        statusBackground.Dispose();
                    }

                    image.Composite(profileBackground, 84, 84, CompositeOperator.Over);

                    profileBackground.Dispose();
                }

                var font = new DrawableFont(fontFile);
                var encoding = new DrawableTextEncoding(System.Text.Encoding.Unicode);
                var fontsize = new DrawableFontPointSize(20);
                var fontbig = new DrawableFontPointSize(35);
                var fontmed = new DrawableFontPointSize(30);
                var fontmedd = new DrawableFontPointSize(26);
                var centeredtext = new DrawableTextAlignment(TextAlignment.Center);
                var white = new DrawableFillColor(new MagickColor(65535, 65535, 65535));

                var userExperience = await profileuser.GetUserExperienceAsync();
                var guildExperience = userExperience.GetGuildExperience(Context.Guild.Id);

                var guildRank = await profileuser.GetGuildRankAsync(Context.Guild);

                //Username
                using (MagickImage label3 = new MagickImage($"label:{user.FullName()}", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.White,
                    Width = 510,
                    Height = 60,
                    TextGravity = Gravity.West,
                    Font = fontFile
                }))
                {
                    image.Composite(label3, 220, 80, CompositeOperator.Over);

                    label3.Dispose();
                }

                image.Draw(font, fontmed, encoding, white, new DrawableText(220, 170, $"Rank {guildRank.Position}/{guildRank.Total}"));
                image.Draw(font, fontmed, encoding, white, new DrawableText(220, 210, $"Level: {guildExperience.Level} ({guildExperience.TotalXP.ToString("N0")})"));

                ulong xpToNextLevel = DiscordUtilities.GetXPLevelRequirement(guildExperience.Level + 1, DiscordUtilities.PHI);

                int innerHeight = 256;

                //Progressbar
                image.Draw(new DrawableFillColor(new MagickColor("#212121")), new DrawableRectangle(20, innerHeight-2, 730, 280));
                image.Draw(new DrawableFillColor(new MagickColor("#dfdfdf")), new DrawableRectangle(22, innerHeight, 728, 278));

                var percentage = (double)guildExperience.XP / xpToNextLevel * 100;
                var mapped = percentage.Remap(0, 100, 22, 728);

                image.Draw(new DrawableFillColor(new MagickColor("#009688")), new DrawableRectangle(22, innerHeight, mapped, 278));

                //Current XP
                image.Draw(font, fontmedd, encoding, new DrawableText(25, 275, (guildExperience.XP).ToString("N0") + "XP"));

                //XP To Next
                using (MagickImage label5 = new MagickImage($"label:{(xpToNextLevel).ToString("N0")}XP", new MagickReadSettings
                {
                    BackgroundColor = MagickColors.Transparent,
                    FillColor = MagickColors.Black,
                    Width = 725,
                    Height = 30,
                    TextGravity = Gravity.East,
                    FontPointsize = 26,
                    Font = fontFile
                }))
                {
                    image.Composite(label5, 0, 250, CompositeOperator.Over);

                    label5.Dispose();
                }

                image.Write(imageLocation);

                image.Dispose();
            }

            await "".QueueMessage(Discord.Models.MessageType.File, Context.User, Context.Channel, imageLocation);
        }

        [Command("heal"), Summary("Shows you how much you can heal by"), RequireDatabase]
        public async Task HealAmount()
        {
            try
            {
                if(Context.DBUser != null)
                {
                    var amnt = Context.DBUser.HP / 0.8;
                    await $"You can heal for: `{Math.Floor(amnt)}`HP".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await DatabaseClient.InsertUserAsync(Context.User);
                    await HealAmount();
                }
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-HEAL", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("heal"), Summary("Heal yourself or others here"), RequireDatabase]
        public async Task Heal(uint hp, [Remainder] IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }

            var contextDB = Context.DBUser;
            var d = await DatabaseClient.GetUserAsync(user.Id).ConfigureAwait(false);
            var userDB = user == null ? null : ((d.Successful) ? (SkuldUser)d.Data : null);

            if (user == null)
            {
                if (contextDB.HP == 10000)
                {
                    await "You're already at max health".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }
                var amount = GetCostOfHP(hp);
                if (contextDB.Money < amount)
                {
                    await "You don't have enough money for this action".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }
                if (hp > (10000 - contextDB.HP))
                {
                    await ("You only need to heal by: " + (10000 - contextDB.HP)).QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }

                contextDB.Money -= amount;
                contextDB.HP += hp;

                if (contextDB.HP > 10000)
                    contextDB.HP = 10000;

                await DatabaseClient.UpdateUserAsync(contextDB);

                await $"You have healed your HP by {hp} for {Configuration.Preferences.MoneySymbol}{amount.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
            }
            else
            {
                if (contextDB.HP == 10000)
                {
                    await "They're already at max health".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }
                var amount = GetCostOfHP(hp);
                if (contextDB.Money < amount)
                {
                    await "You don't have enough money for this action".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }
                if (hp > (10000 - userDB.HP))
                {
                    await ("You only need to heal them by: " + (10000 - userDB.HP)).QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    return;
                }

                contextDB.Money -= amount;
                userDB.HP += hp;

                if (userDB.HP > 10000)
                    userDB.HP = 10000;

                await DatabaseClient.UpdateUserAsync(contextDB);
                await DatabaseClient.UpdateUserAsync(userDB);

                await $"You have healed {user.Mention}'s HP by {hp} for {Configuration.Preferences.MoneySymbol}{amount.ToString("N0")}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
            }
        }

        [Command("rep"), Summary("Gives someone rep or checks your rep"), RequireDatabase]
        public async Task GiveRep([Remainder]IGuildUser user = null)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }
            if (user == null)
            {
                if (Context.DBUser.Reputation.Count() > 0)
                {
                    await $"Your repuation is at: {Context.DBUser.Reputation.Count()}rep\nYour most recent rep was by {Context.Client.GetUser(Context.DBUser.Reputation.FirstOrDefault().Reper).FullName()} at {Context.DBUser.Reputation.FirstOrDefault().Timestamp.FromEpoch()}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await "You have no reputation".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                return;
            }
            else
            {
                if(user.Id == Context.User.Id)
                {
                    await "Cannot give yourself rep".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    return;
                }

                var dbUser = await DatabaseClient.GetUserAsync(user.Id);
                if(dbUser.Successful && dbUser.Data is SkuldUser)
                {
                    var data = dbUser.Data as SkuldUser;
                    if(data.Reputation.Where(x=>x.Reper == Context.User.Id).Count() == 0)
                    {
                        await DatabaseClient.AddReputationAsync(data.ID, Context.User.Id);
                        await $"You gave rep to {user.Mention}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    }
                    else
                    {
                        await "You have already given this person a reputation point.".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    }
                }
                else
                {
                    await DatabaseClient.InsertUserAsync(user);
                    await GiveRep(user);
                    return;
                }
            }
        }

        [Command("unrep"), Summary("Removes a rep"), RequireDatabase]
        public async Task RemoveRep([Remainder]IGuildUser user)
        {
            if (user != null && (user.IsBot || user.IsWebhook))
            {
                await DiscordTools.NoBotsString.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }

            if (user.Id == Context.User.Id)
            {
                await "Cannot remove rep from yourself".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                return;
            }

            var dbUser = await DatabaseClient.GetUserAsync(user.Id);
            if (dbUser.Successful && dbUser.Data is SkuldUser)
            {
                var data = dbUser.Data as SkuldUser;
                if (data.Reputation.Where(x => x.Reper == Context.User.Id).Count() != 0)
                {
                    await DatabaseClient.RemoveReputationAsync(data.ID, Context.User.Id);
                    await $"You removed your rep to {user.Mention}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await $"You haven't given {user.Mention} a reputation point.".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                }
            }
            else
            {
                await dbUser.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
            }
        }

        private ulong GetCostOfHP(uint hp)
            => (ulong)Math.Round(Math.Ceiling(hp / 0.8));
    }

    [Group, Name("Accounts"), RequireDatabase]
    public class Account : InteractiveBase<SkuldCommandContext>
    {
        public SkuldConfig Configuration { get; set; }

        [Command("set-title"), Summary("Sets Title")]
        public async Task SetTitle([Remainder]string title)
        {
            try
            {
                var userResp = await DatabaseClient.GetUserAsync(Context.User.Id).ConfigureAwait(false);
                var user = userResp.Data as SkuldUser;

                user.Title = title;

                var result = await DatabaseClient.UpdateUserAsync(user);
                if (result.Successful)
                {
                    await $"Successfully set your title to **{title}**".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await "Couldn't Parse User Information".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);

                    await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("Accounts", result.Error, LogSeverity.Error, result.Exception));
                }
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-SDESC", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("clear-title"), Summary("Clears Title")]
        public async Task ClearTitle()
        {
            try
            {
                var userResp = await DatabaseClient.GetUserAsync(Context.User.Id);
                var user = userResp.Data as SkuldUser;

                user.Title = "";

                var result = await DatabaseClient.UpdateUserAsync(user);
                if (result.Successful)
                {
                    await "Successfully cleared your title.".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await "Couldn't Parse User Information".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);

                    await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("Accounts", result.Error, LogSeverity.Error, result.Exception));
                }
            }
            catch (Exception ex)
            {
                await ex.Message.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel, null, ex);
                await GenericLogger.AddToLogsAsync(new Skuld.Core.Models.LogMessage("CMD-CDESC", ex.Message, LogSeverity.Error, ex));
            }
        }

        [Command("recurring-block"), Summary("Blocks people from patting you on recurring digits")]
        public async Task BlockRecurring(bool action)
        {
            Context.DBUser.RecurringBlock = action;
            var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
            if(res.Successful)
            {
                await $"Set RecurringBlock to: {action}".QueueMessage(Discord.Models.MessageType.Success, Context.User, Context.Channel);
            }
            else
            {
                await res.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
            }
        }

        [Command("action-block"), Summary("Blocks people from performing actions on you")]
        public async Task BlockActions([Remainder]IUser user)
        {
            var res = await DatabaseClient.IsActionBlockedAsync(Context.User.Id, user.Id);
            if (res.Successful)
            {
                if (!(bool)res.Data)
                {
                    var res2 = await DatabaseClient.BlockActionAsync(Context.User.Id, user.Id);
                    if (res.Successful)
                    {
                        await $"Blocked {user.FullName()} from performing actions on you".QueueMessage(Discord.Models.MessageType.Success, Context.User, Context.Channel);
                    }
                    else
                    {
                        await res2.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    }
                }
                else
                {
                    var res2 = await DatabaseClient.UnblockActionAsync(Context.User.Id, user.Id);
                    if (res.Successful)
                    {
                        await $"Unblocked {user.FullName()} from performing actions on you".QueueMessage(Discord.Models.MessageType.Success, Context.User, Context.Channel);
                    }
                    else
                    {
                        await res2.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    }
                }
            }
        }

        [Command("set-hexbg"), Summary("Sets your background to a Hex Color"), RequireDatabase]
        public async Task SetHexBG(string Hex = null)
        {
            if (Hex != null)
            {
                if (Context.DBUser.Money > 300)
                {
                    Context.DBUser.Money -= 300;
                    if (int.TryParse(Hex, System.Globalization.NumberStyles.HexNumber, null, out int result))
                    {
                        Context.DBUser.Background = Hex;
                        var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
                        if (res.Successful)
                        {
                            await "Set your Background".QueueMessage(Discord.Models.MessageType.Success, Context.User, Context.Channel);
                        }
                        else
                        {
                            await res.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                        }
                    }
                    else
                    {
                        await $"Malformed Entry".QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                        return;
                    }
                }
                else
                {
                    await $"You need at least {Configuration.Preferences.MoneySymbol}300 to change your background".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
            }
            else
            {
                Context.DBUser.Background = "#3F51B5";
                var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
                if (res.Successful)
                {
                    await $"Reset your background to: {Context.DBUser.Background}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await res.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                }
            }
        }

        [Command("buy-custombg"), Summary("Buy permanent custom backgrounds"), RequireDatabase]
        public async Task BuyCBG()
        {
            if (!Context.DBUser.UnlockedCustBG)
            {
                if (Context.DBUser.Money > 40000)
                {
                    Context.DBUser.Money -= 40000;
                    Context.DBUser.UnlockedCustBG = true;
                    var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
                    if (res.Successful)
                    {
                        await $"You've successfully unlocked custom backgrounds, use: {Context.DBGuild.Prefix ?? Configuration.Discord.Prefix}set-custombg [URL] to set your background".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                    }
                }
                else
                {
                    await $"You need at least {Configuration.Preferences.MoneySymbol}40,000 to unlock custom backgrounds".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
            }
            else
            {
                await $"You already unlocked custom backgrounds, use: {Context.DBGuild.Prefix ?? Configuration.Discord.Prefix}set-custombg [URL] to set your background".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
            }
        }

        [Command("set-custombg"), Summary("Sets your custom background Image"), RequireDatabase]
        public async Task SetCBG(Uri link = null)
        {
            if (link != null)
            {
                if (Context.DBUser.Money >= 900)
                {
                    Context.DBUser.Money -= 900;
                    Context.DBUser.Background = link.OriginalString;
                    var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
                    if (res.Successful)
                    {
                        await "Set your Background".QueueMessage(Discord.Models.MessageType.Success, Context.User, Context.Channel);
                    }
                    else
                    {
                        await res.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                    }
                }
                else
                {
                    await $"You need at least {Configuration.Preferences.MoneySymbol}900 to change your background".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
            }
            else
            {
                Context.DBUser.Background = "#3F51B5";
                var res = await DatabaseClient.UpdateUserAsync(Context.DBUser);
                if (res.Successful)
                {
                    await $"Reset your background to: {Context.DBUser.Background}".QueueMessage(Discord.Models.MessageType.Standard, Context.User, Context.Channel);
                }
                else
                {
                    await res.Error.QueueMessage(Discord.Models.MessageType.Failed, Context.User, Context.Channel);
                }
            }
        }
    }
}