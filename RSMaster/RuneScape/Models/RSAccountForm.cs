using System;
using System.Collections.Generic;

namespace RSMaster.RuneScape.Models
{
    public class RSAccountForm
    {
        public RSAccountForm()
        {
            Day = (new Random().Next(1, 28)).ToString();
            Month = (new Random().Next(1, 12)).ToString();
            Year = (new Random().Next(1970, (DateTime.Now.Year - 13))).ToString();
        }

        public string RequestId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Day { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }

        public Dictionary<string, string> Build(string captcha)
        {
            return new Dictionary<string, string>()
            {
                { "theme", "oldschool" },
                { "email1", Email },
                { "onlyOneEmail", "1" },
                { "password1", Password },
                { "onlyOnePassword", "1" },
                { "day", Day },
                { "month", Month },
                { "year", Year },
                { "agree_email", "1" },
                { "agree_email_third_party", "1" },
                { "g-recaptcha-response", captcha },
                { "create-submit", "create" }
            };
        }
    }
}
