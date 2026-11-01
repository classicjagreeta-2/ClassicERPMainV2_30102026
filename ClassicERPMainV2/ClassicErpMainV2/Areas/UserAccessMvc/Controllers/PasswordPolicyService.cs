using System;
using System.Linq;

namespace ClassicErpMainV2.Areas.UserAccessMvc.Services
{
    /// <summary>
    /// Direct port of Private Function passwordstrcut(pwd As String) As Boolean.
    /// The four thresholds (min/max length, required numeric/alpha/special char counts)
    /// were globals read from MAST_PARAM in the legacy app (via lookupVal). Inject them
    /// here instead of hard-coding, so they stay admin-configurable like before.
    /// </summary>
    public class PasswordPolicy
    {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public int MinNumericChars { get; set; }
        public int MinAlphaChars { get; set; }
        public int MinSpecialChars { get; set; }
    }

    public class PasswordPolicyResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public interface IPasswordPolicyService
    {
        PasswordPolicyResult Validate(string password, PasswordPolicy policy);
    }

    public class PasswordPolicyService : IPasswordPolicyService
    {
        public PasswordPolicyResult Validate(string password, PasswordPolicy policy)
        {
            password = password ?? string.Empty;

            if (password.Contains(" "))
                return Fail("Spaces are not allowed in the password.");

            int numeric = password.Count(c => c >= '0' && c <= '9');
            int alpha = password.Count(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));
            int special = password.Length - numeric - alpha;

            if (password.Length > policy.MaxLength)
                return Fail($"Password must be less than or equal to {policy.MaxLength} character(s).");

            if (password.Length < policy.MinLength)
                return Fail($"Password must be {policy.MinLength} character(s) long.");

            if (special < policy.MinSpecialChars)
                return Fail($"Password must have {policy.MinSpecialChars} special character(s).");

            if (numeric < policy.MinNumericChars)
                return Fail($"Password must have {policy.MinNumericChars} numeric character(s).");

            if (alpha < policy.MinAlphaChars)
                return Fail($"Password must have {policy.MinAlphaChars} alphabetic character(s).");

            return new PasswordPolicyResult { IsValid = true };
        }

        private static PasswordPolicyResult Fail(string message) =>
            new PasswordPolicyResult { IsValid = false, ErrorMessage = message };
    }
}
