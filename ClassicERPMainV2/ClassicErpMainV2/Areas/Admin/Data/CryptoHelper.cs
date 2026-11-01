using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ClassicErpMainV2.Areas.Admin.Data
{
    public static class CryptoHelper
    {

        public static string Encode(string plainPassword, string userOra)
        {
            var Username = userOra;
            var PlainText = plainPassword;
            int MULT = 1;
            decimal SEED = 0;
            string EncryptedStr = "";
            decimal RAND2 = 0;
            decimal[] RAND1 = new decimal[10];

            // Ensure inputs are padded to 10 characters
            string Xtext = (PlainText ?? "").PadRight(10, ' ').Substring(0, 10);
            string Xuser = (Username ?? "").PadRight(10, ' ').Substring(0, 10);

            // 1. Generate SEED from Username
            for (int i = 0; i <= 9; i++)
            {
                int val = Strings.Asc(Xuser.Substring(i, 1)) - Strings.Asc('A');
                SEED = SEED + (val * MULT);
                MULT = MULT * 5;
            }

            // 2. Generate pseudo-random array RAND1
            for (int i = 0; i <= 9; i++)
            {
                RAND1[i] = Math.Round(SEED * 7137421);
                RAND1[i] = RAND1[i] + 21132487;
                RAND2 = RAND1[i] / 10000000;
                RAND1[i] = RAND1[i] - (Math.Floor(RAND2) * 10000000);
                RAND1[i] = Math.Floor(RAND1[i] / 100000);
                SEED = SEED + 1;
            }

            // 3. Perform Encryption (Add offset instead of subtracting)
            for (int i = 0; i <= 9; i++)
            {
                string char1 = Xtext.Substring(i, 1);
                int asciiVal = Strings.Asc(char1);

                // Add pseudo-random offset
                int encryptedAscii = asciiVal + Convert.ToInt32(RAND1[i]);

                char chr = Strings.Chr(encryptedAscii);
                EncryptedStr += chr;
            }
            return EncryptedStr;
        }
        public static string Decode(string Access_Str, string Username)
        {
            int MULT = 1;
            decimal SEED = 0;
            string MWORD = "";
            decimal RAND2 = 0;
            decimal[] RAND1 = new decimal[10];

            string XACSTR = Access_Str;
            string Xuser = Username.PadRight(10, ' ');

            for (int i = 0; i <= 9; i++)
            {
                int val = Strings.Asc(Xuser.Substring(i, 1)) - Strings.Asc('A');
                SEED = SEED + (val * MULT);
                MULT = MULT * 5;
            }

            for (int i = 0; i <= 9; i++)
            {
                RAND1[i] = Math.Round(SEED * 7137421);
                RAND1[i] = RAND1[i] + 21132487;
                RAND2 = RAND1[i] / 10000000;
                RAND1[i] = RAND1[i] - (Math.Floor(RAND2) * 10000000);
                RAND1[i] = Math.Floor(RAND1[i] / 100000);
                SEED = SEED + 1;
            }
            try
            {
                for (int i = 0; i <= 9; i++)
                {
                    string char1 = XACSTR.Substring(i, 1);
                    var ass = Strings.Asc(char1);
                    int dd = Strings.Asc(char1);
                    decimal val = dd - RAND1[i];
                    int ff = Convert.ToInt32(val);
                    char chr = Strings.Chr(ff);
                    MWORD = MWORD + chr;
                }
            }
            catch (Exception ex)
            {
                MWORD = "";
                var enc = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                for (int i = 0; i <= 9; i++)
                {
                    string char1 = XACSTR.Substring(i, 1);
                    var ass = Strings.Asc(char1);
                    int dd = Strings.Asc(char1);
                    decimal val = dd - RAND1[i];
                    int ff = Convert.ToInt32(val);
                    char chr = Strings.Chr(ff);
                    MWORD = MWORD + chr;
                }
            }

            if (!string.IsNullOrEmpty(MWORD))
            {
                // for Trim the value
                MWORD = MWORD.Trim();
            }
            return MWORD;
        }
    }
}