using System;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Tests.Helper;
using Xunit;

namespace TotpAuthSharp.Tests
{
    public class TotpValidatorTests
    {
        private readonly ITotpValidator _totpValidator;
        private readonly ITotpGenerator _totpGenerator;

        public TotpValidatorTests()
        {
            // A pinned clock keeps these tests deterministic: with the real clock, a 30-second window could roll
            // over between generating and validating (zero tolerance), or the fixed code below could happen to be
            // valid at the moment the test runs.
            this._totpGenerator = new TotpGenerator(FixedTimeProvider.AtUnixSeconds(1234567890));
            this._totpValidator = new TotpValidator(this._totpGenerator);
        }

        [Fact]
        public void Validate_TotpGeneratedByGoogleAuthenticatorIsNotValid()
        {
            var valid = this._totpValidator.Validate(TotpAuthTests.AccountSecretKey, 284621);
            Assert.False(valid);
        }

        [Fact]
        public void Validate_TotpGeneratedByGoogleAuthenticatorIsValid()
        {
            bool valid;
            long timeStep;

            // With zero tolerance the code is only valid inside its own 30-second window, so retry if a window
            // boundary passed between generating and validating; otherwise the test fails at random.
            do
            {
                timeStep = CurrentTimeStep();
                var totp = this._totpGenerator.Generate(TotpAuthTests.AccountSecretKey);
                valid = this._totpValidator.Validate(TotpAuthTests.AccountSecretKey, totp, 0);
            } while (timeStep != CurrentTimeStep());

            Assert.True(valid);
        }

        private static long CurrentTimeStep() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
    }
}
