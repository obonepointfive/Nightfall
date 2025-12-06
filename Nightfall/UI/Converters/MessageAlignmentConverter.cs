using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Nightfall.UI.Models;

namespace Nightfall.UI.Converters
{
    public sealed class MessageAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChatSender sender)
            {
                return sender == ChatSender.User
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;
            }

            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
