﻿using System;

namespace TotpAuthSharp.Helper;

internal static class Guard
{
    internal static void NotNull(object testee)
    {
        if (testee == null)
        {
            throw new NullReferenceException();
        }
    }
}