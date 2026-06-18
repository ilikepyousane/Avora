using System;

namespace Avora.Views.Notification
{
    public enum NotificationType
    {
        Standard,
        Survey
    }

    public enum ButtonActionType
    {
        Url,        // Открыть URL
        Event,      // Отправить событие (StatSly)
        Close       // Закрыть уведомление
    }

    public enum InputFieldType
    {
        Text,
        Number,
        Multiline
    }
}