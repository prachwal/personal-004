using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

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
    public void PeriodicInterrupt_RaisesIrqAtSixteenHzWhenEnabled()
    {
        var rtc = new MC146818(clock: () => new DateTime(2026, 9, 11, 12, 0, 0), cyclesPerSecond: 160);
        Write(rtc, MC146818.RegisterB, 0x52); // PIE + UIE + 24-hour BCD

        rtc.Tick(10);

        rtc.Irq.Should().BeTrue();
        Read(rtc, MC146818.RegisterC).Should().Be((byte)0xC0);
        rtc.Irq.Should().BeFalse();
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
