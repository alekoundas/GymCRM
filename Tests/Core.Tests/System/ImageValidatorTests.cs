using Core.System;

namespace Core.Tests.System
{
    /// <summary>
    /// A picture is told by its first bytes, never by what the upload called itself, and may not be
    /// bigger than its limit.
    /// </summary>
    public sealed class ImageValidatorTests
    {
        private const int Limit = 100;

        private static readonly byte[] JpegStart = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngStart = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] WebpStart = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50];

        public static readonly TheoryData<string> Kinds = ["jpeg", "png", "webp"];

        // Every byte a kind is told by. The four in the middle of a WebP are its size and are not.
        public static TheoryData<string, int> EveryTellingByte
        {
            get
            {
                TheoryData<string, int> data = new TheoryData<string, int>();

                for (int i = 0; i < 3; i++)
                    data.Add("jpeg", i);

                for (int i = 0; i < 8; i++)
                    data.Add("png", i);

                foreach (int i in new[] { 0, 1, 2, 3, 8, 9, 10, 11 })
                    data.Add("webp", i);

                return data;
            }
        }

        private static byte[] StartOf(string kind)
        {
            switch (kind)
            {
                case "jpeg":
                    return JpegStart;
                case "png":
                    return PngStart;
                default:
                    return WebpStart;
            }
        }

        private static byte[] Picture(string kind, int length)
        {
            byte[] bytes = new byte[length];
            StartOf(kind).AsSpan(0, Math.Min(length, StartOf(kind).Length)).CopyTo(bytes);

            return bytes;
        }

        private static string Text(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }


        // Nothing uploaded.

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t\r\n")]
        public void No_text_at_all_is_allowed(string? text)
        {
            ImageValidator.IsAllowed(text, Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(text, 0).ShouldBeTrue();
        }

        [Fact]
        public void No_bytes_at_all_are_allowed()
        {
            ImageValidator.IsAllowed((byte[]?)null, Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Array.Empty<byte>(), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Array.Empty<byte>(), 0).ShouldBeTrue();
        }

        // A data url with nothing after the comma claims a picture and has none.
        [Fact]
        public void A_data_url_with_no_picture_in_it_is_refused()
        {
            ImageValidator.IsAllowed("data:image/png;base64,", Limit).ShouldBeFalse();
        }


        // The kinds.

        [Theory]
        [MemberData(nameof(Kinds))]
        public void A_picture_of_an_allowed_kind_is_allowed(string kind)
        {
            byte[] picture = Picture(kind, 50);

            ImageValidator.IsAllowed(picture, Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Text(picture), Limit).ShouldBeTrue();
        }

        // The first bytes have to be followed by at least one more.
        [Theory]
        [MemberData(nameof(Kinds))]
        public void The_first_bytes_alone_are_not_a_picture(string kind)
        {
            int start = StartOf(kind).Length;

            for (int length = 1; length <= start; length++)
            {
                ImageValidator.IsAllowed(Picture(kind, length), Limit).ShouldBeFalse($"{length} bytes");
                ImageValidator.IsAllowed(Text(Picture(kind, length)), Limit).ShouldBeFalse($"{length} bytes");
            }

            ImageValidator.IsAllowed(Picture(kind, start + 1), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Text(Picture(kind, start + 1)), Limit).ShouldBeTrue();
        }

        [Theory]
        [MemberData(nameof(EveryTellingByte))]
        public void One_wrong_byte_at_the_start_is_not_a_picture(string kind, int index)
        {
            byte[] picture = Picture(kind, 50);
            picture[index] ^= 0x01;

            ImageValidator.IsAllowed(picture, Limit).ShouldBeFalse();
            ImageValidator.IsAllowed(Text(picture), Limit).ShouldBeFalse();
        }

        [Fact]
        public void The_size_inside_a_webp_may_be_anything()
        {
            byte[] picture = Picture("webp", 50);
            for (int i = 4; i < 8; i++)
                picture[i] = 0xAB;

            ImageValidator.IsAllowed(picture, Limit).ShouldBeTrue();
        }

        // A gif, a bitmap, a pdf, a script and a picture that starts one byte late.
        [Theory]
        [InlineData("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7")]
        [InlineData("Qk06AAAAAAAAADYAAAAoAAAAAQAAAAEAAAABABgAAAAAAA==")]
        [InlineData("JVBERi0xLjQKJcfsj6IKNSAwIG9iago=")]
        [InlineData("PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
        [InlineData("AP/Y/+AAEEpGSUYAAQEAAAEAAQAA")]
        public void Anything_else_is_refused(string text)
        {
            ImageValidator.IsAllowed(text, 1000).ShouldBeFalse();
            ImageValidator.IsAllowed(Convert.FromBase64String(text), 1000).ShouldBeFalse();
        }


        // The text around it.

        [Theory]
        [InlineData("data:image/jpeg;base64,")]
        [InlineData("DATA:image/png;base64,")]
        [InlineData("Data:,")]
        [InlineData("data:text/html;base64,")]
        public void What_a_data_url_calls_itself_does_not_matter(string prefix)
        {
            ImageValidator.IsAllowed(prefix + Text(Picture("jpeg", 50)), Limit).ShouldBeTrue();
        }

        [Theory]
        [InlineData("data:image/jpeg;base64")]
        [InlineData("image/jpeg;base64,")]
        [InlineData(" data:image/jpeg;base64,")]
        [InlineData("x,")]
        public void A_prefix_that_is_not_a_data_url_is_part_of_the_picture_and_spoils_it(string prefix)
        {
            ImageValidator.IsAllowed(prefix + Text(Picture("jpeg", 50)), Limit).ShouldBeFalse();
        }

        [Theory]
        [InlineData("not base64 at all!")]
        [InlineData("/9j/4AAQSkZJRgABAQ=")]
        [InlineData("/9j_4AAQSkZJRgABAQAA")]
        [InlineData("=")]
        public void Text_that_is_not_base64_is_refused(string text)
        {
            ImageValidator.IsAllowed(text, 1000).ShouldBeFalse();
        }

        [Fact]
        public void Line_breaks_inside_the_base64_do_not_matter()
        {
            string text = Text(Picture("png", 50)).Insert(20, "\r\n");

            ImageValidator.IsAllowed(text, Limit).ShouldBeTrue();
        }


        // The limit.

        [Theory]
        [MemberData(nameof(Kinds))]
        public void A_picture_may_be_as_big_as_the_limit_and_no_bigger(string kind)
        {
            ImageValidator.IsAllowed(Picture(kind, Limit - 1), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Picture(kind, Limit), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Picture(kind, Limit + 1), Limit).ShouldBeFalse();

            ImageValidator.IsAllowed(Text(Picture(kind, Limit - 1)), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Text(Picture(kind, Limit)), Limit).ShouldBeTrue();
            ImageValidator.IsAllowed(Text(Picture(kind, Limit + 1)), Limit).ShouldBeFalse();
        }

        // Base64 comes in fours, so its length says the size only to within three bytes. Every
        // remainder, so neither the quick check nor the exact one lets a byte through.
        [Fact]
        public void The_limit_holds_whatever_the_remainder_of_the_base64()
        {
            for (int limit = 60; limit < 72; limit++)
                for (int length = limit - 4; length <= limit + 8; length++)
                    ImageValidator.IsAllowed(Text(Picture("jpeg", length)), limit).ShouldBe(length <= limit, $"{length} bytes against {limit}");
        }

        [Fact]
        public void Text_far_over_the_limit_is_refused_before_it_is_decoded()
        {
            // Not base64, so decoding it would fail as well - the length is what refuses it.
            ImageValidator.IsAllowed(new string('!', 10_000), Limit).ShouldBeFalse();
        }

        [Fact]
        public void A_limit_of_nought_allows_no_picture()
        {
            ImageValidator.IsAllowed(Picture("jpeg", 4), 0).ShouldBeFalse();
            ImageValidator.IsAllowed(Text(Picture("jpeg", 4)), 0).ShouldBeFalse();
        }
    }
}
