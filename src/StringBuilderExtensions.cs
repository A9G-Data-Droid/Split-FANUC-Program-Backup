﻿using System.Text;

namespace SplitFANUCProgramBackup;

internal static class StringBuilderExtensions
{
    public static StringBuilder? TrimEnd(this StringBuilder? sb)
    {
        if (sb == null || sb.Length == 0) return sb;

        int i = sb.Length - 1;

        for (; i >= 0; i--)
            if (!char.IsWhiteSpace(sb[i]))
                break;

        // Trim
        if (i < sb.Length - 1)
            sb.Length = i + 1;

        return sb;
    }
}
