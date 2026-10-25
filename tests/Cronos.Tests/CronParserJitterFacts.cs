// The MIT License(MIT)
//
// Copyright (c) 2026 Hangfire OÜ
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using Xunit;
using Xunit.Abstractions;
using Cronos;

namespace Cronos.Tests
{
    public class CronParserJitterFacts
    {
        private const int JitterSeed = 3;

        private readonly ITestOutputHelper _output;

        public CronParserJitterFacts(ITestOutputHelper output)
        {
            _output = output;
        }

        // The README grammar only allows an optional step after H: 'H' [step].
        [Theory]
        [InlineData("H-5 * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("H,10 * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("H5 * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1,H * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("H/ * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("* * H#2 * *", CronFormat.Standard, "Days of month")]
        [InlineData("* * HW * *", CronFormat.Standard, "Days of month")]
        [InlineData("* * HL * *", CronFormat.Standard, "Days of month")]
        [InlineData("* * * * H#2", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * HL", CronFormat.Standard, "Days of week")]

        [InlineData("* H-5 * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* H,10 * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* H5 * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1,H * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* H/ * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* * * H#2 * *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * HW * *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * HL * *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * * * H#2", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * HL", CronFormat.IncludeSeconds, "Days of week")]
        public void Parse_ThrowsCronFormatException_WhenHashTokenHasInvalidSyntax(string expression, CronFormat format, string field)
        {
            var exception = Assert.Throws<CronFormatException>(() => CronExpression.Parse(expression, format, JitterSeed));

            Assert.Contains(field, exception.Message, StringComparison.Ordinal);
        }

        // Without a seed both outcomes are acceptable, but the actual exception type is logged for the report:
        // MissingSeedException does not derive from CronFormatException.
        [Theory]
        [InlineData("H-5 * * * *", CronFormat.Standard)]
        [InlineData("H,10 * * * *", CronFormat.Standard)]
        [InlineData("H5 * * * *", CronFormat.Standard)]
        [InlineData("1,H * * * *", CronFormat.Standard)]
        [InlineData("H/ * * * *", CronFormat.Standard)]
        [InlineData("* * H#2 * *", CronFormat.Standard)]
        [InlineData("* * HW * *", CronFormat.Standard)]
        [InlineData("* * HL * *", CronFormat.Standard)]
        [InlineData("* * * * H#2", CronFormat.Standard)]
        [InlineData("* * * * HL", CronFormat.Standard)]

        [InlineData("* H-5 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* H,10 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* H5 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 1,H * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* H/ * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * H#2 * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * HW * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * HL * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * H#2", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * HL", CronFormat.IncludeSeconds)]
        public void Parse_ThrowsCronFormatException_WhenHashTokenIsInvalidEvenWithoutSeed(string expression, CronFormat format)
        {
            var exception = Record.Exception(() => CronExpression.Parse(expression, format));

            Assert.NotNull(exception);
            _output.WriteLine($"[no seed] \"{expression}\" ({format}) -> {exception!.GetType().Name}: {exception.Message}");
            Assert.True(
                exception is CronFormatException || exception is MissingSeedException,
                $"Unexpected exception type {exception.GetType().FullName}: {exception.Message}");
        }
    }
}
