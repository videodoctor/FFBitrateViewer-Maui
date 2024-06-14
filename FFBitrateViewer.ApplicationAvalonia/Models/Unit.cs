﻿using System;


namespace FFBitrateViewer.ApplicationAvalonia.Models;

[Flags]
public enum Unit
{
    Unknown = 0,
    // b/s
    BitsPerSecond = 1,
    // Hz
    Hertz = 2
}