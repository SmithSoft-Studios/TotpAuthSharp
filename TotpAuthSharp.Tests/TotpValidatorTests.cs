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
            var totp = this._totpGenerator.Generate(TotpAuthTests.AccountSecretKey);
            var valid = this._totpValidator.Validate(TotpAuthTests.AccountSecretKey, totp, 0);
            Assert.True(valid);
        }
    }
}
