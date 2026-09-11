using NUnit.Framework;
using PetEmulator.Pet.Keyboard;

namespace PetEmulator.Pet.Tests.Keyboard;

[TestFixture]
public sealed class PetKeyboardMatrixTests
{
    // Note: personal-001's "Pia1_binding_keeps_upper_port_a_inputs_high_and_scans_selected_row"
    // test is not ported - it exercises PetPia1Binding, which lives outside this port's scope
    // (Emulator.Pet root, not the Keyboard/Ieee488/CbmDos/Tape/Roms subsystems listed in the task).

    [Test]
    public void Matrix_reads_active_low_columns_for_ten_binary_selected_rows()
    {
        var keyboard = new PetKeyboardMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(keyboard.ReadColumns(0), Is.EqualTo(0xFF));
            Assert.That(keyboard.ReadColumns(9), Is.EqualTo(0xFF));
            Assert.That(keyboard.ReadColumns(10), Is.EqualTo(0xFF));
            Assert.That(keyboard.ReadColumns(15), Is.EqualTo(0xFF));
        });

        keyboard.Press(9, 7);
        Assert.That(keyboard.ReadColumns(9), Is.EqualTo(0x7F));
        Assert.That(keyboard.ReadColumns(1), Is.EqualTo(0xFF));

        keyboard.Release(9, 7);
        Assert.That(keyboard.ReadColumns(9), Is.EqualTo(0xFF));

        keyboard.Press(0, 0);
        keyboard.Reset();
        Assert.That(keyboard.ReadColumns(0), Is.EqualTo(0xFF));
    }
}
