using UnityEngine;
using System.Threading.Tasks;
using System;

namespace LLMVRPrototype
{
    public class RetryPolicy
    {
        public int MaxRetries { get; }
        public int InitialDelayMilliseconds { get; }
        public int MaxDelayMilliseconds { get; }

        public RetryPolicy(
            int maxRetries = 2,
            int initialDelayMilliseconds = 1000,
            int maxDelayMilliseconds = 8000)
        {
            MaxRetries = Math.Max(0, maxRetries);

            InitialDelayMilliseconds = Math.Max(0, initialDelayMilliseconds);

            MaxDelayMilliseconds = Math.Max(InitialDelayMilliseconds, maxDelayMilliseconds);
        }

        /// <summary> 
        /// Returns true when another attempt should be made. attemptNumber is the number of the attempt that just failed, starting at 1. 
        /// </summary>
        public bool ShouldRetry(LLMResponse response, int attemptNumber)
        {
            if (response == null || response.Success)
            {
                return false;
            }

            // MaxRetries means retries after the initial attempt
            if (attemptNumber > MaxRetries)
            {
                return false;
            }

            long statusCode = response.StatusCode;

            // Network or transport failures without an HTTP status
            if (statusCode == 0)
            {
                return true;
            }

            // Rate limiting
            if (statusCode == 429)
            {
                return true;
            }

            // Transient server errors
            if (statusCode >= 500 && statusCode <= 599)
            {
                return true;
            }

            // Do not retry other HTTP errors
            return false;
        }

        /// <summary>
        /// Calculates exponential backoff, capped at MaxDelayMilliseconds attemptNumber starts at 1.
        /// </summary>
        public int GetDelayMilliseconds(int attemptNumber)
        {
            if (attemptNumber < 1)
            {
                attemptNumber = 1;
            }

            double delay = InitialDelayMilliseconds *
                           Math.Pow(2, attemptNumber - 1);

            return (int)Math.Min(delay, MaxDelayMilliseconds);
        }

        public async Task WaitBeforeRetryAsync(int attemptNumber)
        {
            int delayMilliseconds = GetDelayMilliseconds(attemptNumber);

            if (delayMilliseconds > 0)
            {
                await Task.Delay(delayMilliseconds);
            }
        }
    }
}
