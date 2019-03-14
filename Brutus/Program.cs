using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MethodSupportsCancellation

namespace Brutus
{
    class Program
    {
        // *********** TWEAKABLE PARAMETERS *************

        /// <summary>
        /// Defines the alphabet to check (i.e. possible characters)
        /// </summary>
        const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// From which length should be start iterating
        /// </summary>
        const int FromLength = 4;

        /// <summary>
        /// To which length should the iteration run
        /// </summary>
        const int ToLength = 4;

        static readonly Encoding TextEncoding = Encoding.UTF8;

        static void Main()
        {
            const string hashedPassword = "FI6HxbrYqNC0JVh9Nb1gEmynHE791JEyXdEILVXsDPrbeotAAh4tFsAuN+mvH3lGYXSGchCIw7fWfFV18CVSdw==";
            const string salt = "xXwi2Gy+eFXBvcqT+H6EotPDFYc4fYDurBRIe6KxaatItHZJaLQn845QXrDeaIY/zgxw2lkD30j8NCtXdf2zD1QeS0MHOjofmyL55x/PD0SNCayVQY5SRNQuTTOJz32RnU5cvx1DN5TSUD42CvgoLCTnymxQHc3FzAZu2e0GlculaErQck8mStEYw/BQGzrcBSAPogURQH+Bl/MW1F6my7tzkaGtWLnVwICaOFrddbDMkdPM9/LGkyfKT9tcD3Vzcfc13IqYLq1nOeKFdwgloyJCSkKL2tTIrP164wndeLAKJsZiqJgmNGImZL0QQBwfpau5GcUy6JYibDCMHkukiQ==";

            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => GuesPassword(cancellationTokenSource.Token, hashedPassword, salt));

            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
        }

        static void GuesPassword(CancellationToken token, string hashedPassword, string salt)
        {
            var stopwatch = Stopwatch.StartNew();
            var count = 0;
            try
            {
                Parallel.ForEach(GetCandidates(token), candidate =>
                {
                    var hash = Hash(candidate, salt);

                    if (hash == hashedPassword)
                    {
                        Console.WriteLine($"Found it!! The password is: {candidate}");
                        return;
                    }

                    var newValue = Interlocked.Increment(ref count);

                    if (newValue % 1000000 == 0)
                    {
                        Console.WriteLine($"{newValue} candidates processed");
                    }
                });

                var elapsed = stopwatch.Elapsed;
                var rate = count / elapsed.TotalSeconds;

                Console.WriteLine($"Done testing {count} candidates in {elapsed.TotalMinutes:0.0} m - that's {rate:0.0} hashes/s");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // we're exiting
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error in guessing function: {exception}");
            }
        }

        static IEnumerable<string> GetCandidates(CancellationToken token)
        {
            var count = ToLength - FromLength + 1;

            if (count < 1)
            {
                throw new ArgumentException("Please set FromLength and ToLength so that FromLength <= ToLength");
            }

            return Enumerable.Range(FromLength, count).SelectMany(length => GetCandidates(length, token));
        }

        static IEnumerable<string> GetCandidates(int length, CancellationToken token)
        {
            if (length == 0)
            {
                yield return "";
                yield break;
            }

            foreach (var character in Alphabet)
            {
                foreach (var subCandidate in GetCandidates(length - 1, token))
                {
                    token.ThrowIfCancellationRequested();

                    yield return string.Concat(character, subCandidate);
                }
            }
        }

        static string Hash(string password, string salt)
        {
            using (var crypto = SHA512.Create())
            {
                var value = salt + password;
                var bytes = TextEncoding.GetBytes(value);

                var hash = crypto.ComputeHash(bytes, 0, bytes.Length);
                var hashedPassword = Convert.ToBase64String(hash);
                return hashedPassword;
            }
        }
    }
}
