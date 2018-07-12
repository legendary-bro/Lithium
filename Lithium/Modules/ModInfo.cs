﻿namespace Lithium.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    using Lithium.Discord.Context;
    using Lithium.Discord.Preconditions;
    using Lithium.Models;
    using Lithium.Discord.Extensions;

    [CustomPermissions(true)]
    public class ModInfo : Base
    {
        [Command("GetAction")]
        public Task ClearWarnsAsync(int actionID)
        {
            var action = Context.Server.ModerationSetup.ModActions.FirstOrDefault(x => x.ActionId == actionID);
            if (action == null)
            {
                throw new Exception("No action found with that ID");
            }

            return ReplyAsync(new EmbedBuilder { Title = $"Action ${actionID}", Fields = new List<EmbedFieldBuilder> { action.GetLongField(Context.Guild) } });
        }

        [Command("Bans")]
        public Task ViewBansAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.ban, user?.Id);
        }

        [Command("Bans")]
        public Task ViewBansAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.ban, userId);
        }
                
        [Command("LongBans")]
        public Task ViewLongBansAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.ban, user?.Id, true);
        }

        [Command("LongBans")]
        public Task ViewLongBansAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.ban, userId, true);
        }

        [Command("Kicks")]
        public Task ViewKicksAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user?.Id);
        }

        [Command("Kicks")]
        public Task ViewKicksAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId);
        }
        
        [Command("LongKicks")]
        public Task ViewLongKicksAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, user?.Id, true);
        }

        [Command("LongKicks")]
        public Task ViewLongKicksAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.Kick, userId, true);
        }

        [Command("Warns")]
        public Task ViewWarnsAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.warn, user?.Id);
        }

        [Command("Warns")]
        public Task ViewWarnsAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.warn, userId);
        }
                
        [Command("LongWarns")]
        public Task ViewLongWarnsAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.warn, user?.Id, true);
        }

        [Command("LongWarns")]
        public Task ViewLongWarnsAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.warn, userId, true);
        }

        [Command("Mutes")]
        public Task ViewMutesAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.mute, user?.Id);
        }

        [Command("Mutes")]
        public Task ViewMutesAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.mute, userId);
        }
        
        [Command("LongMutes")]
        public Task ViewLongMutesAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.mute, user?.Id, true);
        }

        [Command("LongMutes")]
        public Task ViewLongMutesAsync(ulong userId)
        {
            return GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType.mute, userId, true);
        }

        [Command("ModLog")]
        public Task ViewModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user?.Id);
        }

        [Command("ModLog")]
        public Task ViewModLogAsync(ulong userId)
        {
            return GetModActionsAsync(null, userId);
        }

        [Command("LongModLog")]
        public Task ViewLongModLogAsync(SocketGuildUser user = null)
        {
            return GetModActionsAsync(null, user?.Id, true);
        }

        [Command("LongModLog")]
        public Task ViewLongModLogAsync(ulong userId)
        {
            return GetModActionsAsync(null, userId, true);
        }

        public Task GetModActionsAsync(GuildModel.Moderation.ModEvent.EventType? type, ulong? userID = null, bool showExpired = false)
        {
            List<GuildModel.Moderation.ModEvent> modEvents;
            if (showExpired)
            {
                if (type != null && userID == null)
                {
                    // Show the given mod action for ALL users
                    modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.Action == type).OrderByDescending(x => x.TimeStamp).ToList();
                }
                else if (type == null && userID == null)
                {
                    // Show ALL Mod actions for ALL users
                    modEvents = Context.Server.ModerationSetup.ModActions.OrderByDescending(x => x.TimeStamp).ToList();
                }
                else
                {
                    if (type == null)
                    {
                        modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == userID).OrderByDescending(x => x.TimeStamp).ToList();
                    }
                    else
                    {
                        modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == userID && x.Action == type).OrderByDescending(x => x.TimeStamp).ToList();
                    }
                }
            }
            else
            {               
                if (type != null && userID == null)
                {
                    // Show the given mod action for ALL users
                    modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.Action == type && !x.ExpiredOrRemoved).OrderByDescending(x => x.TimeStamp).ToList();
                }
                else if (type == null && userID == null)
                {
                    // Show ALL Mod actions for ALL users
                    modEvents = Context.Server.ModerationSetup.ModActions.Where(x => !x.ExpiredOrRemoved).OrderByDescending(x => x.TimeStamp).ToList();
                }
                else
                {
                    if (type == null)
                    {
                        modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == userID && !x.ExpiredOrRemoved).OrderByDescending(x => x.TimeStamp).ToList();
                    }
                    else
                    {
                        modEvents = Context.Server.ModerationSetup.ModActions.Where(x => x.UserId == userID && x.Action == type && !x.ExpiredOrRemoved).OrderByDescending(x => x.TimeStamp).ToList();
                    }
                }
            }

            var pages = new List<PaginatedMessage.Page>();
            foreach (var modGroup in modEvents.SplitList(5))
            {
                pages.Add(new PaginatedMessage.Page
                              {
                                  Fields = modGroup.Select(m => m.GetLongField(Context.Guild)).ToList()
                              });
            }

            return PagedReplyAsync(new PaginatedMessage
                                       {
                                           Pages = pages, 
                                           Title = userID.HasValue ? $"{Context.Guild.GetUser(userID.Value)}{(type == null ? null : $" {type}")} {modEvents.Count} Actions" : null
                                       }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }
    }
}
