using Core.Dtos;

namespace Core.Tests.Dtos
{
    /// <summary>
    /// What every endpoint answers with. The client reads isSucceed first and the messages after it.
    /// </summary>
    public sealed class ApiResponseTests
    {
        [Fact]
        public void A_new_response_is_a_success_with_no_data_and_no_messages()
        {
            ApiResponse<object> response = new ApiResponse<object>();

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBeNull();
            response.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void A_new_response_of_a_value_holds_its_default()
        {
            new ApiResponse<int>().Data.ShouldBe(0);
            new ApiResponse<bool>().Data.ShouldBeFalse();
        }


        // Success.

        [Fact]
        public void Success_with_data_carries_the_data_and_says_nothing()
        {
            List<int> data = [1, 2];

            ApiResponse<List<int>> response = new ApiResponse<List<int>>().SetSuccessResponse(data);

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBeSameAs(data);
            response.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void Success_with_a_message_alone_carries_no_data()
        {
            ApiResponse<int> response = new ApiResponse<int>().SetSuccessResponse("Saved");

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBe(0);
            response.Messages.ShouldHaveSingleItem().Key.ShouldBe("success");
            response.Messages["success"].ShouldBe(["Saved"]);
        }

        [Fact]
        public void Success_with_data_and_a_message_carries_both()
        {
            ApiResponse<int> response = new ApiResponse<int>().SetSuccessResponse(7, "Saved");

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBe(7);
            response.Messages.ShouldHaveSingleItem().Key.ShouldBe("success");
            response.Messages["success"].ShouldBe(["Saved"]);
        }

        [Fact]
        public void Success_with_data_and_several_messages_keeps_them_in_order()
        {
            string[] messages = ["first", "second"];

            ApiResponse<int> response = new ApiResponse<int>().SetSuccessResponse(7, messages);

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBe(7);
            response.Messages.ShouldHaveSingleItem().Key.ShouldBe("success");
            response.Messages["success"].ShouldBeSameAs(messages);
        }

        [Fact]
        public void Success_with_its_own_messages_replaces_whatever_was_there()
        {
            Dictionary<string, string[]> messages = new Dictionary<string, string[]> { ["note"] = ["read me"] };

            ApiResponse<int> response = new ApiResponse<int>().SetSuccessResponse("Saved").SetSuccessResponse(7, messages);

            response.IsSucceed.ShouldBeTrue();
            response.Data.ShouldBe(7);
            response.Messages.ShouldBeSameAs(messages);
        }

        [Fact]
        public void A_response_of_text_takes_a_lone_text_as_the_message_and_not_as_the_data()
        {
            ApiResponse<string> response = new ApiResponse<string>().SetSuccessResponse("Saved");

            response.Data.ShouldBeNull();
            response.Messages["success"].ShouldBe(["Saved"]);
        }

        [Fact]
        public void A_response_of_text_takes_two_texts_as_the_data_and_the_message()
        {
            ApiResponse<string> response = new ApiResponse<string>().SetSuccessResponse("http://gym.test", "");

            response.Data.ShouldBe("http://gym.test");
            response.Messages["success"].ShouldBe([""]);
        }


        // Error.

        [Fact]
        public void An_error_is_not_a_success_and_carries_no_data()
        {
            ApiResponse<object> response = new ApiResponse<object>().SetErrorResponse("No");

            response.IsSucceed.ShouldBeFalse();
            response.Data.ShouldBeNull();
            response.Messages.ShouldHaveSingleItem().Key.ShouldBe("error");
            response.Messages["error"].ShouldBe(["No"]);
        }

        [Fact]
        public void Several_errors_keep_their_order()
        {
            string[] errors = ["first", "second"];

            ApiResponse<object> response = new ApiResponse<object>().SetErrorResponse(errors);

            response.IsSucceed.ShouldBeFalse();
            response.Messages.ShouldHaveSingleItem().Key.ShouldBe("error");
            response.Messages["error"].ShouldBeSameAs(errors);
        }

        [Fact]
        public void No_errors_at_all_is_still_an_error()
        {
            ApiResponse<object> response = new ApiResponse<object>().SetErrorResponse(Array.Empty<string>());

            response.IsSucceed.ShouldBeFalse();
            response.Messages["error"].ShouldBeEmpty();
        }

        [Fact]
        public void Errors_with_their_own_names_replace_whatever_was_there()
        {
            Dictionary<string, string[]> errors = new Dictionary<string, string[]> { ["title"] = ["is required"] };

            ApiResponse<object> response = new ApiResponse<object>().SetErrorResponse("No").SetErrorResponse(errors);

            response.IsSucceed.ShouldBeFalse();
            response.Messages.ShouldBeSameAs(errors);
        }


        // One after the other.

        [Fact]
        public void An_error_after_a_success_keeps_the_data_and_both_messages()
        {
            ApiResponse<int> response = new ApiResponse<int>().SetSuccessResponse(7, "Saved").SetErrorResponse("No");

            response.IsSucceed.ShouldBeFalse();
            response.Data.ShouldBe(7);
            response.Messages["success"].ShouldBe(["Saved"]);
            response.Messages["error"].ShouldBe(["No"]);
        }

        [Fact]
        public void A_success_after_an_error_does_not_make_it_a_success_again()
        {
            ApiResponse<int> response = new ApiResponse<int>().SetErrorResponse("No").SetSuccessResponse(7);

            response.IsSucceed.ShouldBeFalse();
            response.Data.ShouldBe(7);
        }

        [Fact]
        public void A_second_message_of_the_same_kind_is_refused()
        {
            Should.Throw<ArgumentException>(() => new ApiResponse<int>().SetSuccessResponse("one").SetSuccessResponse("two"));
            Should.Throw<ArgumentException>(() => new ApiResponse<int>().SetSuccessResponse(1, "one").SetSuccessResponse(2, "two"));
            Should.Throw<ArgumentException>(() => new ApiResponse<int>().SetSuccessResponse(1, ["one"]).SetSuccessResponse(2, ["two"]));
            Should.Throw<ArgumentException>(() => new ApiResponse<int>().SetErrorResponse("one").SetErrorResponse("two"));
            Should.Throw<ArgumentException>(() => new ApiResponse<int>().SetErrorResponse(["one"]).SetErrorResponse(["two"]));
        }

        [Fact]
        public void Every_setter_hands_back_the_response_it_was_called_on()
        {
            ApiResponse<int> response = new ApiResponse<int>();

            response.SetSuccessResponse(1).ShouldBeSameAs(response);
            response.SetSuccessResponse("one").ShouldBeSameAs(response);
            response.SetSuccessResponse(1, new Dictionary<string, string[]>()).ShouldBeSameAs(response);
            response.SetSuccessResponse(1, "one").ShouldBeSameAs(response);
            response.SetSuccessResponse(1, new Dictionary<string, string[]>()).ShouldBeSameAs(response);
            response.SetSuccessResponse(1, ["one"]).ShouldBeSameAs(response);
            response.SetErrorResponse("one").ShouldBeSameAs(response);
            response.SetErrorResponse(new Dictionary<string, string[]>()).ShouldBeSameAs(response);
            response.SetErrorResponse(["one"]).ShouldBeSameAs(response);
        }
    }
}
