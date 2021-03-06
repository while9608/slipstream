﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SlipStream.Client.Agos.Controls
{
    public sealed class NullableDecimalUpDown : UpDownBase<Decimal?>
    {
        protected override string FormatValue()
        {
            return this.Value == null ? string.Empty : this.Value.Value.ToString();
        }

        protected override void OnDecrement()
        {
            if (this.Value != null)
            {
                this.Value = this.Value.Value - 1;
            }
            else
            {
                this.Value = 0;
            }
        }

        protected override void OnIncrement()
        {
            if (this.Value != null)
            {
                this.Value = this.Value.Value + 1;
            }
            else
            {
                this.Value = 0;
            }
        }

        protected override decimal? ParseValue(string text)
        {
            decimal val;
            if (decimal.TryParse(text, out val))
            {
                return val;
            }
            else
            {
                return null;
            }
        }
    }
}
