//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RdJNL.TextTemplatingCore.TextTemplatingCoreLib
{
    public sealed class TemplateException : Exception
    {
        public ImmutableArray<TemplateError> Errors { get; }

        public TemplateException(params TemplateError[] errors)
            : this(errors.AsEnumerable())
        {
        }

        public TemplateException(IEnumerable<TemplateError> errors)
        {
            Errors = errors.ToImmutableArray();
        }
    }
}
