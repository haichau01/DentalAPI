using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using FluentAssertions;

namespace DentalAPI.UnitTests.Accounts;

public sealed class LoginLockoutTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 15, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RecordFailure_FourFailures_DoesNotLock()
    {
        var lockout = new LoginLockout();

        RecordFailures(lockout, LoginLockout.MaxFailedAttempts - 1);

        lockout.IsLocked(_now).Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_FifthFailure_LocksForLockoutDuration()
    {
        var lockout = new LoginLockout();

        RecordFailures(lockout, LoginLockout.MaxFailedAttempts);

        lockout.IsLocked(_now).Should().BeTrue();
        lockout.IsLocked(_now + LoginLockout.Duration - TimeSpan.FromSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void IsLocked_LockoutDurationPassed_ReturnsFalse()
    {
        var lockout = new LoginLockout();
        RecordFailures(lockout, LoginLockout.MaxFailedAttempts);

        lockout.IsLocked(_now + LoginLockout.Duration).Should().BeFalse();
    }

    [Fact]
    public void Reset_AfterFailures_StartsCountingFromZero()
    {
        var lockout = new LoginLockout();
        RecordFailures(lockout, LoginLockout.MaxFailedAttempts - 1);

        lockout.Reset();
        lockout.RecordFailure(_now);

        lockout.FailedCount.Should().Be(1);
        lockout.IsLocked(_now).Should().BeFalse();
    }

    private static void RecordFailures(LoginLockout lockout, int count)
    {
        for (var attempt = 0; attempt < count; attempt++)
        {
            lockout.RecordFailure(_now);
        }
    }
}

public sealed class VerificationCodeTests
{
    private const string CorrectHash = "hash:123456";
    private const string WrongHash = "hash:000000";
    private static readonly DateTimeOffset _sentAt = new(2026, 6, 15, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryConsume_CorrectCodeBeforeExpiry_ReturnsTrue()
    {
        var code = NewCode();

        code.TryConsume(CorrectHash, _sentAt.AddMinutes(4)).Should().BeTrue();
        code.ConsumedAt.Should().NotBeNull();
    }

    [Fact]
    public void TryConsume_AtExpiry_ReturnsFalse()
    {
        var code = NewCode();

        code.TryConsume(CorrectHash, _sentAt + VerificationCode.Lifetime).Should().BeFalse();
    }

    [Fact]
    public void TryConsume_AlreadyUsed_ReturnsFalse()
    {
        var code = NewCode();
        code.TryConsume(CorrectHash, _sentAt);

        code.TryConsume(CorrectHash, _sentAt).Should().BeFalse();
    }

    [Fact]
    public void TryConsume_CorrectCodeAfterMaxFailedAttempts_ReturnsFalse()
    {
        var code = NewCode();
        for (var attempt = 0; attempt < VerificationCode.MaxFailedAttempts; attempt++)
        {
            code.TryConsume(WrongHash, _sentAt);
        }

        code.TryConsume(CorrectHash, _sentAt).Should().BeFalse();
    }

    [Fact]
    public void CanResend_BeforeAndAfterCooldown_ReflectsCooldown()
    {
        var code = NewCode();

        code.CanResend(_sentAt + VerificationCode.ResendCooldown - TimeSpan.FromSeconds(1)).Should().BeFalse();
        code.CanResend(_sentAt + VerificationCode.ResendCooldown).Should().BeTrue();
    }

    private static VerificationCode NewCode() => new(VerificationChannel.Sms, "0900000001", CorrectHash, _sentAt);
}

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("0901234567", "0901234567")]
    [InlineData("+84901234567", "0901234567")]
    [InlineData("84901234567", "0901234567")]
    [InlineData(" 0901234567 ", "0901234567")]
    [InlineData("8490123456", "8490123456")]
    public void Normalize_VietnameseFormats_ReturnsDomesticForm(string input, string expected)
    {
        PhoneNumber.Normalize(input).Should().Be(expected);
    }
}
