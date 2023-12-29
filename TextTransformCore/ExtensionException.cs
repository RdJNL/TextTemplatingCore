//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;

namespace RdJNL.TextTemplatingCore.TextTransformCore
{
    internal sealed class ExtensionException : Exception
    {
        public ExtensionException()
        {
        }

        public ExtensionException(string message)
            : base(message)
        {
        }
    }
}
