﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Lithium.Discord.Contexts;
using Lithium.Discord.Preconditions;
using Lithium.Models;

namespace Lithium.Modules.ServerOwner
{
    [RequireRole.RequireAdmin]
    public class AntiSpam : Base
    {
        [Command("NoInvite")]
        [Summary("NoInvite")]
        [Remarks("Disable the posting of discord invite links in the server")]
        public async Task NoInvite()
        {
            Context.Server.Antispam.Advertising.Invite = !Context.Server.Antispam.Advertising.Invite;
            Context.Server.Save();
            await ReplyAsync($"NoInvite: {Context.Server.Antispam.Advertising.Invite}");
        }
        [Command("NoMentionAll")]
        [Summary("NoMentionAll")]
        [Remarks("Disable the use of @everyone and @here for users")]
        public async Task NoMentionAll()
        {
            Context.Server.Antispam.Mention.MentionAll = !Context.Server.Antispam.Mention.MentionAll;
            Context.Server.Save();
            await ReplyAsync($"NoMentionAll: {Context.Server.Antispam.Mention.MentionAll}");
        }
        [Command("NoMentionMessage")]
        [Summary("NoMentionMessage")]
        [Remarks("Set the No Mention Message response")]
        public async Task NoMentionAll([Remainder]string message = null)
        {
            Context.Server.Antispam.Mention.MentionAllMessage = message;
            Context.Server.Save();
            await ReplyAsync($"No Mention Message: {message ?? "N/A"}");
        }
        [Command("NoMassMention")]
        [Summary("NoMassMention")]
        [Remarks("Toggle auto-deletion of messages with 5+ role or user mentions")]
        public async Task NoMassMention()
        {
            Context.Server.Antispam.Mention.RemoveMassMention = !Context.Server.Antispam.Mention.RemoveMassMention;
            Context.Server.Save();
            await ReplyAsync($"NoMassMention: {Context.Server.Antispam.Mention.RemoveMassMention}");
        }
        [Command("NoIPs")]
        [Summary("NoIps")]
        [Remarks("Toggle auto-deletion of messages containing valid IP addresses")]
        public async Task NoIPs()
        {
            Context.Server.Antispam.Privacy.RemoveIPs = !Context.Server.Antispam.Privacy.RemoveIPs;
            Context.Server.Save();
            await ReplyAsync($"No IP Addresses: {Context.Server.Antispam.Privacy.RemoveIPs}");
        }
        [Command("NoToxicity")]
        [Summary("NoToxicity <threshhold>")]
        [Remarks("Toggle auto-deletion of messages that are too toxic")]
        public async Task NoToxicity(int threshhold = 999)
        {
            if (threshhold == 999 || threshhold < 50 || threshhold > 99)
            {
                await ReplyAsync("Pick a threshhold between 50 and 99");
                return;
            }
            Context.Server.Antispam.Toxicity.ToxicityThreshHold = threshhold;
            Context.Server.Antispam.Toxicity.UsePerspective = !Context.Server.Antispam.Toxicity.UsePerspective;
            Context.Server.Save();
            await ReplyAsync($"Remove Toxic Messages: {Context.Server.Antispam.Toxicity.UsePerspective}\n" +
                             $"Threshhold: {threshhold}");
        }


        [Command("ignore")]
        [Summary("ignore <selection> <@role>")]
        [Remarks("choose a role to ignore when using antispam commands")]
        public async Task IgnoreRole(string selection, IRole role = null)
        {
            if (role == null)
            {
                await IgnoreRole();
                return;
            }

            var intselections = selection.Split(',');
            var ignore = Context.Server.Antispam.IgnoreRoles.FirstOrDefault(x => x.RoleID == role.Id);
            var addrole = false;
            if (ignore == null)
            {
                ignore = new GuildModel.Guild.antispams.IgnoreRole
                {
                    RoleID = role.Id
                };
                addrole = true;
            }


            if (int.TryParse(intselections[0], out var zerocheck))
            {
                if (zerocheck == 0)
                {
                    Context.Server.Antispam.IgnoreRoles.Remove(ignore);
                    await ReplyAsync("Success, Role has been removed form the ignore list");
                }
                else
                {
                    foreach (var s in intselections)
                        if (int.TryParse(s, out var sint))
                        {
                            if (sint < 1 || sint > 6)
                            {
                                await ReplyAsync($"Invalid Input {s}\n" +
                                                 $"only 1-6 are accepted.");
                                return;
                            }

                            switch (sint)
                            {
                                case 1:
                                    ignore.AntiSpam = true;
                                    break;
                                case 2:
                                    ignore.Blacklist = true;
                                    break;
                                case 3:
                                    ignore.Mention = true;
                                    break;
                                case 4:
                                    ignore.Advertising = true;
                                    break;
                                case 5:
                                    ignore.Privacy = true;
                                    break;
                                case 6:
                                    ignore.Toxicity = true;
                                    break;
                            }
                        }
                        else
                        {
                            await ReplyAsync($"Invalid Input {s}");
                            return;
                        }

                    var embed = new EmbedBuilder
                    {
                        Description = $"{role.Mention}\n" +
                                      "__Ignore Antispam Detections__\n" +
                                      $"Bypass Antispam: {ignore.AntiSpam}\n" +
                                      $"Bypass Blacklist: {ignore.Blacklist}\n" +
                                      $"Bypass Mention Everyone and 5+ Role Mentions: {ignore.Mention}\n" +
                                      $"Bypass Invite Link Removal: {ignore.Advertising}\n" +
                                      $"Bypass IP Removal: {ignore.Privacy}\n" +
                                      $"Bypass Toxicity Check: {ignore.Toxicity}"
                    };
                    await ReplyAsync("", false, embed.Build());
                }

                if (addrole) Context.Server.Antispam.IgnoreRoles.Add(ignore);
                Context.Server.Save();
            }
            else
            {
                await ReplyAsync("Input Error!");
            }
        }

