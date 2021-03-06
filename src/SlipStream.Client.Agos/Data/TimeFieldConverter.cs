﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Data;

namespace SlipStream.Client.Agos.Controls
{
    public sealed class TimeFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var date = (DateTime)value;
                return date.ToLongTimeString();
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
