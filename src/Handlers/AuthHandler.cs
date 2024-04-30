using System;
using PepperDash.Core;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Cryptography;
using System.Text;

namespace PJLinkProjectorEpi
{
    public class AuthHandler : IKeyed
    {
        public const string SearchString = Commands.AuthNotice + " "; // "PJLINK "
        public const string ErrorResponse = SearchString + Commands.AuthError; // "PJLINK ERRA"

        public string Password { private get; set; }

        public event EventHandler<Events.AuthEventArgs> AuthStatusUpdated;

        public AuthHandler(string key)
        {
            Key = key;
            Password = "JBMIAProjectorLink"; // TODO: feed password in from config
        }

        private string GenerateMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                // Convert input string to a byte array and compute the hash
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2")); // Use "X2" for uppercase
                }
                return sb.ToString();
            }
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString)) //"PJLINK "
                return;

            if (response.Contains(SearchString + Commands.On)) //"PJLINK 1" auth required
            {
                Match result = Regex.Match(response, SearchString + Commands.On + @" (\d)"); //@"%1POWR=ERR(\d)"
                if (result.Success)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice,
                       String.Format("Received authentication request string: '{0}'", result.Groups[1].Value));
                    string hash = GenerateMD5Hash(result.Groups[1].Value + Password);
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "MD5 Hash for PJLink Authentication: " + hash);

                    OnAuthUpdated(new Events.AuthEventArgs
                    {
                        MD5 = hash,
                    });
                }
                else
                    Debug.Console(1, this, Debug.ErrorLogLevel.Warning,
                        String.Format("Authentication ERROR: '{0}'", response));
                return;
            }
            if (response.Contains(SearchString + Commands.Off)) //"PJLINK 0" no auth required
            {
                OnAuthUpdated(new Events.AuthEventArgs
                {
                    MD5 = "",
                });

                return;
            }

            if (response.Contains(ErrorResponse))
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "Received authentication ERROR");
                return;
            }

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Received an unknown auth response:{0}", response);
        }

        private void OnAuthUpdated(Events.AuthEventArgs args)
        {
            var handler = AuthStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}