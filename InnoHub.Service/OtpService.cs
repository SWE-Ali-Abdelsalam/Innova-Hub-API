// Create a new file: OtpService.cs
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace InnoHub.Service
{
    public class OtpService
    {
        // Store OTP to token mappings without expiration
        private readonly ConcurrentDictionary<string, string> _otpStore
            = new ConcurrentDictionary<string, string>();

        private readonly Random _random = new Random();

        // Generate a random 6-digit OTP
        public string GenerateOtp()
        {
            return _random.Next(100000, 999999).ToString();
        }

        // Store OTP with associated token (no expiry)
        public void StoreOtp(string email, string otp, string token)
        {
            // Store without expiration
            _otpStore[email + otp] = token;
        }

        // Get token by email and OTP
        public string GetTokenByOtp(string email, string otp)
        {
            if (_otpStore.TryGetValue(email + otp, out var token))
            {
                // Remove after use (single use only)
                _otpStore.TryRemove(email + otp, out _);
                return token;
            }

            return null;
        }

        // Optional: Method to manually remove specific OTP
        public bool RemoveOtp(string email, string otp)
        {
            return _otpStore.TryRemove(email + otp, out _);
        }

        // Optional: Method to clear all stored OTPs
        public void ClearAllOtps()
        {
            _otpStore.Clear();
        }

        // Optional: Method to check if OTP exists without removing it
        public bool IsOtpValid(string email, string otp)
        {
            return _otpStore.ContainsKey(email + otp);
        }
    }
}