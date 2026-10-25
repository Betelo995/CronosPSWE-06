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

        [Theory]
        [InlineData("H/59 * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("H/59 * * * *", CronFormat.Standard)]
        [InlineData("* H/59 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* H/23 * * *", CronFormat.Standard)]
        [InlineData("* * H/23 * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * H/28 * *", CronFormat.Standard)]
        [InlineData("* * * H/28 * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * H/12 *", CronFormat.Standard)]
        [InlineData("* * * * H/12 *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * H/7", CronFormat.Standard)]
        [InlineData("* * * * * H/7", CronFormat.IncludeSeconds)]
        public void Parse_AcceptsHashStep_AtDocumentedUpperBound(string expression, CronFormat format)
        {
            var first = CronExpression.Parse(expression, format, JitterSeed);
            var second = CronExpression.Parse(expression, format, JitterSeed);

            Assert.Equal(first, second);
            Assert.NotNull(first.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Theory]
        [InlineData("H/60 * * * * *", CronFormat.IncludeSeconds, "Seconds", "between 1 and 59")]
        [InlineData("H/60 * * * *", CronFormat.Standard, "Minutes", "between 1 and 59")]
        [InlineData("* H/60 * * * *", CronFormat.IncludeSeconds, "Minutes", "between 1 and 59")]
        [InlineData("* H/24 * * *", CronFormat.Standard, "Hours", "between 1 and 23")]
        [InlineData("* * H/24 * * *", CronFormat.IncludeSeconds, "Hours", "between 1 and 23")]
        [InlineData("* * H/29 * *", CronFormat.Standard, "Days of month", "between 1 and 28")]
        [InlineData("* * * H/29 * *", CronFormat.IncludeSeconds, "Days of month", "between 1 and 28")]
        [InlineData("* * * H/13 *", CronFormat.Standard, "Months", "between 1 and 12")]
        [InlineData("* * * * H/13 *", CronFormat.IncludeSeconds, "Months", "between 1 and 12")]
        [InlineData("* * * * H/8", CronFormat.Standard, "Days of week", "between 1 and 7")]
        [InlineData("* * * * * H/8", CronFormat.IncludeSeconds, "Days of week", "between 1 and 7")]
        public void Parse_ThrowsCronFormatException_WhenHashStepExceedsFieldRange(string expression, CronFormat format, string field, string expectedRange)
        {
            var exception = Assert.Throws<CronFormatException>(() => CronExpression.Parse(expression, format, JitterSeed));

            Assert.Contains(field, exception.Message, StringComparison.Ordinal);
            Assert.Contains(expectedRange, exception.Message, StringComparison.Ordinal);
        }

        // Characterization: H/n in day-of-month is capped at 28 (to avoid the 31st of February),
        // while */n allows up to 31 as documented in the README ("1-31").
        [Fact]
        public void Parse_StepUpperBoundDiffersBetweenStarAndHash_InDayOfMonth()
        {
            var star = CronExpression.Parse("* * */29 * *", CronFormat.Standard, JitterSeed);

            Assert.NotNull(star);
            Assert.Throws<CronFormatException>(() => CronExpression.Parse("* * H/29 * *", CronFormat.Standard, JitterSeed));
        }

        [Theory]
        [InlineData("H * * * *", CronFormat.Standard)]
        [InlineData("* H * * *", CronFormat.Standard)]
        [InlineData("* * H * *", CronFormat.Standard)]
        [InlineData("* * * H *", CronFormat.Standard)]
        [InlineData("* * * * H", CronFormat.Standard)]
        [InlineData("H * * * * *", CronFormat.IncludeSeconds)]
        public void Parse_ThrowsCronFormatExceptionFamily_WhenHashIsPresentAndNoSeedIsProvided(string expression, CronFormat format)
        {
            var exception = Assert.ThrowsAny<CronFormatException>(() => CronExpression.Parse(expression, format));
            Assert.IsType<MissingSeedException>(exception);
        }

        [Theory]
        [InlineData("h * * * *", "H * * * *", CronFormat.Standard)]
        [InlineData("* * h * *", "* * H * *", CronFormat.Standard)]
        [InlineData("* * * * h", "* * * * H", CronFormat.Standard)]
        [InlineData("h/15 * * * *", "H/15 * * * *", CronFormat.Standard)]
        [InlineData("* h * * * *", "* H * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * h * *", "* * * H * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * h", "* * * * * H", CronFormat.IncludeSeconds)]
        [InlineData("* h/15 * * * *", "* H/15 * * * *", CronFormat.IncludeSeconds)]
        public void Parse_AcceptsLowercaseHash_LikeUppercase(string lower, string upper, CronFormat format)
        {
            var expected = CronExpression.Parse(upper, format, JitterSeed);
            var actual = CronExpression.Parse(lower, format, JitterSeed);

            Assert.Equal(expected, actual);
        }
    }
}
