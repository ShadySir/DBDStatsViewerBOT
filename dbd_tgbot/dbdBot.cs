using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using dbd_tgbot.Client;
using dbd_tgbot.Constant;
using dbd_tgbot.Model;
using Newtonsoft.Json;

namespace dbdBot
{
    public class DbdBot
    {
        TelegramBotClient botClient = new TelegramBotClient("");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };
        Dictionary<long, bool> DictAskingID = new(), DictAskingCharacterName = new(), DictAskingPerk = new(), DictAskingBuildName = new();
        Dictionary<long, UserStats?> DictUserStats = new();
        Dictionary<long, PerkBuild?> DictTempPerkBuild = new();
        Dictionary<long, PerkBuilds?> DictTempPerkBuilds = new();
        Dictionary<long, string> DictTempRole = new(), DictAskingPageFor = new();
        ShadyClient shadyClient = new();
        List<Perk>? PerkList;
        List<Perk>? SPerkList = new();
        List<Perk>? KPerkList = new();
        List<Killer>? KillerList;
        List<Survivor>? SurvivorList;
        public async Task Start()
        {
            PerkList = shadyClient.GetPerksAsync().Result;
            if (PerkList != null)
            for (int i = 0; i < PerkList.Count; i++)
            {
                if (PerkList[i].role == "Survivor")
                {
                    SPerkList.Add(PerkList[i]);
                }
                if (PerkList[i].role == "Killer")
                {
                    KPerkList.Add(PerkList[i]);
                }
            }
            KillerList = shadyClient.GetKillersAsync().Result;
            SurvivorList = shadyClient.GetSurvivorsAsync().Result;
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var mybot = await botClient.GetMeAsync();
            Console.WriteLine($"bot {mybot.Username} is working");
            Console.ReadKey();
        }
        private Task HandlerError(ITelegramBotClient botclient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram bot API error:\n {apiRequestException.ErrorCode}\n{apiRequestException.Message}", _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandlerCallbackAsync(botClient, update.CallbackQuery);
            }
        }
        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (DictTempPerkBuilds.ContainsKey(message.Chat.Id) && DictTempPerkBuilds[message.Chat.Id] == null)
            {
                DictTempPerkBuilds[message.Chat.Id] = new();
            }
            if (message == null || message.Text == null) return;
            if (message.Text == "/cancel")
            {
                DictTempRole.Remove(message.Chat.Id);
                DictTempPerkBuild.Remove(message.Chat.Id);
                DictAskingID.Remove(message.Chat.Id);
                DictAskingCharacterName.Remove(message.Chat.Id);
                DictAskingPerk.Remove(message.Chat.Id);
                DictAskingPageFor.Remove(message.Chat.Id);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Cancelled");
                return;
            }

            Int64 ID = -1;
            int page = 0;

            if (DictAskingBuildName.ContainsKey(message.Chat.Id) && DictAskingBuildName[message.Chat.Id])
            {
                DictAskingBuildName.Remove(message.Chat.Id);
                DictTempPerkBuild[message.Chat.Id].BuildName = message.Text;

                await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter a character\'s full name");

                DictAskingCharacterName[message.Chat.Id] = true;
                return;
            }
            if (DictAskingID.ContainsKey(message.Chat.Id) && DictAskingID[message.Chat.Id])
            {
                try
                {
                    ID = Int64.Parse(message.Text);
                }
                catch (Exception){
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid ID, it can only contain numbers");
                }

                if (ID != -1)
                {
                    if (shadyClient.GetUserStatsForGameAsync(message.Text).Result == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Couldn\'t find stats, possible reasons:\n-SteamID non existent\n-Dead by Daylight is not purchased\n-Profile or progression is set private");
                        DictAskingID.Remove(message.Chat.Id);
                        return;
                    }
                    DictUserStats[message.Chat.Id] = shadyClient.GetUserStatsForGameAsync(message.Text).Result;
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                                            {
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("General survivor stats", "survivor_stats"),
                                                                    InlineKeyboardButton.WithCallbackData("Killer interactions", "killer_inter"),
                                                                },
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("Healing", "heal"),
                                                                    InlineKeyboardButton.WithCallbackData("Saves", "save"),
                                                                },
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("Escapes", "escape"),
                                                                    InlineKeyboardButton.WithCallbackData("Special gens repaired", "special_gens"),
                                                                },
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("General killer stats", "killer_stats"),
                                                                    InlineKeyboardButton.WithCallbackData("Hooked", "hook"),
                                                                },
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("Killer powers", "power"),
                                                                    InlineKeyboardButton.WithCallbackData("Survivors downed", "down"),
                                                                },
                                                                new []
                                                                {
                                                                    InlineKeyboardButton.WithCallbackData("Survivors downed using killer power", "power_down"),
                                                                }
                                                            }
                    );

                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Showing stats for {ID}\n" +
                        $"\nTotal bloodpoints earned: {((int)DictUserStats[message.Chat.Id].playerstats.stats[FindIndexByStatName("DBD_BloodwebPoints", message.Chat.Id)].value).ToString("#,##0")}" +
                        $"\nCurrent grade as Survivor: {GetRankFromSkulls((int)DictUserStats[message.Chat.Id].playerstats.stats[FindIndexByStatName("DBD_CamperSkulls", message.Chat.Id)].value)}" +
                        $"\nCurrent grade as Killer: {GetRankFromSkulls((int)DictUserStats[message.Chat.Id].playerstats.stats[FindIndexByStatName("DBD_KillerSkulls", message.Chat.Id)].value)}", replyMarkup: keyboard, parseMode: ParseMode.Markdown);
                    try
                    {
                        DictUserStats[message.Chat.Id].playerstats.stats[0].value = 0;
                    }
                    catch (Exception){}
                }
                DictAskingID[message.Chat.Id] = false;
                return;
            }
            if (DictAskingCharacterName.ContainsKey(message.Chat.Id) && DictAskingCharacterName[message.Chat.Id])
            {
                List<string> names = new();
                if (SurvivorList != null)
                {
                    for (int i = 0; i < SurvivorList.Count; i++)
                    {
                        names.Add(SurvivorList[i].name);
                    }
                    if (names.Contains(message.Text))
                    {
                        DictTempRole.Add(message.Chat.Id, "Survivor");
                        DictTempPerkBuild[message.Chat.Id].CharacterName = message.Text;
                        DictAskingCharacterName.Remove(message.Chat.Id);
                        DictAskingPerk[message.Chat.Id] = true;
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter up to 4 perk names to add them to this build." +
                            "\nThe operation will finish after you type /finish" +
                            "\nYou can also remove one of the perks you added by typing /remove_perk {name of the perk you want to remove}");
                        return;
                    }
                }
                if (DictAskingCharacterName.ContainsKey(message.Chat.Id) && DictAskingCharacterName[message.Chat.Id] && KillerList != null)
                {
                    names.Clear();
                    for (int i = 0; i < KillerList.Count; i++)
                    {
                        names.Add(KillerList[i].name);
                    }
                    if (names.Contains(message.Text))
                    {
                        DictTempRole.Add(message.Chat.Id, "Killer");
                        DictTempPerkBuild[message.Chat.Id].CharacterName = message.Text;
                        DictAskingCharacterName.Remove(message.Chat.Id);
                        DictAskingPerk[message.Chat.Id] = true;
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Enter up to 4 perk names to add them to this build." +
                            "\nThe operation will finish after you type /finish" +
                            "\nYou can also remove one of the perks you added by typing /remove_perk {name of the perk you want to remove}");
                        return;
                    }
                }
                if (!DictTempRole.ContainsKey(message.Chat.Id))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Try again or type /cancel to abort the operation");
                }
                return;
            }
            if (DictAskingPerk.ContainsKey(message.Chat.Id) && DictAskingPerk[message.Chat.Id])
            {
                if (message.Text == "/finish")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuild[message.Chat.Id].ToString());
                    DictAskingPerk.Remove(message.Chat.Id);
                    if (DictTempPerkBuilds[message.Chat.Id] == null) {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "s");
                        return;
                    }
                    if (DictTempPerkBuild[message.Chat.Id] == null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "no s");
                        return;
                    }
                    DictTempPerkBuilds[message.Chat.Id].perkBuilds.Add(DictTempPerkBuild[message.Chat.Id]);
                    string jason = JsonConvert.SerializeObject(DictTempPerkBuilds[message.Chat.Id]);
                    System.IO.File.WriteAllText($@"Data\Builds_{message.Chat.Id}.json", jason);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Successfully added this build.\nYou can view all of your saved builds by using /view_builds");
                    return;
                }
                if (message.Text.StartsWith("/remove_perk "))
                {
                    string? PerkNameForRemoval = message.Text.Remove(0, 13);
                    Perk? PerkForRemoval = GetPerkFromName(PerkNameForRemoval);
                    if (PerkForRemoval != null)
                    {
                        if (DictTempPerkBuild[message.Chat.Id].Perks.Remove(PerkForRemoval))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Successfully removed perk {PerkForRemoval.perk_name}");
                            await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuild[message.Chat.Id].ToString());
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"That wasn\'t in your build");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "This perk doesn\'t exist, make sure to double check the spelling");
                    }
                    return;
                }
                Perk? perk = GetPerkFromName(message.Text);
                if (perk == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "This perk doesn\'t exist, make sure to double check the spelling");
                }
                if (perk != null && DictTempPerkBuild[message.Chat.Id] != null) 
                {
                    if (DictTempPerkBuild[message.Chat.Id].Perks.Contains(perk))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "That\'s already in there");
                        return;
                    }
                    if (perk.role == DictTempRole[message.Chat.Id] && DictTempPerkBuild[message.Chat.Id].Perks.Count < 4)
                    {
                        DictTempPerkBuild[message.Chat.Id].Perks.Add(perk);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Added perk {perk.perk_name}");
                    }
                    else if (perk.role == DictTempRole[message.Chat.Id])
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Too many, that\'s cheating");
                    }
                    else if (DictTempPerkBuild[message.Chat.Id].Perks.Count < 4)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "That\'s a wrong perk role");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Nah you can\'t mess up both perk role and counting, come on");
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuild[message.Chat.Id].ToString());
                }
                else if (DictTempPerkBuild[message.Chat.Id] == null)
                {
                    DictTempPerkBuild[message.Chat.Id] = new()
                    {
                        Perks = new()
                    };
                    if (DictTempPerkBuild[message.Chat.Id].Perks.Contains(perk))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "That\'s already in there");
                        return;
                    }
                    if (perk.role == DictTempRole[message.Chat.Id] && DictTempPerkBuild[message.Chat.Id].Perks.Count < 4)
                    {
                        DictTempPerkBuild[message.Chat.Id].Perks.Add(perk);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Added perk {perk.perk_name}");
                    }
                    else if (perk.role == DictTempRole[message.Chat.Id])
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Too many, that\'s cheating");
                    }
                    else if (DictTempPerkBuild[message.Chat.Id].Perks.Count < 4)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "That\'s a wrong perk role");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Nah you can\'t mess up both perk role and counting, come on");
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuild[message.Chat.Id].ToString());
                }
                return;
            }
            if (DictAskingPageFor.ContainsKey(message.Chat.Id))
            {
                try
                {
                    page = int.Parse(message.Text);
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid page, it can only contain numbers");
                }
                if (page != 0)
                {
                    int DisplayIndexFrom, DisplayIndexTo;
                    switch (DictAskingPageFor[message.Chat.Id])
                    {
                        case "survivors":
                            if (page < 1 || page > SurvivorList.Count / 10 + 1)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid page, please enter page from specified range");
                                return;
                            }
                            DisplayIndexFrom = (page - 1) * 10;
                            if (page == SurvivorList.Count / 10 + 1)
                            {
                                DisplayIndexTo = SurvivorList.Count - 1;
                            }
                            else 
                            { 
                                DisplayIndexTo = page * 10 - 1; 
                            }
                            
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Showing survivors page {page} ({DisplayIndexFrom + 1} - {DisplayIndexTo + 1})");
                            for (int i = DisplayIndexFrom; i <= DisplayIndexTo; i++)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"{SurvivorList[i].name}\n\n{SurvivorList[i].overview}");
                            }
                            break;
                        case "killers":
                            if (page < 1 || page > KillerList.Count / 10 + 1)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid page, please enter page from specified range");
                                DictAskingPageFor.Remove(message.Chat.Id);
                                return;
                            }
                            DisplayIndexFrom = (page - 1) * 10;
                            if (page == KillerList.Count / 10 + 1)
                            {
                                DisplayIndexTo = KillerList.Count - 1;
                            }
                            else
                            {
                                DisplayIndexTo = page * 10 - 1;
                            }

                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Showing killers page {page} ({DisplayIndexFrom + 1} - {DisplayIndexTo + 1})");
                            for (int i = DisplayIndexFrom; i <= DisplayIndexTo; i++)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"{KillerList[i].name}\n\n{KillerList[i].overview}");
                            }
                            break;
                        case "s_perks":
                            if (page < 1 || page > SPerkList.Count / 10 + 1)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid page, please enter page from specified range");
                                DictAskingPageFor.Remove(message.Chat.Id);
                                return;
                            }
                            DisplayIndexFrom = (page - 1) * 10;
                            if (page == SPerkList.Count / 10 + 1)
                            {
                                DisplayIndexTo = SPerkList.Count - 1;
                            }
                            else
                            {
                                DisplayIndexTo = page * 10 - 1;
                            }

                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Showing survivor perks page {page} ({DisplayIndexFrom + 1} - {DisplayIndexTo + 1})");
                            for (int i = DisplayIndexFrom; i <= DisplayIndexTo; i++)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"{SPerkList[i].perk_name}\n\n{SPerkList[i].description}");
                            }
                            break;
                        case "k_perks":
                            if (page < 1 || page > KPerkList.Count / 10 + 1)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Invalid page, please enter page from specified range");
                                DictAskingPageFor.Remove(message.Chat.Id);
                                return;
                            }
                            DisplayIndexFrom = (page - 1) * 10;
                            if (page == KPerkList.Count / 10 + 1)
                            {
                                DisplayIndexTo = KPerkList.Count - 1;
                            }
                            else
                            {
                                DisplayIndexTo = page * 10 - 1;
                            }

                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Showing killer perks page {page} ({DisplayIndexFrom + 1} - {DisplayIndexTo + 1})");
                            for (int i = DisplayIndexFrom; i <= DisplayIndexTo; i++)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, $"{KPerkList[i].perk_name}\n\n{KPerkList[i].description}");
                            }
                            break;
                        default:
                            break;
                    }
                }
                return;
            }

            if (message.Text.StartsWith("/edit_build "))
            {
                if (System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                {
                    if (!DictTempPerkBuilds.ContainsKey(message.Chat.Id))
                    {
                        DictTempPerkBuilds.Add(message.Chat.Id, new());
                    }
                    DictTempPerkBuilds[message.Chat.Id] = JsonConvert.DeserializeObject<PerkBuilds>(System.IO.File.ReadAllText($@"Data\Builds_{message.Chat.Id}.json"));
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "You don\'t have any builds");
                    return;
                }
                string? BuildNameForEditing = message.Text.Remove(0, 12);
                PerkBuild? BuildForEditing = null;
                for (int i = 0; i < DictTempPerkBuilds[message.Chat.Id].perkBuilds.Count; i++)
                {
                    if (DictTempPerkBuilds[message.Chat.Id].perkBuilds[i].BuildName == BuildNameForEditing)
                    {
                        BuildForEditing = DictTempPerkBuilds[message.Chat.Id].perkBuilds[i];
                    }
                }
                if (BuildForEditing == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "This build doesn\'t exist, make sure to double check the spelling");
                }
                else
                {
                    if (!DictTempPerkBuild.ContainsKey(message.Chat.Id))
                    {
                        DictTempPerkBuild.Add(message.Chat.Id, BuildForEditing);
                    }
                    else
                    {
                        DictTempPerkBuild[message.Chat.Id] = BuildForEditing;
                    }
                    if (!DictAskingPerk.ContainsKey(message.Chat.Id))
                    {
                        DictAskingPerk.Add(message.Chat.Id, true);
                    }
                    else
                    {
                        DictAskingPerk[message.Chat.Id] = true;
                    }
                    string? role = GetRoleFromName(BuildForEditing.CharacterName);
                    if (role != null)
                    if (!DictTempRole.ContainsKey(message.Chat.Id))
                    {
                        DictTempRole.Add(message.Chat.Id, role);
                    }
                    else
                    {
                        DictTempRole[message.Chat.Id] = role;
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuild[message.Chat.Id].ToString());
                    await botClient.SendTextMessageAsync(message.Chat.Id, "You can type a perk name to add perks, use /remove_perk {name of the perk you want to remove}\n\nUse /finish when you\'re done");
                    return;
                }
                return;
            }
            if (message.Text.StartsWith("/delete_build "))
            {
                if (System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                {
                    if (!DictTempPerkBuilds.ContainsKey(message.Chat.Id))
                    {
                        DictTempPerkBuilds.Add(message.Chat.Id, new());
                    }
                    DictTempPerkBuilds[message.Chat.Id] = JsonConvert.DeserializeObject<PerkBuilds>(System.IO.File.ReadAllText($@"Data\Builds_{message.Chat.Id}.json"));
                    if (DictTempPerkBuilds[message.Chat.Id] == null)
                    {
                        DictTempPerkBuilds[message.Chat.Id] = new();
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "You don\'t have any builds");
                    return;
                }
                string? BuildNameForDeleting = message.Text.Remove(0, 14);
                PerkBuild? BuildForDeleting = null;
                for (int i = 0; i < DictTempPerkBuilds[message.Chat.Id].perkBuilds.Count; i++)
                {
                    if (DictTempPerkBuilds[message.Chat.Id].perkBuilds[i].BuildName == BuildNameForDeleting)
                    {
                        BuildForDeleting = DictTempPerkBuilds[message.Chat.Id].perkBuilds[i];
                    }
                }
                if (BuildForDeleting == null)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "This build doesn\'t exist, make sure to double check the spelling");
                }
                else
                {
                    DictTempPerkBuilds[message.Chat.Id].perkBuilds.Remove(BuildForDeleting);
                    string jason = JsonConvert.SerializeObject(DictTempPerkBuilds[message.Chat.Id]);
                    System.IO.File.WriteAllText($@"Data\Builds_{message.Chat.Id}.json", jason);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Successfully deleted this build.\nYou can view all of your saved builds by using /view_builds");
                    return;
                }
                return;
            }

            switch (message.Text)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Hi, i\'m a bot for viewing your Dead by Daylight stats in Steam, looking up perks, killers and survivors" +
                        "\n\nYou can use /help for the list of my commands");
                    break;
                case "/stats":
                    if (!DictUserStats.ContainsKey(message.Chat.Id))
                    {
                        DictUserStats.Add(message.Chat.Id, null);
                    }
                    if (!DictAskingID.ContainsKey(message.Chat.Id))
                    {
                        DictAskingID.Add(message.Chat.Id, true);
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter Steam ID to display stats for");
                    DictAskingID[message.Chat.Id] = true;
                    break;
                case "/survivor_perks":
                    if (SPerkList != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Select page (1 - {SPerkList.Count / 10 + 1})");
                        if (!DictAskingPageFor.ContainsKey(message.Chat.Id))
                        {
                            DictAskingPageFor.Add(message.Chat.Id, "s_perks");
                        }
                        else
                        {
                            DictAskingPageFor[message.Chat.Id] = "s_perks";
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "I will ask you for pages until you use /cancel");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "DBD API Error: couldn\'t get perks");
                    }
                    break;
                case "/killer_perks":
                    if (KPerkList != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Select page (1 - {KPerkList.Count / 10 + 1})");
                        if (!DictAskingPageFor.ContainsKey(message.Chat.Id))
                        {
                            DictAskingPageFor.Add(message.Chat.Id, "k_perks");
                        }
                        else
                        {
                            DictAskingPageFor[message.Chat.Id] = "k_perks";
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "I will ask you for pages until you use /cancel");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "DBD API Error: couldn\'t get perks");
                    }
                    break;
                case "/survivors":
                    if (SurvivorList != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Select page (1 - {SurvivorList.Count / 10 + 1})");
                        if (!DictAskingPageFor.ContainsKey(message.Chat.Id))
                        {
                            DictAskingPageFor.Add(message.Chat.Id, "survivors");
                        }
                        else
                        {
                            DictAskingPageFor[message.Chat.Id] = "survivors";
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "I will ask you for pages until you use /cancel");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "DBD API Error: couldn\'t get survivors");
                    }
                    break;
                case "/killers":
                    if (KillerList != null)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Select page (1 - {KillerList.Count / 10 + 1})");
                        if (!DictAskingPageFor.ContainsKey(message.Chat.Id))
                        {
                            DictAskingPageFor.Add(message.Chat.Id, "killers");
                        }
                        else
                        {
                            DictAskingPageFor[message.Chat.Id] = "killers";
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "I will ask you for pages until you use /cancel");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "DBD API Error: couldn\'t get killers");
                    }
                    break;
                case "/add_build":
                    if (!System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                    {
                        string jason = System.IO.File.ReadAllText(@"Data\default.json");
                        System.IO.File.WriteAllText($@"Data\Builds_{message.Chat.Id}.json", JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PerkBuilds>(jason)));
                    }
                    if (!DictTempPerkBuilds.ContainsKey(message.Chat.Id))
                    {
                        DictTempPerkBuilds.Add(message.Chat.Id, new());
                        DictTempPerkBuilds[message.Chat.Id].perkBuilds = new();
                        DictTempPerkBuilds[message.Chat.Id].ChatID = message.Chat.Id;
                    }
                    else
                    {
                        if (DictTempPerkBuilds[message.Chat.Id] == null)
                        {
                            DictTempPerkBuilds[message.Chat.Id] = new()
                            {
                                perkBuilds = new(),
                                ChatID = message.Chat.Id
                            };
                        }
                        if (DictTempPerkBuilds[message.Chat.Id].perkBuilds.Count >= 10)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "You\'ve reached your limit of builds, consider deleting some");
                        }
                    }
                    if (!DictAskingCharacterName.ContainsKey(message.Chat.Id))
                    {
                        DictTempPerkBuild.Add(message.Chat.Id, new());
                        DictTempPerkBuild[message.Chat.Id].Perks = new();
                        DictAskingCharacterName.Add(message.Chat.Id, false);
                        DictAskingBuildName.Add(message.Chat.Id, true);
                        DictAskingPerk.Add(message.Chat.Id, false);
                    }
                    if (System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                    {
                        DictTempPerkBuilds[message.Chat.Id] = JsonConvert.DeserializeObject<PerkBuilds>(System.IO.File.ReadAllText($@"Data\Builds_{message.Chat.Id}.json"));
                        if (DictTempPerkBuilds[message.Chat.Id] == null)
                        {
                            DictTempPerkBuilds[message.Chat.Id] = new();
                        }
                    }
                    else
                    {
                        //System.IO.File.Create($@"Data\Builds_{message.Chat.Id}.json");
                    }

                    if (DictTempPerkBuilds[message.Chat.Id] == null)
                    {
                        DictTempPerkBuilds[message.Chat.Id] = new();
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Please enter the build name");
                    break;
                case "/view_builds":
                    if (System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                    {
                        if (!DictTempPerkBuilds.ContainsKey(message.Chat.Id))
                        {
                            DictTempPerkBuilds.Add(message.Chat.Id, new());
                        }
                        DictTempPerkBuilds[message.Chat.Id] = JsonConvert.DeserializeObject<PerkBuilds>(System.IO.File.ReadAllText($@"Data\Builds_{message.Chat.Id}.json"));
                        if (DictTempPerkBuilds[message.Chat.Id] == null)
                        {
                            DictTempPerkBuilds[message.Chat.Id] = new();
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Here are your favorite builds" +
                            "\nYou can: " +
                            "\nedit any of them using /edit_build {build name}" +
                            "\nadd a new one using /add_build" +
                            "\ndelete one of them using /delete_build {build name}" +
                            "\ndelete all of them using /delete_all");

                        try
                        {
                            if (DictTempPerkBuilds[message.Chat.Id] != null)
                                for (int i = 1; i < DictTempPerkBuilds[message.Chat.Id].perkBuilds.Count; i++)
                                {
                                    if (DictTempPerkBuilds[message.Chat.Id].perkBuilds[i] != null)
                                        await botClient.SendTextMessageAsync(message.Chat.Id, DictTempPerkBuilds[message.Chat.Id].perkBuilds[i].ToString());
                                }
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Nothing here yet");
                    }
                    break;
                case "/delete_all":
                    if (System.IO.File.Exists($@"Data\Builds_{message.Chat.Id}.json"))
                    {
                        System.IO.File.Delete($@"Data\Builds_{message.Chat.Id}.json");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Burned to the ground!");
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "You didn\'t have anything anyway");
                    }
                    break;
                case "/help":
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Here\'s what i can do:" +
                        "\n/stats - you can enter a SteamID and see DbD stats for that Steam profile" +
                        "\n/survivor_perks - see a list of all survivor perks" +
                        "\n/killer_perks - see a list of all killer perks" +
                        "\n/survivors - see a list of all survivors" +
                        "\n/killers - see a list of all killers" +
                        "\n/add_build - add a perk build to your favorites (up to 10)" +
                        "\n/view_builds - see a list of your favorite builds" +
                        "\n/edit_build {build name} - edit an existing build" +
                        "\n/delete_build {build name} - delete an existing build" +
                        "\n/delete_all - delete all builds" +
                        "\n/cancel - cancel the current action");
                    break;
                default:
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Something ain\'t right");
                    break;
            }
            return;
        }
        private async Task HandlerCallbackAsync(ITelegramBotClient botClient, CallbackQuery? callbackQuery)
        {
            try
            { 
                long chatID = callbackQuery.Message.Chat.Id;
                switch (callbackQuery.Data)
                {
                    case "survivor_stats":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Survivor stats*\n" +
                            $"\nPlayed games with full loadout: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_CamperFullLoadout", chatID)].value}`" +
                            $"\nPerfect games: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_CamperMaxScoreByCategory", chatID)].value}`" +
                            $"\nGenerators repaired: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_GeneratorPct_float", chatID)].value}`" +
                            $"\nGenerators repaired with no perks equipped: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter18_Camper_Stat2_float", chatID)].value}`" +
                            $"\nDamaged generators repaired (once per gen): `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Camper8_Stat1", chatID)].value}`" +
                            $"\nSuccessful skill checks: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SkillCheckSuccess", chatID)].value}`" +
                            $"\nItems Depleted: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter17_Camper_Stat1", chatID)].value}`" +
                            $"\nHex Totems Cleansed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC3_Camper_Stat1", chatID)].value}`" +
                            $"\nHex Totems Blessed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter21_Camper_Stat1", chatID)].value}`" +
                            $"\nExit Gates Opened: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC7_Camper_Stat2", chatID)].value}`" +
                            $"\nHooks Sabotaged: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter10_Camper_Stat1", chatID)].value}`" +
                            $"\nChests Searched: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC7_Camper_Stat1", chatID)].value}`" +
                            $"\nChests Searched in the Basement: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Event1_Stat2", chatID)].value}`" +
                            $"\nMystery boxes opened: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Event1_Stat3", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "killer_inter":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Killer interactions*\n" +
                            $"\nBasic attack or projectiles dodged: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter19_Camper_Stat2", chatID)].value}`" +
                            $"\nEscaped a chase after pallet stun: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter19_Camper_Stat1", chatID)].value}`" +
                            $"\nEscaped a chase injured after getting hit: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter22_Camper_Stat1", chatID)].value}`" +
                            $"\nEscaped a chase by hiding in a locker: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter24_Camper_Stat1", chatID)].value}`" +
                            $"\nProtection hits for unhooked survivor: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_HitNearHook", chatID)].value}`" +
                            $"\nProtection hits while a survivor is been carried: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter14_Camper_Stat1", chatID)].value}`" +
                            $"\nVaults while in chase: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Camper8_Stat2", chatID)].value}`" +
                            $"\nDodged attack before vaulting: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC9_Camper_Stat1", chatID)].value}`" +
                            $"\nWiggled from the killers grasp: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter12_Camper_Stat1", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "heal":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Healing*\n" +
                            $"\nSurvivors healed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_HealPct_float", chatID)].value}`" +
                            $"\nSurvivors healed while you are injured: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter11_Camper_Stat1_float", chatID)].value}`" +
                            $"\nSurvivors healed while 3 other survivors are injured, dying or hooked: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter17_Camper_Stat2_float", chatID)].value}`" +
                            $"\nSurvivors healed that found you while injured: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter23_Camper_Stat2", chatID)].value}`" +
                            $"\nSurvivors healed from dying to injured state: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter15_Camper_Stat1", chatID)].value}`" +
                            $"\nObsessions healed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter16_Camper_Stat1_float", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "save":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Saves*\n" +
                            $"\nSurvivors saved: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_UnhookOrHeal", chatID)].value}`" +
                            $"\nSurvivors saved during endgame: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_UnhookOrHeal_PostExit", chatID)].value}`" +
                            $"\nKillers pallet stunned while carrying a survivor: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter18_Camper_Stat1", chatID)].value}`" +
                            $"\nSelf unhooked: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter9_Camper_Stat1", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "escape":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Escapes*\n" +
                            $"\nTotal escapes: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Escape", chatID)].value}`" +
                            $"\nEscaped while crawling: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_EscapeKO", chatID)].value}`" +
                            $"\nEscaped after unhooking yourself: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_HookedAndEscape", chatID)].value}`" +
                            $"\nEscaped through the hatch: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_EscapeThroughHatch", chatID)].value}`" +
                            $"\nEscaped through the hatch while crawling: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter12_Camper_Stat2", chatID)].value}`" +
                            $"\nEscaped through the hatch with everyone: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_AllEscapeThroughHatch", chatID)].value}`" +
                            $"\nEscaped after been downed once: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC8_Camper_Stat1", chatID)].value}`" +
                            $"\nEscaped after been injured for half of the trial: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Camper9_Stat2", chatID)].value}`" +
                            $"\nEscaped with no bloodloss as obsession: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_EscapeNoBlood_Obsession", chatID)].value}`" +
                            $"\nEscaped after completing the last generator as last remaining survivor: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_LastSurvivorGeneratorEscape", chatID)].value}`" +
                            $"\nEscaped with a new item: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_CamperNewItem", chatID)].value}`" +
                            $"\nEscaped with no bloodloss from asylum: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_EscapeNoBlood_MapAsy_Asylum", chatID)].value}`" +
                            $"\nEscaped with item someone else brought into game: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_CamperEscapeWithItemFrom", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "special_gens":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Repaired special generator and escaped*\n" +
                            $"\nDisturbed Ward: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapAsy_Asylum", chatID)].value}`" +
                            $"\nLampkin Lane: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapSub_Street", chatID)].value}`" +
                            $"\nThe Pale Rose: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapSwp_PaleRose", chatID)].value}`" +
                            $"\nMother's Dwelling: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapBrl_MaHouse", chatID)].value}`" +
                            $"\nThe Game: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapFin_Hideout", chatID)].value}`" +
                            $"\nFather Campbell's Chapel: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapAsy_Chapel", chatID)].value}`" +
                            $"\nFamily Residence: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapHti_Manor", chatID)].value}`" +
                            $"\nMount Ormond Resort: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapKny_Cottage", chatID)].value}`" +
                            $"\nThe Temple of Purgation: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapBrl_Temple", chatID)].value}`" +
                            $"\nThe Underground Complex: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapQat_Lab", chatID)].value}`" +
                            $"\nSanctum of Wrath: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapHti_Shrine", chatID)].value}`" +
                            $"\nDead Dawg Saloon: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapUkr_Saloon", chatID)].value}`" +
                            $"\nMidwich Elementary School: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapWal_Level_01", chatID)].value}`" +
                            $"\nRaccoon City Police Station: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapEcl_Level_01", chatID)].value}`" +
                            $"\nEyrie of Crows: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapIon_Level_01", chatID)].value}`" +
                            $"\nGarden of Joy: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_FixSecondFloorGenerator_MapMtr_Level_1", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "killer_stats":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Killer stats*\n" +
                            $"\nPlayed with full loadout: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SlasherFullLoadout", chatID)].value}`" +
                            $"\nPerfect Games: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SlasherMaxScoreByCategory", chatID)].value}`" +
                            $"\nSurvivors Killed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_KilledCampers", chatID)].value}`" +
                            $"\nSurvivors Sacrificed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SacrificedCampers", chatID)].value}`" +
                            $"\nSacrificed all survivors before last generator: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter11_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors killed/sacrificed after last generator: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC8_Slasher_Stat2", chatID)].value}`" +
                            $"\nKilled all 4 survivors with tier 3 Evil Within: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SlasherPowerKillAllCampers", chatID)].value}`" +
                            $"\nObsessions Sacrificed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC7_Slasher_Stat2", chatID)].value}`" +
                            $"\nHatches Closed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter13_Slasher_Stat1", chatID)].value}`" +
                            $"\nGenerators damaged while at least one survivor is hooked: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC9_Slasher_Stat1", chatID)].value}`" +
                            $"\nGenerators damaged while undetectable: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter23_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors grabbed while repairing a gen: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter12_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors grabbed while hiding inside a locker: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter24_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors hit who dropped a pallet while in chase: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter9_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors hit while carrying a survivor: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter10_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors interrupted while cleansing a totem: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter15_Slasher_Stat2", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "hook":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Hookes*\n" +
                            $"\nSuvivors hooked before a generator is repaired: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter20_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors hooked during end game collapse: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter18_Slasher_Stat2", chatID)].value}`" +
                            $"\nHooked a survivor while 3 other survivors were injured: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter14_Slasher_Stat1", chatID)].value}`" +
                            $"\nHad 3 survivors hooked in the basement at same time: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Event1_Stat1", chatID)].value}`" +
                            $"\nSurvivors hooked in the basement: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC6_Slasher_Stat2", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "power":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Killer powers*\n" +
                            $"\nBear trap catches: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_TrapPickup", chatID)].value}`" +
                            $"\nUncloak Attacks: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_UncloakAttack", chatID)].value}`" +
                            $"\nChainsaw Hits (Billy): `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_ChainsawHit", chatID)].value}`" +
                            $"\nBlink Attacks: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SlasherChainAttack", chatID)].value}`" +
                            $"\nPhantasms Triggered: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC3_Slasher_Stat1", chatID)].value}`" +
                            $"\nHit each survivor once after teleporting to your phantasm trap: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC3_Slasher_Stat2", chatID)].value}`" +
                            $"\nEvil Within Tier Ups: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_SlasherTierIncrement", chatID)].value}`" +
                            $"\nShock Therapy Hits: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC4_Slasher_Stat1", chatID)].value}`" +
                            $"\nTrials with all survivors in madness tier 3: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC4_Slasher_Stat2", chatID)].value}`" +
                            $"\nHatchets Thrown: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC5_Slasher_Stat1", chatID)].value}`" +
                            $"\nPulled into Dream State: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC7_Slasher_Stat1", chatID)].value}`" +
                            $"\nReverse Bear Traps Placed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC8_Slasher_Stat1", chatID)].value}`" +
                            $"\nCages of Atonement: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter16_Slasher_Stat1", chatID)].value}`" +
                            $"\nLethal Rush Hits: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter17_Slasher_Stat1", chatID)].value}`" +
                            $"\nLacerations: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter19_Slasher_Stat1", chatID)].value}`" +
                            $"\nPossessed Chains: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter21_Slasher_Stat1", chatID)].value}`" +
                            $"\nCondemned: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter23_Slasher_Stat1", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "down":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Survivors downs*\n" +
                            $"\nSurvivors downed while suffering from oblivious: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter16_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while Exposed: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter17_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while carrying a survivor: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter19_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while in Deep Wound: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter10_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed near a raised pallet: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter13_Slasher_Stat2", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    case "power_down":
                        await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Survivors downed using killers power*\n" +
                            $"\nSurvivors downed with a Hatchet (24+ meters): `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC5_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed with a Chainsaw (Bubba): `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC6_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed while Intoxicated: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_DLC9_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed after Haunting: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter9_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while having max sickness: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter11_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while marked (Ghostface): `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter12_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while using Shred: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter13_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed while using Blood Fury: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter14_Slasher_Stat2", chatID)].value}`" +
                            $"\nSurvivors downed while Speared: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter15_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed while Victor is clinging to them: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter18_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed while contaminated: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter20_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed while using Dire Crows: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter22_Slasher_Stat1", chatID)].value}`" +
                            $"\nSurvivors downed during nightfall: `{(int)DictUserStats[chatID].playerstats.stats[FindIndexByStatName("DBD_Chapter24_Slasher_Stat1", chatID)].value}`", parseMode: ParseMode.Markdown);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception){}
        }
        private int FindIndexByStatName(string name, long chatID)
        {
            if (DictUserStats[chatID] != null)
            for (int i = 0; i < DictUserStats[chatID].playerstats.stats.Length; i++)
            {
                if (DictUserStats[chatID].playerstats.stats[i].name == name)
                {
                    return i;
                }
            }
            return 0;
        }
        private Perk? GetPerkFromName(string perk_name)
        {
            for (int i = 0; i < PerkList.Count; i++)
            {
                if (PerkList[i].perk_name == perk_name)
                {
                    return PerkList[i];
                }
            }
            return null;
        }
        private string? GetRoleFromName(string name)
        {
            for (int i = 0; i < KillerList.Count; i++)
            {
                if (KillerList[i].name == name)
                {
                    return "Killer";
                }
            }
            for (int i = 0; i < SurvivorList.Count; i++)
            {
                if (SurvivorList[i].name == name)
                {
                    return "Survivor";
                }
            }
            return null;
        }
        private string GetRankFromSkulls(int skulls)
        {
            switch (skulls)
            {
                case int n when (n >= 0 && n <= 2):
                    return "Ash IV";
                case int n when (n >= 2 && n <= 5):
                    return "Ash III";
                case int n when (n >= 6 && n <= 9):
                    return "Ash II";
                case int n when (n >= 10 && n <= 13):
                    return "Ash I";
                case int n when (n >= 14 && n <= 17):
                    return "Bronze IV";
                case int n when (n >= 18 && n <= 21):
                    return "Bronze III";
                case int n when (n >= 22 && n <= 25):
                    return "Bronze II";
                case int n when (n >= 26 && n <= 29):
                    return "Bronze I";
                case int n when (n >= 30 && n <= 34):
                    return "Silver IV";
                case int n when (n >= 35 && n <= 39):
                    return "Silver III";
                case int n when (n >= 40 && n <= 44):
                    return "Silver II";
                case int n when (n >= 45 && n <= 49):
                    return "Silver I";
                case int n when (n >= 50 && n <= 54):
                    return "Gold IV";
                case int n when (n >= 55 && n <= 59):
                    return "Gold III";
                case int n when (n >= 60 && n <= 64):
                    return "Gold II";
                case int n when (n >= 65 && n <= 69):
                    return "Gold I";
                case int n when (n >= 70 && n <= 74):
                    return "Iridescent IV";
                case int n when (n >= 75 && n <= 79):
                    return "Iridescent III";
                case int n when (n >= 80 && n <= 84):
                    return "Iridescent II";
                case int n when (n == 85):
                    return "Iridescent I";
                default:
                    return "n/a";
            }
        }
    }
}