        [Command("ignore")]
        [Summary("ignore")]
        [Remarks("ignore role setup information")]
        public async Task IgnoreRole()
        {
            await ReplyAsync("", false, new EmbedBuilder
            {
                Description =
                    $"You can select roles to ignore from all spam type checks in this module using the ignore command.\n" +
                    $"__Key__\n" +
                    $"`1` - Antispam\n" +
                    $"`2` - Blacklist\n" +
                    $"`3` - Mention\n" +
                    $"`4` - Invite\n" +
                    $"`5` - IP Addresses\n" +
                    $"`6` - Toxicity\n\n" +
                    $"__usage__\n" +
                    $"`{Config.Load().DefaultPrefix} 1 @role` - this allows the role to spam without being limited/removed\n" +
                    $"You can use commas to use multiple settings on the same role.\n" +
                    $"`{Config.Load().DefaultPrefix} 1,2,3 @role` - this allows the role to spam, use blacklisted words and bypass mention filtering without being removed\n" +
                    $"`{Config.Load().DefaultPrefix} 0 @role` - resets the ignore config and will add all limits back to the role"
            }.Build());
        }

        [Command("WarnSpammers")]
        [Summary("WarnSpammers <type>")]
        [Remarks("Toggle Auto-Warning of people detected by any of the antispam methods")]
        public async Task WarnSpammers([Remainder]string selection)
        {
            var intselections = selection.Split(',');

            if (int.TryParse(intselections[0], out var zerocheck))
            {
                if (zerocheck == 0)
                {
                    Context.Server.Antispam.Antispam.WarnOnDetection = false;
                    Context.Server.Antispam.Blacklist.WarnOnDetection = false;
                    Context.Server.Antispam.Advertising.WarnOnDetection = false;
                    Context.Server.Antispam.Mention.WarnOnDetection = false;
                    Context.Server.Antispam.Toxicity.WarnOnDetection = false;
                    Context.Server.Antispam.Privacy.WarnOnDetection = false;
                    await ReplyAsync("Success, All have been reset.");
                }
                else
                {
                    foreach (var s in intselections)
                    {
                        if (int.TryParse(s, out var sint))
                        {
                            if (sint < 1 || sint > 6)
                            {
                                await ReplyAsync($"Invalid Input {s}\n" +
                                                 $"only 1-6 are accepted.");
                                return;
                            }

                            switch (sint)
                            {
                                case 1:
                                    Context.Server.Antispam.Antispam.WarnOnDetection = true;
                                    break;
                                case 2:
                                    Context.Server.Antispam.Blacklist.WarnOnDetection = true;
                                    break;
                                case 3:
                                    Context.Server.Antispam.Mention.WarnOnDetection = true;
                                    break;
                                case 4:
                                    Context.Server.Antispam.Advertising.WarnOnDetection = true;
                                    break;
                                case 5:
                                    Context.Server.Antispam.Privacy.WarnOnDetection = true;
                                    break;
                                case 6:
                                    Context.Server.Antispam.Toxicity.WarnOnDetection = true;
                                    break;
                            }
                        }
                        else
                        {
                            await ReplyAsync($"Invalid Input {s}");
                            return;
                        }
                    }
                }

                var embed = new EmbedBuilder
                {
                    Description = $"__AutoMod Antispam Detections__\n" +
                                    $"Warn on Antispam: {Context.Server.Antispam.Antispam.WarnOnDetection}\n" +
                                    $"Warn on Blacklist: {Context.Server.Antispam.Blacklist.WarnOnDetection}\n" +
                                    $"Warn on Mention Everyone and 5+ Role Mentions: {Context.Server.Antispam.Mention.WarnOnDetection}\n" +
                                    $"Warn on Invite Link Removal: {Context.Server.Antispam.Advertising.WarnOnDetection}\n" +
                                    $"Warn on IP Removal: {Context.Server.Antispam.Privacy.WarnOnDetection}\n" +
                                    $"Warn on Toxicity Check: {Context.Server.Antispam.Toxicity.WarnOnDetection}"
                };
                await ReplyAsync("", false, embed.Build());
                Context.Server.Save();
            }
            else
            {
                await ReplyAsync("Input Error!");
            }
        }


        [Command("WarnSpammers")]
        [Summary("WarnSpammers")]
        [Remarks("Warn Spammers Setup Info")]
        public async Task WarnSpammers()
        {
            await ReplyAsync("", false, new EmbedBuilder
            {
                Description =
                    $"You can select roles to warn from all spam type checks in this module using the WarnSpammers command.\n" +
                    $"__Key__\n" +
                    $"`1` - Antispam\n" +
                    $"`2` - Blacklist\n" +
                    $"`3` - Mention\n" +
                    $"`4` - Invite\n" +
                    $"`5` - IP Addresses\n" +
                    $"`6` - Toxicity\n\n" +
                    $"__usage__\n" +
                    $"`{Config.Load().Prefix} 1 @role` - this allows the role to spam without being limited/removed\n" +
                    $"You can use commas to use multiple settings on the same role\n." +
                    $"`{Config.Load().Prefix} 1,2,3 @role` - this allows the role to spam, use blacklisted words and bypass mention filtering without being removed\n" +
                    $"`{Config.Load().Prefix} 0 @role` - resets the ignore config and will add all limits back to the role"
            }.Build());
        }
    }
}
