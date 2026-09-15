using System.Diagnostics;
using System.Text.Json;
using PetEmulator.Cpc464;

var root = FindRoot();
var letters = new[] {(8,5),(6,6),(7,6),(7,5),(7,2),(6,5),(6,4),(5,4),(4,3),(5,5),(4,5),(4,4),(4,6),(5,6),(4,2),(3,3),(8,3),(6,2),(7,4),(6,3),(5,2),(6,7),(7,3),(7,7),(5,3),(8,7)};
var digits = new[] {(4,0),(8,0),(8,1),(7,1),(7,0),(6,1),(6,0),(5,1),(5,0),(4,1)};
var output = Path.Combine(root, "build", "cpc464-sieve-benchmark.json");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
var machine = new Cpc464Machine(File.ReadAllBytes(Path.Combine(root, "roms/cpc464/cpc464.rom")));
machine.Run(2_000_000);
var program = new[] { "10 DIM A(8190)", "20 FOR I=2 TO 8190", "30 IF A(I)=0 THEN P=P+1:FOR J=I*I TO 8190 STEP I:A(J)=1:NEXT J", "40 NEXT I", "50 PRINT P" };
foreach (var line in program) { Type(machine, line); Press(machine, 2, 2, false); }
var start = machine.CycleCount; var watch = Stopwatch.StartNew(); var checkpoints = new List<object>();
Write(false, null);
const ulong chunk = 5_000_000;
for (var i = 1; i <= 400; i++)
{
    machine.Run(chunk);
    checkpoints.Add(new { instructionBudget = (ulong)i * chunk, cycleCount = machine.CycleCount, wallClockSeconds = watch.Elapsed.TotalSeconds, pc = machine.Cpu.Registers.PC });
    Write(false, null);
    var count = DecodePrimeCount(machine);
    if (count is not null) { watch.Stop(); Write(true, count); break; }
}

void Write(bool completed, int? count)
{
    var json = new { sieve = new { startCycleCount = start, endCycleCount = machine.CycleCount, wallClockSeconds = watch.Elapsed.TotalSeconds, primeCount = count, expectedPrimeCount = 1899, completed, checkpointInstructionBudget = chunk, checkpoints } };
    File.WriteAllText(output, JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
}

int? DecodePrimeCount(Cpc464Machine m)
{
    m.Bus.GateArray.RenderFrame(); var p = m.Bus.GateArray.Pixels; var digits = new List<int>();
    for (var col = 1; col < 8; col++) { var glyph = Glyph(p, 10, col); if (glyph.All(x => x == 0)) break; var d = Enumerable.Range(0, 10).FirstOrDefault(n => glyph.SequenceEqual(ReferenceGlyph(n))); if (!glyph.SequenceEqual(ReferenceGlyph(d))) return null; digits.Add(d); }
    if (digits.Count == 0) return null; var value = digits.Aggregate(0, (v, d) => v * 10 + d); return value is >= 100 and <= 10000 ? value : null;
}

byte[] ReferenceGlyph(int digit)
{
    var fresh = new Cpc464Machine(File.ReadAllBytes(Path.Combine(root, "roms/cpc464/cpc464.rom"))); fresh.Run(2_000_000); Type(fresh, $"PRINT {digit}"); Press(fresh, 2, 2, false); fresh.Bus.GateArray.RenderFrame(); return Glyph(fresh.Bus.GateArray.Pixels, 10, 1);
}
byte[] Glyph(byte[] p, int row, int col) { var counts = new int[16]; for (var y=0;y<8;y++) for(var x=0;x<8;x++) counts[p[(row*8+y)*320+col*8+x]]++; var bg=(byte)Array.IndexOf(counts,counts.Max()); var g=new byte[64]; for(var y=0;y<8;y++) for(var x=0;x<8;x++) g[y*8+x]=(byte)(p[(row*8+y)*320+col*8+x]==bg?0:1); return g; }
void Type(Cpc464Machine m, string text)
{
    foreach (var ch in text)
    {
        var shifted = ch is '(' or ')' or '=' or '>' or '*' or '"' or '+';
        var k = ch switch { >= 'A' and <= 'Z' => letters[ch-'A'], >= '0' and <= '9' => digits[ch-'0'], ' ' => (5,7), '(' => (5,0), ')' => (4,1), '=' or '-' => (3,1), '>' => (3,7), '*' or ':' => (3,5), '+' or ';' => (3,4), '"' => (8,1), _ => throw new Exception($"unsupported {ch}") };
        Press(m, (byte)k.Item1, (byte)k.Item2, shifted);
    }
}
void Press(Cpc464Machine m, byte row, byte col, bool shift) { if (shift) m.Bus.Keyboard.SetKey(2,5,true); m.Bus.Keyboard.SetKey(row,col,true); m.Run(25_000); m.Bus.Keyboard.SetKey(row,col,false); if (shift) m.Bus.Keyboard.SetKey(2,5,false); m.Run(15_000); }
static string FindRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null && !File.Exists(Path.Combine(d.FullName,"PetEmulator.slnx"))) d=d.Parent; return d?.FullName ?? throw new DirectoryNotFoundException(); }
