using System;
using CommunityToolkit.Mvvm.Messaging;

namespace LCTWorks.WinUI.Extensions;

public static class MessengerExtensions
{
    public static void TryRegister<TMessage>(this IMessenger messenger, object recipient, MessageHandler<object, TMessage> handler)
        where TMessage : class
        => messenger.TryRegister<object, TMessage>(recipient, handler);

    public static void TryRegister<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
    {
        if (!CheckNull(messenger, recipient, handler))
        {
            return;
        }
        if (messenger.IsRegistered<TMessage>(recipient))
        {
            messenger.Unregister<TMessage>(recipient);
        }
        messenger.Register(recipient, handler);
    }

    public static void TryRegister<TRecipient, TMessage, TToken>(this IMessenger messenger, TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
                where TRecipient : class
                where TMessage : class
                where TToken : IEquatable<TToken>
    {
        if (!CheckNull(messenger, recipient, handler))
        {
            return;
        }
        if (token == null)
        {
            return;
        }
        if (messenger.IsRegistered<TMessage, TToken>(recipient, token))
        {
            messenger.Unregister<TMessage, TToken>(recipient, token);
        }
        messenger.Register(recipient, token, handler);
    }

    private static bool CheckNull<TRecipient, TMessage>(IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
                                where TRecipient : class
        where TMessage : class
    {
        return messenger != null && recipient != null && handler != null;
    }
}