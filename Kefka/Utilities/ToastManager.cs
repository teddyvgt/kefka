﻿using System;
using System.Windows;
using System.Windows.Media;
using Kefka.Models;
using ff14bot;

namespace Kefka.Utilities
{
    internal class ToastManager
    {
        internal static void AddToast(string message, TimeSpan timeSpan, Color textColor, Color outlineColor, FontFamily font, FontWeight fontWeight, double fontSize)
        {
            if (!MainSettingsModel.Instance.ShowToastMessages) return;

            Core.OverlayManager.AddToast(() => $"{message}", timeSpan, textColor, outlineColor, new FontFamily($"{font}"), fontWeight, fontSize);
        }
    }
}