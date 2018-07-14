﻿namespace Lithium.Discord.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using global::Discord;

    using Lithium.Discord.Context;
    using Lithium.Discord.Extensions;
    using Lithium.Handlers;
    using Lithium.Models;

    public class AutoModerator
    {
        public Dictionary<ulong, List<NoSpam>> NoSpamList { get; set; } = new Dictionary<ulong, List<NoSpam>>();


        public AutoModerator(Perspective.Api _toxicityAPI)
        {
            ToxicityAPI = _toxicityAPI;
        }

        private Perspective.Api ToxicityAPI { get; }

        internal async Task<bool> AntiInviteAsync(Context context)
        {
            if (Regex.Match(context.Message.Content, @"discord(?:\.gg|\.me|app\.com\/invite)\/([\w\-]+)").Success)
            {
                await context.Message?.DeleteAsync();
                var emb = new EmbedBuilder();
                emb.Description = context.Server.AntiSpam.Advertising.Response != null
                                      ? context.Server.AntiSpam.Advertising.Response.DoReplacements(context)
                                      : $"{context.User} - This server does not allow you to send invite links in chat";

                // Description = context.Server.AntiSpam.Advertising.NoInviteMessage ?? $"{context.User?.Mention} - no sending invite links... the admins might get angry"
                await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                if (context.Server.AntiSpam.Advertising.WarnOnDetection)
                {
                    await context.Server.ModActionAsync(
                        context.User.CastToSocketGuildUser(),
                        context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                        context.Channel.CastToSocketTextChannel(),
                        null,
                        GuildModel.Moderation.ModEvent.AutoReason.discordInvites,
                        GuildModel.Moderation.ModEvent.EventType.Warn,
                        new GuildModel.Moderation.ModEvent.Trigger
                            {
                                Message = context.Message.Content,
                                ChannelId = context.Channel.Id
                            },
                        null,
                        GuildModel.AdditionalDetails.Default());
                }

                return true;
            }

            return false;
        }

        internal async Task<bool> AntiIpAsync(Context context)
        {
            if (Regex.IsMatch(context.Message.Content, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
            {
                await context.Message?.DeleteAsync();
                var emb = new EmbedBuilder { Title = $"{context.User} - This server does not allow you to post IP addresses" };
                await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                if (context.Server.AntiSpam.Privacy.WarnOnDetection)
                {
                    await context.Server.ModActionAsync(
                        context.User.CastToSocketGuildUser(),
                        context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                        context.Channel.CastToSocketTextChannel(),
                        null,
                        GuildModel.Moderation.ModEvent.AutoReason.ipAddresses,
                        GuildModel.Moderation.ModEvent.EventType.Warn,
                        new GuildModel.Moderation.ModEvent.Trigger
                            {
                                Message = context.Message.Content,
                                ChannelId = context.Channel.Id
                            },
                        null,
                        GuildModel.AdditionalDetails.Default());
                }

                return true;
            }

            return false;
        }

        internal async Task<bool> AntiMentionAsync(Context context)
        {
            if (context.Server.AntiSpam.Mention.RemoveMassMention)
            {
                if (context.Message.MentionedRoles.Count + context.Message.MentionedUsers.Count >= context.Server.AntiSpam.Mention.MassMentionLimit)
                {
                    await context.Message?.DeleteAsync();
                    var emb = new EmbedBuilder { Description = $"{context.User} - This server does not allow you to mention 5+ roles or uses at once" };
                    await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                    if (context.Server.AntiSpam.Mention.WarnOnDetection)
                    {
                        await context.Server.ModActionAsync(
                            context.User.CastToSocketGuildUser(),
                            context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                            context.Channel.CastToSocketTextChannel(),
                            null,
                            GuildModel.Moderation.ModEvent.AutoReason.massMention,
                            GuildModel.Moderation.ModEvent.EventType.Warn,
                            new GuildModel.Moderation.ModEvent.Trigger
                                {
                                    Message = context.Message.Content,
                                    ChannelId = context.Channel.Id
                                },
                            null,
                        GuildModel.AdditionalDetails.Default());
                    }

                    return true;
                }
            }

            if (context.Server.AntiSpam.Mention.MentionAll)
            {
                if (context.Message.Content.Contains("@everyone") || context.Message.Content.Contains("@here"))
                {
                    await (context.Message?.DeleteAsync()).ConfigureAwait(false);
                    var emb = new EmbedBuilder();
                    if (context.Server.AntiSpam.Mention.MentionAllResponse != null)
                    {
                        emb.Description = context.Server.AntiSpam.Mention.MentionAllResponse.DoReplacements(context);
                    }
                    else
                    {
                        emb.Title = $"{context.User} - This server has disabled the ability for you to mention @everyone and @here";
                    }

                    await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                    if (context.Server.AntiSpam.Mention.WarnOnDetection)
                    {
                        await context.Server.ModActionAsync(
                            context.User.CastToSocketGuildUser(),
                            context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                            context.Channel.CastToSocketTextChannel(),
                            null,
                            GuildModel.Moderation.ModEvent.AutoReason.mentionAll,
                            GuildModel.Moderation.ModEvent.EventType.Warn,
                            new GuildModel.Moderation.ModEvent.Trigger
                                {
                                    Message = context.Message.Content,
                                    ChannelId = context.Channel.Id
                                },
                            null,
                        GuildModel.AdditionalDetails.Default());
                    }

                    return true;
                }
            }

            return false;
        }

        internal async Task<bool> AntiSpamAsync(Context context)
        {
            var detected = false;
            NoSpamList.TryGetValue(context.Guild.Id, out var SpamGuild); // .FirstOrDefault(x => x.GuildID == ((SocketGuildUser)context.User).context.Server.Id);
            if (SpamGuild == null)
            {
                NoSpamList.Add(context.Guild.Id, new List<NoSpam> { new NoSpam { UserID = context.User.Id, Messages = new List<NoSpam.Msg> { new NoSpam.Msg { LastMessage = context.Message.Content, LastMessageDate = DateTime.UtcNow } } } });
            }
            else
            {
                var user = SpamGuild.FirstOrDefault(x => x.UserID == context.User.Id);
                if (user == null)
                {
                    SpamGuild.Add(new NoSpam { UserID = context.User.Id, Messages = new List<NoSpam.Msg> { new NoSpam.Msg { LastMessage = context.Message.Content, LastMessageDate = DateTime.UtcNow } } });
                }
                else
                {
                    user.Messages.Add(new NoSpam.Msg { LastMessage = context.Message.Content, LastMessageDate = DateTime.UtcNow });
                    if (user.Messages.Count >= 2)
                    {
                        var messages = user.Messages.Where(x => x.LastMessageDate > DateTime.UtcNow - context.Server.AntiSpam.AntiSpam.RateLimiting.BlockPeriod).ToList();

                        // Here we detect spam based on whether or not a user is sending the same message repeatedly
                        if (messages.GroupBy(n => n.LastMessage.ToLower()).Any(c => c.Count() > 2)

                            // Count the amount of messages in the no-spam period and compare to the anti spam message limit
                            || messages.Count(x => x.LastMessageDate > DateTime.UtcNow - context.Server.AntiSpam.AntiSpam.RateLimiting.TimePeriod) > context.Server.AntiSpam.AntiSpam.RateLimiting.MessageCount)
                        {
                            detected = true;
                        }
                    }

                    if (user.Messages.Count > context.Server.AntiSpam.AntiSpam.RateLimiting.MessageCount + 5)
                    {
                        // Filter out messages so that we only keep a log of the most recent messages
                        user.Messages = user.Messages.Where(x => x.LastMessageDate <= DateTime.UtcNow - TimeSpan.FromMinutes(2)).ToList();
                    }

                    if (detected)
                    {
                        if (DateTime.UtcNow < user.RateLimitUntil)
                        {
                            LogHandler.LogMessage("Deleted Message (BlockPeriod)", LogSeverity.Debug);
                            await context.Message?.DeleteAsync();
                            return true;
                        }

                        if (!context.Server.AntiSpam.AntiSpam.WhiteList.Any(x => context.Message.Content.ToLower().Contains(x.ToLower())))
                        {
                            await context.Message?.DeleteAsync();

                            LogHandler.LogMessage("Deleted Message and Updated Block period", LogSeverity.Debug);
                            user.RateLimitUntil = DateTime.UtcNow + context.Server.AntiSpam.AntiSpam.RateLimiting.BlockPeriod;

                            var emb = new EmbedBuilder { Description = $"{context.User} - No Spamming!!" };
                            await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                            if (context.Server.AntiSpam.AntiSpam.WarnOnDetection)
                            {
                                await context.Server.ModActionAsync(
                                    context.User.CastToSocketGuildUser(),
                                    context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                                    context.Channel.CastToSocketTextChannel(),
                                    null,
                                    GuildModel.Moderation.ModEvent.AutoReason.messageSpam,
                                    GuildModel.Moderation.ModEvent.EventType.Warn,
                                    new GuildModel.Moderation.ModEvent.Trigger
                                        {
                                            Message = context.Message.Content,
                                            ChannelId = context.Channel.Id
                                        },
                                    null,
                                    GuildModel.AdditionalDetails.Default());
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal async Task<bool> CheckBlacklistAsync(Context context)
        {
            if (context.Server.AntiSpam.Blacklist.BlacklistWordSet.Any())
            {
                var detected = false;
                var blacklistMessage = context.Server.AntiSpam.Blacklist.DefaultBlacklistMessage;
                var blacklistWords = context.Server.AntiSpam.Blacklist.BlacklistWordSet.FirstOrDefault(words => words.WordList.Any(x => context.Message.Content.ToLower().Contains(x.ToLower())));
                string blacklistMatch = null;
                if (blacklistWords != null)
                {
                    detected = true;
                    blacklistMessage = blacklistWords.BlacklistResponse ?? context.Server.AntiSpam.Blacklist.DefaultBlacklistMessage;
                    blacklistMatch = blacklistWords.WordList.FirstOrDefault(w => context.Message.Content.Contains(w, StringComparison.OrdinalIgnoreCase));
                }

                if (detected)
                {
                    await context.Message?.DeleteAsync();

                    if (!string.IsNullOrEmpty(blacklistMessage))
                    {
                        var result = blacklistMessage.DoReplacements(context);
                        await context.Channel.SendMessageAsync(result);
                    }

                    if (context.Server.AntiSpam.Blacklist.WarnOnDetection)
                    {
                        await context.Server.ModActionAsync(
                            context.User.CastToSocketGuildUser(),
                            context.Guild.GetUser(context.Client.CurrentUser.Id) ?? throw new NullReferenceException(),
                            context.Channel.CastToSocketTextChannel(),
                            null,
                            GuildModel.Moderation.ModEvent.AutoReason.blacklist,
                            GuildModel.Moderation.ModEvent.EventType.Warn,
                            new GuildModel.Moderation.ModEvent.Trigger
                                {
                                    Message = context.Message.Content,
                                    ChannelId = context.Channel.Id
                                },
                            null,
                            GuildModel.AdditionalDetails.QuickModOnlyDetails("Blacklist Match", $"{blacklistMatch ?? "N/A"}"));
                    }

                    return true;
                }
            }

            return false;
        }

        internal async Task<bool> CheckToxicityAsync(Context context)
        {
            if (context.Server.AntiSpam.Toxicity.UsePerspective)
            {
                if (!string.IsNullOrWhiteSpace(context.Message.Content))
                {
                    try
                    {
                        var res = ToxicityAPI.QueryToxicity(context.Message.Content);
                        if (res.attributeScores.TOXICITY.summaryScore.value * 100 > context.Server.AntiSpam.Toxicity.ToxicityThreshold)
                        {
                            await context.Message?.DeleteAsync();
                            var emb = new EmbedBuilder { Title = $"Toxicity Threshold Breached ({res.attributeScores.TOXICITY.summaryScore.value * 100})", Description = $"{context.User.Mention}" };
                            await context.Channel.SendMessageAsync(string.Empty, false, emb.Build());
                            if (context.Server.AntiSpam.Toxicity.WarnOnDetection)
                            {
                                await context.Server.ModActionAsync(
                                    context.User.CastToSocketGuildUser(),
                                    context.Guild.GetUser(context.Client.CurrentUser.Id),
                                    context.Channel.CastToSocketTextChannel(),
                                    null,
                                    GuildModel.Moderation.ModEvent.AutoReason.toxicity,
                                    GuildModel.Moderation.ModEvent.EventType.Warn,
                                    new GuildModel.Moderation.ModEvent.Trigger
                                        {
                                            Message = context.Message.Content,
                                            ChannelId = context.Channel.Id
                                        },
                                    null,
                                    GuildModel.AdditionalDetails.QuickModOnlyDetails("Toxicity Reading", $"{res.attributeScores.TOXICITY.summaryScore.value * 100}"));
                            }

                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }
            }

            return false;
        }

        internal Task RunChecksAsync(Context context)
        {
            if (context.Guild == null || context.IsPrivate || context.Server == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                var exemptCheck = context.Server.AntiSpam.IgnoreList.Where(i => context.User.CastToSocketGuildUser().Roles.Any(r => r.Id == i.Key)).ToList();

                var _ = Task.Run(
                    async () =>
                        {
                            if (context.Server.AntiSpam.AntiSpam.NoSpam && !exemptCheck.Any(e => e.Value.AntiSpam))
                            {
                                if (await AntiSpamAsync(context))
                                {
                                    return;
                                }
                            }

                            if (context.Server.AntiSpam.Advertising.Invite && !exemptCheck.Any(e => e.Value.Advertising))
                            {
                                if (await AntiInviteAsync(context))
                                {
                                    return;
                                }
                            }

                            if ((context.Server.AntiSpam.Mention.RemoveMassMention || context.Server.AntiSpam.Mention.MentionAll) && !exemptCheck.Any(e => e.Value.Mention))
                            {
                                if (await AntiMentionAsync(context))
                                {
                                    return;
                                }
                            }

                            if (context.Server.AntiSpam.Privacy.RemoveIPs && !exemptCheck.Any(e => e.Value.Privacy))
                            {
                                if (await AntiIpAsync(context))
                                {
                                    return;
                                }
                            }

                            if (context.Server.AntiSpam.Blacklist.BlacklistWordSet.Any() && !exemptCheck.Any(e => e.Value.Blacklist))
                            {
                                if (await CheckBlacklistAsync(context))
                                {
                                    return;
                                }
                            }

                            if (context.Server.AntiSpam.Toxicity.UsePerspective && !exemptCheck.Any(e => e.Value.Toxicity))
                            {
                                if (await CheckToxicityAsync(context))
                                {
                                    return;
                                }
                            }
                        });
            }
            catch (Exception e)
            {
                LogHandler.LogMessage($"AntiSpam Error G:[{context.Guild.Id}] GN:{context.Guild.Name} C:{context.Channel.Name} U:{context.User.Username}\n" + $"{e}", LogSeverity.Error);
            }

            return Task.CompletedTask;
        }

        public class Delays
        {
            public DateTime _delay { get; set; } = DateTime.UtcNow;

            public ulong GuildID { get; set; }
        }

        public class NoSpam
        {
            public List<Msg> Messages { get; set; } = new List<Msg>();

            public ulong UserID { get; set; }

            public DateTime RateLimitUntil { get; set; } = DateTime.MinValue;

            public class Msg
            {
                public string LastMessage { get; set; }

                public DateTime LastMessageDate { get; set; }
            }
        }
    }
}