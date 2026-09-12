using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;
using PetEmulator.Core;

namespace PetEmulator.Chips.Tests;

public sealed class MC146818Tests
{
    [Test]
    public void TimeRegisters_AreReturnedAsBcdIn24HourMode()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 21, 34, 56));

        Read(rtc, MC146818.Hours).Should().Be(0x21);
        Read(rtc, MC146818.Minutes).Should().Be(0x34);
        Read(rtc, MC146818.Seconds).Should().Be(0x56);
        Read(rtc, MC146818.DayOfMonth).Should().Be(0x11);
        Read(rtc, MC146818.DayOfWeek).Should().Be(0x06); // Friday; Sunday is 1
        Read(rtc, MC146818.Month).Should().Be(0x09);
        Read(rtc, MC146818.Year).Should().Be(0x26);
    }

    [Test]
    public void TimeRegisters_SupportBinaryAndTwelveHourModes()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 21, 34, 56));

        Write(rtc, MC146818.RegisterB, 0x00); // 12-hour BCD
        Read(rtc, MC146818.Hours).Should().Be(0x89);

        Write(rtc, MC146818.RegisterB, MC146818.DataModeBinary);
        Read(rtc, MC146818.Hours).Should().Be(0x89);

        Write(rtc, MC146818.RegisterB, MC146818.DataModeBinary | MC146818.Hour24Mode);
        Read(rtc, MC146818.Hours).Should().Be(21);
    }

    [Test]
    public void TimeRegisters_EncodeMidnightAndNoonInTwelveHourMode()
    {
        var midnight = new MC146818(clock: () => new DateTime(2026, 9, 11, 0, 0, 0));
        Write(midnight, MC146818.RegisterB, 0);
        Read(midnight, MC146818.Hours).Should().Be(0x12);

        var noon = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0));
        Write(noon, MC146818.RegisterB, 0);
        Read(noon, MC146818.Hours).Should().Be(0x92);
    }

    [Test]
    public void Tick_RaisesUpdateEndedIrq_AndReadingStatusClearsIt()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 100);
        Write(rtc, MC146818.RegisterB, 0x12); // UIE + 24-hour BCD

        rtc.Tick(100);

        rtc.Irq.Should().BeTrue();
        Read(rtc, MC146818.RegisterC).Should().Be((byte)0x90);
        rtc.Irq.Should().BeFalse();
    }

    [Test]
    public void SetBit_AllowsWritingTimeWithoutAdvancingTheClock()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 100);
        Write(rtc, MC146818.RegisterB, MC146818.SetTime | 0x02);
        Write(rtc, MC146818.Hours, 0x23);
        Write(rtc, MC146818.Minutes, 0x45);
        Write(rtc, MC146818.Seconds, 0x01);
        Write(rtc, MC146818.RegisterB, 0x12);

        rtc.Tick(100);

        Read(rtc, MC146818.Hours).Should().Be(0x23);
        Read(rtc, MC146818.Minutes).Should().Be(0x45);
        Read(rtc, MC146818.Seconds).Should().Be(0x02);
    }

    [Test]
    public void SetBit_AllowsWritingCalendarFields()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0));
        Write(rtc, MC146818.RegisterB, MC146818.SetTime | MC146818.Hour24Mode);

        Write(rtc, MC146818.Year, 0x27);
        Write(rtc, MC146818.Month, 0x02);
        Write(rtc, MC146818.DayOfMonth, 0x28);
        Write(rtc, MC146818.DayOfWeek, 0x07);

        Read(rtc, MC146818.Year).Should().Be(0x27);
        Read(rtc, MC146818.Month).Should().Be(0x02);
        Read(rtc, MC146818.DayOfMonth).Should().Be(0x28);
        Read(rtc, MC146818.DayOfWeek).Should().Be(0x07);
    }

    [Test]
    public void SetBit_DecodesTwelveHourMidnightAndPmValues()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0));
        Write(rtc, MC146818.RegisterB, MC146818.SetTime);

        Write(rtc, MC146818.Hours, 0x12); // 12 AM
        Read(rtc, MC146818.Hours).Should().Be(0x12);
        Write(rtc, MC146818.Hours, 0x91); // 1 PM
        Read(rtc, MC146818.Hours).Should().Be(0x91);

        Write(rtc, MC146818.RegisterB, MC146818.SetTime | MC146818.Hour24Mode | MC146818.DataModeBinary);
        Write(rtc, MC146818.Minutes, 34);
        Read(rtc, MC146818.Minutes).Should().Be(34);
    }

    [Test]
    public void TimeWrites_AreIgnoredUnlessSetModeIsEnabled()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 34, 56));

        Write(rtc, MC146818.Seconds, 0);
        Write(rtc, MC146818.Minutes, 0);
        Write(rtc, MC146818.Hours, 0);
        Write(rtc, MC146818.DayOfWeek, 1);
        Write(rtc, MC146818.DayOfMonth, 1);
        Write(rtc, MC146818.Month, 1);
        Write(rtc, MC146818.Year, 0);

        rtc.CurrentTime.Should().Be(new DateTime(2026, 9, 11, 12, 34, 56));
        Read(rtc, MC146818.DayOfWeek).Should().Be(0x06);
    }

    [Test]
    public void PeriodicInterrupt_RaisesIrqAtSixteenHzWhenEnabled()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 160);
        Write(rtc, MC146818.RegisterA, 0x2C); // rate 12 = 16 Hz
        Write(rtc, MC146818.RegisterB, 0x52); // PIE + UIE + 24-hour BCD

        rtc.Tick(10);

        rtc.Irq.Should().BeTrue();
        Read(rtc, MC146818.RegisterC).Should().Be((byte)0xC0);
        rtc.Irq.Should().BeFalse();
    }

    [Test]
    public void PeriodicInterrupt_IsDisabledForInvalidRateAndWhenPieIsCleared()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 160);
        Write(rtc, MC146818.RegisterA, 0x20);
        Write(rtc, MC146818.RegisterB, 0x42);

        rtc.Tick(160);
        Read(rtc, MC146818.RegisterC).Should().Be(MC146818.UpdateEndedFlag);

        Write(rtc, MC146818.RegisterA, 0x2C);
        Write(rtc, MC146818.RegisterB, 0x02);
        rtc.Tick(10);
        Read(rtc, MC146818.RegisterC).Should().Be(0);
    }

    [Test]
    public void PeriodicInterrupt_SupportsEveryRegisterARateCode()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 1);
        Write(rtc, MC146818.RegisterB, 0x42);

        for (byte rate = 3; rate <= 15; rate++)
        {
            Write(rtc, MC146818.RegisterA, (byte)(0x20 | rate)); // divider DV=010 plus rate
            rtc.Tick(1);
            Read(rtc, MC146818.RegisterC).Should().Be(MC146818.UpdateEndedFlag |
                MC146818.PeriodicFlag | MC146818.InterruptRequestFlag);
        }
    }

    [Test]
    public void RegisterA_ReportsUpdateInProgressBeforeSecondAndStopsWhenDividerIsDisabled()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 100);

        rtc.Tick(99);
        (Read(rtc, MC146818.RegisterA) & MC146818.UpdateInProgress).Should().NotBe(0);

        rtc.Tick(1);
        (Read(rtc, MC146818.RegisterA) & MC146818.UpdateInProgress).Should().Be(0);
        Read(rtc, MC146818.Seconds).Should().Be(0x01);

        Write(rtc, MC146818.RegisterA, 0x00); // divider stopped
        rtc.Tick(100);
        Read(rtc, MC146818.Seconds).Should().Be(0x01);
        (Read(rtc, MC146818.RegisterA) & MC146818.UpdateInProgress).Should().Be(0);
    }

    [Test]
    public void AlarmMatch_RaisesAlarmFlagAndIrqOnlyWhenAieIsEnabled()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 34, 59), cyclesPerSecond: 100);
        Write(rtc, MC146818.AlarmSeconds, 0x00);
        Write(rtc, MC146818.AlarmMinutes, 0x35);
        Write(rtc, MC146818.AlarmHours, 0x12);
        Write(rtc, MC146818.RegisterB, MC146818.Hour24Mode);

        rtc.Tick(100);
        Read(rtc, MC146818.RegisterC).Should().Be(MC146818.UpdateEndedFlag | MC146818.AlarmFlag);

        Write(rtc, MC146818.AlarmSeconds, 0x01);
        Write(rtc, MC146818.RegisterB, MC146818.Hour24Mode | MC146818.AlarmInterruptEnable);
        rtc.Tick(100);
        Read(rtc, MC146818.RegisterC).Should().Be(MC146818.UpdateEndedFlag | MC146818.AlarmFlag |
            MC146818.InterruptRequestFlag);
    }

    [Test]
    public void AlarmRegisters_SupportDontCareFields()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 34, 59), cyclesPerSecond: 1);
        Write(rtc, MC146818.AlarmSeconds, 0xC0);
        Write(rtc, MC146818.AlarmMinutes, 0xC0);
        Write(rtc, MC146818.AlarmHours, 0xC0);
        Write(rtc, MC146818.RegisterB, MC146818.Hour24Mode | MC146818.AlarmInterruptEnable);

        rtc.Tick(1);

        (Read(rtc, MC146818.RegisterC) & (MC146818.AlarmFlag | MC146818.InterruptRequestFlag))
            .Should().Be(MC146818.AlarmFlag | MC146818.InterruptRequestFlag);
    }

    [Test]
    public void AlarmRegisters_CanBeReadBackIndependently()
    {
        var rtc = new MC146818();
        Write(rtc, MC146818.AlarmSeconds, 0x12);
        Write(rtc, MC146818.AlarmMinutes, 0x34);
        Write(rtc, MC146818.AlarmHours, 0x56);

        Read(rtc, MC146818.AlarmSeconds).Should().Be(0x12);
        Read(rtc, MC146818.AlarmMinutes).Should().Be(0x34);
        Read(rtc, MC146818.AlarmHours).Should().Be(0x56);
    }

    [Test]
    public void RegisterInterface_MasksSelectionAndReportsBusAccesses()
    {
        var accesses = new List<BusAccess>();
        var rtc = new MC146818(name: "RTC", baseAddress: 0x9000);
        rtc.Observer = accesses.Add;

        rtc.Write(0x9000, 0xFF);
        rtc.Read(0x9000).Should().Be(0x3F);
        rtc.Read(0x9001).Should().Be(0);

        accesses.Should().ContainInOrder(
            new BusAccess(true, 0x9000, 0xFF),
            new BusAccess(false, 0x9000, 0x3F),
            new BusAccess(false, 0x9001, 0));
    }

    [Test]
    public void RegisterInterface_ExposesControlRegistersAndIgnoresReadOnlyWrites()
    {
        var rtc = new MC146818();

        rtc.Name.Should().Be("MC146818 RTC");
        rtc.Length.Should().Be(2);
        Read(rtc, MC146818.RegisterA).Should().Be(0x20);
        Read(rtc, MC146818.RegisterB).Should().Be(MC146818.Hour24Mode);
        Read(rtc, MC146818.RegisterD).Should().Be(0x80);

        Write(rtc, MC146818.RegisterC, 0xFF);
        Write(rtc, MC146818.RegisterD, 0xFF);
        Read(rtc, 0x3F).Should().Be(0);
    }

    [Test]
    public void SetBit_StopsClockUntilCleared()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 100);
        Write(rtc, MC146818.RegisterB, MC146818.SetTime | MC146818.Hour24Mode);
        rtc.Tick(100);
        Read(rtc, MC146818.Seconds).Should().Be(0);

        Write(rtc, MC146818.RegisterB, MC146818.Hour24Mode);
        rtc.Tick(100);
        Read(rtc, MC146818.Seconds).Should().Be(1);
    }

    [Test]
    public void Tick_AdvancesAcrossMonthAndLeapYearBoundaries()
    {
        var leapDay = new MC146818(clock: () => new DateTime(2024, 2, 28, 23, 59, 59), cyclesPerSecond: 1);
        leapDay.Tick(1);
        leapDay.CurrentTime.Should().Be(new DateTime(2024, 2, 29));

        var newYear = new MC146818(clock: () => new DateTime(2023, 12, 31, 23, 59, 59), cyclesPerSecond: 1);
        newYear.Tick(1);
        newYear.CurrentTime.Should().Be(new DateTime(2024, 1, 1));
    }

    [Test]
    public void SetBit_PreservesFractionalCyclesWhileClockIsStopped()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 100);
        rtc.Tick(40);
        Write(rtc, MC146818.RegisterB, MC146818.SetTime | MC146818.Hour24Mode);
        rtc.Tick(100);
        Read(rtc, MC146818.Seconds).Should().Be(0);

        Write(rtc, MC146818.RegisterB, MC146818.Hour24Mode);
        rtc.Tick(60);
        Read(rtc, MC146818.Seconds).Should().Be(1);
    }

    [Test]
    public void Constructor_RejectsZeroCyclesPerSecond()
    {
        var action = () => new MC146818(cyclesPerSecond: 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Reset_ReloadsClockAndClearsStatus()
    {
        var now = new DateTime(2026, 9, 11, 12, 0, 0);
        var rtc = new MC146818(clock: () => now, cyclesPerSecond: 100);
        Write(rtc, MC146818.RegisterB, 0x12);
        rtc.Tick(100);
        Read(rtc, MC146818.RegisterC).Should().Be(0x90);

        now = now.AddHours(1);
        rtc.Reset();

        rtc.CurrentTime.Should().Be(now);
        Read(rtc, MC146818.RegisterC).Should().Be(0);
    }

    private static byte Read(MC146818 rtc, byte register)
    {
        rtc.Write(0, register);
        return rtc.Read(1);
    }

    private static void Write(MC146818 rtc, byte register, byte value)
    {
        rtc.Write(0, register);
        rtc.Write(1, value);
    }
}
