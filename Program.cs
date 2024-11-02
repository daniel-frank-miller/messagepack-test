// See https://aka.ms/new-console-template for more information
using System.Net;
using System.Runtime.CompilerServices;
using MemoryPack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.NodaTime;
using MessagePack.Resolvers;
using NodaTime;
using Vogen;
var resolver = CompositeResolver.Create(
    NodatimeResolver.Instance,
    ContractlessStandardResolver.Instance
);
MessagePackSerializer.DefaultOptions = new(resolver);
var r = new TestRequest(1, 2, "fuck", new(321), IPAddress.Parse("1.2.3.4"), SystemClock.Instance.GetCurrentInstant(), null, new(100), Fuck.From(3), Fuck.From(321));
Console.WriteLine($"r: {r}");

var bytes = MessagePackSerializer.Serialize(r);
Console.WriteLine($"bytes: {bytes.Length}");

// var mpBytes = MemoryPackSerializer.Serialize(r);
// Console.WriteLine($"mpBytes: {mpBytes.Length}");

var d = MessagePackSerializer.Deserialize<TestRequest>(bytes);
Console.WriteLine($"d: {d}");

// var mpD = MemoryPackSerializer.Deserialize<TestRequest>(mpBytes);
// Console.WriteLine($"mpD: {mpD}");

Console.WriteLine("Hello, World!");

// [MemoryPackable]
public partial record TestRequest(
    int X,
    int Y,
    string Foo,
    Another Hi,
    [property: IPAddressFormatter]
    IPAddress Ip,
    [property: InstantFormatter]
    Instant Now,
    [property: InstantFormatter]
    Instant? Now2,
    SomeVo SomeVo,
    [property: ValueObjectFormatter<Fuck, int>]
    Fuck Fuck,
    Fuck? Fuck2
);
[MemoryPackable]
public partial record Another(
    int X
);

// [ValueObject<string>]
// [MessagePackFormatter(typeof(SomeVoMessagePackFormatter))]
public readonly record struct SomeVo(int Value);

[ValueObject<int>]
public readonly partial struct Fuck : IValueObject<Fuck, int>;

public class FuckMessagePackFormatter() : IMessagePackFormatter<Fuck>
{
    public void Serialize(ref MessagePackWriter writer, Fuck value, MessagePackSerializerOptions options) => writer.Write(value.Value);

    public Fuck Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil()) throw new NotImplementedException(); // this must be some more general for sure :)

        options.Security.DepthStep(ref reader);

        var value = reader.ReadInt32();

        reader.Depth--;
        return Fuck.From(value!);
    }
}

// public class IPAddressMsgPackFormatter

public class IPAddressFormatter : MemoryPackCustomFormatterAttribute<IPAddress>
{
    public override IMemoryPackFormatter<IPAddress> GetFormatter() => new Formatter();

    private class Formatter : MemoryPackFormatter<IPAddress>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IPAddress? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteValue(value.ToString());
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IPAddress? value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = IPAddress.Parse(reader.ReadString()!);
        }
    }
}

public interface IValueObject<TSelf, TPrimitive>
{
    abstract static TSelf From(TPrimitive primitive);
    TPrimitive Value { get; }
}

[AttributeUsage(AttributeTargets.All)]
public class ValueObjectFormatter<TVo, TPrimitive> : MemoryPackCustomFormatterAttribute<TVo>
    where TVo : IValueObject<TVo, TPrimitive>
{
    public override IMemoryPackFormatter<TVo> GetFormatter() => new Formatter();

    private class Formatter : MemoryPackFormatter<TVo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TVo value)
        {
            writer.WriteValue(value.Value);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TVo value)
        {
            value = TVo.From(reader.ReadValue<TPrimitive>()!);
        }
    }
}

[AttributeUsage(AttributeTargets.All)]
public class InstantFormatter : MemoryPackCustomFormatterAttribute<Instant>
{
    public override IMemoryPackFormatter<Instant> GetFormatter() => new Formatter();

    private class Formatter : MemoryPackFormatter<Instant>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Instant value)
        {
            writer.WriteValue(value.ToUnixTimeTicks());
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Instant value)
        {
            value = Instant.FromUnixTimeTicks(reader.ReadValue<long>());
        }
    }
}

public class Foo
{
    [ModuleInitializer]
    public static void Register()
    {
        Console.WriteLine("hahaha I ran");
    }
}