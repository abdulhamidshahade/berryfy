using System.Text;

namespace Berryfy.Application.Halpers
{
    public class EmailNormalizer
    {
        public static string NormalizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            email = email.Trim().ToLower();
            email = email.Normalize(NormalizationForm.FormKC);

            var parts = email.Split('@');

            if (parts.Length != 2)
                return email;

            string localPart = parts[0];
            string domain = parts[1];

            //if (domain == "gmail.com")
            //{
            //    localPart = localPart.Replace(".", "");
            //}

            return $"{localPart}@{domain}";
        }
    }
}
