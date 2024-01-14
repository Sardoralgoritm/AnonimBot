using AnonimBot.Configs;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    public static async Task Main(string[] args)
    {
        var TOKEN = Config.TOKEN;
        var ADMIN = Config.ADMIN;
        var botClient = new TelegramBotClient(TOKEN);

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;

            if (message.Chat.Id == ADMIN)
            {
                if (message.ReplyToMessage != null)
                {
                    if (message.ReplyToMessage.ForwardFrom != null)
                    {
                        var chatId = message.ReplyToMessage.ForwardFrom.Id;
                        Message sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"From: *{RemoveNonAlphanumeric(message.Chat.FirstName)}* \nMessage: `{messageText}`",
                            parseMode: ParseMode.MarkdownV2,
                            replyToMessageId: message.ReplyToMessage.ForwardFromMessageId
                            );

                        await botClient.SendTextMessageAsync(
                            chatId: ADMIN,
                            text: $"*Xabaringiz yetkazildi*",
                            parseMode: ParseMode.MarkdownV2,
                            cancellationToken: cancellationToken);

                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: ADMIN,
                        text: $"*Iltimos xabarni userni xabariga reply qilib jo'nating*",
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    }
                }
            }
            else
            {
                Message forward_Message = await botClient.ForwardMessageAsync(
                chatId: ADMIN,
                fromChatId: message.Chat.Id,
                messageId: message.MessageId,
                cancellationToken: cancellationToken
                );
                if (forward_Message.ForwardFrom != null)
                {
                    await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"*Xabaringiz yetkazildi*",
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.DeleteMessageAsync(
                        chatId: ADMIN,
                        messageId: forward_Message.MessageId,
                        cancellationToken: cancellationToken);

                    await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"*Bot xabaringizni Adminga forward qila olmadi\nIltimos Forward sozlamalaringizni to'g'rilang\nAks holda Admin sizga javob yoza olmaydi*",
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                }
            }
        }
        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

    }
    private static string? RemoveNonAlphanumeric(string input)
    {
        // Faqat harflar va raqamlar qoladi
        string pattern = "[^a-zA-Z0-9]";

        // Regex orqali belgilarni o'chirish
        string? result = null;
        if (input != null) result = Regex.Replace(input, pattern, "");
        return result;
    }
}