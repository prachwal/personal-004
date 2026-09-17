using Microsoft.Extensions.Logging;
using PetEmulator.Cpc;

namespace PetEmulator.Cpc464;

/// <summary>CPC464 compatibility type; the cassette implementation is shared by all CPC models.</summary>
public sealed class Cpc464Cassette : CpcCassette
{
    public Cpc464Cassette(ILogger? logger = null) : base(logger)
    {
    }
}

