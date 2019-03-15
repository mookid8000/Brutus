using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Brutus.Extensions;

// ReSharper disable MethodSupportsCancellation

namespace Brutus
{
    class Program
    {
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

        /// <summary>
        /// The hashed password
        /// </summary>
        const string HashedPassword = "FI6HxbrYqNC0JVh9Nb1gEmynHE791JEyXdEILVXsDPrbeotAAh4tFsAuN+mvH3lGYXSGchCIw7fWfFV18CVSdw==";

        /// <summary>
        /// The salt
        /// </summary>
        const string Salt = "xXwi2Gy+eFXBvcqT+H6EotPDFYc4fYDurBRIe6KxaatItHZJaLQn845QXrDeaIY/zgxw2lkD30j8NCtXdf2zD1QeS0MHOjofmyL55x/PD0SNCayVQY5SRNQuTTOJz32RnU5cvx1DN5TSUD42CvgoLCTnymxQHc3FzAZu2e0GlculaErQck8mStEYw/BQGzrcBSAPogURQH+Bl/MW1F6my7tzkaGtWLnVwICaOFrddbDMkdPM9/LGkyfKT9tcD3Vzcfc13IqYLq1nOeKFdwgloyJCSkKL2tTIrP164wndeLAKJsZiqJgmNGImZL0QQBwfpau5GcUy6JYibDCMHkukiQ==";

        static readonly Encoding TextEncoding = Encoding.UTF8;

        static void Main()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var totalNumberOfCandidates = GetLengths()
                .Sum(length => (long)Math.Pow(Alphabet.Length, length));

            Console.WriteLine($@"Running tests with alphabet

    {Alphabet}

with password lengths from {FromLength} to {ToLength}.

Total number of candidates: {totalNumberOfCandidates}");

            Task.Run(() => GuessPassword(cancellationTokenSource.Token, HashedPassword, Salt, totalNumberOfCandidates));

            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
        }

        static void GuessPassword(CancellationToken token, string hashedPassword, string salt, long totalNumberOfCandidates)
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
                        var percentage = 100 * newValue / (double)totalNumberOfCandidates;
                        var elapsedUntilNow = stopwatch.Elapsed;
                        var estimatedEndTime = GetEstimatedTimeLeft(totalNumberOfCandidates, newValue, elapsedUntilNow);

                        Console.WriteLine($"{percentage:0.00} % - {newValue} candidates processed, est. finish: {estimatedEndTime}");
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

        static string GetEstimatedTimeLeft(long totalNumberOfCandidates, int newValue, TimeSpan elapsedUntilNow)
        {
            if (newValue <= 0) return "???";

            var finishRatio = newValue / (double)totalNumberOfCandidates;
            var secondsLeft = elapsedUntilNow.TotalSeconds / finishRatio;
            var estimatedTotalTime = TimeSpan.FromSeconds(secondsLeft);
            var estimatedTimeLeft = estimatedTotalTime-elapsedUntilNow;

            return estimatedTimeLeft.ToHumanReadableTimeSpan();
        }

        static IEnumerable<string> GetCandidates(CancellationToken token) => GetLengths().SelectMany(length => GetCandidates(length, token));

        static IEnumerable<int> GetLengths()
        {
            var count = ToLength - FromLength + 1;

            if (count < 1)
            {
                throw new ArgumentException("Please set FromLength and ToLength so that FromLength <= ToLength");
            }

            return Enumerable.Range(FromLength, count);
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
