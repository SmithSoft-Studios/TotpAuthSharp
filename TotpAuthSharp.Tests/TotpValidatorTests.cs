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
            this._totpGenerator = new TotpGenerator();
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
